using System.ComponentModel.DataAnnotations;

namespace ZealousMindedPeopleGeo.Validation;

/// <summary>
/// Email, который можно не указывать: пустая строка или одни пробелы проходят,
/// заполненное значение проверяется как <see cref="EmailAddressAttribute"/>.
/// Сам <see cref="EmailAddressAttribute"/> пропускает только <c>null</c>, а пустое
/// поле формы приходит строкой <c>""</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class OptionalEmailAddressAttribute : ValidationAttribute
{
    private static readonly EmailAddressAttribute Email = new();

    public override bool IsValid(object? value) => value switch
    {
        null => true,
        string text => string.IsNullOrWhiteSpace(text) || Email.IsValid(text),
        _ => false
    };
}
