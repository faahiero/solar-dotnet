using Solar.Domain.Enums;

namespace Solar.Domain.Entities;

public class User
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public string Nick { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public DateOnly? Birthdate { get; set; }
    public string? EnrollmentCode { get; set; }

    public string? Email { get; set; }
    public string EncryptedPassword { get; set; } = string.Empty;
    public string? PasswordSalt { get; set; }
    public string? ResetPasswordToken { get; set; }
    public DateTime? ResetPasswordSentAt { get; set; }
    public string? AuthenticationToken { get; set; }
    public string? SessionToken { get; set; }

    // Devise Trackable
    public int SignInCount { get; set; }
    public DateTime? CurrentSignInAt { get; set; }
    public DateTime? LastSignInAt { get; set; }
    public string? CurrentSignInIp { get; set; }
    public string? LastSignInIp { get; set; }

    public string? Cpf { get; set; }
    public bool? Gender { get; set; }
    public string? Telephone { get; set; }
    public string? CellPhone { get; set; }
    public string? Institution { get; set; }
    public string? SpecialNeeds { get; set; }

    public string? Address { get; set; }
    public string? AddressNumber { get; set; }
    public string? AddressComplement { get; set; }
    public string? AddressNeighborhood { get; set; }
    public string? Zipcode { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }

    public string? Bio { get; set; }
    public string? Interests { get; set; }
    public string? Site { get; set; }

    public bool Active { get; set; } = true;
    public bool Registered { get; set; }
    public bool Integrated { get; set; }
    public bool Selfregistration { get; set; }
    public int? DigitalClassUserId { get; set; }
    public int? OauthApplicationId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Allocation> Allocations { get; set; } = [];
    public ICollection<AcademicAllocationUser> AcademicAllocationUsers { get; set; } = [];
}
