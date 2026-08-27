using Microsoft.Extensions.DependencyInjection;

namespace Solar.Application.Common.Mediator;

public class SolarMediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public SolarMediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));

        var handler = _serviceProvider.GetService(handlerType);
        if (handler == null)
        {
            throw new InvalidOperationException($"Nenhum handler foi registrado para a requisição do tipo '{requestType.Name}' retornando '{typeof(TResponse).Name}'.");
        }

        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));
        var behaviors = _serviceProvider.GetServices(behaviorType).Cast<dynamic>().Reverse().ToList();

        var method = handlerType.GetMethod("HandleAsync")!;
        RequestHandlerDelegate<TResponse> currentDelegate = () => (Task<TResponse>)method.Invoke(handler, new object[] { request, cancellationToken })!;

        foreach (var behavior in behaviors)
        {
            var next = currentDelegate;
            var currentBehavior = behavior;
            currentDelegate = () => (Task<TResponse>)currentBehavior.HandleAsync((dynamic)request, next, cancellationToken);
        }

        return await currentDelegate();
    }
}
