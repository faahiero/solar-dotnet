namespace Solar.Domain.Entities;

public class LessonModule
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? Order { get; set; }
    public bool IsDefault { get; set; }

    // Navigation
    public ICollection<Lesson> Lessons { get; set; } = [];
}

public class Lesson
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public long? ScheduleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Address { get; set; } = string.Empty;
    public int TypeLesson { get; set; } // 0 = ZIP/Package, 1 = Link
    public bool Privacy { get; set; }
    public int Order { get; set; }
    public int Status { get; set; }
    public long? LessonModuleId { get; set; }
    public long? ImportedFromId { get; set; }
    public bool ReceiveUpdates { get; set; }

    // Navigation
    public User? User { get; set; }
    public Schedule? Schedule { get; set; }
    public LessonModule? LessonModule { get; set; }
    public ICollection<LessonUser> LessonViews { get; set; } = [];
    public ICollection<LessonNote> Notes { get; set; } = [];
}

public class LessonUser
{
    public long Id { get; set; }
    public long LessonId { get; set; }
    public long UserId { get; set; }
    public bool Visualized { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Lesson? Lesson { get; set; }
    public User? User { get; set; }
}

public class LessonNote
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public long LessonId { get; set; }
    public long UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Lesson? Lesson { get; set; }
    public User? User { get; set; }
}
