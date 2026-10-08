namespace FspPlatformApi.DTOs.Employers;

/// <summary>Отклик кандидата на вакансию (классический сценарий — дополнительный к механике приглашений).</summary>
public record ApplyDto
{
    /// <summary>Сопроводительное письмо.</summary>
    public string? CoverLetter { get; init; }
}