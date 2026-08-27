using FluentValidation;
using Solar.WebApi.Endpoints;

namespace Solar.WebApi.Validators;

public class CreateGroupRequestValidator : AbstractValidator<CreateGroupRequest>
{
    public CreateGroupRequestValidator()
    {
        RuleFor(x => x.OfferId)
            .GreaterThan(0)
            .WithMessage("O ID da oferta (OfferId) deve ser maior que zero.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .When(x => x.Name != null)
            .WithMessage("O nome da turma não pode estar vazio quando informado.");
    }
}

public class CreateSemesterRequestValidator : AbstractValidator<CreateSemesterRequest>
{
    public CreateSemesterRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("O nome do semestre é obrigatório.")
            .MaximumLength(50)
            .WithMessage("O nome do semestre deve ter no máximo 50 caracteres.");
    }
}

public class CreateCourseRequestValidator : AbstractValidator<CreateCourseRequest>
{
    public CreateCourseRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("O nome do curso é obrigatório.")
            .MaximumLength(150)
            .WithMessage("O nome do curso deve ter no máximo 150 caracteres.");

        RuleFor(x => x.PassingGrade)
            .InclusiveBetween(0.0, 10.0)
            .When(x => x.PassingGrade.HasValue)
            .WithMessage("A média de aprovação deve estar entre 0.0 e 10.0.");
    }
}

public class CreateAllocationRequestValidator : AbstractValidator<CreateAllocationRequest>
{
    public CreateAllocationRequestValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("O ID do usuário (UserId) é obrigatório e deve ser maior que zero.");

        RuleFor(x => x.ProfileId)
            .GreaterThan(0)
            .WithMessage("O ID do perfil (ProfileId) é obrigatório.");
    }
}

public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.Subject)
            .NotEmpty()
            .WithMessage("O assunto da mensagem é obrigatório.")
            .MaximumLength(200)
            .WithMessage("O assunto não pode exceder 200 caracteres.");

        RuleFor(x => x.Body)
            .NotEmpty()
            .WithMessage("O corpo da mensagem é obrigatório.");
    }
}
