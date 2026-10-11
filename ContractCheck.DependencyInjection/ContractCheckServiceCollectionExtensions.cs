using Microsoft.Extensions.DependencyInjection;

namespace ContractCheck.DependencyInjection;

/// <summary>
/// Wraps existing service registrations in <see cref="Dbc.Make(Type, object)"/> proxies, so every resolved service checks
/// its contracts. Register services as usual, then call <c>AddContractChecks</c>:
/// <code>
/// services.AddScoped&lt;IAccount, Account&gt;();
/// services.AddContractChecks&lt;IAccount&gt;();   // or services.AddContractChecks() for every interface that has contracts
/// </code>
/// </summary>
/// <remarks>
/// Lifetimes and keys stay as registered. The container still disposes the implementation exactly once: for a service
/// interface that is not disposable, the original registration is kept under a private key and the container creates and
/// disposes the implementation; a disposable interface's proxy forwards <c>Dispose</c> to the implementation. Instance
/// registrations are wrapped right away and, as before, not disposed by the container. Open-generic registrations can't be
/// wrapped by a factory and are left alone.
/// </remarks>
public static class ContractCheckServiceCollectionExtensions
{
    /// <summary>Wraps every registration of <typeparamref name="TService"/> (keyed ones included) in a contract-checking proxy.</summary>
    /// <typeparam name="TService">The service interface.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns><paramref name="services"/>.</returns>
    /// <exception cref="ArgumentException"><typeparamref name="TService"/> is not an interface.</exception>
    /// <exception cref="InvalidOperationException">Nothing is registered for <typeparamref name="TService"/> – call this after registering it.</exception>
    public static IServiceCollection AddContractChecks<TService>(this IServiceCollection services)
        where TService : class =>
        services.AddContractChecks(typeof(TService));

    /// <summary>Wraps every registration of <paramref name="serviceType"/> (keyed ones included) in a contract-checking proxy.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceType">The service interface; a closed interface type.</param>
    /// <returns><paramref name="services"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="serviceType"/> is not a closed interface type.</exception>
    /// <exception cref="InvalidOperationException">Nothing is registered for <paramref name="serviceType"/> – call this after registering it.</exception>
    public static IServiceCollection AddContractChecks(this IServiceCollection services, Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(serviceType);
        if (!serviceType.IsInterface || serviceType.ContainsGenericParameters)
        {
            throw new ArgumentException($"AddContractChecks needs a closed interface type, but {serviceType} is not one.", nameof(serviceType));
        }

        bool registered = false;
        for (int i = 0; i < services.Count; i++)
        {
            if (services[i].ServiceType != serviceType || services[i].ServiceKey is InnerKey)
            {
                continue;
            }

            registered = true;
            Decorate(services, i);
        }

        if (!registered)
        {
            throw new InvalidOperationException($"No service is registered for {serviceType}; call AddContractChecks after registering it.");
        }

        return services;
    }

    /// <summary>
    /// Wraps every registration whose service type is a closed interface with contracts (<see cref="Dbc.HasContracts(Type)"/>)
    /// in a contract-checking proxy; other registrations are left alone.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns><paramref name="services"/>.</returns>
    public static IServiceCollection AddContractChecks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        Dictionary<Type, bool> hasContracts = [];
        for (int i = 0; i < services.Count; i++)
        {
            Type serviceType = services[i].ServiceType;
            if (!serviceType.IsInterface || serviceType.ContainsGenericParameters || services[i].ServiceKey is InnerKey)
            {
                continue;
            }

            if (!hasContracts.TryGetValue(serviceType, out bool checks))
            {
                checks = hasContracts[serviceType] = Dbc.HasContracts(serviceType);
            }

            if (checks)
            {
                Decorate(services, i);
            }
        }

        return services;
    }

    /// <summary>Replaces <c>services[index]</c> with a registration that returns a proxy, unless it already does.</summary>
    private static void Decorate(IServiceCollection services, int index)
    {
        ServiceDescriptor original = services[index];
        Type serviceType = original.ServiceType;
        object? key = original.ServiceKey;

        if (IsDecorated(original))
        {
            return;
        }

        object? instance = original.IsKeyedService ? original.KeyedImplementationInstance : original.ImplementationInstance;
        if (instance is not null)
        {
            services[index] = original.IsKeyedService
                ? new ServiceDescriptor(serviceType, key, Dbc.Make(serviceType, instance))
                : new ServiceDescriptor(serviceType, Dbc.Make(serviceType, instance));
            return;
        }

        bool disposableInterface = typeof(IDisposable).IsAssignableFrom(serviceType) || typeof(IAsyncDisposable).IsAssignableFrom(serviceType);
        if (disposableInterface || ReferenceEquals(key, KeyedService.AnyKey))
        {
            // The proxy forwards Dispose (or the key is only known at resolution): create the implementation here.
            Func<IServiceProvider, object?, object> create = Creator(original);
            var direct = new DecoratedFactory(serviceType, (provider, requestedKey) => create(provider, requestedKey));
            services[index] = original.IsKeyedService
                ? new ServiceDescriptor(serviceType, key, direct.CreateKeyed, original.Lifetime)
                : new ServiceDescriptor(serviceType, direct.Create, original.Lifetime);
            return;
        }

        // The container keeps creating (and disposing) the implementation under a private key; the proxy isn't disposable.
        var inner = new InnerKey(key);
        services.Add(Inner(original, inner));
        var decorated = new DecoratedFactory(serviceType, (provider, _) => provider.GetRequiredKeyedService(serviceType, inner));
        services[index] = original.IsKeyedService
            ? new ServiceDescriptor(serviceType, key, decorated.CreateKeyed, original.Lifetime)
            : new ServiceDescriptor(serviceType, decorated.Create, original.Lifetime);
    }

    private static bool IsDecorated(ServiceDescriptor descriptor) =>
        (descriptor.IsKeyedService ? descriptor.KeyedImplementationFactory?.Target : descriptor.ImplementationFactory?.Target) is DecoratedFactory;

    /// <summary>The original registration, keyed with <paramref name="inner"/>.</summary>
    private static ServiceDescriptor Inner(ServiceDescriptor original, InnerKey inner)
    {
        Type serviceType = original.ServiceType;
        if (!original.IsKeyedService)
        {
            return original.ImplementationType is { } type
                ? new ServiceDescriptor(serviceType, inner, type, original.Lifetime)
                : new ServiceDescriptor(serviceType, inner, (provider, _) => original.ImplementationFactory!(provider), original.Lifetime);
        }

        return original.KeyedImplementationType is { } keyedType
            ? new ServiceDescriptor(serviceType, inner, keyedType, original.Lifetime)
            : new ServiceDescriptor(serviceType, inner, (provider, _) => original.KeyedImplementationFactory!(provider, original.ServiceKey), original.Lifetime);
    }

    /// <summary>Creates the implementation the way <paramref name="original"/> would.</summary>
    private static Func<IServiceProvider, object?, object> Creator(ServiceDescriptor original)
    {
        if (!original.IsKeyedService)
        {
            return original.ImplementationType is { } type
                ? (provider, _) => ActivatorUtilities.CreateInstance(provider, type)
                : (provider, _) => original.ImplementationFactory!(provider);
        }

        return original.KeyedImplementationType is { } keyedType
            ? (provider, _) => ActivatorUtilities.CreateInstance(provider, keyedType)
            : (provider, requestedKey) => original.KeyedImplementationFactory!(provider, requestedKey);
    }

    /// <summary>The factory of a decorated registration; its type marks the registration as decorated.</summary>
    private sealed class DecoratedFactory(Type serviceType, Func<IServiceProvider, object?, object> implementation)
    {
        public object Create(IServiceProvider provider) => Dbc.Make(serviceType, implementation(provider, null));

        public object CreateKeyed(IServiceProvider provider, object? key) => Dbc.Make(serviceType, implementation(provider, key));
    }

    /// <summary>The private key of an original registration kept for the container to create and dispose.</summary>
    private sealed class InnerKey(object? originalKey)
    {
        public object? OriginalKey { get; } = originalKey;

        public override string ToString() => $"ContractCheck inner registration ({OriginalKey ?? "unkeyed"})";
    }
}
