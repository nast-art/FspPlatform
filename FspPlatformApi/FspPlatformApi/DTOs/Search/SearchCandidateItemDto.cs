using FspPlatformApi.DTOs.Candidates;

namespace FspPlatformApi.DTOs.Search;

/// <summary>Позиция выдачи: карточка кандидата + скор + объяснимость (ТЗ критерий 1).</summary>
public record SearchCandidateItemDto
{
    /// <summary>Карточка кандидата (публичная, без контактов до accept).</summary>
    public required CandidatePublicCardDto Candidate { get; init; }

    /// <summary>Итоговый скор ранжирования (70% тест + до 30 ФСП + до 10 полнота).</summary>
    public required decimal RankScore { get; init; }

    /// <summary>Объяснение: почему кандидат в выдаче и на этой позиции.</summary>
    public required Dictionary<string, object> Explanation { get; init; }

    /// <summary>Позиция в топе (1 — лучший).</summary>
    public required int Position { get; init; }
}