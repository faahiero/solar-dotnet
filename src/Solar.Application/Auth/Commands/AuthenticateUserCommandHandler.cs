using Solar.Application.Common.Mediator;
using Solar.Domain.Common;

namespace Solar.Application.Auth.Commands;

public record AuthenticateUserCommand(string Login, string Password, string? RemoteIp) : ICommand<Result<LoginResponse>>;

public class AuthenticateUserCommandHandler : ICommandHandler<AuthenticateUserCommand, Result<LoginResponse>>
{
    private readonly AuthenticateUserUseCase _useCase;

    public AuthenticateUserCommandHandler(AuthenticateUserUseCase useCase)
    {
        _useCase = useCase;
    }

    public async Task<Result<LoginResponse>> HandleAsync(AuthenticateUserCommand command, CancellationToken cancellationToken = default)
    {
        var request = new LoginRequest
        {
            Login = command.Login,
            Password = command.Password,
            RemoteIp = command.RemoteIp
        };

        return await _useCase.ExecuteResultAsync(request, cancellationToken);
    }
}
