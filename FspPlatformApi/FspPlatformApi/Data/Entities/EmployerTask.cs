using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class EmployerTask
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public short SpecializationId { get; set; }

    public short? GradeId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public decimal? MaxScore { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<CandidateTask> CandidateTasks { get; set; } = new List<CandidateTask>();

    public virtual Company Company { get; set; } = null!;

    public virtual Grade? Grade { get; set; }

    public virtual Specialization Specialization { get; set; } = null!;
}
