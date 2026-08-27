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
    public int? Status { get; set; } // 0 = Inbox Unread, 1 = Read, 3 = Sent, 7 = Trash
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool Read => Status == 1 || Status == 3;
    public bool Trash => Status == 7;
    public int Folder => Status == 3 ? 1 : Status == 7 ? 2 : 0;

    // Navigation
    public InternalMessage? Message { get; set; }
    public User? User { get; set; }
}

public class SupportMaterialFile
{
    public long Id { get; set; }
    public string? AttachmentFileName { get; set; }
    public string? AttachmentContentType { get; set; }
    public int? AttachmentFileSize { get; set; }
    public DateTime? AttachmentUpdatedAt { get; set; }
    public string? Folder { get; set; }
    public string? Url { get; set; }
    public int MaterialType { get; set; } // 0 = Apoio, etc.
    public string? Title { get; set; }
    public int? Order { get; set; }
}

public class Bibliography
{
    public long Id { get; set; }
    public int TypeBibliography { get; set; } // 1 = Livro, 2 = Periódico, 3 = Artigo, etc.
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Address { get; set; }
    public string? Publisher { get; set; }
    public int? CountPages { get; set; }
    public string? Pages { get; set; }
    public int? Volume { get; set; }
    public int? Edition { get; set; }
    public int? PublicationYear { get; set; }
    public string? Periodicity { get; set; }
    public string? Issn { get; set; }
    public string? Isbn { get; set; }
    public string? Url { get; set; }
    public DateOnly? AccessedIn { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? AttachmentFileName { get; set; }
    public string? AttachmentContentType { get; set; }
    public long? AttachmentFileSize { get; set; }
    public DateTime? AttachmentUpdatedAt { get; set; }

    // Navigation
    public ICollection<Author> Authors { get; set; } = [];
}

public class Author
{
    public long Id { get; set; }
    public long BibliographyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Bibliography? Bibliography { get; set; }
}

public class ScheduleEvent
{
    public long Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public long? ScheduleId { get; set; }
    public int TypeEvent { get; set; } = 2; // 0 = Aula, 1 = Encontro Presencial, 2 = Recesso
    public string? StartHour { get; set; }
    public string? EndHour { get; set; }
    public string? Place { get; set; }
    public bool? Integrated { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? ContentExam { get; set; }

    // Navigation
    public Schedule? Schedule { get; set; }
}
