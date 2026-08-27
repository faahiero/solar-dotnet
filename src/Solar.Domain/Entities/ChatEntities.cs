namespace Solar.Domain.Entities;

public class ChatRoom
{
    public long Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public long? ScheduleId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Schedule? Schedule { get; set; }
    public ICollection<ChatMessage> Messages { get; set; } = [];
    public ICollection<ChatParticipant> Participants { get; set; } = [];
}

public class ChatMessage
{
    public long Id { get; set; }
    public long ChatRoomId { get; set; }
    public long UserId { get; set; }
    public long? AllocationId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? UserName { get; set; }
    public string? UserNick { get; set; }

    // Navigation
    public ChatRoom? ChatRoom { get; set; }
    public User? User { get; set; }
    public Allocation? Allocation { get; set; }
}

public class ChatParticipant
{
    public long Id { get; set; }
    public long ChatRoomId { get; set; }
    public long UserId { get; set; }
    public int Status { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ChatRoom? ChatRoom { get; set; }
    public User? User { get; set; }
}
