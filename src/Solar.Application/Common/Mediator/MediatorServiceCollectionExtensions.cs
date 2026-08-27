using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Solar.Application.Common.Mediator.Behaviors;

namespace Solar.Application.Common.Mediator;

public static class MediatorServiceCollectionExtensions
{
    public static IServiceCollection AddSolarMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddScoped<ISender, SolarMediator>();
        services.AddScoped<IMediator, SolarMediator>();

        if (assemblies.Length == 0)
        {
            assemblies = new[] { typeof(IMediatorContractsMarker).Assembly };
        }

        foreach (var assembly in assemblies)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .SelectMany(t => t.GetInterfaces(), (t, i) => new { Implementation = t, Interface = i })
                .Where(x => x.Interface.IsGenericType && x.Interface.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

            foreach (var handler in handlerTypes)
            {
                services.AddScoped(handler.Interface, handler.Implementation);
            }
        }

        // Registra Pipeline Behaviors globais
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingPipelineBehavior<,>));

        return services;
    }
}

public interface IMediatorContractsMarker { }
