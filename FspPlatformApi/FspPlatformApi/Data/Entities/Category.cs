using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class Category
{
    public short Id { get; set; }

    public short SpecializationId { get; set; }

    public short GradeId { get; set; }

    public string DisplayName { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<CandidateCategory> CandidateCategories { get; set; } = new List<CandidateCategory>();

    public virtual Grade Grade { get; set; } = null!;

    public virtual Specialization Specialization { get; set; } = null!;
}
