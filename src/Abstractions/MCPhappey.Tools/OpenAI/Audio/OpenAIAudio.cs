using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.Pipeline;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using OpenAI;
using OAI = OpenAI;

namespace MCPhappey.Tools.OpenAI.Audio;

public static class OpenAIAudio
{
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
    [McpServerTool(Title = "Generate speech from text",
        Destructive = false)]
    public static async Task<CallToolResult> OpenAIAudio_CreateSpeech(
        [Description("The text to generate audio for")]
        [MaxLength(4096)]
        string input,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Additional instructions about the generated audio.")]
        string? instructions = null,
        [Description("The voice of your generated audio.")]
        Voice? voice = Voice.Alloy,
        [Description("Playback speed factor")]
        float? speed = 1,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(input);
        var openAiClient = serviceProvider.GetRequiredService<OpenAIClient>();

        OAI.Audio.GeneratedSpeechVoice speechVoice = voice.HasValue ? voice.Value.ToGeneratedSpeechVoice()
            : OAI.Audio.GeneratedSpeechVoice.Alloy;

        var audioClient = openAiClient.GetAudioClient("gpt-4o-mini-tts");

        var item = await audioClient.GenerateSpeechAsync(input,
                         speechVoice,
                         new OAI.Audio.SpeechGenerationOptions()
                         {
                             SpeedRatio = speed,
                             Instructions = instructions,
                         }, cancellationToken);

        var uploaded = await requestContext.Server.Upload(serviceProvider,
            requestContext.ToOutputFileName("mp3"),
            item.Value, cancellationToken);

        return new CallToolResult()
        {
            Content = [
                uploaded != null ? new EmbeddedResourceBlock() {
                    Resource = new BlobResourceContents() {
                        MimeType = MimeTypes.AudioMP3,
                        Uri = uploaded.Uri,
                        Blob = Convert.ToBase64String(item.Value)
                    }
            } : new AudioContentBlock() {
                MimeType = MimeTypes.AudioMP3,
                Data = Convert.ToBase64String(item.Value),
            }]
        };
    }

    [Description("Generate text from an audio input file")]
    [McpServerTool(Title = "Transcribe audio to text",
        Destructive = false)]
    public static async Task<IEnumerable<ContentBlock>> OpenAIAudio_CreateTranscription(
       [Description("Url of the audio file")]
        string url,
       IServiceProvider serviceProvider,
       RequestContext<CallToolRequestParams> requestContext,
       [Description("A prompt to improve the quality of the generated transcripts.")]
        string? prompt = null,
       [Description("The language of the audio to transcribe")]
        string? language = null,
       [Description("Temperature")]
        float? temperature = null,
       CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(url);
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();
        var openAiClient = serviceProvider.GetRequiredService<OpenAIClient>();

        var downloads = await downloadService.ScrapeContentAsync(serviceProvider, requestContext.Server,
            url!, cancellationToken);
        var download = downloads.FirstOrDefault();

        if (download?.Contents.Length > 0 || string.IsNullOrEmpty(download?.Filename))
        {
            throw new ArgumentException(url);
        }

        var audioClient = openAiClient.GetAudioClient("gpt-4o-transcribe");
        var item = await audioClient.CreateTranscriptionText(download.Contents,
                         download.Filename,
                         new OAI.Audio.AudioTranscriptionOptions()
                         {
                             Prompt = prompt,
                             Language = language,
                             Temperature = temperature
                         },
                         cancellationToken);

        var uploaded = await requestContext.Server.Upload(serviceProvider,
            requestContext.ToOutputFileName("txt"),
            BinaryData.FromString(item), cancellationToken);

        return [new TextContentBlock() {
                Text = item
            }, new EmbeddedResourceBlock(){
                Resource = new TextResourceContents() {
                    MimeType = MimeTypes.PlainText,
                    Uri = uploaded?.Uri!,
                    Text = item
                }
            }];
    }
}

