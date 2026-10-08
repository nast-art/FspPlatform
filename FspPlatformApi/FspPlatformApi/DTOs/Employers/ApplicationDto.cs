namespace FspPlatformApi.DTOs.Employers;

/// <summary>Отклик со статусом (ТЗ 2.2 п.3: обе стороны видят актуальный статус).</summary>
public record ApplicationDto
{
    /// <summary>ID отклика.</summary>
    public required Guid Id { get; init; }

    /// <summary>ID вакансии.</summary>
    public required Guid VacancyId { get; init; }

    /// <summary>Название вакансии.</summary>
    public required string VacancyTitle { get; init; }

    /// <summary>Компания.</summary>
    public required string CompanyName { get; init; }

    /// <summary>Зарплата от, руб.</summary>
    public required int SalaryFrom { get; init; }

    /// <summary>Зарплата до, руб.</summary>
    public required int SalaryTo { get; init; }

    /// <summary>Статус («отправлен» / «просмотрен» / «принят» / «отклонен» / «отозван»).</summary>
    public required string Status { get; init; }

    /// <summary>Дата отклика.</summary>
    public required DateTime SubmittedAt { get; init; }

    // ---------- Кандидат ----------
    // ТЗ 2.2 п.2: контактные данные раскрываются работодателю после отклика —
    // поэтому эти поля заполняются ТОЛЬКО в запросах работодателя
    // (GetCompanyApplicationsAsync / GetVacancyApplicationsAsync).
    // В списке самого кандидата (GetMyApplicationsAsync) они остаются null.

    /// <summary>ID кандидата (для deep-link на карточку /employer/candidates/:id).</summary>
    public Guid? CandidateId { get; init; }

    /// <summary>ФИО кандидата («Фамилия Имя»; может быть пустым, если профиль не заполнен).</summary>
    public string? CandidateName { get; init; }

    /// <summary>Сопроводительное письмо из отклика.</summary>
    public string? CoverLetter { get; init; }

    /// <summary>Email кандидата (только для работодателя после отклика).</summary>
    public string? Email { get; init; }

    /// <summary>Телефон кандидата (только для работодателя после отклика).</summary>
    public string? Phone { get; init; }

    /// <summary>Telegram кандидата (только для работодателя после отклика).</summary>
    public string? Telegram { get; init; }
}