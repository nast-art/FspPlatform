using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class TestSession
{
    public Guid Id { get; set; }

    public Guid CandidateId { get; set; }

    public short SpecializationId { get; set; }

    public short TargetGradeId { get; set; }

    public Guid? AssignmentId { get; set; }

    public string Status { get; set; } = null!;

    public short AttemptNo { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public decimal? TotalScore { get; set; }

    public decimal? MaxScore { get; set; }

    public decimal? Percent { get; set; }

    public bool? Passed { get; set; }

    public short? ResultingGradeId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual TestAssignment? Assignment { get; set; }

    public virtual CandidateProfile Candidate { get; set; } = null!;

    public virtual ICollection<GradeChangeHistory> GradeChangeHistories { get; set; } = new List<GradeChangeHistory>();

    public virtual Grade? ResultingGrade { get; set; }

    public virtual Specialization Specialization { get; set; } = null!;

    public virtual Status StatusNavigation { get; set; } = null!;

    public virtual Grade TargetGrade { get; set; } = null!;

    public virtual ICollection<TestSessionAnswer> TestSessionAnswers { get; set; } = new List<TestSessionAnswer>();
}
