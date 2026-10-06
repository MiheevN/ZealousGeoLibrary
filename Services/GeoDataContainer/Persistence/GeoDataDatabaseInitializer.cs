using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.GeoDataContainer.Persistence;

/// <summary>
/// Подготовка БД гео-данных: создание таблицы точек и перенос данных из таблицы
/// участников прежних версий библиотеки.
/// </summary>
public static class GeoDataDatabaseInitializer
{
    /// <summary>Таблица прежних версий: по строке на участника.</summary>
    public const string LegacyParticipantsTable = "GeoDataParticipants";

    /// <summary>Под этим именем остаётся прежняя таблица после переноса данных.</summary>
    public const string MigratedParticipantsTable = "GeoDataParticipants_Migrated";

    /// <summary>
    /// Создаёт таблицу точек, если её нет, и переносит в неё участников из
    /// <see cref="LegacyParticipantsTable"/>.
    /// </summary>
    /// <param name="context">Контекст БД гео-данных</param>
    /// <param name="logger">Логгер (опционально)</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Сколько участников перенесено</returns>
    public static async Task<int> InitializeAsync(GeoDataDbContext context, ILogger? logger = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // В существующей БД EnsureCreated ничего не делает, даже если нашей таблицы в ней нет.
        if (!await context.Database.EnsureCreatedAsync(ct)
            && context.Database.IsRelational()
            && !await CanQueryAsync(() => context.Points.AnyAsync(ct)))
        {
            await context.GetService<IRelationalDatabaseCreator>().CreateTablesAsync(ct);
            logger?.LogInformation("Created table GeoPoints in the existing geo-data database");
        }

        return await MigrateLegacyParticipantsAsync(context, logger, ct);
    }

    /// <summary>
    /// Переносит участников из <see cref="LegacyParticipantsTable"/> в таблицу точек и
    /// переименовывает прежнюю таблицу в <see cref="MigratedParticipantsTable"/>. Строки
    /// без координат точкой не станут и остаются в переименованной таблице. Если прежней
    /// таблицы нет, ничего не делает.
    /// </summary>
    /// <param name="context">Контекст БД гео-данных</param>
    /// <param name="logger">Логгер (опционально)</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Сколько участников перенесено</returns>
    public static async Task<int> MigrateLegacyParticipantsAsync(GeoDataDbContext context, ILogger? logger = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Database.IsRelational())
        {
            return 0;
        }

        var sql = context.GetService<ISqlGenerationHelper>();
        var selectLegacy = "SELECT * FROM " + sql.DelimitIdentifier(LegacyParticipantsTable);

        List<LegacyParticipantRow> rows;
        try
        {
            rows = await context.Database.SqlQueryRaw<LegacyParticipantRow>(selectLegacy).ToListAsync(ct);
        }
        catch (DbException)
        {
            return 0; // прежней таблицы нет
        }

        var points = rows
            .Select(row => (row.ContainerId, Participant: row.ToParticipant()))
            .Where(item => item.Participant.Latitude is not null && item.Participant.Longitude is not null)
            .Select(item => (item.ContainerId, Point: item.Participant.ToGeoPoint()))
            .Where(item => item.Point.Validate() is null)
            .ToList();

        var containerIds = points.Select(item => item.ContainerId).Distinct().ToList();
        var existing = (await context.Points
                .Where(p => containerIds.Contains(p.ContainerId))
                .Select(p => new { p.ContainerId, p.Id })
                .ToListAsync(ct))
            .Select(p => (p.ContainerId, p.Id))
            .ToHashSet();

        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        var migrated = 0;
        foreach (var (containerId, point) in points)
        {
            if (existing.Add((containerId, point.Id)))
            {
                context.Points.Add(GeoPointEntity.FromPoint(containerId, point));
                migrated++;
            }
        }

        await context.SaveChangesAsync(ct);

        // Таблицу не удаляем: в ней остаются строки, которые не стали точками.
        var rename = context.GetService<IMigrationsSqlGenerator>().Generate(new MigrationOperation[]
        {
            new RenameTableOperation { Name = LegacyParticipantsTable, NewName = MigratedParticipantsTable }
        });
        foreach (var command in rename)
        {
            await context.Database.ExecuteSqlRawAsync(command.CommandText, ct);
        }

        await transaction.CommitAsync(ct);

        logger?.LogInformation(
            "Moved {Migrated} of {Total} participants from {LegacyTable} to GeoPoints; the old table is kept as {KeptTable}",
            migrated, rows.Count, LegacyParticipantsTable, MigratedParticipantsTable);

        return migrated;
    }

    private static async Task<bool> CanQueryAsync(Func<Task> query)
    {
        try
        {
            await query();
            return true;
        }
        catch (DbException)
        {
            return false;
        }
    }

    // Столбцы таблицы GeoDataParticipants в том виде, в каком её создавали прежние версии.
    internal sealed class LegacyParticipantRow
    {
        public Guid Id { get; set; }
        public string ContainerId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? SocialMedia { get; set; }
        public string? Message { get; set; }
        public string? LifeGoals { get; set; }
        public string? Skills { get; set; }
        public string? Discord { get; set; }
        public string? Telegram { get; set; }
        public string? Vk { get; set; }
        public string? Website { get; set; }
        public DateTime RegisteredAt { get; set; }

        public Participant ToParticipant()
        {
            var hasSocialContacts = Discord is not null || Telegram is not null || Vk is not null || Website is not null;

            return new Participant
            {
                Id = Id,
                Name = Name,
                Address = Address,
                Email = Email,
                Location = Location,
                Latitude = Latitude,
                Longitude = Longitude,
                City = City,
                Country = Country,
                SocialMedia = SocialMedia,
                Message = Message,
                LifeGoals = LifeGoals,
                Skills = Skills,
                SocialContacts = hasSocialContacts
                    ? new SocialContacts { Discord = Discord, Telegram = Telegram, Vk = Vk, Website = Website }
                    : null,
                RegisteredAt = RegisteredAt
            };
        }
    }
}
