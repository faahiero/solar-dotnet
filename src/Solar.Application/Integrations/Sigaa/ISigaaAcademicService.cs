namespace Solar.Application.Integrations.Sigaa;

public record SigaaUserRecord
{
    public string Cpf { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? EnrollmentCode { get; init; }
    public DateOnly? Birthdate { get; init; }
    public string? Institution { get; init; }
}

public interface ISigaaAcademicService
{
    Task<SigaaUserRecord?> FindUserByCpfAsync(string cpf, CancellationToken cancellationToken = default);
    Task<SigaaUserRecord?> FindUserByLoginAsync(string login, CancellationToken cancellationToken = default);
}
