using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class Specialization
{
    public short Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public short? ParentId { get; set; }

    public short SortOrder { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<CandidateSpecialization> CandidateSpecializations { get; set; } = new List<CandidateSpecialization>();

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

    public virtual ICollection<EmployerTask> EmployerTasks { get; set; } = new List<EmployerTask>();

    public virtual ICollection<GradeChangeHistory> GradeChangeHistories { get; set; } = new List<GradeChangeHistory>();

    public virtual ICollection<Specialization> InverseParent { get; set; } = new List<Specialization>();

    public virtual Specialization? Parent { get; set; }

    public virtual ICollection<TestAssignment> TestAssignments { get; set; } = new List<TestAssignment>();

    public virtual ICollection<TestQuestion> TestQuestions { get; set; } = new List<TestQuestion>();

    public virtual ICollection<TestSession> TestSessions { get; set; } = new List<TestSession>();

    public virtual ICollection<Vacancy> Vacancies { get; set; } = new List<Vacancy>();
}
