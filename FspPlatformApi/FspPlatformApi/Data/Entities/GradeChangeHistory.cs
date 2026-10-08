using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class GradeChangeHistory
{
    public long Id { get; set; }

    public Guid CandidateId { get; set; }

    public short SpecializationId { get; set; }

    public short? OldGradeId { get; set; }

    public short NewGradeId { get; set; }

    public string Reason { get; set; } = null!;

    public Guid? TestSessionId { get; set; }

    public DateTime ChangedAt { get; set; }

    public virtual CandidateProfile Candidate { get; set; } = null!;

    public virtual Grade NewGrade { get; set; } = null!;

    public virtual Grade? OldGrade { get; set; }

    public virtual Status ReasonNavigation { get; set; } = null!;

    public virtual Specialization Specialization { get; set; } = null!;

    public virtual TestSession? TestSession { get; set; }
}
