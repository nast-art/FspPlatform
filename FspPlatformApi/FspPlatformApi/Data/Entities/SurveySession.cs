using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class SurveySession
{
    public Guid Id { get; set; }

    public Guid CandidateId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string Source { get; set; } = null!;

    public virtual CandidateProfile Candidate { get; set; } = null!;

    public virtual ICollection<SurveyAnswer> SurveyAnswers { get; set; } = new List<SurveyAnswer>();
}
