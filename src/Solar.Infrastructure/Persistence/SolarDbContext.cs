using Microsoft.EntityFrameworkCore;
using Solar.Application.Administration;
using Solar.Application.Auth;
using Solar.Domain.Entities;
using Solar.Domain.Enums;

namespace Solar.Infrastructure.Persistence;

public class SolarDbContext : DbContext, ISolarAuthDbContext, IBlacklistDbContext
{
    public SolarDbContext(DbContextOptions<SolarDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Allocation> Allocations => Set<Allocation>();
    public DbSet<AllocationTag> AllocationTags => Set<AllocationTag>();
    public DbSet<AcademicAllocation> AcademicAllocations => Set<AcademicAllocation>();
    public DbSet<AcademicAllocationUser> AcademicAllocationUsers => Set<AcademicAllocationUser>();

    // Assessments & Question Bank
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionItem> QuestionItems => Set<QuestionItem>();
    public DbSet<ExamQuestion> ExamQuestions => Set<ExamQuestion>();
    public DbSet<ExamUserAttempt> ExamUserAttempts => Set<ExamUserAttempt>();
    public DbSet<ExamResponse> ExamResponses => Set<ExamResponse>();
    public DbSet<ExamResponseQuestionItem> ExamResponseQuestionItems => Set<ExamResponseQuestionItem>();

    // Academic Structure & Hierarchy
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CurriculumUnitType> CurriculumUnitTypes => Set<CurriculumUnitType>();
    public DbSet<CurriculumUnit> CurriculumUnits => Set<CurriculumUnit>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Semester> Semesters => Set<Semester>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<RelatedTaggable> RelatedTaggables => Set<RelatedTaggable>();

    // Forums & Discussions
    public DbSet<Discussion> Discussions => Set<Discussion>();
    public DbSet<DiscussionPost> DiscussionPosts => Set<DiscussionPost>();
    public DbSet<DiscussionPostFile> DiscussionPostFiles => Set<DiscussionPostFile>();

    // Lessons & Notes
    public DbSet<LessonModule> LessonModules => Set<LessonModule>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonUser> LessonUsers => Set<LessonUser>();
    public DbSet<LessonNote> LessonNotes => Set<LessonNote>();

    // Assignments & Submissions
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<AssignmentFile> AssignmentFiles => Set<AssignmentFile>();
    public DbSet<GroupAssignment> GroupAssignments => Set<GroupAssignment>();
    public DbSet<GroupParticipant> GroupParticipants => Set<GroupParticipant>();
    public DbSet<SubmissionComment> SubmissionComments => Set<SubmissionComment>();

    // Communication & Notifications
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ReadNotification> ReadNotifications => Set<ReadNotification>();
    public DbSet<UserBlacklist> UserBlacklists => Set<UserBlacklist>();
    public DbSet<InternalMessage> InternalMessages => Set<InternalMessage>();
    public DbSet<UserInternalMessage> UserInternalMessages => Set<UserInternalMessage>();

    // Supporting Materials & Events
    public DbSet<SupportMaterialFile> SupportMaterialFiles => Set<SupportMaterialFile>();
    public DbSet<Bibliography> Bibliographies => Set<Bibliography>();
    public DbSet<ScheduleEvent> ScheduleEvents => Set<ScheduleEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Users
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200);
            entity.Property(e => e.Nick).HasColumnName("nick").HasMaxLength(35).IsRequired();
            entity.Property(e => e.Username).HasColumnName("username").IsRequired();
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.EncryptedPassword).HasColumnName("encrypted_password").IsRequired();
            entity.Property(e => e.PasswordSalt).HasColumnName("password_salt");
            entity.Property(e => e.ResetPasswordToken).HasColumnName("reset_password_token");
            entity.Property(e => e.ResetPasswordSentAt).HasColumnName("reset_password_sent_at");
            entity.Property(e => e.AuthenticationToken).HasColumnName("authentication_token");
            entity.Property(e => e.SessionToken).HasColumnName("session_token");
            entity.Ignore(e => e.SignInCount);
            entity.Ignore(e => e.CurrentSignInAt);
            entity.Ignore(e => e.LastSignInAt);
            entity.Ignore(e => e.CurrentSignInIp);
            entity.Ignore(e => e.LastSignInIp);
            entity.Property(e => e.Birthdate).HasColumnName("birthdate");
            entity.Property(e => e.EnrollmentCode).HasColumnName("enrollment_code").HasMaxLength(20);
            entity.Property(e => e.Cpf).HasColumnName("cpf").HasMaxLength(14);
            entity.Property(e => e.Gender).HasColumnName("gender");
            entity.Property(e => e.Telephone).HasColumnName("telephone").HasMaxLength(50);
            entity.Property(e => e.CellPhone).HasColumnName("cell_phone").HasMaxLength(100);
            entity.Property(e => e.Institution).HasColumnName("institution").HasMaxLength(120);
            entity.Property(e => e.SpecialNeeds).HasColumnName("special_needs").HasMaxLength(50);
            entity.Property(e => e.Address).HasColumnName("address").HasMaxLength(150);
            entity.Property(e => e.AddressNumber).HasColumnName("address_number").HasMaxLength(10);
            entity.Property(e => e.AddressComplement).HasColumnName("address_complement").HasMaxLength(50);
            entity.Property(e => e.AddressNeighborhood).HasColumnName("address_neighborhood").HasMaxLength(50);
            entity.Property(e => e.Zipcode).HasColumnName("zipcode").HasMaxLength(11);
            entity.Property(e => e.City).HasColumnName("city").HasMaxLength(100);
            entity.Property(e => e.State).HasColumnName("state").HasMaxLength(100);
            entity.Property(e => e.Country).HasColumnName("country").HasMaxLength(100);
            entity.Property(e => e.Bio).HasColumnName("bio");
            entity.Property(e => e.Interests).HasColumnName("interests");
            entity.Property(e => e.Site).HasColumnName("site");
            entity.Property(e => e.Active).HasColumnName("active").HasDefaultValue(true);
            entity.Property(e => e.Registered).HasColumnName("registered").HasDefaultValue(false);
            entity.Property(e => e.Integrated).HasColumnName("integrated").HasDefaultValue(false);
            entity.Property(e => e.Selfregistration).HasColumnName("selfregistration").HasDefaultValue(false);
            entity.Property(e => e.DigitalClassUserId).HasColumnName("digital_class_user_id");
            entity.Property(e => e.OauthApplicationId).HasColumnName("oauth_application_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Cpf);
            entity.HasIndex(e => e.Email);
        });

        // Profiles
        modelBuilder.Entity<Profile>(entity =>
        {
            entity.ToTable("profiles");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
            entity.Property(e => e.Types).HasColumnName("types");
            entity.Property(e => e.Status).HasColumnName("status").HasDefaultValue(true);
            entity.Ignore(e => e.CreatedAt);
            entity.Ignore(e => e.UpdatedAt);
        });

        // Allocations
        modelBuilder.Entity<Allocation>(entity =>
        {
            entity.ToTable("allocations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.AllocationTagId).HasColumnName("allocation_tag_id");
            entity.Property(e => e.ProfileId).HasColumnName("profile_id");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.ParcialGrade).HasColumnName("parcial_grade");
            entity.Property(e => e.FinalExamGrade).HasColumnName("final_exam_grade");
            entity.Property(e => e.FinalGrade).HasColumnName("final_grade");
            entity.Property(e => e.WorkingHours).HasColumnName("working_hours").HasColumnType("numeric(5,2)");
            entity.Property(e => e.GradeSituation).HasColumnName("grade_situation");
            entity.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id");
            entity.Property(e => e.OriginGroupId).HasColumnName("origin_group_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.User)
                .WithMany(u => u.Allocations)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Profile)
                .WithMany(p => p.Allocations)
                .HasForeignKey(e => e.ProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AllocationTag)
                .WithMany(t => t.Allocations)
                .HasForeignKey(e => e.AllocationTagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AllocationTags
        modelBuilder.Entity<AllocationTag>(entity =>
        {
            entity.ToTable("allocation_tags");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.OfferId).HasColumnName("offer_id");
            entity.Property(e => e.CurriculumUnitId).HasColumnName("curriculum_unit_id");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.CurriculumUnitTypeId).HasColumnName("curriculum_unit_type_id");
            entity.Property(e => e.SettedSituation).HasColumnName("setted_situation").HasDefaultValue(false);
            entity.Property(e => e.SituationDate).HasColumnName("situation_date");
            entity.Property(e => e.SituationDateAcId).HasColumnName("situation_date_ac_id");
            entity.Property(e => e.CalculationType).HasColumnName("calculation_type").HasDefaultValue(CalculationType.WeightedFormula);
        });

        // AcademicAllocations
        modelBuilder.Entity<AcademicAllocation>(entity =>
        {
            entity.ToTable("academic_allocations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AllocationTagId).HasColumnName("allocation_tag_id");
            entity.Property(e => e.AcademicToolType).HasColumnName("academic_tool_type").IsRequired();
            entity.Property(e => e.AcademicToolId).HasColumnName("academic_tool_id");
            entity.Property(e => e.Evaluative).HasColumnName("evaluative").HasDefaultValue(false);
            entity.Property(e => e.Frequency).HasColumnName("frequency").HasDefaultValue(false);
            entity.Property(e => e.FinalExam).HasColumnName("final_exam").HasDefaultValue(false);
            entity.Property(e => e.FrequencyAutomatic).HasColumnName("frequency_automatic").HasDefaultValue(false);
            entity.Property(e => e.MaxWorkingHours).HasColumnName("max_working_hours").HasColumnType("numeric(5,2)").HasDefaultValue(1);
            entity.Property(e => e.EquivalentAcademicAllocationId).HasColumnName("equivalent_academic_allocation_id");
            entity.Property(e => e.Weight).HasColumnName("weight").HasColumnType("numeric(5,2)").HasDefaultValue(1);
            entity.Property(e => e.FinalWeight).HasColumnName("final_weight").HasColumnType("numeric(5,2)").HasDefaultValue(100);

            entity.HasOne(e => e.AllocationTag)
                .WithMany(t => t.AcademicAllocations)
                .HasForeignKey(e => e.AllocationTagId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // AcademicAllocationUsers
        modelBuilder.Entity<AcademicAllocationUser>(entity =>
        {
            entity.ToTable("academic_allocation_users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.AcademicAllocationId).HasColumnName("academic_allocation_id");
            entity.Property(e => e.GroupAssignmentId).HasColumnName("group_assignment_id");
            entity.Property(e => e.Grade).HasColumnName("grade");
            entity.Property(e => e.WorkingHours).HasColumnName("working_hours").HasColumnType("numeric(5,2)");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.NewAfterEvaluation).HasColumnName("new_after_evaluation").HasDefaultValue(false);
            entity.Property(e => e.EvaluatedByResponsible).HasColumnName("evaluated_by_responsible").HasDefaultValue(false);
            entity.Property(e => e.Ignore).HasColumnName("ignore").HasDefaultValue(false);
            entity.Property(e => e.CommentsCount).HasColumnName("comments_count").HasDefaultValue(0);
            entity.Property(e => e.ScheduleEventFilesCount).HasColumnName("schedule_event_files_count").HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.User)
                .WithMany(u => u.AcademicAllocationUsers)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.AcademicAllocation)
                .WithMany(a => a.AcademicAllocationUsers)
                .HasForeignKey(e => e.AcademicAllocationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Exams
        modelBuilder.Entity<Exam>(entity =>
        {
            entity.ToTable("exams");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").IsRequired();
            entity.Property(e => e.Duration).HasColumnName("duration");
            entity.Property(e => e.StartHour).HasColumnName("start_hour");
            entity.Property(e => e.EndHour).HasColumnName("end_hour");
            entity.Property(e => e.RandomQuestions).HasColumnName("random_questions").HasDefaultValue(false);
            entity.Property(e => e.RaffleOrder).HasColumnName("raffle_order").HasDefaultValue(false);
            entity.Property(e => e.AutoCorrection).HasColumnName("auto_correction").HasDefaultValue(true);
            entity.Property(e => e.NumberQuestions).HasColumnName("number_questions");
            entity.Property(e => e.Attempts).HasColumnName("attempts").HasDefaultValue(1);
            entity.Property(e => e.AttemptsCorrection).HasColumnName("attempts_correction");
            entity.Property(e => e.BlockContent).HasColumnName("block_content").HasDefaultValue(false);
            entity.Property(e => e.Uninterrupted).HasColumnName("uninterrupted").HasDefaultValue(false);
            entity.Property(e => e.Controlled).HasColumnName("controlled").HasDefaultValue(false);
            entity.Property(e => e.ImmediateResultRelease).HasColumnName("immediate_result_release").HasDefaultValue(false);
            entity.Property(e => e.ResultRelease).HasColumnName("result_release");
            entity.Property(e => e.CanPublish).HasColumnName("can_publish").HasDefaultValue(true);
            entity.Property(e => e.ResultEmail).HasColumnName("result_email").HasDefaultValue(false);
            entity.Property(e => e.Status).HasColumnName("status").HasDefaultValue(true);
            entity.Property(e => e.ScheduleId).HasColumnName("schedule_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // Questions
        modelBuilder.Entity<Question>(entity =>
        {
            entity.ToTable("questions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Enunciation).HasColumnName("enunciation").IsRequired();
            entity.Property(e => e.TypeQuestion).HasColumnName("type_question");
            entity.Property(e => e.Status).HasColumnName("status").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // QuestionItems
        modelBuilder.Entity<QuestionItem>(entity =>
        {
            entity.ToTable("question_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.Description).HasColumnName("description").IsRequired();
            entity.Property(e => e.Value).HasColumnName("value").HasDefaultValue(false);
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.Question)
                .WithMany(q => q.QuestionItems)
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ExamQuestions
        modelBuilder.Entity<ExamQuestion>(entity =>
        {
            entity.ToTable("exam_questions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ExamId).HasColumnName("exam_id");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.Score).HasColumnName("score").HasDefaultValue(1.0);
            entity.Property(e => e.Order).HasColumnName("order");
            entity.Property(e => e.Annulled).HasColumnName("annulled").HasDefaultValue(false);
            entity.Property(e => e.UseQuestion).HasColumnName("use_question").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.Exam)
                .WithMany(ex => ex.ExamQuestions)
                .HasForeignKey(e => e.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Question)
                .WithMany(q => q.ExamQuestions)
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ExamUserAttempts
        modelBuilder.Entity<ExamUserAttempt>(entity =>
        {
            entity.ToTable("exam_user_attempts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AcademicAllocationUserId).HasColumnName("academic_allocation_user_id");
            entity.Property(e => e.Grade).HasColumnName("grade");
            entity.Property(e => e.Start).HasColumnName("start");
            entity.Property(e => e.End).HasColumnName("end");
            entity.Property(e => e.Complete).HasColumnName("complete").HasDefaultValue(false);
            entity.Property(e => e.TotalTime).HasColumnName("total_time").HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.AcademicAllocationUser)
                .WithMany()
                .HasForeignKey(e => e.AcademicAllocationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ExamResponses
        modelBuilder.Entity<ExamResponse>(entity =>
        {
            entity.ToTable("exam_responses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ExamUserAttemptId).HasColumnName("exam_user_attempt_id");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.Grade).HasColumnName("grade");
            entity.Property(e => e.Duration).HasColumnName("duration");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.ExamUserAttempt)
                .WithMany(a => a.Responses)
                .HasForeignKey(e => e.ExamUserAttemptId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ExamResponseQuestionItems
        modelBuilder.Entity<ExamResponseQuestionItem>(entity =>
        {
            entity.ToTable("exam_responses_question_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ExamResponseId).HasColumnName("exam_response_id");
            entity.Property(e => e.QuestionItemId).HasColumnName("question_item_id");
            entity.Property(e => e.Value).HasColumnName("value");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.ExamResponse)
                .WithMany(r => r.SelectedItems)
                .HasForeignKey(e => e.ExamResponseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Courses
        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("courses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(40);
            entity.Property(e => e.PassingGrade).HasColumnName("passing_grade");
            entity.Property(e => e.MinGradeToFinalExam).HasColumnName("min_grade_to_final_exam");
            entity.Property(e => e.MinFinalExamGrade).HasColumnName("min_final_exam_grade");
            entity.Property(e => e.FinalExamPassingGrade).HasColumnName("final_exam_passing_grade");
            entity.Property(e => e.MinHours).HasColumnName("min_hours");
            entity.Property(e => e.HasExamHeader).HasColumnName("has_exam_header").HasDefaultValue(false);
            entity.Property(e => e.HeaderExam).HasColumnName("header_exam");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // CurriculumUnitTypes
        modelBuilder.Entity<CurriculumUnitType>(entity =>
        {
            entity.ToTable("curriculum_unit_types");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(50).IsRequired();
            entity.Property(e => e.AllowsEnrollment).HasColumnName("allows_enrollment").HasDefaultValue(true);
            entity.Property(e => e.IconName).HasColumnName("icon_name").HasMaxLength(60);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // CurriculumUnits
        modelBuilder.Entity<CurriculumUnit>(entity =>
        {
            entity.ToTable("curriculum_units");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CurriculumUnitTypeId).HasColumnName("curriculum_unit_type_id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(40);
            entity.Property(e => e.Resume).HasColumnName("resume");
            entity.Property(e => e.Syllabus).HasColumnName("syllabus");
            entity.Property(e => e.Objectives).HasColumnName("objectives");
            entity.Property(e => e.Prerequisites).HasColumnName("prerequisites");
            entity.Property(e => e.Credits).HasColumnName("credits");
            entity.Property(e => e.WorkingHours).HasColumnName("working_hours");
            entity.Property(e => e.MinHours).HasColumnName("min_hours");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.CurriculumUnitType)
                .WithMany(t => t.CurriculumUnits)
                .HasForeignKey(e => e.CurriculumUnitTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Schedules
        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.ToTable("schedules");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // Semesters
        modelBuilder.Entity<Semester>(entity =>
        {
            entity.ToTable("semesters");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.OfferScheduleId).HasColumnName("offer_schedule_id");
            entity.Property(e => e.EnrollmentScheduleId).HasColumnName("enrollment_schedule_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.OfferSchedule)
                .WithMany()
                .HasForeignKey(e => e.OfferScheduleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.EnrollmentSchedule)
                .WithMany()
                .HasForeignKey(e => e.EnrollmentScheduleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Offers
        modelBuilder.Entity<Offer>(entity =>
        {
            entity.ToTable("offers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CurriculumUnitId).HasColumnName("curriculum_unit_id");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.SemesterId).HasColumnName("semester_id");
            entity.Property(e => e.OfferScheduleId).HasColumnName("offer_schedule_id");
            entity.Property(e => e.EnrollmentScheduleId).HasColumnName("enrollment_schedule_id");
            entity.Property(e => e.AllowPermanentChanges).HasColumnName("allow_permanent_changes").HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.CurriculumUnit)
                .WithMany(c => c.Offers)
                .HasForeignKey(e => e.CurriculumUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Course)
                .WithMany(c => c.Offers)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Semester)
                .WithMany(s => s.Offers)
                .HasForeignKey(e => e.SemesterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Groups
        modelBuilder.Entity<Group>(entity =>
        {
            entity.ToTable("groups");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OfferId).HasColumnName("offer_id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Location).HasColumnName("location");
            entity.Property(e => e.Status).HasColumnName("status").HasDefaultValue(true);
            entity.Property(e => e.Integrated).HasColumnName("integrated").HasDefaultValue(false);
            entity.Property(e => e.MainGroupId).HasColumnName("main_group_id");
            entity.Property(e => e.DigitalClassDirectoryId).HasColumnName("digital_class_directory_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.Offer)
                .WithMany(o => o.Groups)
                .HasForeignKey(e => e.OfferId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.MainGroup)
                .WithMany(g => g.SubGroups)
                .HasForeignKey(e => e.MainGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // RelatedTaggables
        modelBuilder.Entity<RelatedTaggable>(entity =>
        {
            entity.ToTable("related_taggables");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GroupStatus).HasColumnName("group_status");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.GroupAtId).HasColumnName("group_at_id");
            entity.Property(e => e.OfferId).HasColumnName("offer_id");
            entity.Property(e => e.OfferAtId).HasColumnName("offer_at_id");
            entity.Property(e => e.SemesterId).HasColumnName("semester_id");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.CourseAtId).HasColumnName("course_at_id");
            entity.Property(e => e.CurriculumUnitId).HasColumnName("curriculum_unit_id");
            entity.Property(e => e.CurriculumUnitAtId).HasColumnName("curriculum_unit_at_id");
            entity.Property(e => e.CurriculumUnitTypeId).HasColumnName("curriculum_unit_type_id");
            entity.Property(e => e.CurriculumUnitTypeAtId).HasColumnName("curriculum_unit_type_at_id");
            entity.Property(e => e.OfferScheduleId).HasColumnName("offer_schedule_id");
        });

        // Discussions
        modelBuilder.Entity<Discussion>(entity =>
        {
            entity.ToTable("discussions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ScheduleId).HasColumnName("schedule_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.Schedule)
                .WithMany()
                .HasForeignKey(e => e.ScheduleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // DiscussionPosts
        modelBuilder.Entity<DiscussionPost>(entity =>
        {
            entity.ToTable("discussion_posts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ProfileId).HasColumnName("profile_id");
            entity.Property(e => e.Content).HasColumnName("content").IsRequired();
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.Level).HasColumnName("level").HasDefaultValue(1);
            entity.Property(e => e.AcademicAllocationId).HasColumnName("academic_allocation_id");
            entity.Property(e => e.AcademicAllocationUserId).HasColumnName("academic_allocation_user_id");
            entity.Property(e => e.Draft).HasColumnName("draft").HasDefaultValue(false);
            entity.Property(e => e.ChildrenCount).HasColumnName("children_count").HasDefaultValue(0);
            entity.Property(e => e.ChildrenDraftsCount).HasColumnName("children_drafts_count").HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Profile)
                .WithMany()
                .HasForeignKey(e => e.ProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Parent)
                .WithMany(p => p.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DiscussionPostFiles
        modelBuilder.Entity<DiscussionPostFile>(entity =>
        {
            entity.ToTable("discussion_post_files");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DiscussionPostId).HasColumnName("discussion_post_id");
            entity.Property(e => e.AttachmentFileName).HasColumnName("attachment_file_name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.AttachmentContentType).HasColumnName("attachment_content_type").HasMaxLength(255);
            entity.Property(e => e.AttachmentFileSize).HasColumnName("attachment_file_size");
            entity.Property(e => e.AttachmentUpdatedAt).HasColumnName("attachment_updated_at");

            entity.HasOne(e => e.DiscussionPost)
                .WithMany(p => p.Files)
                .HasForeignKey(e => e.DiscussionPostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LessonModules
        modelBuilder.Entity<LessonModule>(entity =>
        {
            entity.ToTable("lesson_modules");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
            entity.Property(e => e.Order).HasColumnName("order");
            entity.Property(e => e.IsDefault).HasColumnName("is_default").HasDefaultValue(false);
        });

        // Lessons
        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.ToTable("lessons");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ScheduleId).HasColumnName("schedule_id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Address).HasColumnName("address").IsRequired();
            entity.Property(e => e.TypeLesson).HasColumnName("type_lesson");
            entity.Property(e => e.Privacy).HasColumnName("privacy").HasDefaultValue(false);
            entity.Property(e => e.Order).HasColumnName("order");
            entity.Property(e => e.Status).HasColumnName("status").HasDefaultValue(0);
            entity.Property(e => e.LessonModuleId).HasColumnName("lesson_module_id");
            entity.Property(e => e.ImportedFromId).HasColumnName("imported_from_id");
            entity.Property(e => e.ReceiveUpdates).HasColumnName("receive_updates").HasDefaultValue(false);

            entity.HasOne(e => e.LessonModule)
                .WithMany(m => m.Lessons)
                .HasForeignKey(e => e.LessonModuleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // LessonUsers
        modelBuilder.Entity<LessonUser>(entity =>
        {
            entity.ToTable("lesson_users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LessonId).HasColumnName("lesson_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Visualized).HasColumnName("visualized").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.Lesson)
                .WithMany(l => l.LessonViews)
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LessonNotes
        modelBuilder.Entity<LessonNote>(entity =>
        {
            entity.ToTable("lesson_notes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(150);
            entity.Property(e => e.Description).HasColumnName("description").IsRequired();
            entity.Property(e => e.LessonId).HasColumnName("lesson_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.Lesson)
                .WithMany(l => l.Notes)
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Assignments
        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.ToTable("assignments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ScheduleId).HasColumnName("schedule_id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(1024).IsRequired();
            entity.Property(e => e.Enunciation).HasColumnName("enunciation");
            entity.Property(e => e.TypeAssignment).HasColumnName("type_assignment").HasDefaultValue(0);
            entity.Property(e => e.StartHour).HasColumnName("start_hour");
            entity.Property(e => e.EndHour).HasColumnName("end_hour");
            entity.Property(e => e.Controlled).HasColumnName("controlled").HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // AssignmentFiles
        modelBuilder.Entity<AssignmentFile>(entity =>
        {
            entity.ToTable("assignment_files");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AcademicAllocationUserId).HasColumnName("academic_allocation_user_id");
            entity.Property(e => e.AttachmentFileName).HasColumnName("attachment_file_name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.AttachmentContentType).HasColumnName("attachment_content_type").HasMaxLength(255);
            entity.Property(e => e.AttachmentFileSize).HasColumnName("attachment_file_size");
            entity.Property(e => e.AttachmentUpdatedAt).HasColumnName("attachment_updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.NoteEditedAt).HasColumnName("note_edited_at");
        });

        // GroupAssignments
        modelBuilder.Entity<GroupAssignment>(entity =>
        {
            entity.ToTable("group_assignments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GroupName).HasColumnName("group_name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.GroupUpdatedAt).HasColumnName("group_updated_at");
            entity.Property(e => e.AcademicAllocationId).HasColumnName("academic_allocation_id");
        });

        // GroupParticipants
        modelBuilder.Entity<GroupParticipant>(entity =>
        {
            entity.ToTable("group_participants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GroupAssignmentId).HasColumnName("group_assignment_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ParticipantUpdatedAt).HasColumnName("participant_updated_at");

            entity.HasOne(e => e.GroupAssignment)
                .WithMany(g => g.Participants)
                .HasForeignKey(e => e.GroupAssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SubmissionComments
        modelBuilder.Entity<SubmissionComment>(entity =>
        {
            entity.ToTable("comments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AcademicAllocationUserId).HasColumnName("academic_allocation_user_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Description).HasColumnName("description").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // Notifications
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // ReadNotifications
        modelBuilder.Entity<ReadNotification>(entity =>
        {
            entity.ToTable("read_notifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.Notification)
                .WithMany(n => n.ReadByUsers)
                .HasForeignKey(e => e.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // InternalMessages
        modelBuilder.Entity<InternalMessage>(entity =>
        {
            entity.ToTable("messages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Ignore(e => e.UserId);
            entity.Ignore(e => e.Sender);
            entity.Property(e => e.Subject).HasColumnName("subject").HasMaxLength(255);
            entity.Property(e => e.Body).HasColumnName("content").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // UserInternalMessages
        modelBuilder.Entity<UserInternalMessage>(entity =>
        {
            entity.ToTable("user_messages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.MessageId).HasColumnName("message_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.Ignore(e => e.Read);
            entity.Ignore(e => e.Trash);
            entity.Ignore(e => e.Folder);

            entity.HasOne(e => e.Message)
                .WithMany(m => m.UserMessages)
                .HasForeignKey(e => e.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SupportMaterialFiles
        modelBuilder.Entity<SupportMaterialFile>(entity =>
        {
            entity.ToTable("support_material_files");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AttachmentFileName).HasColumnName("attachment_file_name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.AttachmentContentType).HasColumnName("attachment_content_type").HasMaxLength(255);
            entity.Property(e => e.AttachmentFileSize).HasColumnName("attachment_file_size");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // Bibliographies
        modelBuilder.Entity<Bibliography>(entity =>
        {
            entity.ToTable("bibliographies");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Author).HasColumnName("author").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Publisher).HasColumnName("publisher").HasMaxLength(255);
            entity.Property(e => e.Year).HasColumnName("year").HasMaxLength(10);
            entity.Property(e => e.Url).HasColumnName("url");
            entity.Property(e => e.TypeBibliography).HasColumnName("type_bibliography").HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // ScheduleEvents
        modelBuilder.Entity<ScheduleEvent>(entity =>
        {
            entity.ToTable("schedule_events");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ScheduleId).HasColumnName("schedule_id");
            entity.Property(e => e.Location).HasColumnName("location");
            entity.Property(e => e.TypeEvent).HasColumnName("type_event").HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.Schedule)
                .WithMany()
                .HasForeignKey(e => e.ScheduleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserBlacklist
        modelBuilder.Entity<UserBlacklist>(entity =>
        {
            entity.ToTable("user_blacklist");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Cpf).HasColumnName("cpf").HasMaxLength(11);
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.Active).HasColumnName("active").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
