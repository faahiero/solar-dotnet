namespace Solar.Domain.Entities;

public class Assignment
{
    public long Id { get; set; }
    public long? ScheduleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Enunciation { get; set; }
    public int TypeAssignment { get; set; } // 0 = Individual, 1 = Grupo
    public string? StartHour { get; set; }
    public string? EndHour { get; set; }
    public bool Controlled { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Schedule? Schedule { get; set; }
}

public class AssignmentFile
{
    public long Id { get; set; }
    public long AcademicAllocationUserId { get; set; }
    public string AttachmentFileName { get; set; } = string.Empty;
    public string? AttachmentContentType { get; set; }
    public long? AttachmentFileSize { get; set; }
    public DateTime? AttachmentUpdatedAt { get; set; }
    public long? UserId { get; set; }
    public string? Note { get; set; }
    public DateTime? NoteEditedAt { get; set; }

    // Navigation
    public AcademicAllocationUser? AcademicAllocationUser { get; set; }
    public User? User { get; set; }
}

public class GroupAssignment
{
    public long Id { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public DateTime? GroupUpdatedAt { get; set; }
    public long? AcademicAllocationId { get; set; }

    // Navigation
    public ICollection<GroupParticipant> Participants { get; set; } = [];
}

public class GroupParticipant
{
    public long Id { get; set; }
    public long GroupAssignmentId { get; set; }
    public long UserId { get; set; }
    public DateTime? ParticipantUpdatedAt { get; set; }

    // Navigation
    public GroupAssignment? GroupAssignment { get; set; }
    public User? User { get; set; }
}

public class SubmissionComment
{
    public long Id { get; set; }
    public long AcademicAllocationUserId { get; set; }
    public long UserId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public AcademicAllocationUser? AcademicAllocationUser { get; set; }
    public User? User { get; set; }
}
