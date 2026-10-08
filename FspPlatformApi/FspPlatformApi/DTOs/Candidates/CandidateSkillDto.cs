namespace FspPlatformApi.DTOs.Candidates;

/// <summary>Навык кандидата с уровнем владения (для фильтров подборки и силы профиля).</summary>
public record CandidateSkillDto
{
    /// <summary>ID навыка из справочника skills.</summary>
    public required short SkillId { get; init; }

    /// <summary>Уровень владения 1–5.</summary>
    public required short Proficiency { get; init; }

    /// <summary>Опыт владения навыком, лет.</summary>
    public decimal? YearsExp { get; init; }
}