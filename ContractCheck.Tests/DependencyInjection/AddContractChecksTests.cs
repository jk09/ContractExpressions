#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractCheck.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace ContractCheck.Tests.DependencyInjection;

/// <summary>
/// <c>AddContractChecks</c> (ContractCheck.DependencyInjection): wraps existing registrations in contract-checking proxies,
/// keeping lifetimes and keys, and leaves disposal of the implementation to the container – exactly once.
/// </summary>
public class AddContractChecksTests
{
    [Theory]
    [InlineData(ServiceLifetime.Transient)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Singleton)]
    public void Type_registrations_resolve_to_checking_proxies_with_their_lifetime(ServiceLifetime lifetime)
    {
        IServiceCollection services = Services();
        services.Add(new ServiceDescriptor(typeof(IMeter), typeof(Meter), lifetime));
        services.AddContractChecks<IMeter>();
        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);

        using IServiceScope first = provider.CreateScope();
        using IServiceScope second = provider.CreateScope();
        IMeter a = first.ServiceProvider.GetRequiredService<IMeter>();
        IMeter b = first.ServiceProvider.GetRequiredService<IMeter>();
        IMeter c = second.ServiceProvider.GetRequiredService<IMeter>();

        Assert.IsNotType<Meter>(a);
        Expect.Violation(ContractFailureKind.Precondition, () => a.Add(0));
        a.Add(2);
        Assert.Equal(lifetime != ServiceLifetime.Transient, ReferenceEquals(a, b));
        Assert.Equal(lifetime == ServiceLifetime.Singleton, ReferenceEquals(a, c));
    }

    [Fact]
    public void Factory_registrations_are_wrapped()
    {
        IServiceCollection services = Services();
        services.AddScoped<IMeter>(provider => new Meter(provider.GetRequiredService<DisposalLog>()) { Label = "from a factory" });
        services.AddContractChecks<IMeter>();
        using ServiceProvider provider = services.BuildServiceProvider();

        IMeter meter = provider.CreateScope().ServiceProvider.GetRequiredService<IMeter>();

        Assert.Equal("from a factory", meter.Label);
        Expect.Violation(ContractFailureKind.Precondition, () => meter.Add(-1));
    }

    [Fact]
    public void Instance_registrations_are_wrapped_once_and_still_not_disposed_by_the_container()
    {
        var log = new DisposalLog();
        var instance = new Meter(log);
        IServiceCollection services = Services();
        services.AddSingleton<IMeter>(instance);
        services.AddContractChecks<IMeter>();

        using (ServiceProvider provider = services.BuildServiceProvider())
        {
            IMeter meter = provider.GetRequiredService<IMeter>();
            Assert.Same(meter, provider.GetRequiredService<IMeter>());
            Expect.Violation(ContractFailureKind.Precondition, () => meter.Add(0));
        }

        Assert.Equal(0, log.Disposals);
    }

    [Fact]
    public void Keyed_registrations_are_wrapped_and_factories_get_their_key()
    {
        IServiceCollection services = Services();
        services.AddKeyedScoped<IMeter, Meter>("type");
        services.AddKeyedSingleton<IMeter>("factory", (provider, key) => new Meter(provider.GetRequiredService<DisposalLog>()) { Label = (string)key! });
        services.AddContractChecks<IMeter>();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        IMeter byType = scope.ServiceProvider.GetRequiredKeyedService<IMeter>("type");
        IMeter byFactory = scope.ServiceProvider.GetRequiredKeyedService<IMeter>("factory");

        Expect.Violation(ContractFailureKind.Precondition, () => byType.Add(0));
        Assert.Equal("factory", byFactory.Label);
        Expect.Violation(ContractFailureKind.Precondition, () => byFactory.Add(0));
    }

    [Fact]
    public void The_container_disposes_the_implementation_of_a_non_disposable_interface_exactly_once()
    {
        IServiceCollection services = Services();
        services.AddScoped<IMeter, Meter>();
        services.AddContractChecks<IMeter>();
        using ServiceProvider provider = services.BuildServiceProvider();
        DisposalLog log = provider.GetRequiredService<DisposalLog>();

        using (IServiceScope scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IMeter>().Add(1);
        }

        Assert.Equal(1, log.Disposals);
    }

    [Fact]
    public void A_disposable_interface_is_disposed_through_the_proxy_exactly_once()
    {
        IServiceCollection services = Services();
        services.AddScoped<IDisposableMeter, DisposableMeter>();
        services.AddContractChecks<IDisposableMeter>();
        using ServiceProvider provider = services.BuildServiceProvider();
        DisposalLog log = provider.GetRequiredService<DisposalLog>();

        using (IServiceScope scope = provider.CreateScope())
        {
            IDisposableMeter meter = scope.ServiceProvider.GetRequiredService<IDisposableMeter>();
            Expect.Violation(ContractFailureKind.Precondition, () => meter.Add(0));
        }

        Assert.Equal(1, log.Disposals);
    }

    [Fact]
    public void Calling_it_twice_does_not_wrap_twice()
    {
        IServiceCollection services = Services();
        services.AddTransient<IMeter, Meter>();
        services.AddContractChecks<IMeter>();
        ServiceDescriptor[] once = [.. services];

        services.AddContractChecks<IMeter>();
        services.AddContractChecks();

        Assert.Equal(once, [.. services]);
    }

    [Fact]
    public void Missing_registrations_and_non_interfaces_are_rejected()
    {
        Assert.Contains("call AddContractChecks after registering it", Assert.Throws<InvalidOperationException>(() => Services().AddContractChecks<IMeter>()).Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => Services().AddContractChecks<Meter>());
        Assert.Throws<ArgumentException>(() => Services().AddContractChecks(typeof(IRepository<>)));
    }

    [Fact]
    public void Without_a_type_it_wraps_every_interface_that_has_contracts()
    {
        IServiceCollection services = Services();
        services.AddScoped<IMeter, Meter>();
        services.AddScoped<IDisposableMeter, DisposableMeter>();
        services.AddScoped<IPlain, Plain>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddContractChecks();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        Assert.IsNotType<Meter>(scope.ServiceProvider.GetRequiredService<IMeter>());
        Assert.IsNotType<DisposableMeter>(scope.ServiceProvider.GetRequiredService<IDisposableMeter>());
        Assert.IsType<Plain>(scope.ServiceProvider.GetRequiredService<IPlain>());
        Assert.IsType<Repository<string>>(scope.ServiceProvider.GetRequiredService<IRepository<string>>());
    }

    private static IServiceCollection Services()
    {
        var services = new ServiceCollection();
        services.AddSingleton<DisposalLog>();
        return services;
    }
}

public sealed class DisposalLog
{
    private int disposals;

    public int Disposals => Volatile.Read(ref disposals);

    public void Disposed() => Interlocked.Increment(ref disposals);
}

[ContractClass(typeof(MeterContracts))]
public interface IMeter
{
    int Reading { get; }

    string Label { get; }

    void Add(int amount);
}

public interface IDisposableMeter : IMeter, IDisposable
{
}

public interface IPlain
{
    void Run();
}

public interface IRepository<T>
{
    T? Find(int id);
}

public sealed class Meter(DisposalLog log) : IMeter, IDisposable
{
    public int Reading { get; private set; }

    public string Label { get; init; } = string.Empty;

    public void Add(int amount) => Reading += amount;

    public void Dispose() => log.Disposed();
}

public sealed class DisposableMeter(DisposalLog log) : IDisposableMeter
{
    public int Reading { get; private set; }

    public string Label => string.Empty;

    public void Add(int amount) => Reading += amount;

    public void Dispose() => log.Disposed();
}

public sealed class Plain : IPlain
{
    public void Run()
    {
    }
}

public sealed class Repository<T> : IRepository<T>
{
    public T? Find(int id) => default;
}

[ContractClassFor(typeof(IMeter))]
internal sealed class MeterContracts
{
    public MeterContracts()
    {
        Dbc.Def(static (IMeter x, int amount) => x.Add(amount), static (IMeter x, int amount) => Contract.Requires(amount > 0));
        Dbc.Def(static (IMeter x) => Contract.Invariant(x.Reading >= 0));
    }
}
