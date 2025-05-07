using System.Text;
using MCPhappey.Tools.Extensions;
using OpenAI.Audio;

namespace MCPhappey.Tools.OpenAI.Audio;

public static class OpenAIAudioExtensions
{
    public static async Task<string> CreateTranscriptionText(this AudioClient audioClient,
        BinaryData content, string filename,
        CancellationToken cancellationToken = default)
    {
        var splitted = content.Split(25);

        StringBuilder resultString = new();

        foreach (var fileSplit in splitted ?? [])
        {
            var item = await audioClient.CreateTranscription(fileSplit, filename,
                cancellationToken: cancellationToken);

            resultString.AppendLine(item?.Text);
        }

        return resultString.ToString();

    }

    public static async Task<AudioTranscription> CreateTranscription(this AudioClient audioClient,
       BinaryData content, string filename,
       CancellationToken cancellationToken = default) =>
        await audioClient.TranscribeAudioAsync(content.ToStream(), filename,
            cancellationToken: cancellationToken);

}
