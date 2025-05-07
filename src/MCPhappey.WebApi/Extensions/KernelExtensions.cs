
using Microsoft.KernelMemory;

namespace MCPhappey.WebApi.Extensions;

public static class KernelExtensions
{
    public static IServiceCollection AddKernelMemoryWithOptions(
            this IServiceCollection services,
            Action<IKernelMemoryBuilder> configure,
            KernelMemoryBuilderBuildOptions buildOptions)
    {
        // 1. Maak een nieuwe builder
        var builder = new KernelMemoryBuilder(services);

        // 2. Voer de configuratie uit
        configure(builder);

        // 3. Bouw met je eigen opties
        var memoryClient = builder.Build(buildOptions);

        // 4. Registreer de client
        services.AddSingleton(memoryClient);

        return services;
    }
}