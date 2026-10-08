using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class EmployerSearchResult
{
    public Guid RequestId { get; set; }

    public Guid CandidateId { get; set; }

    public decimal RankScore { get; set; }

    public string Explanation { get; set; } = null!;

    public short Position { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual CandidateProfile Candidate { get; set; } = null!;

    public virtual EmployerSearchRequest Request { get; set; } = null!;
}
