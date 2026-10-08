using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class SurveyAnswer
{
    public long Id { get; set; }

    public Guid SessionId { get; set; }

    public string QuestionKey { get; set; } = null!;

    public string AnswerValue { get; set; } = null!;

    public DateTime AnsweredAt { get; set; }

    public virtual SurveySession Session { get; set; } = null!;
}
