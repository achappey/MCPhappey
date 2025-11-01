using System.ComponentModel;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
using MCPhappey.Tools.AIML.Images;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.Pipeline;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.AIML.Vision;

public static class AIMLVision
{
    private static readonly string OCR_BASE_URL = "https://api.aimlapi.com/v1/ocr";
    private static readonly string VISION_BASE_URL = "https://api.aimlapi.com/v1/ocr";

    [Description("Extracts structured text from documents or images using Google's Document AI OCR model. Supports PDFs, scans, and SharePoint/OneDrive file URLs.")]
    [McpServerTool(Title = "AI/ML Google OCR extraction",
              Name = "aiml_vision_google_extract", Destructive = false, ReadOnly = true)]
    public static async Task<CallToolResult?> AIMLVision_GoogleExtract(
            [Description("Input file URL. Protected SharePoint and/or OneDrive links are supported.")] string fileUrl,
            IServiceProvider serviceProvider,
            RequestContext<CallToolRequestParams> requestContext,
             CancellationToken cancellationToken = default)
             => await requestContext.WithExceptionCheck(async () =>
            await requestContext.WithStructuredContent(async () =>
          {

              var settings = serviceProvider.GetRequiredService<AIMLSettings>();
              var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
              var downloadService = serviceProvider.GetRequiredService<DownloadService>();
              var files = await downloadService.DownloadContentAsync(serviceProvider, requestContext.Server, fileUrl, cancellationToken);
              var file = files.FirstOrDefault() ?? throw new Exception("File not found");

              // Step 2: Build JSON payload
              var jsonBody = JsonSerializer.Serialize(new
              {
                  model = "google/gc-document-ai",
                  mimeType = file.MimeType,
                  document = Convert.ToBase64String(file.Contents)
              });

              using var client = clientFactory.CreateClient();
              using var request = new HttpRequestMessage(HttpMethod.Post, OCR_BASE_URL);
              request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
              request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MimeTypes.Json));
              request.Content = new StringContent(jsonBody, Encoding.UTF8, MimeTypes.Json);

              // Step 3: Send request
              using var resp = await client.SendAsync(request, cancellationToken);
              var jsonResponse = await resp.Content.ReadAsStreamAsync(cancellationToken);
              if (!resp.IsSuccessStatusCode)
                  throw new Exception($"{resp.StatusCode}: {jsonResponse}");

              return await JsonNode.ParseAsync(jsonResponse, cancellationToken: cancellationToken);

          }));


    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum VisionFeatureType
    {
        [EnumMember(Value = "FACE_DETECTION")]
        FaceDetection,

        [EnumMember(Value = "LANDMARK_DETECTION")]
        LandmarkDetection,

        [EnumMember(Value = "LOGO_DETECTION")]
        LogoDetection,

        [EnumMember(Value = "LABEL_DETECTION")]
        LabelDetection,

        [EnumMember(Value = "TEXT_DETECTION")]
        TextDetection,

        [EnumMember(Value = "DOCUMENT_TEXT_DETECTION")]
        DocumentTextDetection,

        [EnumMember(Value = "SAFE_SEARCH_DETECTION")]
        SafeSearchDetection,

        [EnumMember(Value = "IMAGE_PROPERTIES")]
        ImageProperties,

        [EnumMember(Value = "CROP_HINTS")]
        CropHints,

        [EnumMember(Value = "WEB_DETECTION")]
        WebDetection,

        [EnumMember(Value = "PRODUCT_SEARCH")]
        ProductSearch,

        [EnumMember(Value = "OBJECT_LOCALIZATION")]
        ObjectLocalization
    }

    [Description("Identifies visual features such as objects, faces, landmarks, or logos in images.")]
    [McpServerTool(
  Title = "AI/ML Vision Optical Feature Recognition",
  Name = "aiml_vision_optical_feature_recognition",
  Destructive = false,
  ReadOnly = true
)]
    public static async Task<CallToolResult?> AIMLVision_OpticalFeatureRecognition(
  [Description("Image URL to analyze. SharePoint and OneDrive links are supported.")]
        string imageUrl,
  [Description("Feature type to detect (e.g. OBJECT_LOCALIZATION, LOGO_DETECTION, FACE_DETECTION).")]
        VisionFeatureType featureType,
  [Description("Maximum number of results to return per feature type.")]
        int maxResults,
  IServiceProvider serviceProvider,
  RequestContext<CallToolRequestParams> requestContext,
  CancellationToken cancellationToken = default)
  => await requestContext.WithExceptionCheck(async () =>
  await requestContext.WithStructuredContent(async () =>
  {
      var settings = serviceProvider.GetRequiredService<AIMLSettings>();
      var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
      var downloadService = serviceProvider.GetRequiredService<DownloadService>();
      var files = await downloadService.DownloadContentAsync(serviceProvider, requestContext.Server, imageUrl, cancellationToken);
      var file = files.FirstOrDefault() ?? throw new Exception("Image not found");

      // Step 1: Prepare JSON body
      var jsonBody = JsonSerializer.Serialize(new
      {
          image = new
          {
              content = Convert.ToBase64String(file.Contents)
          },
          features = new[]
          {
                    new
                    {
                        type = featureType,
                        maxResults,
                        model = "builtin/latest"
                    }
          }
      });

      // Step 2: HTTP request
      using var client = clientFactory.CreateClient();
      using var request = new HttpRequestMessage(HttpMethod.Post, VISION_BASE_URL);
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
      request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
      request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

      // Step 3: Send request
      using var resp = await client.SendAsync(request, cancellationToken);
      var jsonResponse = await resp.Content.ReadAsStreamAsync(cancellationToken);
      if (!resp.IsSuccessStatusCode)
          throw new Exception($"{resp.StatusCode}: {await resp.Content.ReadAsStringAsync(cancellationToken)}");

      return await JsonNode.ParseAsync(jsonResponse, cancellationToken: cancellationToken);
  }));
}
