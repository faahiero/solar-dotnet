namespace Solar.Application.Integrations.Sigaa;

/// <summary>
/// Representa os dados cadastrais e acadêmicos retornados pelo WebService SOAP do SIGAA / SI3 da UFC.
/// Espelha o array de atributos de app/models/user.rb:811 (User.user_ma_attributes).
/// </summary>
public record SigaaUserRecord
{
    public string Cpf { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? EnrollmentCode { get; init; }
    public DateOnly? Birthdate { get; init; }
    public string? Gender { get; init; }
    public string? Address { get; init; }
    public string? AddressNumber { get; init; }
    public string? Neighborhood { get; init; }
    public string? Zipcode { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Country { get; init; }
    public string? CellPhone { get; init; }
    public string? SpecialNeeds { get; init; }
    public string? Institution { get; init; } = "Universidade Federal do Ceará (UFC)";
    public bool Integrated { get; init; } = true;
}

public interface ISigaaAcademicService
{
    Task<SigaaUserRecord?> FindUserByCpfAsync(string cpf, CancellationToken cancellationToken = default);
    Task<SigaaUserRecord?> FindUserByLoginAsync(string login, CancellationToken cancellationToken = default);
    Task<List<string>> ValidateUserAsync(string cpf, string email, string login, CancellationToken cancellationToken = default);
}
