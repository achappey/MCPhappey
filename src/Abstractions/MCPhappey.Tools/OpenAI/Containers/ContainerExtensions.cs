using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using OpenAI.Containers;

namespace MCPhappey.Tools.OpenAI.Containers;

public static class ContainerExtensions
{
    public const string BASE_URL = "https://api.openai.com/v1/containers";

    public static async Task<T> WithContainerClient<T>(
        this IServiceProvider serviceProvider, Func<ContainerClient, Task<T>> func)
    {
        var openAiClient = serviceProvider.GetRequiredService<OpenAIClient>();

        return await func(openAiClient.GetContainerClient());
    }
}
