namespace Solar.Domain.Entities;

public class PersonalConfiguration
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string? Theme { get; set; } = "blue";
    public string? DefaultLocale { get; set; } = "pt_BR";
    public bool Message { get; set; } = true;
    public bool Exam { get; set; } = true;
    public bool Post { get; set; } = true;
    public bool AcademicTool { get; set; } = true;

    // Navigation
    public User? User { get; set; }
}

public class PublicFile
{
    public long Id { get; set; }
    public long AllocationTagId { get; set; }
    public long UserId { get; set; }
    public string AttachmentFileName { get; set; } = string.Empty;
    public string? AttachmentContentType { get; set; }
    public int? AttachmentFileSize { get; set; }
    public DateTime? AttachmentUpdatedAt { get; set; }
    public bool IsPrivate { get; set; } = false;

    // Navigation
    public AllocationTag? AllocationTag { get; set; }
    public User? User { get; set; }
}

public class QuestionLabel
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<QuestionLabelQuestion> QuestionLabelQuestions { get; set; } = [];
}

public class QuestionLabelQuestion
{
    public long QuestionId { get; set; }
    public long QuestionLabelId { get; set; }

    // Navigation
    public Question? Question { get; set; }
    public QuestionLabel? QuestionLabel { get; set; }
}

public class AssignmentEnunciationFile
{
    public long Id { get; set; }
    public long? AssignmentId { get; set; }
    public string AttachmentFileName { get; set; } = string.Empty;
    public string? AttachmentContentType { get; set; }
    public int? AttachmentFileSize { get; set; }
    public DateTime? AttachmentUpdatedAt { get; set; }

    // Navigation
    public Assignment? Assignment { get; set; }
}

public class DiscussionEnunciationFile
{
    public long Id { get; set; }
    public long? DiscussionId { get; set; }
    public string? AttachmentFileName { get; set; }
    public string? AttachmentContentType { get; set; }
    public long? AttachmentFileSize { get; set; }
    public DateTime? AttachmentUpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Discussion? Discussion { get; set; }
}

public class ScheduleEventFile
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public long? AcademicAllocationUserId { get; set; }
    public string? AttachmentFileName { get; set; }
    public string? AttachmentContentType { get; set; }
    public int? AttachmentFileSize { get; set; }
    public string? FileCorrection { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
    public AcademicAllocationUser? AcademicAllocationUser { get; set; }
}

public class Comment
{
    public long Id { get; set; }
    public long AcademicAllocationUserId { get; set; }
    public long UserId { get; set; }
    public string? Content { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RemovedAt { get; set; }

    // Navigation
    public AcademicAllocationUser? AcademicAllocationUser { get; set; }
    public User? User { get; set; }
    public ICollection<CommentFile> Files { get; set; } = [];
}

public class CommentFile
{
    public long Id { get; set; }
    public long CommentId { get; set; }
    public string AttachmentFileName { get; set; } = string.Empty;
    public string? AttachmentContentType { get; set; }
    public int? AttachmentFileSize { get; set; }
    public DateTime? AttachmentUpdatedAt { get; set; }

    // Navigation
    public Comment? Comment { get; set; }
}
