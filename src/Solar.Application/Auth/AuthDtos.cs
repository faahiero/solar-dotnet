namespace Solar.Application.Auth;

public record LoginRequest
{
    public string Login { get; init; } = string.Empty; // Aceita tanto Username quanto CPF
    public string Password { get; init; } = string.Empty;
    public string? RemoteIp { get; init; }
}

public record LoginResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? Token { get; init; }
    public UserProfileDto? User { get; init; }
    public bool PasswordUpgraded { get; init; }
}

public record UserProfileDto
{
    public long Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Cpf { get; init; }
    public int ProfileTypes { get; init; }
}

public record VerifyCpfRequest
{
    public string Cpf { get; init; } = string.Empty;
}

public record VerifyCpfResponse
{
    public bool ExistsInLocal { get; init; }
    public bool ExistsInSigaa { get; init; }
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string Message { get; init; } = string.Empty;
}
