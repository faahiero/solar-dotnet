namespace Solar.Domain.Entities;

public class UserBlacklist
{
    public long Id { get; set; }
    public string Cpf { get; set; } = string.Empty;
    public long? UserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual User? User { get; set; }
}
