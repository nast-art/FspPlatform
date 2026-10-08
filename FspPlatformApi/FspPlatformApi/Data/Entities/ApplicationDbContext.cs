using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FspPlatformApi.Data.Entities;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Application> Applications { get; set; }

    public virtual DbSet<AtsIntegration> AtsIntegrations { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<AuthToken> AuthTokens { get; set; }

    public virtual DbSet<CandidateCategory> CandidateCategories { get; set; }

    public virtual DbSet<CandidateProfile> CandidateProfiles { get; set; }

    public virtual DbSet<CandidateSkill> CandidateSkills { get; set; }

    public virtual DbSet<CandidateSpecialization> CandidateSpecializations { get; set; }

    public virtual DbSet<CandidateTask> CandidateTasks { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Company> Companies { get; set; }

    public virtual DbSet<ConsentLog> ConsentLogs { get; set; }

    public virtual DbSet<EmployerSearchRequest> EmployerSearchRequests { get; set; }

    public virtual DbSet<EmployerSearchResult> EmployerSearchResults { get; set; }

    public virtual DbSet<EmployerTask> EmployerTasks { get; set; }

    public virtual DbSet<FspAchievement> FspAchievements { get; set; }

    public virtual DbSet<FspProfile> FspProfiles { get; set; }

    public virtual DbSet<Grade> Grades { get; set; }

    public virtual DbSet<GradeChangeHistory> GradeChangeHistories { get; set; }

    public virtual DbSet<Invitation> Invitations { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Skill> Skills { get; set; }

    public virtual DbSet<SkillCategory> SkillCategories { get; set; }

    public virtual DbSet<Specialization> Specializations { get; set; }

    public virtual DbSet<Status> Statuses { get; set; }

    public virtual DbSet<SurveyAnswer> SurveyAnswers { get; set; }

    public virtual DbSet<SurveySession> SurveySessions { get; set; }

    public virtual DbSet<TestAssignment> TestAssignments { get; set; }

    public virtual DbSet<TestQuestion> TestQuestions { get; set; }

    public virtual DbSet<TestSession> TestSessions { get; set; }

    public virtual DbSet<TestSessionAnswer> TestSessionAnswers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VCandidatePublicCard> VCandidatePublicCards { get; set; }

    public virtual DbSet<VCategoryRanking> VCategoryRankings { get; set; }

    public virtual DbSet<VQuestionStat> VQuestionStats { get; set; }

    public virtual DbSet<Vacancy> Vacancies { get; set; }

    public virtual DbSet<VacancyReport> VacancyReports { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresExtension("btree_gist")
            .HasPostgresExtension("citext")
            .HasPostgresExtension("pg_trgm")
            .HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("applications_pkey");

            entity.ToTable("applications");

            entity.HasIndex(e => new { e.VacancyId, e.CandidateId }, "applications_vacancy_id_candidate_id_key").IsUnique();

            entity.HasIndex(e => new { e.CandidateId, e.Status, e.SubmittedAt }, "idx_applications_candidate").IsDescending(false, false, true);

            entity.HasIndex(e => new { e.VacancyId, e.Status }, "idx_applications_vacancy");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CandidateId).HasColumnName("candidate_id");
            entity.Property(e => e.CoverLetter).HasColumnName("cover_letter");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'отправлен'::text")
                .HasColumnName("status");
            entity.Property(e => e.SubmittedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("submitted_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.VacancyId).HasColumnName("vacancy_id");

            entity.HasOne(d => d.Candidate).WithMany(p => p.Applications)
                .HasForeignKey(d => d.CandidateId)
                .HasConstraintName("applications_candidate_id_fkey");

            entity.HasOne(d => d.StatusNavigation).WithMany(p => p.Applications)
                .HasPrincipalKey(p => p.Code)
                .HasForeignKey(d => d.Status)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("applications_status_fkey");

            entity.HasOne(d => d.Vacancy).WithMany(p => p.Applications)
                .HasForeignKey(d => d.VacancyId)
                .HasConstraintName("applications_vacancy_id_fkey");
        });

        modelBuilder.Entity<AtsIntegration>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ats_integrations_pkey");

            entity.ToTable("ats_integrations");

            entity.HasIndex(e => new { e.CompanyId, e.Provider }, "ats_integrations_company_id_provider_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.Config)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("config");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.LastSyncAt).HasColumnName("last_sync_at");
            entity.Property(e => e.Provider).HasColumnName("provider");

            entity.HasOne(d => d.Company).WithMany(p => p.AtsIntegrations)
                .HasForeignKey(d => d.CompanyId)
                .HasConstraintName("ats_integrations_company_id_fkey");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("audit_log_pkey");

            entity.ToTable("audit_log");

            entity.HasIndex(e => new { e.Entity, e.EntityId, e.CreatedAt }, "idx_audit_entity").IsDescending(false, false, true);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Action).HasColumnName("action");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Entity).HasColumnName("entity");
            entity.Property(e => e.EntityId).HasColumnName("entity_id");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.NewValue)
                .HasColumnType("jsonb")
                .HasColumnName("new_value");
            entity.Property(e => e.OldValue)
                .HasColumnType("jsonb")
                .HasColumnName("old_value");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("audit_log_user_id_fkey");
        });

        modelBuilder.Entity<AuthToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("auth_tokens_pkey");

            entity.ToTable("auth_tokens");

            entity.HasIndex(e => e.TokenHash, "auth_tokens_token_hash_key").IsUnique();

            entity.HasIndex(e => new { e.UserId, e.TokenType, e.ExpiresAt }, "idx_auth_tokens_user");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.TokenHash).HasColumnName("token_hash");
            entity.Property(e => e.TokenType).HasColumnName("token_type");
            entity.Property(e => e.UsedAt).HasColumnName("used_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.TokenTypeNavigation).WithMany(p => p.AuthTokens)
                .HasPrincipalKey(p => p.Code)
                .HasForeignKey(d => d.TokenType)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("auth_tokens_token_type_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.AuthTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("auth_tokens_user_id_fkey");
        });

        modelBuilder.Entity<CandidateCategory>(entity =>
        {
            entity.HasKey(e => new { e.CandidateId, e.CategoryId }).HasName("candidate_categories_pkey");

            entity.ToTable("candidate_categories");

            entity.HasIndex(e => e.CategoryId, "idx_candidate_categories_cat");

            entity.Property(e => e.CandidateId).HasColumnName("candidate_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.IsPrimary).HasColumnName("is_primary");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Candidate).WithMany(p => p.CandidateCategories)
                .HasForeignKey(d => d.CandidateId)
                .HasConstraintName("candidate_categories_candidate_id_fkey");

            entity.HasOne(d => d.Category).WithMany(p => p.CandidateCategories)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("candidate_categories_category_id_fkey");
        });

        modelBuilder.Entity<CandidateProfile>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("candidate_profiles_pkey");

            entity.ToTable("candidate_profiles");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.About).HasColumnName("about");
            entity.Property(e => e.City).HasColumnName("city");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExperienceYears)
                .HasPrecision(4, 1)
                .HasColumnName("experience_years");
            entity.Property(e => e.FirstName).HasColumnName("first_name");
            entity.Property(e => e.IsProfilePublic)
                .HasDefaultValue(true)
                .HasColumnName("is_profile_public");
            entity.Property(e => e.LastActivityAt).HasColumnName("last_activity_at");
            entity.Property(e => e.LastName).HasColumnName("last_name");
            entity.Property(e => e.MiddleName).HasColumnName("middle_name");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.ProfileCompleteness).HasColumnName("profile_completeness");
            entity.Property(e => e.ProfilePdfPath).HasColumnName("profile_pdf_path");
            entity.Property(e => e.ResumePdfPath).HasColumnName("resume_pdf_path");
            entity.Property(e => e.ShowContacts).HasColumnName("show_contacts");
            entity.Property(e => e.SoftSkills).HasColumnName("soft_skills");
            entity.Property(e => e.Telegram).HasColumnName("telegram");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.User).WithOne(p => p.CandidateProfile)
                .HasForeignKey<CandidateProfile>(d => d.UserId)
                .HasConstraintName("candidate_profiles_user_id_fkey");

            entity.HasMany(d => d.Roles).WithMany(p => p.Candidates)
                .UsingEntity<Dictionary<string, object>>(
                    "CandidateRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("candidate_roles_role_id_fkey"),
                    l => l.HasOne<CandidateProfile>().WithMany()
                        .HasForeignKey("CandidateId")
                        .HasConstraintName("candidate_roles_candidate_id_fkey"),
                    j =>
                    {
                        j.HasKey("CandidateId", "RoleId").HasName("candidate_roles_pkey");
                        j.ToTable("candidate_roles");
                        j.IndexerProperty<Guid>("CandidateId").HasColumnName("candidate_id");
                        j.IndexerProperty<short>("RoleId").HasColumnName("role_id");
                    });
        });

        modelBuilder.Entity<CandidateSkill>(entity =>
        {
            entity.HasKey(e => new { e.CandidateId, e.SkillId }).HasName("candidate_skills_pkey");

            entity.ToTable("candidate_skills");

            entity.Property(e => e.CandidateId).HasColumnName("candidate_id");
            entity.Property(e => e.SkillId).HasColumnName("skill_id");
            entity.Property(e => e.Proficiency)
                .HasDefaultValue((short)1)
                .HasColumnName("proficiency");
            entity.Property(e => e.YearsExp)
                .HasPrecision(4, 1)
                .HasColumnName("years_exp");

            entity.HasOne(d => d.Candidate).WithMany(p => p.CandidateSkills)
                .HasForeignKey(d => d.CandidateId)
                .HasConstraintName("candidate_skills_candidate_id_fkey");

            entity.HasOne(d => d.Skill).WithMany(p => p.CandidateSkills)
                .HasForeignKey(d => d.SkillId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("candidate_skills_skill_id_fkey");
        });

        modelBuilder.Entity<CandidateSpecialization>(entity =>
        {
            entity.HasKey(e => new { e.CandidateId, e.SpecializationId }).HasName("candidate_specializations_pkey");

            entity.ToTable("candidate_specializations");

            entity.Property(e => e.CandidateId).HasColumnName("candidate_id");
            entity.Property(e => e.SpecializationId).HasColumnName("specialization_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CurrentGradeId).HasColumnName("current_grade_id");
            entity.Property(e => e.DeclaredGradeId).HasColumnName("declared_grade_id");
            entity.Property(e => e.IsPrimary).HasColumnName("is_primary");
            entity.Property(e => e.LastGradeChangeAt).HasColumnName("last_grade_change_at");
            entity.Property(e => e.LastTestAt).HasColumnName("last_test_at");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'ожидает_тестирования'::text")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Candidate).WithMany(p => p.CandidateSpecializations)
                .HasForeignKey(d => d.CandidateId)
                .HasConstraintName("candidate_specializations_candidate_id_fkey");

            entity.HasOne(d => d.CurrentGrade).WithMany(p => p.CandidateSpecializationCurrentGrades)
                .HasForeignKey(d => d.CurrentGradeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("candidate_specializations_current_grade_id_fkey");

            entity.HasOne(d => d.DeclaredGrade).WithMany(p => p.CandidateSpecializationDeclaredGrades)
                .HasForeignKey(d => d.DeclaredGradeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("candidate_specializations_declared_grade_id_fkey");

            entity.HasOne(d => d.Specialization).WithMany(p => p.CandidateSpecializations)
                .HasForeignKey(d => d.SpecializationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("candidate_specializations_specialization_id_fkey");

            entity.HasOne(d => d.StatusNavigation).WithMany(p => p.CandidateSpecializations)
                .HasPrincipalKey(p => p.Code)
                .HasForeignKey(d => d.Status)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("candidate_specializations_status_fkey");
        });

        modelBuilder.Entity<CandidateTask>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("candidate_tasks_pkey");

            entity.ToTable("candidate_tasks");

            entity.HasIndex(e => new { e.TaskId, e.CandidateId }, "candidate_tasks_task_id_candidate_id_key").IsUnique();

            entity.HasIndex(e => new { e.CandidateId, e.Status, e.AssignedAt }, "idx_candidate_tasks_candidate").IsDescending(false, false, true);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.AnswerPayload)
                .HasColumnType("jsonb")
                .HasColumnName("answer_payload");
            entity.Property(e => e.AnswerText).HasColumnName("answer_text");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("assigned_at");
            entity.Property(e => e.CandidateId).HasColumnName("candidate_id");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.Score)
                .HasPrecision(6, 2)
                .HasColumnName("score");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'назначено'::text")
                .HasColumnName("status");
            entity.Property(e => e.TaskId).HasColumnName("task_id");

            entity.HasOne(d => d.Candidate).WithMany(p => p.CandidateTasks)
                .HasForeignKey(d => d.CandidateId)
                .HasConstraintName("candidate_tasks_candidate_id_fkey");

            entity.HasOne(d => d.StatusNavigation).WithMany(p => p.CandidateTasks)
                .HasPrincipalKey(p => p.Code)
                .HasForeignKey(d => d.Status)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("candidate_tasks_status_fkey");

            entity.HasOne(d => d.Task).WithMany(p => p.CandidateTasks)
                .HasForeignKey(d => d.TaskId)
                .HasConstraintName("candidate_tasks_task_id_fkey");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("categories_pkey");

            entity.ToTable("categories");

            entity.HasIndex(e => new { e.SpecializationId, e.GradeId }, "categories_specialization_id_grade_id_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DisplayName).HasColumnName("display_name");
            entity.Property(e => e.GradeId).HasColumnName("grade_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.SpecializationId).HasColumnName("specialization_id");

            entity.HasOne(d => d.Grade).WithMany(p => p.Categories)
                .HasForeignKey(d => d.GradeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("categories_grade_id_fkey");

            entity.HasOne(d => d.Specialization).WithMany(p => p.Categories)
                .HasForeignKey(d => d.SpecializationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("categories_specialization_id_fkey");
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("companies_pkey");

            entity.ToTable("companies");

            entity.HasIndex(e => e.OwnerUserId, "idx_companies_owner");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.ContactEmail)
                .HasColumnType("citext")
                .HasColumnName("contact_email");
            entity.Property(e => e.ContactPhone).HasColumnName("contact_phone");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Industry).HasColumnName("industry");
            entity.Property(e => e.LogoPath).HasColumnName("logo_path");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.OwnerUserId).HasColumnName("owner_user_id");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'активен'::text")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.VerificationLevel).HasColumnName("verification_level");
            entity.Property(e => e.VerifiedAt).HasColumnName("verified_at");
            entity.Property(e => e.Website).HasColumnName("website");

            entity.HasOne(d => d.OwnerUser).WithMany(p => p.Companies)
                .HasForeignKey(d => d.OwnerUserId)
                .HasConstraintName("companies_owner_user_id_fkey");

            entity.HasOne(d => d.StatusNavigation).WithMany(p => p.Companies)
                .HasPrincipalKey(p => p.Code)
                .HasForeignKey(d => d.Status)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("companies_status_fkey");
        });

        modelBuilder.Entity<ConsentLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("consent_log_pkey");

            entity.ToTable("consent_log");

            entity.HasIndex(e => new { e.UserId, e.ConsentType, e.GrantedAt }, "idx_consent_log_user").IsDescending(false, false, true);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ConsentType).HasColumnName("consent_type");
            entity.Property(e => e.Granted).HasColumnName("granted");
            entity.Property(e => e.GrantedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("granted_at");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.TermsVersion).HasColumnName("terms_version");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.ConsentTypeNavigation).WithMany(p => p.ConsentLogs)
                .HasPrincipalKey(p => p.Code)
                .HasForeignKey(d => d.ConsentType)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("consent_log_consent_type_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.ConsentLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("consent_log_user_id_fkey");
        });

        modelBuilder.Entity<EmployerSearchRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("employer_search_requests_pkey");

            entity.ToTable("employer_search_requests");

            entity.HasIndex(e => new { e.CompanyId, e.CreatedAt }, "idx_search_requests_company").IsDescending(false, true);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Filters)
                .HasColumnType("jsonb")
                .HasColumnName("filters");
            entity.Property(e => e.Title).HasColumnName("title");

            entity.HasOne(d => d.Company).WithMany(p => p.EmployerSearchRequests)
                .HasForeignKey(d => d.CompanyId)
                .HasConstraintName("employer_search_requests_company_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.EmployerSearchRequests)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("employer_search_requests_created_by_fkey");
        });

        modelBuilder.Entity<EmployerSearchResult>(entity =>
        {
            entity.HasKey(e => new { e.RequestId, e.CandidateId }).HasName("employer_search_results_pkey");

            entity.ToTable("employer_search_results");

            entity.HasIndex(e => new { e.RequestId, e.RankScore }, "idx_search_results_rank").IsDescending(false, true);

            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.CandidateId).HasColumnName("candidate_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Explanation)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("explanation");
            entity.Property(e => e.Position).HasColumnName("position");
            entity.Property(e => e.RankScore)
                .HasPrecision(8, 3)
                .HasColumnName("rank_score");

            entity.HasOne(d => d.Candidate).WithMany(p => p.EmployerSearchResults)
                .HasForeignKey(d => d.CandidateId)
                .HasConstraintName("employer_search_results_candidate_id_fkey");

            entity.HasOne(d => d.Request).WithMany(p => p.EmployerSearchResults)
                .HasForeignKey(d => d.RequestId)
                .HasConstraintName("employer_search_results_request_id_fkey");
        });

        modelBuilder.Entity<EmployerTask>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("employer_tasks_pkey");

            entity.ToTable("employer_tasks");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.GradeId).HasColumnName("grade_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.MaxScore)
                .HasPrecision(6, 2)
                .HasColumnName("max_score");
            entity.Property(e => e.Payload)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("payload");
            entity.Property(e => e.SpecializationId).HasColumnName("specialization_id");
            entity.Property(e => e.Title).HasColumnName("title");

            entity.HasOne(d => d.Company).WithMany(p => p.EmployerTasks)
                .HasForeignKey(d => d.CompanyId)
                .HasConstraintName("employer_tasks_company_id_fkey");

            entity.HasOne(d => d.Grade).WithMany(p => p.EmployerTasks)
                .HasForeignKey(d => d.GradeId)
                .HasConstraintName("employer_tasks_grade_id_fkey");

            entity.HasOne(d => d.Specialization).WithMany(p => p.EmployerTasks)
                .HasForeignKey(d => d.SpecializationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("employer_tasks_specialization_id_fkey");
        });

        modelBuilder.Entity<FspAchievement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("fsp_achievements_pkey");

            entity.ToTable("fsp_achievements");

            entity.HasIndex(e => new { e.FspId, e.EventName, e.EventDate }, "fsp_achievements_fsp_id_event_name_event_date_key").IsUnique();

            entity.HasIndex(e => new { e.FspId, e.EventDate }, "idx_fsp_achievements_fsp").IsDescending(false, true);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Direction).HasColumnName("direction");
            entity.Property(e => e.EventDate).HasColumnName("event_date");
            entity.Property(e => e.EventName).HasColumnName("event_name");
            entity.Property(e => e.EventType).HasColumnName("event_type");
            entity.Property(e => e.FspId).HasColumnName("fsp_id");
            entity.Property(e => e.ResultPlace).HasColumnName("result_place");
            entity.Property(e => e.ResultScore)
                .HasPrecision(8, 2)
                .HasColumnName("result_score");
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.SourcePayload)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("source_payload");
            entity.Property(e => e.Verified).HasColumnName("verified");
            entity.Property(e => e.Weight)
                .HasPrecision(4, 2)
                .HasDefaultValue(1.00m)
                .HasColumnName("weight");

            entity.HasOne(d => d.EventTypeNavigation).WithMany(p => p.FspAchievements)
                .HasPrincipalKey(p => p.Code)
                .HasForeignKey(d => d.EventType)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fsp_achievements_event_type_fkey");

            entity.HasOne(d => d.Fsp).WithMany(p => p.FspAchievements)
                .HasForeignKey(d => d.FspId)
                .HasConstraintName("fsp_achievements_fsp_id_fkey");
        });

        modelBuilder.Entity<FspProfile>(entity =>
        {
            entity.HasKey(e => e.FspId).HasName("fsp_profiles_pkey");

            entity.ToTable("fsp_profiles");

            entity.Property(e => e.FspId)
                .ValueGeneratedNever()
                .HasColumnName("fsp_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.LinkedAt).HasColumnName("linked_at");
        });

        modelBuilder.Entity<Grade>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("grades_pkey");

            entity.ToTable("grades");

            entity.HasIndex(e => e.Code, "grades_code_key").IsUnique();

            entity.HasIndex(e => e.Rank, "grades_rank_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.MinExpYears)
                .HasPrecision(4, 1)
                .HasColumnName("min_exp_years");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Rank).HasColumnName("rank");
        });

        modelBuilder.Entity<GradeChangeHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("grade_change_history_pkey");

            entity.ToTable("grade_change_history");

            entity.HasIndex(e => new { e.CandidateId, e.SpecializationId, e.ChangedAt }, "idx_grade_history_candidate").IsDescending(false, false, true);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CandidateId).HasColumnName("candidate_id");
            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("changed_at");
            entity.Property(e => e.NewGradeId).HasColumnName("new_grade_id");
            entity.Property(e => e.OldGradeId).HasColumnName("old_grade_id");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.SpecializationId).HasColumnName("specialization_id");
            entity.Property(e => e.TestSessionId).HasColumnName("test_session_id");

            entity.HasOne(d => d.Candidate).WithMany(p => p.GradeChangeHistories)
                .HasForeignKey(d => d.CandidateId)
                .HasConstraintName("grade_change_history_candidate_id_fkey");

            entity.HasOne(d => d.NewGrade).WithMany(p => p.GradeChangeHistoryNewGrades)
                .HasForeignKey(d => d.NewGradeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("grade_change_history_new_grade_id_fkey");

            entity.HasOne(d => d.OldGrade).WithMany(p => p.GradeChangeHistoryOldGrades)
                .HasForeignKey(d => d.OldGradeId)
                .HasConstraintName("grade_change_history_old_grade_id_fkey");

            entity.HasOne(d => d.ReasonNavigation).WithMany(p => p.GradeChangeHistories)
                .HasPrincipalKey(p => p.Code)
                .HasForeignKey(d => d.Reason)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("grade_change_history_reason_fkey");

            entity.HasOne(d => d.Specialization).WithMany(p => p.GradeChangeHistories)
                .HasForeignKey(d => d.SpecializationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("grade_change_history_specialization_id_fkey");

            entity.HasOne(d => d.TestSession).WithMany(p => p.GradeChangeHistories)
                .HasForeignKey(d => d.TestSessionId)
                .HasConstraintName("grade_change_history_test_session_id_fkey");
        });

        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("invitations_pkey");

            entity.ToTable("invitations");

            entity.HasIndex(e => new { e.CandidateId, e.Status, e.SentAt }, "idx_invitations_candidate").IsDescending(false, false, true);

            entity.HasIndex(e => new { e.CompanyId, e.Status, e.SentAt }, "idx_invitations_company").IsDescending(false, false, true);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CandidateId).HasColumnName("candidate_id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.ContactMethod)
                .HasDefaultValueSql("'почта'::text")
                .HasColumnName("contact_method");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .HasDefaultValueSql("'RUB'::bpchar")
                .IsFixedLength()
                .HasColumnName("currency");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.RespondedAt).HasColumnName("responded_at");
            entity.Property(e => e.SalaryFrom).HasColumnName("salary_from");
            entity.Property(e => e.SalaryTo).HasColumnName("salary_to");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("sent_at");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'отправлено'::text")
                .HasColumnName("status");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.VacancyId).HasColumnName("vacancy_id");
            entity.Property(e => e.ViewedAt).HasColumnName("viewed_at");

            entity.HasOne(d => d.Candidate).WithMany(p => p.Invitations)
                .HasForeignKey(d => d.CandidateId)
                .HasConstraintName("invitations_candidate_id_fkey");

            entity.HasOne(d => d.Company).WithMany(p => p.Invitations)
                .HasForeignKey(d => d.CompanyId)
                .HasConstraintName("invitations_company_id_fkey");

            entity.HasOne(d => d.ContactMethodNavigation).WithMany(p => p.InvitationContactMethodNavigations)
                .HasPrincipalKey(p => p.Code)
                .HasForeignKey(d => d.ContactMethod)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("invitations_contact_method_fkey");

            entity.HasOne(d => d.StatusNavigation).WithMany(p => p.InvitationStatusNavigations)
                .HasPrincipalKey(p => p.Code)
                .HasForeignKey(d => d.Status)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("invitations_status_fkey");

            entity.HasOne(d => d.Vacancy).WithMany(p => p.Invitations)
                .HasForeignKey(d => d.VacancyId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("invitations_vacancy_id_fkey");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notifications_pkey");

            entity.ToTable("notifications");

            entity.HasIndex(e => new { e.UserId, e.ReadAt, e.CreatedAt }, "idx_notifications_user").IsDescending(false, false, true);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Payload)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("payload");
            entity.Property(e => e.ReadAt).HasColumnName("read_at");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("notifications_user_id_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("roles_pkey");

            entity.ToTable("roles");

            entity.HasIndex(e => e.Name, "roles_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name).HasColumnName("name");
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("skills_pkey");

            entity.ToTable("skills");

            entity.HasIndex(e => e.Name, "skills_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name).HasColumnName("name");

            entity.HasOne(d => d.Category).WithMany(p => p.Skills)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("skills_category_id_fkey");
        });

        modelBuilder.Entity<SkillCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("skill_categories_pkey");

            entity.ToTable("skill_categories");

            entity.HasIndex(e => e.Name, "skill_categories_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
        });

        modelBuilder.Entity<Specialization>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("specializations_pkey");

            entity.ToTable("specializations");

            entity.HasIndex(e => e.Code, "specializations_code_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("specializations_parent_id_fkey");
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("statuses_pkey");

            entity.ToTable("statuses");

            entity.HasIndex(e => new { e.Entity, e.SortOrder }, "idx_statuses_entity");

            entity.HasIndex(e => e.Code, "statuses_code_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Entity).HasColumnName("entity");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
        });

        modelBuilder.Entity<SurveyAnswer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("survey_answers_pkey");

            entity.ToTable("survey_answers");

            entity.HasIndex(e => new { e.SessionId, e.QuestionKey }, "idx_survey_answers_session");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AnswerValue)
                .HasColumnType("jsonb")
                .HasColumnName("answer_value");
            entity.Property(e => e.AnsweredAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("answered_at");
            entity.Property(e => e.QuestionKey).HasColumnName("question_key");
            entity.Property(e => e.SessionId).HasColumnName("session_id");

            entity.HasOne(d => d.Session).WithMany(p => p.SurveyAnswers)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("survey_answers_session_id_fkey");
        });

        modelBuilder.Entity<SurveySession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("survey_sessions_pkey");

            entity.ToTable("survey_sessions");

            entity.HasIndex(e => new { e.CandidateId, e.StartedAt }, "idx_survey_sessions_candidate").IsDescending(false, true);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CandidateId).HasColumnName("candidate_id");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.Source)
                .HasDefaultValueSql("'регистрация'::text")
                .HasColumnName("source");
            entity.Property(e => e.StartedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("started_at");

            entity.HasOne(d => d.Candidate).WithMany(p => p.SurveySessions)
                .HasForeignKey(d => d.CandidateId)
                .HasConstraintName("survey_sessions_candidate_id_fkey");
        });

        modelBuilder.Entity<TestAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("test_assignments_pkey");

            entity.ToTable("test_assignments");

            entity.HasIndex(e => new { e.SpecializationId, e.GradeId, e.Version }, "test_assignments_specialization_id_grade_id_version_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DifficultyMix)
                .HasDefaultValueSql("'{\"1\": 3, \"2\": 4, \"3\": 2, \"4\": 1}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("difficulty_mix");
            entity.Property(e => e.DurationMinutes)
                .HasDefaultValue((short)30)
                .HasColumnName("duration_minutes");
            entity.Property(e => e.GradeId).HasColumnName("grade_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.PassThreshold)
                .HasPrecision(5, 2)
                .HasDefaultValue(60.00m)
                .HasColumnName("pass_threshold");
            entity.Property(e => e.QuestionsCount)
                .HasDefaultValue((short)10)
                .HasColumnName("questions_count");
            entity.Property(e => e.SpecializationId).HasColumnName("specialization_id");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Version)
                .HasDefaultValue(1)
                .HasColumnName("version");

            entity.HasOne(d => d.Grade).WithMany(p => p.TestAssignments)
                .HasForeignKey(d => d.GradeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("test_assignments_grade_id_fkey");

            entity.HasOne(d => d.Specialization).WithMany(p => p.TestAssignments)
                .HasForeignKey(d => d.SpecializationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("test_assignments_specialization_id_fkey");
        });

        modelBuilder.Entity<TestQuestion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("test_questions_pkey");

            entity.ToTable("test_questions");

            entity.HasIndex(e => e.QuestionGroup, "idx_test_questions_group");

            entity.HasIndex(e => new { e.SpecializationId, e.GradeId, e.QuestionType, e.IsActive }, "idx_test_questions_lookup");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CorrectPayload)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("correct_payload");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Difficulty)
                .HasDefaultValue((short)1)
                .HasColumnName("difficulty");
            entity.Property(e => e.GradeId).HasColumnName("grade_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.MaxScore)
                .HasPrecision(6, 2)
                .HasDefaultValue(1m)
                .HasColumnName("max_score");
            entity.Property(e => e.Payload)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("payload");
            entity.Property(e => e.Prompt).HasColumnName("prompt");
            entity.Property(e => e.QuestionGroup)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("question_group");
            entity.Property(e => e.QuestionType)
                .HasDefaultValueSql("'тест_с_вариантами'::text")
                .HasColumnName("question_type");
            entity.Property(e => e.SpecializationId).HasColumnName("specialization_id");

            entity.HasOne(d => d.Grade).WithMany(p => p.TestQuestions)
                .HasForeignKey(d => d.GradeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("test_questions_grade_id_fkey");

            entity.HasOne(d => d.QuestionTypeNavigation).WithMany(p => p.TestQuestions)
                .HasPrincipalKey(p => p.Code)
                .HasForeignKey(d => d.QuestionType)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("test_questions_question_type_fkey");

            entity.HasOne(d => d.Specialization).WithMany(p => p.TestQuestions)
                .HasForeignKey(d => d.SpecializationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("test_questions_specialization_id_fkey");
        });

        modelBuilder.Entity<TestSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("test_sessions_pkey");

            entity.ToTable("test_sessions");

            entity.HasIndex(e => new { e.CandidateId, e.SpecializationId, e.StartedAt }, "idx_test_sessions_candidate").IsDescending(false, false, true);

            entity.HasIndex(e => new { e.SpecializationId, e.ResultingGradeId, e.Passed }, "idx_test_sessions_result").HasFilter("(status = 'завершен'::text)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.AssignmentId).HasColumnName("assignment_id");
            entity.Property(e => e.AttemptNo)
                .HasDefaultValue((short)1)
                .HasColumnName("attempt_no");
            entity.Property(e => e.CandidateId).HasColumnName("candidate_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.FinishedAt).HasColumnName("finished_at");
            entity.Property(e => e.MaxScore)
                .HasPrecision(8, 2)
                .HasColumnName("max_score");
            entity.Property(e => e.Passed).HasColumnName("passed");
            entity.Property(e => e.Percent)
                .HasPrecision(5, 2)
                .HasComputedColumnSql("\nCASE\n    WHEN (max_score > (0)::numeric) THEN round(((total_score / max_score) * (100)::numeric), 2)\n    ELSE NULL::numeric\nEND", true)
                .HasColumnName("percent");
            entity.Property(e => e.ResultingGradeId).HasColumnName("resulting_grade_id");
            entity.Property(e => e.SpecializationId).HasColumnName("specialization_id");
            entity.Property(e => e.StartedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("started_at");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'в_процессе'::text")
                .HasColumnName("status");
            entity.Property(e => e.TargetGradeId).HasColumnName("target_grade_id");
            entity.Property(e => e.TotalScore)
                .HasPrecision(8, 2)
                .HasColumnName("total_score");

            entity.HasOne(d => d.Assignment).WithMany(p => p.TestSessions)
                .HasForeignKey(d => d.AssignmentId)
                .HasConstraintName("test_sessions_assignment_id_fkey");

            entity.HasOne(d => d.Candidate).WithMany(p => p.TestSessions)
                .HasForeignKey(d => d.CandidateId)
                .HasConstraintName("test_sessions_candidate_id_fkey");

            entity.HasOne(d => d.ResultingGrade).WithMany(p => p.TestSessionResultingGrades)
                .HasForeignKey(d => d.ResultingGradeId)
                .HasConstraintName("test_sessions_resulting_grade_id_fkey");

            entity.HasOne(d => d.Specialization).WithMany(p => p.TestSessions)
                .HasForeignKey(d => d.SpecializationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("test_sessions_specialization_id_fkey");

            entity.HasOne(d => d.StatusNavigation).WithMany(p => p.TestSessions)
                .HasPrincipalKey(p => p.Code)
                .HasForeignKey(d => d.Status)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("test_sessions_status_fkey");

            entity.HasOne(d => d.TargetGrade).WithMany(p => p.TestSessionTargetGrades)
                .HasForeignKey(d => d.TargetGradeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("test_sessions_target_grade_id_fkey");
        });

        modelBuilder.Entity<TestSessionAnswer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("test_session_answers_pkey");

            entity.ToTable("test_session_answers");

            entity.HasIndex(e => new { e.SessionId, e.QuestionId }, "test_session_answers_session_id_question_id_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AnswerPayload)
                .HasColumnType("jsonb")
                .HasColumnName("answer_payload");
            entity.Property(e => e.AnsweredAt).HasColumnName("answered_at");
            entity.Property(e => e.AutoGraded)
                .HasDefaultValue(true)
                .HasColumnName("auto_graded");
            entity.Property(e => e.GradedAt).HasColumnName("graded_at");
            entity.Property(e => e.OrderNo).HasColumnName("order_no");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.Score)
                .HasPrecision(6, 2)
                .HasColumnName("score");
            entity.Property(e => e.SessionId).HasColumnName("session_id");

            entity.HasOne(d => d.Question).WithMany(p => p.TestSessionAnswers)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("test_session_answers_question_id_fkey");

            entity.HasOne(d => d.Session).WithMany(p => p.TestSessionAnswers)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("test_session_answers_session_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.FspId, "idx_users_fsp_id").HasFilter("(fsp_id IS NOT NULL)");

            entity.HasIndex(e => e.Role, "idx_users_role").HasFilter("is_active");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.HasIndex(e => e.FspId, "users_fsp_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasColumnType("citext")
                .HasColumnName("email");
            entity.Property(e => e.EmailConfirmed).HasColumnName("email_confirmed");
            entity.Property(e => e.FspId).HasColumnName("fsp_id");
            entity.Property(e => e.FspLinkedAt).HasColumnName("fsp_linked_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Fsp).WithOne(p => p.User)
                .HasForeignKey<User>(d => d.FspId)
                .HasConstraintName("users_fsp_id_fkey");

            entity.HasOne(d => d.RoleNavigation).WithMany(p => p.Users)
                .HasPrincipalKey(p => p.Code)
                .HasForeignKey(d => d.Role)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("users_role_fkey");
        });

        modelBuilder.Entity<VCandidatePublicCard>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_candidate_public_card");

            entity.Property(e => e.About).HasColumnName("about");
            entity.Property(e => e.CandidateId).HasColumnName("candidate_id");
            entity.Property(e => e.City).HasColumnName("city");
            entity.Property(e => e.CurrentGradeId).HasColumnName("current_grade_id");
            entity.Property(e => e.ExperienceYears)
                .HasPrecision(4, 1)
                .HasColumnName("experience_years");
            entity.Property(e => e.FirstName).HasColumnName("first_name");
            entity.Property(e => e.FspId).HasColumnName("fsp_id");
            entity.Property(e => e.HasFspHistory).HasColumnName("has_fsp_history");
            entity.Property(e => e.IsProfilePublic).HasColumnName("is_profile_public");
            entity.Property(e => e.LastActivityAt).HasColumnName("last_activity_at");
            entity.Property(e => e.LastName).HasColumnName("last_name");
            entity.Property(e => e.MiddleName).HasColumnName("middle_name");
            entity.Property(e => e.ProfilePdfPath).HasColumnName("profile_pdf_path");
            entity.Property(e => e.SoftSkills).HasColumnName("soft_skills");
            entity.Property(e => e.SpecializationId).HasColumnName("specialization_id");
        });

        modelBuilder.Entity<VCategoryRanking>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_category_rankings");

            entity.Property(e => e.BestTestPassed).HasColumnName("best_test_passed");
            entity.Property(e => e.BestTestPercent).HasColumnName("best_test_percent");
            entity.Property(e => e.CandidateId).HasColumnName("candidate_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.FspAchievementsWeight).HasColumnName("fsp_achievements_weight");
            entity.Property(e => e.PositionInCategory).HasColumnName("position_in_category");
            entity.Property(e => e.RankScore).HasColumnName("rank_score");
        });

        modelBuilder.Entity<VQuestionStat>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_question_stats");

            entity.Property(e => e.Attempts).HasColumnName("attempts");
            entity.Property(e => e.AvgRelativeScore).HasColumnName("avg_relative_score");
            entity.Property(e => e.Difficulty).HasColumnName("difficulty");
            entity.Property(e => e.DiscriminationIndex).HasColumnName("discrimination_index");
            entity.Property(e => e.GradeId).HasColumnName("grade_id");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.SpecializationId).HasColumnName("specialization_id");
        });

        modelBuilder.Entity<Vacancy>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("vacancies_pkey");

            entity.ToTable("vacancies");

            entity.HasIndex(e => new { e.SalaryFrom, e.SalaryTo }, "idx_vacancies_salary");

            entity.HasIndex(e => new { e.SpecializationId, e.Status, e.PublishedAt }, "idx_vacancies_search")
                .IsDescending(false, false, true)
                .HasFilter("(status = 'активен'::text)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .HasDefaultValueSql("'RUB'::bpchar")
                .IsFixedLength()
                .HasColumnName("currency");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EmploymentType).HasColumnName("employment_type");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.GradeFromId).HasColumnName("grade_from_id");
            entity.Property(e => e.GradeToId).HasColumnName("grade_to_id");
            entity.Property(e => e.Location).HasColumnName("location");
            entity.Property(e => e.PublishedAt).HasColumnName("published_at");
            entity.Property(e => e.SalaryFrom).HasColumnName("salary_from");
            entity.Property(e => e.SalaryTo).HasColumnName("salary_to");
            entity.Property(e => e.SpecializationId).HasColumnName("specialization_id");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'черновик'::text")
                .HasColumnName("status");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.ViewsCount).HasColumnName("views_count");

            entity.HasOne(d => d.Company).WithMany(p => p.Vacancies)
                .HasForeignKey(d => d.CompanyId)
                .HasConstraintName("vacancies_company_id_fkey");

            entity.HasOne(d => d.GradeFrom).WithMany(p => p.VacancyGradeFroms)
                .HasForeignKey(d => d.GradeFromId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("vacancies_grade_from_id_fkey");

            entity.HasOne(d => d.GradeTo).WithMany(p => p.VacancyGradeTos)
                .HasForeignKey(d => d.GradeToId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("vacancies_grade_to_id_fkey");

            entity.HasOne(d => d.Specialization).WithMany(p => p.Vacancies)
                .HasForeignKey(d => d.SpecializationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("vacancies_specialization_id_fkey");

            entity.HasOne(d => d.StatusNavigation).WithMany(p => p.Vacancies)
                .HasPrincipalKey(p => p.Code)
                .HasForeignKey(d => d.Status)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("vacancies_status_fkey");

            entity.HasMany(d => d.Skills).WithMany(p => p.Vacancies)
                .UsingEntity<Dictionary<string, object>>(
                    "VacancySkill",
                    r => r.HasOne<Skill>().WithMany()
                        .HasForeignKey("SkillId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("vacancy_skills_skill_id_fkey"),
                    l => l.HasOne<Vacancy>().WithMany()
                        .HasForeignKey("VacancyId")
                        .HasConstraintName("vacancy_skills_vacancy_id_fkey"),
                    j =>
                    {
                        j.HasKey("VacancyId", "SkillId").HasName("vacancy_skills_pkey");
                        j.ToTable("vacancy_skills");
                        j.IndexerProperty<Guid>("VacancyId").HasColumnName("vacancy_id");
                        j.IndexerProperty<short>("SkillId").HasColumnName("skill_id");
                    });
        });

        modelBuilder.Entity<VacancyReport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("vacancy_reports_pkey");

            entity.ToTable("vacancy_reports");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Details).HasColumnName("details");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.ReporterId).HasColumnName("reporter_id");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'активен'::text")
                .HasColumnName("status");
            entity.Property(e => e.VacancyId).HasColumnName("vacancy_id");

            entity.HasOne(d => d.Company).WithMany(p => p.VacancyReports)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("vacancy_reports_company_id_fkey");

            entity.HasOne(d => d.Reporter).WithMany(p => p.VacancyReports)
                .HasForeignKey(d => d.ReporterId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("vacancy_reports_reporter_id_fkey");

            entity.HasOne(d => d.StatusNavigation).WithMany(p => p.VacancyReports)
                .HasPrincipalKey(p => p.Code)
                .HasForeignKey(d => d.Status)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("vacancy_reports_status_fkey");

            entity.HasOne(d => d.Vacancy).WithMany(p => p.VacancyReports)
                .HasForeignKey(d => d.VacancyId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("vacancy_reports_vacancy_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
