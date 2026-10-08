namespace FspPlatformApi.DTOs.Invitations;

/// <summary>
/// Направление приглашения конкретному кандидату — основная механика выхода на контакт (ТЗ критерий 1).
/// Содержит описание предложения, уровень ЗП (обязателен, ТЗ 2.2 п.1), без обязательной привязки к вакансии.
/// </summary>
public record SendInvitationDto
{
    /// <summary>ID кандидата.</summary>
    public required Guid CandidateId { get; init; }

    /// <summary>Опциональная привязка к вакансии (null — прямое предложение).</summary>
    public Guid? VacancyId { get; init; }

    /// <summary>Название предложения.</summary>
    public required string Title { get; init; }

    /// <summary>Описание предложения.</summary>
    public required string Message { get; init; }

    /// <summary>Зарплата от, руб.</summary>
    public required int SalaryFrom { get; init; }

    /// <summary>Зарплата до, руб.</summary>
    public required int SalaryTo { get; init; }

    /// <summary>Способ связи после принятия: «почта» | «телефон» | «телеграм» | «через_приложение».</summary>
    public required string ContactMethod { get; init; }
}