namespace Solar.Domain.Entities;

public class DiscussionPost
{
    public const int MaxIndentLevel = 7;

    public long Id { get; set; }
    public long UserId { get; set; }
    public int ProfileId { get; set; }
    public string Content { get; set; } = string.Empty;
    public long? ParentId { get; set; }
    public int Level { get; set; } = 1;
    public long? AcademicAllocationId { get; set; }
    public long? AcademicAllocationUserId { get; set; }
    public bool Draft { get; set; }
    public int ChildrenCount { get; set; }
    public int ChildrenDraftsCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
    public Profile? Profile { get; set; }
    public DiscussionPost? Parent { get; set; }
    public ICollection<DiscussionPost> Children { get; set; } = [];
    public ICollection<DiscussionPostFile> Files { get; set; } = [];
}
