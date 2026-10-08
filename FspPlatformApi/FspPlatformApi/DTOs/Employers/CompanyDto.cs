namespace FspPlatformApi.DTOs.Employers;

/// <summary>Компания работодателя.</summary>
public record CompanyDto
{
    /// <summary>ID компании.</summary>
    public required Guid Id { get; init; }

    /// <summary>Название.</summary>
    public required string Name { get; init; }

    /// <summary>Направление деятельности.</summary>
    public string? Industry { get; init; }

    /// <summary>Описание.</summary>
    public string? Description { get; init; }

    /// <summary>Сайт.</summary>
    public string? Website { get; init; }

    /// <summary>Контактный email.</summary>
    public string? ContactEmail { get; init; }

    /// <summary>Контактный телефон.</summary>
    public string? ContactPhone { get; init; }

    /// <summary>Уровень верификации (0 — MVP, механизм защиты за рамками MVP).</summary>
    public required int VerificationLevel { get; init; }
}