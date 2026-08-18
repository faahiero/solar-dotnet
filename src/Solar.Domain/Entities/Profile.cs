using Solar.Domain.Enums;

namespace Solar.Domain.Entities;

public class Profile
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProfileType Types { get; set; } = ProfileType.NoType;
    public bool Status { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Allocation> Allocations { get; set; } = [];
}
