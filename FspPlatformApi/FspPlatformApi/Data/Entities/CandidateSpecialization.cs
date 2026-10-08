using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class CandidateSpecialization
{
    public Guid CandidateId { get; set; }

    public short SpecializationId { get; set; }

    public short DeclaredGradeId { get; set; }

    public short CurrentGradeId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? LastTestAt { get; set; }

    public DateTime? LastGradeChangeAt { get; set; }

    public bool IsPrimary { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CandidateProfile Candidate { get; set; } = null!;

    public virtual Grade CurrentGrade { get; set; } = null!;

    public virtual Grade DeclaredGrade { get; set; } = null!;

    public virtual Specialization Specialization { get; set; } = null!;

    public virtual Status StatusNavigation { get; set; } = null!;
}
