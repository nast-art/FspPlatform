namespace FspPlatformApi.DTOs.Candidates;

/// <summary>
/// Создание/обновление профиля кандидата (вручную по форме; ТЗ: ФИО, контакты, стек, роли, стаж, софт-скиллы).
/// Контакты заполняются здесь, но видны работодателю только после принятия приглашения/отклика (ТЗ 2.2 п.2).
/// </summary>
public record CandidateProfileUpsertDto
{
    /// <summary>Фамилия.</summary>
    public required string LastName { get; init; }

    /// <summary>Имя.</summary>
    public required string FirstName { get; init; }

    /// <summary>Отчество.</summary>
    public string? MiddleName { get; init; }

    /// <summary>Телефон (контакт, скрыт до accept).</summary>
    public string? Phone { get; init; }

    /// <summary>Telegram (контакт, скрыт до accept).</summary>
    public string? Telegram { get; init; }

    /// <summary>Город.</summary>
    public string? City { get; init; }

    /// <summary>Краткое описание / самопрезентация.</summary>
    public string? About { get; init; }

    /// <summary>Стаж, лет.</summary>
    public required decimal ExperienceYears { get; init; }

    /// <summary>Софт-скиллы.</summary>
    public string[]? SoftSkills { get; init; }

    /// <summary>Стек с уровнями владения.</summary>
    public List<CandidateSkillDto>? Skills { get; init; }

    /// <summary>Роли в команде (ID из справочника roles).</summary>
    public List<short>? RoleIds { get; init; }

    /// <summary>Приватность: доступен ли профиль работодателям.</summary>
    public required bool IsProfilePublic { get; init; }
}