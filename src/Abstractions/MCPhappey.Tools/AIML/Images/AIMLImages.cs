using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Json.More;
using MCPhappey.Common.Extensions;
using MCPhappey.Common.Models;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.Pipeline;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.AIML.Images;

public static class AIMLImages
{
    private static readonly string BASE_URL = "https://api.aimlapi.com/v1/images/generations";

    private static (int r, int g, int b) HexToRgb(this string hex)
    {
        hex = hex.TrimStart('#');

        if (hex.Length == 3) // short form like #f80
            hex = string.Concat(hex.Select(c => $"{c}{c}"));

        if (hex.Length != 6)
            throw new ArgumentException("Invalid hex color.", nameof(hex));

        return (
            Convert.ToInt32(hex[..2], 16),
            Convert.ToInt32(hex.Substring(2, 2), 16),
            Convert.ToInt32(hex.Substring(4, 2), 16)
        );
    }


    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ImageStyle
    {
        [EnumMember(Value = "realistic_image")]
        RealisticImage,

        [EnumMember(Value = "digital_illustration")]
        DigitalIllustration,

        [EnumMember(Value = "vector_illustration")]
        VectorIllustration,

        [EnumMember(Value = "realistic_image/b_and_w")]
        RealisticImage_BW,

        [EnumMember(Value = "realistic_image/hard_flash")]
        RealisticImage_HardFlash,

        [EnumMember(Value = "realistic_image/hdr")]
        RealisticImage_HDR,

        [EnumMember(Value = "realistic_image/natural_light")]
        RealisticImage_NaturalLight,

        [EnumMember(Value = "realistic_image/studio_portrait")]
        RealisticImage_StudioPortrait,

        [EnumMember(Value = "realistic_image/enterprise")]
        RealisticImage_Enterprise,

        [EnumMember(Value = "realistic_image/motion_blur")]
        RealisticImage_MotionBlur,

        [EnumMember(Value = "digital_illustration/pixel_art")]
        DigitalIllustration_PixelArt,

        [EnumMember(Value = "digital_illustration/hand_drawn")]
        DigitalIllustration_HandDrawn,

        [EnumMember(Value = "digital_illustration/grain")]
        DigitalIllustration_Grain,

        [EnumMember(Value = "digital_illustration/infantile_sketch")]
        DigitalIllustration_InfantileSketch,

        [EnumMember(Value = "digital_illustration/2d_art_poster")]
        DigitalIllustration_2DArtPoster,

        [EnumMember(Value = "digital_illustration/handmade_3d")]
        DigitalIllustration_Handmade3D,

        [EnumMember(Value = "digital_illustration/hand_drawn_outline")]
        DigitalIllustration_HandDrawnOutline,

        [EnumMember(Value = "digital_illustration/engraving_color")]
        DigitalIllustration_EngravingColor,

        [EnumMember(Value = "digital_illustration/2d_art_poster_2")]
        DigitalIllustration_2DArtPoster2,

        [EnumMember(Value = "vector_illustration/engraving")]
        VectorIllustration_Engraving,

        [EnumMember(Value = "vector_illustration/line_art")]
        VectorIllustration_LineArt,

        [EnumMember(Value = "vector_illustration/line_circuit")]
        VectorIllustration_LineCircuit,

        [EnumMember(Value = "vector_illustration/linocut")]
        VectorIllustration_Linocut
    }
    /*

    [Description("Please fill in the AI/ML SeedDream v4 image request details.")]
    public class AIMLNewSeedDreamImage
    {
        [JsonPropertyName("prompt")]
        [Required]
        [Description("The image generation prompt.")]
        public string Prompt { get; set; } = default!;

        [JsonPropertyName("height")]
        [Required]
        [Range(1024, 4096)]
        [Description("Height of the image to generate in number of pixels.")]
        public int Height { get; set; } = 1024;

        [JsonPropertyName("width")]
        [Required]
        [Range(1024, 4096)]
        [Description("Width of the image to generate in number of pixels.")]
        public int Width { get; set; } = 1024;

        [JsonPropertyName("num_images")]
        [Range(1, 4)]
        [Description("Number of images to generate.")]
        public int NumImages { get; set; } = 1;

        [JsonPropertyName("seed")]
        [Range(1, int.MaxValue)]
        [Description("Seed.")]
        public int? Seed { get; set; }

        [JsonPropertyName("enable_safety_checker")]
        [Required]
        [Description("If the safety checker should be enabled.")]
        public bool EnableSafetyChecker { get; set; } = true;

        [JsonPropertyName("sync_mode")]
        [Required]
        [Description("When enabled, the function will wait for the image to be generated and uploaded before returning the response.")]
        public bool SyncMode { get; set; } = false;

        [JsonPropertyName("filename")]
        [Required]
        [Description("Output filename without extension.")]
        public string Filename { get; set; } = default!;
    }


    [Description("Generate an image using AI/ML SeedDream v4 Text-to-Image model.")]
    [McpServerTool(Title = "Generate image with SeedDream v4",
        Name = "aiml_images_seeddream_create", Destructive = false)]
    public static async Task<CallToolResult?> AIMLImages_SeedDreamCreate(
       [Description("Prompt for the image generation."), MaxLength(4000)] string prompt,
       IServiceProvider serviceProvider,
       RequestContext<CallToolRequestParams> requestContext,
       [Description("Width of the image."), Range(1024, 4096)] int width = 1024,
       [Description("Height of the image."), Range(1024, 4096)] int height = 1024,
       [Description("Random seed value (optional).")] int? seed = null,
       [Description("Enable the safety checker (default true).")] bool enableSafetyChecker = true,
       [Description("Generate images synchronously (default false).")] bool syncMode = false,
       [Description("Number of images to generate (1-4)."), Range(1, 4)] int numImages = 1,
       [Description("Output filename without extension. Defaults to autogenerated name.")] string? filename = null,
       CancellationToken cancellationToken = default)
       => await requestContext.WithExceptionCheck(async () =>
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);

        var settings = serviceProvider.GetRequiredService<AIMLSettings>();
        var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        // Step 1: Ask user for any missing params
        var (typed, notAccepted, _) = await requestContext.Server.TryElicit(
            new AIMLNewSeedDreamImage
            {
                Prompt = prompt,
                Width = width,
                Height = height,
                EnableSafetyChecker = enableSafetyChecker,
                Seed = seed,
                SyncMode = syncMode,
                NumImages = numImages,
                Filename = filename?.ToOutputFileName() ?? requestContext.ToOutputFileName("png"),
            },
            cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typed == null) return "User input missing.".ToErrorCallToolResponse();

        // Step 2: Build JSON body
        var jsonBody = JsonSerializer.Serialize(new
        {
            prompt = typed.Prompt,
            model = "bytedance/seedream-v4-text-to-image",
            image_size = new
            {
                width = typed.Width,
                height = typed.Height
            },
            seed,
            num_images = typed.NumImages,
            enable_safety_checker = enableSafetyChecker,
            sync_mode = syncMode
        });

        using var client = clientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.aimlapi.com/v1/images/generations");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MimeTypes.Json));
        request.Content = new StringContent(jsonBody, Encoding.UTF8, MimeTypes.Json);

        // Step 3: Send request
        using var resp = await client.SendAsync(request, cancellationToken);
        var jsonResponse = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode)
            throw new Exception($"{resp.StatusCode}: {jsonResponse}");

        using var doc = JsonDocument.Parse(jsonResponse);
        var base64 = doc.RootElement
            .GetProperty("data")[0]
            .GetProperty("b64_json")
            .GetString();

        if (string.IsNullOrWhiteSpace(base64))
            throw new Exception("No image data returned from SeedDream API.");

        var bytes = Convert.FromBase64String(base64);

        // Step 4: Upload to MCP storage
        var uploaded = await requestContext.Server.Upload(
            serviceProvider,
            $"{typed.Filename}.png",
            BinaryData.FromBytes(bytes),
            cancellationToken);

        if (uploaded == null)
            throw new Exception("Upload failed.");

        // Step 5: Return as result + preview image
        return new CallToolResult()
        {
            Content =
            [
                uploaded,
            new ImageContentBlock()
            {
                Data = Convert.ToBase64String(bytes),
                MimeType = "image/png"
            }
            ]
        };
    });
    */


    [Description("Generate an image using AI/ML AI Recraft image model. For both height and width, the value must be a multiple of 32.")]
    [McpServerTool(Title = "Generate image with AI/ML Recraft",
        Name = "aiml_images_recraft_create", Destructive = false)]
    public static async Task<CallToolResult?> AIMLImages_RecraftCreate(
       [Description("Prompt for the image generation."), MaxLength(4000)] string prompt,
       IServiceProvider serviceProvider,
       RequestContext<CallToolRequestParams> requestContext,
       [Description("Width of the image."), Range(64, 1536)] int width = 1024,
       [Description("Height of the image."), Range(64, 1536)] int height = 768,
       // [Description("List of colors in hex format.")] List<string>? colors = null,
       ImageStyle? style = ImageStyle.RealisticImage,
       [Description("Output filename without extension. Defaults to autogenerated name.")] string? filename = null,
       CancellationToken cancellationToken = default)
       => await requestContext.WithExceptionCheck(async () =>
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);

        var settings = serviceProvider.GetRequiredService<AIMLSettings>();
        var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        // Step 1: Ask user for additional image parameters
        var (typed, notAccepted, _) = await requestContext.Server.TryElicit(
            new AIMLNewRecraftImage
            {
                Prompt = prompt,
                Filename = filename?.ToOutputFileName() ?? requestContext.ToOutputFileName("png"),
                Width = width,
                Style = style ?? ImageStyle.RealisticImage,
                Height = height,
            },
            cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typed == null) return "User input missing.".ToErrorCallToolResponse();

        // var colorItems = colors?.Select(a => a.HexToRgb());
        // Step 2: Build JSON payload
        var jsonBody = JsonSerializer.Serialize(new
        {
            prompt = typed.Prompt,
            model = "recraft-v3",
            //colors = colorItems ?? [],
            image_size = new
            {
                height = typed.Height,
                width = typed.Width,
            }
        });

        using var client = clientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, BASE_URL);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MimeTypes.Json));
        request.Content = new StringContent(jsonBody, Encoding.UTF8, MimeTypes.Json);

        // Step 3: Send request
        using var resp = await client.SendAsync(request, cancellationToken);
        var jsonResponse = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode)
            throw new Exception($"{resp.StatusCode}: {jsonResponse}");

        using var doc = JsonDocument.Parse(jsonResponse);
        var url = doc.RootElement
            .GetProperty("data")[0]
            .GetProperty("url")
            .GetString();

        if (string.IsNullOrWhiteSpace(url))
            throw new Exception("No image data returned from AI/ML API.");

        var filesData = await downloadService.DownloadContentAsync(serviceProvider, requestContext.Server, url, cancellationToken);
        var fileData = filesData.FirstOrDefault() ?? throw new Exception("Something went wrong");
        //     var bytes = Convert.FromBase64String(b64);

        // Step 4: Upload to MCP storage
        var uploaded = await requestContext.Server.Upload(
            serviceProvider,
            $"{typed.Filename}.webp",
            fileData.Contents,
            cancellationToken);

        if (uploaded == null)
            throw new Exception("Upload failed.");

        return new CallToolResult()
        {
            Content =
            [
                uploaded,
                new ImageContentBlock()
                {
                    Data = Convert.ToBase64String(fileData.Contents),
                    MimeType = "image/webp"
                }
            ]
        };
    });


    [Description("Please fill in the AI image request details.")]
    public class AIMLNewRecraftImage
    {
        [JsonPropertyName("prompt")]
        [Required]
        [Description("The image prompt.")]
        public string Prompt { get; set; } = default!;

        [Required]
        [JsonPropertyName("style")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Description("The image style.")]
        public ImageStyle Style { get; set; } = ImageStyle.RealisticImage;

        [JsonPropertyName("height")]
        [Required]
        [Range(64, 1536)]
        [Description("Height of the image to generate in number of pixels.")]
        public int Height { get; set; } = 768;

        [JsonPropertyName("width")]
        [Required]
        [Range(64, 1536)]
        [Description("Width of the image to generate in number of pixels.")]
        public int Width { get; set; } = 1024;

        [JsonPropertyName("filename")]
        [Required]
        [Description("The new image file name.")]
        public string Filename { get; set; } = default!;



    }

    [Description("Generate an image using AI/ML AI Gemini 2.5 Flash (aka Nano Banana) image model.")]
    [McpServerTool(Title = "Generate image with AI/ML Gemini 2.5 Flash",
           Name = "aiml_images_gemini_flash_create", Destructive = false)]
    public static async Task<CallToolResult?> AIMLImages_GeminiFlashCreate(
          [Description("Prompt for the image generation."), MaxLength(4000)] string prompt,
          IServiceProvider serviceProvider,
          RequestContext<CallToolRequestParams> requestContext,
       [Description("Number of images to generate (1-4)."), Range(1, 4)] int numImages = 1,
          [Description("Output filename without extension. Defaults to autogenerated name.")] string? filename = null,
          CancellationToken cancellationToken = default)
          => await requestContext.WithExceptionCheck(async () =>
       {
           ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);

           var settings = serviceProvider.GetRequiredService<AIMLSettings>();
           var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
           var downloadService = serviceProvider.GetRequiredService<DownloadService>();

           // Step 1: Ask user for additional image parameters
           var (typed, notAccepted, _) = await requestContext.Server.TryElicit(
               new AIMLNewGeminiFlashImage
               {
                   Prompt = prompt,
                   Filename = filename?.ToOutputFileName() ?? requestContext.ToOutputFileName("png"),
                   NumImages = numImages,
               },
               cancellationToken);

           if (notAccepted != null) return notAccepted;
           if (typed == null) return "User input missing.".ToErrorCallToolResponse();

           // Step 2: Build JSON payload
           var jsonBody = JsonSerializer.Serialize(new
           {
               prompt = typed.Prompt,
               model = "google/gemini-2.5-flash-image",
               num_images = typed.NumImages
           });

           using var client = clientFactory.CreateClient();
           using var request = new HttpRequestMessage(HttpMethod.Post, BASE_URL);
           request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
           request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MimeTypes.Json));
           request.Content = new StringContent(jsonBody, Encoding.UTF8, MimeTypes.Json);

           // Step 3: Send request
           using var resp = await client.SendAsync(request, cancellationToken);
           var jsonResponse = await resp.Content.ReadAsStringAsync(cancellationToken);
           if (!resp.IsSuccessStatusCode)
               throw new Exception($"{resp.StatusCode}: {jsonResponse}");

           using var doc = JsonDocument.Parse(jsonResponse);
           var dataArray = doc.RootElement.GetProperty("data");

           if (dataArray.GetArrayLength() == 0)
               throw new Exception("No image data returned from AI/ML API.");

           var allUploads = new List<ContentBlock>();

           foreach (var item in dataArray.EnumerateArray())
           {
               var url = item.GetProperty("url").GetString();
               if (string.IsNullOrWhiteSpace(url))
                   continue;

               // Download
               var filesData = await downloadService.DownloadContentAsync(
                   serviceProvider, requestContext.Server, url, cancellationToken);

               foreach (var file in filesData)
               {
                   var safeName = $"{Path.GetFileNameWithoutExtension(typed.Filename)}_{Guid.NewGuid():N}.jpeg";
                   var uploaded = await requestContext.Server.Upload(
                       serviceProvider, safeName, file.Contents, cancellationToken);

                   if (uploaded != null)
                   {
                       allUploads.Add(uploaded);
                       allUploads.Add(new ImageContentBlock()
                       {
                           Data = Convert.ToBase64String(file.Contents),
                           MimeType = "image/jpeg"
                       });
                   }
               }
           }

           if (allUploads.Count == 0)
               throw new Exception("No images could be downloaded or uploaded.");

           return new CallToolResult()
           {
               Content =
               [
                   .. allUploads,
                new EmbeddedResourceBlock()
                {
                    Resource = new TextResourceContents()
                    {
                        MimeType = MimeTypes.Json,
                        Text = doc.RootElement.ToJsonString(),
                        Uri = BASE_URL
                    }
                }
               ]
           };

       });

    [Description("Generate an image edit using AI/ML AI Gemini 2.5 Flash (aka Nano Banana) image model.")]
    [McpServerTool(Title = "Generate image edit with AI/ML Gemini 2.5 Flash",
              Name = "aiml_images_gemini_flash_edit", Destructive = false)]
    public static async Task<CallToolResult?> AIMLImages_GeminiFlashEdit(
            [Description("Prompt for the image generation."), MaxLength(4000)] string prompt,
            [Description("Input file URLs. Protected SharePoint and/or OneDrive links are supported")] IEnumerable<string> imageUrls,
            IServiceProvider serviceProvider,
            RequestContext<CallToolRequestParams> requestContext,
            [Description("Number of images to generate (1-4)."), Range(1, 4)] int numImages = 1,
            [Description("Output filename without extension. Defaults to autogenerated name.")] string? filename = null,
             CancellationToken cancellationToken = default)
             => await requestContext.WithExceptionCheck(async () =>
          {
              ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);

              var settings = serviceProvider.GetRequiredService<AIMLSettings>();
              var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
              var downloadService = serviceProvider.GetRequiredService<DownloadService>();

              var attachedLinks = new List<FileItem>();
              foreach (var imagUrl in imageUrls)
              {
                  var data = await downloadService.DownloadContentAsync(serviceProvider, requestContext.Server, imagUrl, cancellationToken);
                  attachedLinks.AddRange(data);
              }

              if (attachedLinks.Count > 0)
              {
                  await requestContext.Server.SendMessageNotificationAsync(
                      $"Attached {attachedLinks.Count} file(s) for image edit.", LoggingLevel.Info, cancellationToken);
              }

              // Step 1: Ask user for additional image parameters
              var (typed, notAccepted, _) = await requestContext.Server.TryElicit(
                  new AIMLNewGeminiFlashImage
                  {
                      Prompt = prompt,
                      Filename = filename?.ToOutputFileName() ?? requestContext.ToOutputFileName("png"),
                      NumImages = numImages,
                  },
                  cancellationToken);

              if (notAccepted != null) return notAccepted;
              if (typed == null) return "User input missing.".ToErrorCallToolResponse();

              // Step 2: Build JSON payload
              var jsonBody = JsonSerializer.Serialize(new
              {
                  prompt = typed.Prompt,
                  model = "google/gemini-2.5-flash-image-edit",
                  num_images = typed.NumImages,
                  image_urls = attachedLinks.Select(a => Convert.ToBase64String(a.Contents))
              });

              using var client = clientFactory.CreateClient();
              using var request = new HttpRequestMessage(HttpMethod.Post, BASE_URL);
              request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
              request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MimeTypes.Json));
              request.Content = new StringContent(jsonBody, Encoding.UTF8, MimeTypes.Json);

              // Step 3: Send request
              using var resp = await client.SendAsync(request, cancellationToken);
              var jsonResponse = await resp.Content.ReadAsStringAsync(cancellationToken);
              if (!resp.IsSuccessStatusCode)
                  throw new Exception($"{resp.StatusCode}: {jsonResponse}");

              using var doc = JsonDocument.Parse(jsonResponse);
              var dataArray = doc.RootElement.GetProperty("data");

              if (dataArray.GetArrayLength() == 0)
                  throw new Exception("No image data returned from AI/ML API.");

              var allUploads = new List<ContentBlock>();

              foreach (var item in dataArray.EnumerateArray())
              {
                  var url = item.GetProperty("url").GetString();
                  if (string.IsNullOrWhiteSpace(url))
                      continue;

                  // Download
                  var filesData = await downloadService.DownloadContentAsync(
                      serviceProvider, requestContext.Server, url, cancellationToken);

                  foreach (var file in filesData)
                  {
                      var safeName = $"{Path.GetFileNameWithoutExtension(typed.Filename)}_{Guid.NewGuid():N}.jpeg";
                      var uploaded = await requestContext.Server.Upload(
                          serviceProvider, safeName, file.Contents, cancellationToken);

                      if (uploaded != null)
                      {
                          allUploads.Add(uploaded);
                          allUploads.Add(new ImageContentBlock()
                          {
                              Data = Convert.ToBase64String(file.Contents),
                              MimeType = "image/jpeg"
                          });
                      }
                  }
              }

              if (allUploads.Count == 0)
                  throw new Exception("No images could be downloaded or uploaded.");

              return new CallToolResult()
              {
                  Content =
                  [
                      .. allUploads,
                new EmbeddedResourceBlock()
                {
                    Resource = new TextResourceContents()
                    {
                        MimeType = MimeTypes.Json,
                        Text = doc.RootElement.ToJsonString(),
                        Uri = BASE_URL
                    }
                }
                  ]
              };
          });

    [Description("Please fill in the AI image request details.")]
    public class AIMLNewGeminiFlashImage
    {
        [JsonPropertyName("prompt")]
        [Required]
        [Description("The image prompt.")]
        public string Prompt { get; set; } = default!;

        [JsonPropertyName("num_images")]
        [Range(1, 4)]
        [Description("Number of images to generate.")]
        public int NumImages { get; set; } = 1;

        [JsonPropertyName("filename")]
        [Required]
        [Description("The new image file name.")]
        public string Filename { get; set; } = default!;
    }

    [Description("Generate an image using AI/ML Stable Diffusion v3.5 Large model.")]
    [McpServerTool(
        Title = "Generate image with AI/ML Stable Diffusion v3.5 Large",
        Name = "aiml_images_stable_diffusion_v35_create",
        Destructive = false)]
    public static async Task<CallToolResult?> AIMLImages_StableDiffusionV35Create(
        [Description("Prompt for the image generation."), MaxLength(4000)] string prompt,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Width of the image (must be multiple of 32)."), Range(64, 2048)] int width = 1024,
        [Description("Height of the image (must be multiple of 32)."), Range(64, 2048)] int height = 768,
        [Description("Elements or styles to avoid in the generated image.")] string? negativePrompt = null,
        [Description("CFG (Classifier Free Guidance) scale (1–20). Higher values mean closer adherence to the prompt."), Range(1, 20)] double guidanceScale = 7.5,
        [Description("Number of inference steps (1–50). Higher values mean better quality but slower generation."), Range(1, 50)] int numInferenceSteps = 25,
        [Description("Enable the safety checker (default true).")] bool enableSafetyChecker = true,
        [Description("Number of images to generate (1–4)."), Range(1, 4)] int numImages = 1,
        [Description("Output format of the image (jpeg or png).")] string outputFormat = "jpeg",
        [Description("Seed for deterministic generation (optional).")] long? seed = null,
        [Description("Output filename without extension. Defaults to autogenerated name.")] string? filename = null,
        CancellationToken cancellationToken = default)
        => await requestContext.WithExceptionCheck(async () =>
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);

        var settings = serviceProvider.GetRequiredService<AIMLSettings>();
        var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        // Step 1: Ask user for any missing fields
        var (typed, notAccepted, _) = await requestContext.Server.TryElicit(
            new AIMLNewStableDiffusionV35Image
            {
                Prompt = prompt,
                Filename = filename?.ToOutputFileName() ?? requestContext.ToOutputFileName(outputFormat),
                Width = width,
                Height = height,
                NegativePrompt = negativePrompt,
                GuidanceScale = guidanceScale,
                NumInferenceSteps = numInferenceSteps,
                EnableSafetyChecker = enableSafetyChecker,
                NumImages = numImages,
                OutputFormat = outputFormat,
                Seed = seed
            },
            cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typed == null) return "User input missing.".ToErrorCallToolResponse();

        // Step 2: Build JSON payload
        var jsonBody = JsonSerializer.Serialize(new
        {
            model = "stable-diffusion-v35-large",
            prompt = typed.Prompt,
            negative_prompt = typed.NegativePrompt,
            image_size = new { width = typed.Width, height = typed.Height },
            guidance_scale = typed.GuidanceScale,
            num_inference_steps = typed.NumInferenceSteps,
            enable_safety_checker = typed.EnableSafetyChecker,
            num_images = typed.NumImages,
            output_format = typed.OutputFormat,
            seed = typed.Seed
        });

        using var client = clientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, BASE_URL);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MimeTypes.Json));
        request.Content = new StringContent(jsonBody, Encoding.UTF8, MimeTypes.Json);

        // Step 3: Send request
        using var resp = await client.SendAsync(request, cancellationToken);
        var jsonResponse = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode)
            throw new Exception($"{resp.StatusCode}: {jsonResponse}");

        using var doc = JsonDocument.Parse(jsonResponse);
        var dataArray = doc.RootElement.GetProperty("data");

        if (dataArray.GetArrayLength() == 0)
            throw new Exception("No image data returned from AI/ML API.");

        var allUploads = new List<ContentBlock>();

        foreach (var item in dataArray.EnumerateArray())
        {
            var url = item.GetProperty("url").GetString();
            if (string.IsNullOrWhiteSpace(url))
                continue;

            // Download
            var filesData = await downloadService.DownloadContentAsync(
                serviceProvider, requestContext.Server, url, cancellationToken);

            foreach (var file in filesData)
            {
                var safeName = $"{Path.GetFileNameWithoutExtension(typed.Filename)}_{Guid.NewGuid():N}.{typed.OutputFormat}";
                var uploaded = await requestContext.Server.Upload(
                    serviceProvider, safeName, file.Contents, cancellationToken);

                if (uploaded != null)
                {
                    allUploads.Add(uploaded);
                    allUploads.Add(new ImageContentBlock()
                    {
                        Data = Convert.ToBase64String(file.Contents),
                        MimeType = "image/" + typed.OutputFormat
                    });
                }
            }
        }

        if (allUploads.Count == 0)
            throw new Exception("No images could be downloaded or uploaded.");

        return new CallToolResult()
        {
            Content =
            [
                .. allUploads,
                new EmbeddedResourceBlock()
                {
                    Resource = new TextResourceContents()
                    {
                        MimeType = MimeTypes.Json,
                        Text = doc.RootElement.ToJsonString(),
                        Uri = BASE_URL
                    }
                }
            ]
        };
    });

    [Description("Please fill in the AI Stable Diffusion v3.5 image generation request.")]
    public class AIMLNewStableDiffusionV35Image
    {
        [JsonPropertyName("prompt")]
        [Required]
        [Description("The main text prompt describing the desired image.")]
        public string Prompt { get; set; } = default!;

        [JsonPropertyName("negative_prompt")]
        [Description("Optional description of elements to avoid in the generated image.")]
        public string? NegativePrompt { get; set; }

        [JsonPropertyName("guidance_scale")]
        [Range(1, 20)]
        [Description("CFG (Classifier Free Guidance) scale (1–20). Higher values mean closer adherence to the prompt.")]
        public double GuidanceScale { get; set; } = 7.5;

        [JsonPropertyName("num_inference_steps")]
        [Range(1, 50)]
        [Description("Number of inference steps to perform (1–50).")]
        public int NumInferenceSteps { get; set; } = 25;

        [JsonPropertyName("enable_safety_checker")]
        [Description("Enable the safety checker.")]
        public bool EnableSafetyChecker { get; set; } = true;

        [JsonPropertyName("num_images")]
        [Range(1, 4)]
        [Description("Number of images to generate.")]
        public int NumImages { get; set; } = 1;

        [JsonPropertyName("output_format")]
        [Required]
        [Description("Output format of the generated image (jpeg or png).")]
        public string OutputFormat { get; set; } = "jpeg";

        [JsonPropertyName("seed")]
        [Description("Optional seed for deterministic image generation.")]
        public long? Seed { get; set; }

        [JsonPropertyName("height")]
        [Required]
        [Range(64, 2048)]
        [Description("Height of the generated image (multiple of 32).")]
        public int Height { get; set; } = 768;

        [JsonPropertyName("width")]
        [Required]
        [Range(64, 2048)]
        [Description("Width of the generated image (multiple of 32).")]
        public int Width { get; set; } = 1024;

        [JsonPropertyName("filename")]
        [Required]
        [Description("Output filename without extension.")]
        public string Filename { get; set; } = default!;
    }

    private const string OPENAI_IMAGE_MODEL = "openai/gpt-image-1";

    [Description("Generate an image using the OpenAI GPT-Image-1 model via AI/ML API.")]
    [McpServerTool(Title = "Generate image with OpenAI GPT-Image-1",
          Name = "aiml_images_openai_create", Destructive = false)]
    public static async Task<CallToolResult?> AIMLImages_OpenAIImageCreate(
          [Description("Text prompt describing the image content, style, or composition.")]
        [MaxLength(32000)] string prompt,
          IServiceProvider serviceProvider,
          RequestContext<CallToolRequestParams> requestContext,
          [Description("Output format of the image (png, jpeg, or webp). Default: png")]
        string outputFormat = "png",
          [Description("Image size (1024x1024, 1024x1536, 1536x1024). Default: 1024x1024")]
        string size = "1024x1024",
          [Description("Quality of the generated image (low, medium, high). Default: medium")]
        string quality = "medium",
          [Description("Background type (transparent, opaque, auto). Default: auto")]
        string background = "auto",
          [Description("Output filename without extension. Defaults to autogenerated name.")]
        string? filename = null,
          CancellationToken cancellationToken = default)
          => await requestContext.WithExceptionCheck(async () =>
      {
          ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);

          var settings = serviceProvider.GetRequiredService<AIMLSettings>();
          var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
          var downloadService = serviceProvider.GetRequiredService<DownloadService>();

          var typed = new AIMLNewOpenAIImage
          {
              Prompt = prompt,
              OutputFormat = outputFormat,
              Size = size,
              Quality = quality,
              Background = background,
              Filename = filename?.ToOutputFileName() ?? requestContext.ToOutputFileName(outputFormat)
          };

          // JSON body
          var jsonBody = JsonSerializer.Serialize(new
          {
              model = OPENAI_IMAGE_MODEL,
              prompt = typed.Prompt,
              background = typed.Background,
              n = 1,
              output_compression = 100,
              output_format = typed.OutputFormat,
              quality = typed.Quality,
              size = typed.Size,
              response_format = "url"
          });

          using var client = clientFactory.CreateClient();
          using var request = new HttpRequestMessage(HttpMethod.Post, BASE_URL);
          request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
          request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
          request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

          using var resp = await client.SendAsync(request, cancellationToken);
          var jsonResponse = await resp.Content.ReadAsStringAsync(cancellationToken);
          if (!resp.IsSuccessStatusCode)
              throw new Exception($"{resp.StatusCode}: {jsonResponse}");

          using var doc = JsonDocument.Parse(jsonResponse);
          var url = doc.RootElement.GetProperty("data")[0].GetProperty("url").GetString();

          if (string.IsNullOrWhiteSpace(url))
              throw new Exception("No image data returned from OpenAI Image API.");

          // Download + upload
          var filesData = await downloadService.DownloadContentAsync(serviceProvider, requestContext.Server, url!, cancellationToken);
          var fileData = filesData.FirstOrDefault() ?? throw new Exception("Image download failed.");

          var uploaded = await requestContext.Server.Upload(
              serviceProvider,
              $"{typed.Filename}.{typed.OutputFormat}",
              fileData.Contents,
              cancellationToken);

          if (uploaded == null)
              throw new Exception("Upload failed.");

          return new CallToolResult()
          {
              Content =
              [
                  uploaded,
                new ImageContentBlock()
                {
                    Data = Convert.ToBase64String(fileData.Contents),
                    MimeType = "image/" + typed.OutputFormat
                }
              ]
          };
      });


    [Description("Edit an existing image using the OpenAI GPT-Image-1 model via AI/ML API.")]
    [McpServerTool(Title = "Edit image with OpenAI GPT-Image-1",
        Name = "aiml_images_openai_edit", Destructive = false)]
    public static async Task<CallToolResult?> AIMLImages_OpenAIImageEdit(
        [Description("Text prompt describing the desired edit or modification.")]
        [MaxLength(32000)] string prompt,
        [Description("Input image URLs. Local or SharePoint/OneDrive links supported.")]
        IEnumerable<string> imageUrls,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Optional mask image URL (transparent areas mark editable regions).")] string? maskUrl = null,
        [Description("Output format (png, jpeg, or webp). Default: png")] string outputFormat = "png",
        [Description("Background type (transparent, opaque, auto). Default: auto")] string background = "auto",
        [Description("Output filename without extension. Defaults to autogenerated name.")] string? filename = null,
        CancellationToken cancellationToken = default)
        => await requestContext.WithExceptionCheck(async () =>
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);

        var settings = serviceProvider.GetRequiredService<AIMLSettings>();
        var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        var attached = new List<FileItem>();
        foreach (var imageUrl in imageUrls)
        {
            var items = await downloadService.DownloadContentAsync(serviceProvider, requestContext.Server, imageUrl, cancellationToken);
            attached.AddRange(items);
        }

        FileItem? maskFile = null;
        if (!string.IsNullOrWhiteSpace(maskUrl))
        {
            var maskData = await downloadService.DownloadContentAsync(serviceProvider, requestContext.Server, maskUrl, cancellationToken);
            maskFile = maskData.FirstOrDefault();
        }

        if (attached.Count == 0)
            throw new Exception("No image input provided.");

        using var client = clientFactory.CreateClient();
        using var form = new MultipartFormDataContent();

        form.Add(new StringContent(OPENAI_IMAGE_MODEL), "model");
        form.Add(new StringContent(prompt), "prompt");
        form.Add(new StringContent(background), "background");
        form.Add(new StringContent(outputFormat), "output_format");
        form.Add(new StringContent("url"), "response_format");

        foreach (var file in attached)
        {
            var content = new ByteArrayContent(file.Contents.ToArray());
            content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(content, "image", Path.GetFileName(file.Filename ?? "input.png"));
        }

        if (maskFile != null)
        {
            var maskContent = new ByteArrayContent(maskFile.Contents.ToArray());
            maskContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(maskContent, "mask", "mask.png");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.aimlapi.com/v1/images/edits");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = form;

        using var resp = await client.SendAsync(request, cancellationToken);
        var jsonResponse = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode)
            throw new Exception($"{resp.StatusCode}: {jsonResponse}");

        using var doc = JsonDocument.Parse(jsonResponse);
        var url = doc.RootElement.GetProperty("data")[0].GetProperty("url").GetString();

        if (string.IsNullOrWhiteSpace(url))
            throw new Exception("No image data returned from OpenAI Edit API.");

        var filesData = await downloadService.DownloadContentAsync(serviceProvider, requestContext.Server, url!, cancellationToken);
        var fileData = filesData.FirstOrDefault() ?? throw new Exception("Image download failed.");

        var uploaded = await requestContext.Server.Upload(
            serviceProvider,
            $"{filename?.ToOutputFileName() ?? requestContext.ToOutputFileName(outputFormat)}.{outputFormat}",
            fileData.Contents,
            cancellationToken);

        if (uploaded == null)
            throw new Exception("Upload failed.");

        return new CallToolResult()
        {
            Content =
            [
                uploaded,
                new ImageContentBlock()
                {
                    Data = Convert.ToBase64String(fileData.Contents),
                    MimeType = "image/" + outputFormat
                }
            ]
        };
    });


    [Description("Please fill in the OpenAI image generation request details.")]
    public class AIMLNewOpenAIImage
    {
        [JsonPropertyName("prompt")]
        [Required]
        public string Prompt { get; set; } = default!;

        [JsonPropertyName("background")]
        public string Background { get; set; } = "auto";

        [JsonPropertyName("output_format")]
        public string OutputFormat { get; set; } = "png";

        [JsonPropertyName("quality")]
        public string Quality { get; set; } = "medium";

        [JsonPropertyName("size")]
        public string Size { get; set; } = "1024x1024";

        [JsonPropertyName("filename")]
        public string Filename { get; set; } = default!;
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ReveAspectRatio
    {
        [EnumMember(Value = "1:1")]
        Square,
        [EnumMember(Value = "3:2")]
        ThreeTwo,
        [EnumMember(Value = "2:3")]
        TwoThree,
        [EnumMember(Value = "4:3")]
        FourThree,
        [EnumMember(Value = "3:4")]
        ThreeFour,
        [EnumMember(Value = "16:9")]
        SixteenNine,
        [EnumMember(Value = "9:16")]
        NineSixteen
    }

    [Description("Please fill in the AI/ML Reve Create Image request details.")]
    public class AIMLNewReveCreateImage
    {
        [JsonPropertyName("prompt")]
        [Required]
        [MaxLength(2560)]
        [Description("The text prompt describing the content, style, or composition of the image.")]
        public string Prompt { get; set; } = default!;

        [JsonPropertyName("aspect_ratio")]
        [Required]
        [Description("The aspect ratio of the generated image.")]
        public ReveAspectRatio AspectRatio { get; set; } = ReveAspectRatio.ThreeTwo;

        [JsonPropertyName("convert_base64_to_url")]
        [Description("If true, returns the image URL instead of base64. Default true.")]
        public bool ConvertBase64ToUrl { get; set; } = true;

        [JsonPropertyName("output_format")]
        [Description("The format of the generated image (json, png, jpeg, webp). Default: json.")]
        public string OutputFormat { get; set; } = "json";

        [JsonPropertyName("filename")]
        [Required]
        [Description("Output filename without extension.")]
        public string Filename { get; set; } = default!;
    }

    [Description("Generate an image using the AI/ML Reve Create Image model.")]
    [McpServerTool(Title = "Generate image with AI/ML Reve Create",
        Name = "aiml_images_reve_create", Destructive = false)]
    public static async Task<CallToolResult?> AIMLImages_ReveCreate(
        [Description("Prompt describing the desired image content, style, or composition.")]
        [MaxLength(2560)] string prompt,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Output filename without extension. Defaults to autogenerated name.")]
        string? filename = null,
        CancellationToken cancellationToken = default)
        => await requestContext.WithExceptionCheck(async () =>
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);

        var settings = serviceProvider.GetRequiredService<AIMLSettings>();
        var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        // Step 1: Ask for user confirmation / missing params
        var (typed, notAccepted, _) = await requestContext.Server.TryElicit(
            new AIMLNewReveCreateImage
            {
                Prompt = prompt,
                Filename = filename?.ToOutputFileName() ?? requestContext.ToOutputFileName("png"),
                AspectRatio = ReveAspectRatio.ThreeTwo,
                ConvertBase64ToUrl = true,
                OutputFormat = "json"
            },
            cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typed == null) return "User input missing.".ToErrorCallToolResponse();

        // Step 2: Build JSON payload
        var jsonBody = JsonSerializer.Serialize(new
        {
            model = "reve/create-image",
            prompt = typed.Prompt,
            aspect_ratio = typed.AspectRatio.GetEnumMemberValue(),
            convert_base64_to_url = typed.ConvertBase64ToUrl,
            output_format = typed.OutputFormat
        });

        using var client = clientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, BASE_URL);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        using var resp = await client.SendAsync(request, cancellationToken);
        var jsonResponse = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode)
            throw new Exception($"{resp.StatusCode}: {jsonResponse}");

        using var doc = JsonDocument.Parse(jsonResponse);
        var root = doc.RootElement;

        // Step 3: Determine if URL or base64 was returned
        string? imageUrl = null;
        string? base64 = null;

        if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Array)
        {
            var item = dataElement[0];
            if (item.TryGetProperty("url", out var urlProp))
                imageUrl = urlProp.GetString();
            else if (item.TryGetProperty("b64_json", out var b64Prop))
                base64 = b64Prop.GetString();
        }

        if (imageUrl == null && base64 == null)
            throw new Exception("No image data returned from AI/ML Reve API.");

        BinaryData fileData;
        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            var files = await downloadService.DownloadContentAsync(serviceProvider, requestContext.Server, imageUrl, cancellationToken);
            var file = files.FirstOrDefault() ?? throw new Exception("Image download failed.");
            fileData = file.Contents;
        }
        else
        {
            var bytes = Convert.FromBase64String(base64!);
            fileData = BinaryData.FromBytes(bytes);
        }

        // Step 4: Upload to MCP storage
        var uploaded = await requestContext.Server.Upload(
            serviceProvider,
            $"{typed.Filename}.png",
            fileData,
            cancellationToken);

        if (uploaded == null)
            throw new Exception("Upload failed.");

        return new CallToolResult()
        {
            Content =
            [
                uploaded,
                new ImageContentBlock()
                {
                    Data = Convert.ToBase64String(fileData.ToArray()),
                    MimeType = "image/png"
                },
                new EmbeddedResourceBlock()
                {
                    Resource = new TextResourceContents()
                    {
                        MimeType = "application/json",
                        Text = doc.RootElement.ToJsonString(),
                        Uri = BASE_URL
                    }
                }
            ]
        };
    });

    [Description("Please fill in the AI/ML Reve Remix Edit Image request details.")]
    public class AIMLNewReveRemixEditImage
    {
        [JsonPropertyName("prompt")]
        [Required]
        [MaxLength(2560)]
        [Description("The text prompt describing the content, style, or composition of the remixed image.")]
        public string Prompt { get; set; } = default!;

        [JsonPropertyName("aspect_ratio")]
        [Description("The aspect ratio of the generated image.")]
        public ReveAspectRatio AspectRatio { get; set; } = ReveAspectRatio.ThreeTwo;

        [JsonPropertyName("convert_base64_to_url")]
        [Description("If true, returns the image URL instead of base64. Default true.")]
        public bool ConvertBase64ToUrl { get; set; } = true;

        [JsonPropertyName("output_format")]
        [Description("The format of the generated image (json, png, jpeg, webp). Default: json.")]
        public string OutputFormat { get; set; } = "json";

        [JsonPropertyName("filename")]
        [Required]
        [Description("Output filename without extension.")]
        public string Filename { get; set; } = default!;
    }

    [Description("Combine or remix multiple images using the AI/ML Reve Remix Edit Image model.")]
    [McpServerTool(Title = "Remix images with AI/ML Reve Remix Edit",
        Name = "aiml_images_reve_remix_edit", Destructive = false)]
    public static async Task<CallToolResult?> AIMLImages_ReveRemixEdit(
        [Description("Prompt describing how to remix or combine the images.")]
        [MaxLength(2560)] string prompt,
        [Description("List of input image URLs (1–4). Local or SharePoint/OneDrive links supported.")]
        IEnumerable<string> imageUrls,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Output filename without extension. Defaults to autogenerated name.")]
        string? filename = null,
        CancellationToken cancellationToken = default)
        => await requestContext.WithExceptionCheck(async () =>
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);
        if (imageUrls == null || !imageUrls.Any())
            throw new ArgumentException("At least one image URL must be provided.", nameof(imageUrls));

        var settings = serviceProvider.GetRequiredService<AIMLSettings>();
        var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        // Step 1: Ask for any missing parameters
        var (typed, notAccepted, _) = await requestContext.Server.TryElicit(
            new AIMLNewReveRemixEditImage
            {
                Prompt = prompt,
                Filename = filename?.ToOutputFileName() ?? requestContext.ToOutputFileName("png"),
                AspectRatio = ReveAspectRatio.ThreeTwo,
                ConvertBase64ToUrl = true,
                OutputFormat = "json"
            },
            cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typed == null) return "User input missing.".ToErrorCallToolResponse();

        // Step 2: Download all images and convert to base64
        var base64Images = new List<string>();
        foreach (var url in imageUrls.Take(4))
        {
            var data = await downloadService.DownloadContentAsync(serviceProvider, requestContext.Server, url, cancellationToken);
            foreach (var file in data)
                base64Images.Add(Convert.ToBase64String(file.Contents));
        }

        if (base64Images.Count == 0)
            throw new Exception("No image data could be downloaded for remix.");

        // Step 3: Build JSON payload
        var jsonBody = JsonSerializer.Serialize(new
        {
            model = "reve/remis-edit-image",
            prompt = typed.Prompt,
            image_urls = base64Images,
            aspect_ratio = typed.AspectRatio.GetEnumMemberValue(),
            convert_base64_to_url = typed.ConvertBase64ToUrl,
            output_format = typed.OutputFormat
        });

        using var client = clientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, BASE_URL);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        // Step 4: Send request
        using var resp = await client.SendAsync(request, cancellationToken);
        var jsonResponse = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode)
            throw new Exception($"{resp.StatusCode}: {jsonResponse}");

        using var doc = JsonDocument.Parse(jsonResponse);
        var root = doc.RootElement;

        // Step 5: Extract URL or base64
        string? imageUrlOut = null;
        string? base64 = null;

        if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Array)
        {
            var item = dataEl[0];
            if (item.TryGetProperty("url", out var urlProp))
                imageUrlOut = urlProp.GetString();
            else if (item.TryGetProperty("b64_json", out var b64Prop))
                base64 = b64Prop.GetString();
        }

        if (imageUrlOut == null && base64 == null)
            throw new Exception("No image data returned from AI/ML Reve Remix API.");

        BinaryData fileData;
        if (!string.IsNullOrWhiteSpace(imageUrlOut))
        {
            var files = await downloadService.DownloadContentAsync(serviceProvider, requestContext.Server, imageUrlOut, cancellationToken);
            var file = files.FirstOrDefault() ?? throw new Exception("Image download failed.");
            fileData = file.Contents;
        }
        else
        {
            var bytes = Convert.FromBase64String(base64!);
            fileData = BinaryData.FromBytes(bytes);
        }

        // Step 6: Upload to MCP storage
        var uploaded = await requestContext.Server.Upload(
            serviceProvider,
            $"{typed.Filename}.png",
            fileData,
            cancellationToken);

        if (uploaded == null)
            throw new Exception("Upload failed.");

        return new CallToolResult()
        {
            Content =
            [
                uploaded,
                new ImageContentBlock()
                {
                    Data = Convert.ToBase64String(fileData.ToArray()),
                    MimeType = "image/png"
                },
                new EmbeddedResourceBlock()
                {
                    Resource = new TextResourceContents()
                    {
                        MimeType = "application/json",
                        Text = doc.RootElement.ToJsonString(),
                        Uri = BASE_URL
                    }
                }
            ]
        };
    });

}



public class AIMLSettings
{
    public string ApiKey { get; set; } = default!;
}
