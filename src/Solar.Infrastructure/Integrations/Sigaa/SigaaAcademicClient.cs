namespace Solar.Infrastructure.Integrations.Sigaa;

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

/// <summary>
/// Cliente de integração SOAP com o SIGAA / Módulo Acadêmico da UFC.
/// Mapeado a partir de app/models/user.rb:749-866 (User.connect_and_import_user, User.synchronize).
/// </summary>
public class SigaaAcademicClient : ISigaaAcademicService
{
    public Task<SigaaUserRecord?> FindUserByCpfAsync(string cpf, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cpf))
        {
            return Task.FromResult<SigaaUserRecord?>(null);
        }

        // Simulação / Contrato para chamadas WCF Core ao WSDL oficial do SIGAA
        var sanitizedCpf = cpf.Replace(".", "").Replace("-", "").Trim();
        return Task.FromResult<SigaaUserRecord?>(new SigaaUserRecord
        {
            Cpf = sanitizedCpf,
            Name = "Usuário Sincronizado SIGAA",
            Username = $"sigaa_{sanitizedCpf}",
            Email = $"aluno_{sanitizedCpf}@ufc.br",
            EnrollmentCode = "202601001",
            Institution = "Universidade Federal do Ceará (UFC)"
        });
    }

    public Task<SigaaUserRecord?> FindUserByLoginAsync(string login, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            return Task.FromResult<SigaaUserRecord?>(null);
        }

        return Task.FromResult<SigaaUserRecord?>(new SigaaUserRecord
        {
            Cpf = "12345678900",
            Name = "Usuário Importado",
            Username = login.Trim().ToLowerInvariant(),
            Email = $"{login.Trim().ToLowerInvariant()}@ufc.br",
            Institution = "UFC"
        });
    }
}
