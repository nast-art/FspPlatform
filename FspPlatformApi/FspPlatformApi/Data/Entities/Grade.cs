using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class Grade
{
    public short Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public short Rank { get; set; }

    public decimal? MinExpYears { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<CandidateSpecialization> CandidateSpecializationCurrentGrades { get; set; } = new List<CandidateSpecialization>();

    public virtual ICollection<CandidateSpecialization> CandidateSpecializationDeclaredGrades { get; set; } = new List<CandidateSpecialization>();

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

    public virtual ICollection<EmployerTask> EmployerTasks { get; set; } = new List<EmployerTask>();

    public virtual ICollection<GradeChangeHistory> GradeChangeHistoryNewGrades { get; set; } = new List<GradeChangeHistory>();

    public virtual ICollection<GradeChangeHistory> GradeChangeHistoryOldGrades { get; set; } = new List<GradeChangeHistory>();

    public virtual ICollection<TestAssignment> TestAssignments { get; set; } = new List<TestAssignment>();

    public virtual ICollection<TestQuestion> TestQuestions { get; set; } = new List<TestQuestion>();

    public virtual ICollection<TestSession> TestSessionResultingGrades { get; set; } = new List<TestSession>();

    public virtual ICollection<TestSession> TestSessionTargetGrades { get; set; } = new List<TestSession>();

    public virtual ICollection<Vacancy> VacancyGradeFroms { get; set; } = new List<Vacancy>();

    public virtual ICollection<Vacancy> VacancyGradeTos { get; set; } = new List<Vacancy>();
}
