using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class VQuestionStat
{
    public Guid? QuestionId { get; set; }

    public short? SpecializationId { get; set; }

    public short? GradeId { get; set; }

    public short? Difficulty { get; set; }

    public long? Attempts { get; set; }

    public decimal? AvgRelativeScore { get; set; }

    public double? DiscriminationIndex { get; set; }
}
