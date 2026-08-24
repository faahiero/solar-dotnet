using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Solar.Domain.Entities;

namespace Solar.Application.Auth;

public class RegisterUserUseCase
{
    private readonly ISolarAuthDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly PasswordPolicyService _passwordPolicy;

    public RegisterUserUseCase(
        ISolarAuthDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        PasswordPolicyService passwordPolicy)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _passwordPolicy = passwordPolicy ?? throw new ArgumentNullException(nameof(passwordPolicy));
    }

    public async Task<RegisterUserResponse> ExecuteAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Validações básicas obrigatórias
        if (string.IsNullOrWhiteSpace(request.Name))
            return new RegisterUserResponse { Success = false, Message = "O nome completo é obrigatório." };

        if (string.IsNullOrWhiteSpace(request.Cpf))
            return new RegisterUserResponse { Success = false, Message = "O CPF é obrigatório." };

        if (string.IsNullOrWhiteSpace(request.Username))
            return new RegisterUserResponse { Success = false, Message = "O nome de usuário (login) é obrigatório." };

        if (string.IsNullOrWhiteSpace(request.Email))
            return new RegisterUserResponse { Success = false, Message = "O e-mail principal é obrigatório." };

        if (string.IsNullOrWhiteSpace(request.Password))
            return new RegisterUserResponse { Success = false, Message = "A senha é obrigatória." };

        if (!string.Equals(request.Password, request.PasswordConfirmation))
            return new RegisterUserResponse { Success = false, Message = "A confirmação de senha não confere." };

        if (!string.IsNullOrEmpty(request.EmailConfirmation) && !string.Equals(request.Email.Trim(), request.EmailConfirmation.Trim(), StringComparison.OrdinalIgnoreCase))
            return new RegisterUserResponse { Success = false, Message = "A confirmação de e-mail não confere." };

        var sanitizedCpf = request.Cpf.Replace(".", "").Replace("-", "").Trim();
        var sanitizedUsername = request.Username.Trim().ToLowerInvariant();
        var sanitizedEmail = request.Email.Trim().ToLowerInvariant();

        // 2. Validação de Política e Força da Senha (NIST SP 800-63B)
        var policyResult = _passwordPolicy.ValidatePassword(request.Password, sanitizedUsername, sanitizedCpf);
        if (!policyResult.IsValid)
        {
            return new RegisterUserResponse
            {
                Success = false,
                Message = policyResult.Errors.FirstOrDefault() ?? "A senha fornecida não atende aos requisitos de segurança."
            };
        }

        // 3. Verifica duplicidades no banco de dados
        var existingCpf = await _dbContext.Users.AnyAsync(u => u.Cpf == sanitizedCpf || u.Cpf == request.Cpf, cancellationToken);
        if (existingCpf)
            return new RegisterUserResponse { Success = false, Message = "Este CPF já possui cadastro no Solar LMS." };

        var existingUser = await _dbContext.Users.AnyAsync(u => u.Username.ToLower() == sanitizedUsername, cancellationToken);
        if (existingUser)
            return new RegisterUserResponse { Success = false, Message = "Este nome de usuário já está em uso por outra conta." };

        var existingEmail = await _dbContext.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == sanitizedEmail, cancellationToken);
        if (existingEmail)
            return new RegisterUserResponse { Success = false, Message = "Este endereço de e-mail já está cadastrado no sistema." };

        // 4. Criação da entidade User
        var user = new User
        {
            Name = request.Name.Trim(),
            Nick = string.IsNullOrWhiteSpace(request.Nick) ? request.Name.Trim().Split(' ')[0] : request.Nick.Trim(),
            Username = sanitizedUsername,
            Email = sanitizedEmail,
            Cpf = sanitizedCpf,
            Birthdate = request.Birthdate,
            Gender = request.Gender,
            SpecialNeeds = request.HasSpecialNeeds ? request.SpecialNeeds : null,
            Address = request.Address,
            AddressNumber = request.AddressNumber,
            AddressComplement = request.AddressComplement,
            AddressNeighborhood = request.AddressNeighborhood,
            Zipcode = request.Zipcode,
            State = request.State,
            City = request.City,
            Telephone = request.Telephone,
            CellPhone = request.CellPhone,
            Institution = request.Institution,
            Active = true,
            Registered = true,
            Selfregistration = true,
            Integrated = false,
            TermsAcceptedAt = request.AcceptTerms ? DateTime.UtcNow : null,
            TermsAcceptedIp = request.RemoteIp ?? "127.0.0.1",
            TermsVersion = request.TermsVersion ?? "v2.0_2026",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.EncryptedPassword = _passwordHasher.HashPassword(user, request.Password);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RegisterUserResponse
        {
            Success = true,
            Message = "Cadastro realizado com sucesso! Bem-vindo(a) ao Solar LMS.",
            User = new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Name = user.Name ?? user.Username,
                Email = user.Email ?? string.Empty,
                Cpf = user.Cpf,
                ProfileTypes = 1 // Estudante padrão
            }
        };
    }

    public async Task<RegisterUserResponse> ImportFromSigaaAsync(ImportSigaaUserRequest request, string name, string email, string? enrollmentCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Cpf))
            return new RegisterUserResponse { Success = false, Message = "O CPF é obrigatório." };

        if (string.IsNullOrWhiteSpace(request.Password))
            return new RegisterUserResponse { Success = false, Message = "A senha de acesso é obrigatória." };

        if (!string.Equals(request.Password, request.PasswordConfirmation))
            return new RegisterUserResponse { Success = false, Message = "A confirmação de senha não confere." };

        var sanitizedCpf = request.Cpf.Replace(".", "").Replace("-", "").Trim();

        // Validação de Política de Senha
        var policyResult = _passwordPolicy.ValidatePassword(request.Password, sanitizedCpf, sanitizedCpf);
        if (!policyResult.IsValid)
        {
            return new RegisterUserResponse
            {
                Success = false,
                Message = policyResult.Errors.FirstOrDefault() ?? "A senha fornecida não atende aos requisitos de segurança."
            };
        }

        // Verifica se já existe localmente
        var localUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Cpf == sanitizedCpf || u.Cpf == request.Cpf, cancellationToken);
        if (localUser != null)
            return new RegisterUserResponse { Success = false, Message = "Usuário já cadastrado no Solar LMS. Prossiga para o Login." };

        var username = sanitizedCpf;

        // Se o username já estiver em uso, gera um sufixo
        if (await _dbContext.Users.AnyAsync(u => u.Username.ToLower() == username, cancellationToken))
        {
            username = $"sigaa_{sanitizedCpf.Substring(Math.Max(0, sanitizedCpf.Length - 4))}";
        }

        var user = new User
        {
            Name = name,
            Nick = name.Split(' ')[0],
            Username = username,
            Email = email.ToLowerInvariant(),
            Cpf = sanitizedCpf,
            EnrollmentCode = enrollmentCode,
            Active = true,
            Registered = true,
            Selfregistration = true,
            Integrated = true,
            TermsAcceptedAt = request.AcceptTerms ? DateTime.UtcNow : null,
            TermsAcceptedIp = request.RemoteIp ?? "127.0.0.1",
            TermsVersion = request.TermsVersion ?? "v2.0_2026",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.EncryptedPassword = _passwordHasher.HashPassword(user, request.Password);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RegisterUserResponse
        {
            Success = true,
            Message = "Conta ativada e sincronizada com sucesso a partir dos dados do SIGAA!",
            User = new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Name = user.Name ?? user.Username,
                Email = user.Email ?? string.Empty,
                Cpf = user.Cpf,
                ProfileTypes = 1
            }
        };
    }
}
