namespace Solar.Domain.Entities;

public class LegalDocumentVersion
{
    public long Id { get; set; }
    public int Kind { get; set; }
    public int? VersionNumber { get; set; }
    public string? Content { get; set; }
    public string? ChangeSummary { get; set; }
    public int Status { get; set; } = 0;
    public DateTime? EffectiveAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public long? PublishedById { get; set; }
    public long? CreatedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? PublishedBy { get; set; }
    public User? CreatedBy { get; set; }
    public ICollection<UserTermAcceptance> Acceptances { get; set; } = [];
}

public class UserTermAcceptance
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long LegalDocumentVersionId { get; set; }
    public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
    public LegalDocumentVersion? LegalDocumentVersion { get; set; }
}
