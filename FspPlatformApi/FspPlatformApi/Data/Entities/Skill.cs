using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class Skill
{
    public short Id { get; set; }

    public string Name { get; set; } = null!;

    public short CategoryId { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<CandidateSkill> CandidateSkills { get; set; } = new List<CandidateSkill>();

    public virtual SkillCategory Category { get; set; } = null!;

    public virtual ICollection<Vacancy> Vacancies { get; set; } = new List<Vacancy>();
}
