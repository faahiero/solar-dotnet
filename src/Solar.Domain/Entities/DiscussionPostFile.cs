namespace Solar.Domain.Entities;

public class DiscussionPostFile
{
    public long Id { get; set; }
    public long DiscussionPostId { get; set; }
    public string AttachmentFileName { get; set; } = string.Empty;
    public string? AttachmentContentType { get; set; }
    public long? AttachmentFileSize { get; set; }
    public DateTime? AttachmentUpdatedAt { get; set; }

    // Navigation
    public DiscussionPost? DiscussionPost { get; set; }
}
