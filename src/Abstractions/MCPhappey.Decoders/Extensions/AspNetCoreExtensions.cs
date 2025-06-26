using Microsoft.KernelMemory;

namespace MCPhappey.Decoders.Extensions;

public static class AspNetCoreExtensions
{
    public static IKernelMemoryBuilder WithDecoders(
        this IKernelMemoryBuilder builder)
    {
        return builder.WithContentDecoder<EpubDecoder>()
            .WithContentDecoder<JsonDecoder>();
    }
}