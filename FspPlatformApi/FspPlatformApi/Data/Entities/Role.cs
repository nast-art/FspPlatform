using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class Role
{
    public short Id { get; set; }

    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<CandidateProfile> Candidates { get; set; } = new List<CandidateProfile>();
}
