using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class CandidateCategory
{
    public Guid CandidateId { get; set; }

    public short CategoryId { get; set; }

    public bool IsPrimary { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CandidateProfile Candidate { get; set; } = null!;

    public virtual Category Category { get; set; } = null!;
}
