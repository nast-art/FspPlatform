namespace FspPlatformApi.DTOs.Invitations;

/// <summary>
/// Приглашение. Кандидат видит условия (включая ЗП и компанию) до начала общения (ТЗ 2.2 п.1–2).
/// </summary>
public record InvitationDto
{
    /// <summary>ID приглашения.</summary>
    public required Guid Id { get; init; }

    /// <summary>ID кандидата.</summary>
    public required Guid CandidateId { get; init; }

    /// <summary>Имя кандидата (для кабинета работодателя).</summary>
    public required string CandidateName { get; init; }

    /// <summary>Категория кандидата (если известна).</summary>
    public string? CategoryName { get; init; }

    /// <summary>Связанная вакансия (null — прямое предложение).</summary>
    public Guid? VacancyId { get; init; }

    /// <summary>Название предложения.</summary>
    public required string Title { get; init; }

    /// <summary>Описание предложения.</summary>
    public required string Message { get; init; }

    /// <summary>Зарплата от, руб.</summary>
    public required int SalaryFrom { get; init; }

    /// <summary>Зарплата до, руб.</summary>
    public required int SalaryTo { get; init; }

    /// <summary>Валюта.</summary>
    public required string Currency { get; init; }

    /// <summary>Название компании.</summary>
    public required string CompanyName { get; init; }

    /// <summary>Способ связи.</summary>
    public required string ContactMethod { get; init; }

    /// <summary>Статус («отправлено» / «просмотрено» / «принято» / «отклонено» / «истекло» / «отозвано»).</summary>
    public required string Status { get; init; }

    /// <summary>Дата отправки.</summary>
    public required DateTime SentAt { get; init; }

    /// <summary>Дата просмотра кандидатом.</summary>
    public DateTime? ViewedAt { get; init; }

    /// <summary>Дата ответа.</summary>
    public DateTime? RespondedAt { get; init; }

    /// <summary>Срок действия.</summary>
    public DateTime? ExpiresAt { get; init; }
}