namespace Solar.Domain.Entities;

public class Notification
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
    public ICollection<ReadNotification> ReadByUsers { get; set; } = [];
}

public class ReadNotification
{
    public long Id { get; set; }
    public long NotificationId { get; set; }
    public long UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Notification? Notification { get; set; }
    public User? User { get; set; }
}

public class InternalMessage
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? Sender { get; set; }
    public ICollection<UserInternalMessage> UserMessages { get; set; } = [];
}

public class UserInternalMessage
{
    public long Id { get; set; }
    public long MessageId { get; set; }
    public long UserId { get; set; }
    public bool Read { get; set; }
    public bool Trash { get; set; }
    public bool Deleted { get; set; }
    public int Folder { get; set; } // 0 = Inbox, 1 = Sent, 2 = Trash

    // Navigation
    public InternalMessage? Message { get; set; }
    public User? User { get; set; }
}

public class SupportMaterialFile
{
    public long Id { get; set; }
    public string AttachmentFileName { get; set; } = string.Empty;
    public string? AttachmentContentType { get; set; }
    public long? AttachmentFileSize { get; set; }
    public string? Description { get; set; }
    public long? UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
}

public class Bibliography
{
    public long Id { get; set; }
    public string Author { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Publisher { get; set; }
    public string? Year { get; set; }
    public string? Url { get; set; }
    public int TypeBibliography { get; set; } // 0 = Básica, 1 = Complementar
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ScheduleEvent
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long ScheduleId { get; set; }
    public string? Location { get; set; }
    public int TypeEvent { get; set; } // 0 = Aula, 1 = Encontro Presencial, 2 = Recesso

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Schedule? Schedule { get; set; }
}
