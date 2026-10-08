using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class TestSessionAnswer
{
    public long Id { get; set; }

    public Guid SessionId { get; set; }

    public Guid QuestionId { get; set; }

    public short OrderNo { get; set; }

    public string? AnswerPayload { get; set; }

    public decimal? Score { get; set; }

    public bool AutoGraded { get; set; }

    public DateTime? GradedAt { get; set; }

    public DateTime? AnsweredAt { get; set; }

    public virtual TestQuestion Question { get; set; } = null!;

    public virtual TestSession Session { get; set; } = null!;
}
