using Microsoft.KernelMemory;

namespace MCPhappey.Decoders.Extensions;

public static class AspNetCoreExtensions
{
    public static IKernelMemoryBuilder WithDecoders(
        this IKernelMemoryBuilder builder, string openAiApiKey)
    {
        
        builder.WithContentDecoder(new AudioDecoder(openAiApiKey, "audio/mpeg", ".mp3"));
        builder.WithContentDecoder(new AudioDecoder(openAiApiKey, "audio/x-wav", ".wav"));
        builder.WithContentDecoder(new AudioDecoder(openAiApiKey, "audio/mp4", ".m4a"));
        builder.WithContentDecoder(new AudioDecoder(openAiApiKey, "audio/ogg", ".ogg"));

        return builder.WithContentDecoder<EpubDecoder>()
            .WithContentDecoder<JsonDecoder>()
            .WithContentDecoder<RtfDecoder>()
            .WithContentDecoder<HtmlDecoder>();
    }
}