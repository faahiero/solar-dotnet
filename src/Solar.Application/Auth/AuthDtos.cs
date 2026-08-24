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
    public string? Email { get; init; }
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

public record RegisterUserRequest
{
    // Step 1: Dados Pessoais
    public string Name { get; init; } = string.Empty;
    public string Cpf { get; init; } = string.Empty;
    public DateOnly? Birthdate { get; init; }
    public bool? Gender { get; init; }
    public bool HasSpecialNeeds { get; init; }
    public string? SpecialNeeds { get; init; }

    // Step 2: Dados de Acesso
    public string? Nick { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string PasswordConfirmation { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string EmailConfirmation { get; init; } = string.Empty;
    public string? AlternateEmail { get; init; }

    // Step 3: Dados de Contato
    public string? Address { get; init; }
    public string? AddressNumber { get; init; }
    public string? AddressComplement { get; init; }
    public string? AddressNeighborhood { get; init; }
    public string? Zipcode { get; init; }
    public string? State { get; init; }
    public string? City { get; init; }
    public string? Telephone { get; init; }
    public string? CellPhone { get; init; }

    // Step 4: Outras Informações
    public string? Institution { get; init; }
    public string? RemoteIp { get; init; }
}

public record RegisterUserResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public UserProfileDto? User { get; init; }
    public string? Token { get; init; }
}

public record ImportSigaaUserRequest
{
    public string Cpf { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string PasswordConfirmation { get; init; } = string.Empty;
    public string? RemoteIp { get; init; }
}
