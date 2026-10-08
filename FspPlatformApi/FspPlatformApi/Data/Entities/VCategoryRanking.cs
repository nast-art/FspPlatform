using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class VCategoryRanking
{
    public short? CategoryId { get; set; }

    public Guid? CandidateId { get; set; }

    public decimal? BestTestPercent { get; set; }

    public bool? BestTestPassed { get; set; }

    public decimal? FspAchievementsWeight { get; set; }

    public decimal? RankScore { get; set; }

    public long? PositionInCategory { get; set; }
}
