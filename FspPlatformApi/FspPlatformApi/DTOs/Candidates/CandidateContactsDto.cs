namespace FspPlatformApi.DTOs.Candidates;

/// <summary>
/// Контакты кандидата. Выдаются работодателю только после того, как кандидат принял приглашение
/// или его отклик был принят (ТЗ 2.2 п.2). Каждый запрос фиксируется в audit_log (view_contacts).
/// </summary>
public record CandidateContactsDto
{
    /// <summary>Email.</summary>
    public string? Email { get; init; }

    /// <summary>Телефон.</summary>
    public string? Phone { get; init; }

    /// <summary>Telegram.</summary>
    public string? Telegram { get; init; }
}