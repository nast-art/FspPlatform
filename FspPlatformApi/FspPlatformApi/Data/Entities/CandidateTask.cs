using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class CandidateTask
{
    public Guid Id { get; set; }

    public Guid TaskId { get; set; }

    public Guid CandidateId { get; set; }

    public string Status { get; set; } = null!;

    public string? AnswerText { get; set; }

    public string? AnswerPayload { get; set; }

    public decimal? Score { get; set; }

    public DateTime AssignedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual CandidateProfile Candidate { get; set; } = null!;

    public virtual Status StatusNavigation { get; set; } = null!;

    public virtual EmployerTask Task { get; set; } = null!;
}
