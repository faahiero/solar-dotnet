namespace Solar.Domain.Entities;

public class Discussion
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long ScheduleId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Schedule? Schedule { get; set; }
    public ICollection<DiscussionPost> Posts { get; set; } = [];
}
