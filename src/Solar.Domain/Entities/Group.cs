namespace Solar.Domain.Entities;

public class Group
{
    public long Id { get; set; }
    public long OfferId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Location { get; set; }
    public bool Status { get; set; } = true;
    public bool Integrated { get; set; }
    public long? MainGroupId { get; set; } // Aglutinação de turmas
    public int? DigitalClassDirectoryId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Offer? Offer { get; set; }
    public Group? MainGroup { get; set; }
    public ICollection<Group> SubGroups { get; set; } = [];
}
