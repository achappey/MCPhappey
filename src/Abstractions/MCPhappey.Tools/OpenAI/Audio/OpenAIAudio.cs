using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using OpenAI;
using OAI = OpenAI;

namespace MCPhappey.Tools.OpenAI.Audio;

public static class OpenAIAudio
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Voice
    {
        Alloy,
        Echo,
        Fable,
        Onyx,
        Nova,
        Shimmer,
        Ash,
        Coral,
        Sage
    }

    [Description("Generate audio from the input text")]
    public static async Task<CallToolResponse> OpenAIAudio_CreateSpeech(
        [Description("The text to generate audio for")]
        [MaxLength(4096)]
        string input,
        IServiceProvider serviceProvider,
        IMcpServer mcpServer,
        [Description("Which model to use. tts-1, tts-1-hd or gpt-4o-mini-tts")]
        string? model = "tts-1-hd",
        [Description("Control the voice of your generated audio with additional instructions. Does not work with tts-1 or tts-1-hd.")]
        string? instructions = null,
        // [Description("Control the voice of your generated audio with additional instructions. Does not work with tts-1 or tts-1-hd.")]
        //  Voice? voice = Voice.Alloy,
        [Description("Playback speed factor")]
        float? speed = 1,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(input);
        var uploadService = serviceProvider.GetRequiredService<UploadService>();
        var openAiClient = serviceProvider.GetRequiredService<OpenAIClient>();

        OAI.Audio.GeneratedSpeechVoice speechVoice = OAI.Audio.GeneratedSpeechVoice.Alloy;
        var audioClient = openAiClient.GetAudioClient(model);

        var item = await audioClient.GenerateSpeechAsync(input,
                         speechVoice,
                         new OAI.Audio.SpeechGenerationOptions()
                         {
                             SpeedRatio = speed,
                         }, cancellationToken);

        var uploaded = await uploadService.UploadToRoot(mcpServer, serviceProvider, $"OpenAI-Audio-{DateTime.Now.Ticks}.mp3",
            item.Value, cancellationToken);

        return new CallToolResponse()
        {
            Content = [new Content(){
                Type = uploaded != null ? "resource" : "audio",
                MimeType = "audio/mpeg",
                Data =  uploaded != null ? null : Convert.ToBase64String(item.Value) ,
                Resource = uploaded != null ? new TextResourceContents() {
                    MimeType = "audio/mpeg",
                    Uri = uploaded?.Uri ?? string.Empty,
                    Text = Convert.ToBase64String(item.Value)
                } : null
            }]
        };
    }

    [Description("Generate text from an audio input file")]
    public static async Task<CallToolResponse> OpenAIAudio_CreateTranscription(
       [Description("Url of the audio file")]
        string url,
       IServiceProvider serviceProvider,
       IMcpServer mcpServer,
       CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(url);
        var uploadService = serviceProvider.GetRequiredService<UploadService>();
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();
        var openAiClient = serviceProvider.GetRequiredService<OpenAIClient>();

        var downloads = await downloadService.ScrapeContentAsync(serviceProvider, mcpServer,
            url!, cancellationToken);
        var download = downloads.FirstOrDefault();

        if (download?.Contents.IsEmpty != false || string.IsNullOrEmpty(download?.Filename))
        {
            throw new ArgumentException(url);
        }

        var audioClient = openAiClient.GetAudioClient("gpt-4o-transcribe");

        var item = await audioClient.CreateTranscriptionText(download.Contents,
                         download.Filename,
                         cancellationToken);

        var uploaded = await uploadService.UploadToRoot(mcpServer, serviceProvider, $"OpenAI-Audio-Transcription-{DateTime.Now.Ticks}.txt",
            BinaryData.FromString(item), cancellationToken);

        return new CallToolResponse()
        {
            Content = [new Content(){
                Type = "resource",
                Resource = new TextResourceContents() {
                    MimeType = "text/plain",
                    Uri = uploaded?.Uri ?? string.Empty,
                    Text = item
                }
            }]
        };
    }
}

