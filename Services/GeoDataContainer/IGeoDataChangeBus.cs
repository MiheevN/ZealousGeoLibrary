namespace ZealousMindedPeopleGeo.Services.GeoDataContainer;

/// <summary>
/// Шина изменений гео-данных между серверами приложения. Внутри одного сервера
/// изменения и так доходят до всех пользователей через
/// <see cref="IGeoDataContainerManager.OnDataChanged"/>; шина нужна, когда серверов
/// несколько (за балансировщиком) и у них общее хранилище. Реализацию — Redis pub/sub,
/// SignalR backplane, очередь сообщений — регистрирует приложение. Без неё
/// синхронизация работает в пределах одного сервера.
/// </summary>
public interface IGeoDataChangeBus
{
    /// <summary>
    /// Отправляет изменение остальным серверам. Менеджер контейнеров вызывает его
    /// после каждого изменения, сделанного на этом сервере.
    /// </summary>
    /// <param name="change">Что изменилось</param>
    /// <param name="ct">Токен отмены операции</param>
    ValueTask PublishAsync(GeoDataChange change, CancellationToken ct = default);

    /// <summary>
    /// Изменение пришло с сервера. Реализация вызывает его для каждого полученного
    /// сообщения, в том числе для своих собственных: менеджер их отбросит по
    /// <see cref="GeoDataChange.Origin"/>.
    /// </summary>
    event Action<GeoDataChange>? ChangeReceived;
}

/// <summary>
/// Сообщение шины: контейнер, вид изменения и сервер, где оно произошло.
/// </summary>
/// <param name="ContainerId">Контейнер, данные которого изменились</param>
/// <param name="ChangeType">Вид изменения</param>
/// <param name="Origin">Сервер-источник: по нему менеджер узнаёт свои сообщения</param>
public sealed record GeoDataChange(string ContainerId, GeoDataChangeType ChangeType, Guid Origin);
