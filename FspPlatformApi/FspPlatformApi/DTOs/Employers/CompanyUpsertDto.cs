namespace FspPlatformApi.DTOs.Employers;

/// <summary>Профиль компании-работодателя (ТЗ: описание, направление, контакты).</summary>
public record CompanyUpsertDto
{
    /// <summary>Название организации.</summary>
    public required string Name { get; init; }

    /// <summary>Направление деятельности.</summary>
    public string? Industry { get; init; }

    /// <summary>Описание организации.</summary>
    public string? Description { get; init; }

    /// <summary>Сайт.</summary>
    public string? Website { get; init; }

    /// <summary>Контактный email.</summary>
    public string? ContactEmail { get; init; }

    /// <summary>Контактный телефон.</summary>
    public string? ContactPhone { get; init; }
}