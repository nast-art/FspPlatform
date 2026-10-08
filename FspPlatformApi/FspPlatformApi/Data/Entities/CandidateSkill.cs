using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class CandidateSkill
{
    public Guid CandidateId { get; set; }

    public short SkillId { get; set; }

    public short Proficiency { get; set; }

    public decimal? YearsExp { get; set; }

    public virtual CandidateProfile Candidate { get; set; } = null!;

    public virtual Skill Skill { get; set; } = null!;
}
