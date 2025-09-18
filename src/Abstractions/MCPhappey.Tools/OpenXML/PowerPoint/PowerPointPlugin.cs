using System.ComponentModel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
using MCPhappey.Tools.Extensions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using P = DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml;
using MCPhappey.Common.Extensions;

namespace MCPhappey.Tools.OpenXML.PowerPoint;

public static class PowerPointPlugin
{
    [Description("Add a new slide to a PowerPoint presentation")]
    [McpServerTool(Name = "openxml_powerpoint_add_slide", ReadOnly = false, OpenWorld = false, Idempotent = true, Destructive = true)]
    public static async Task<CallToolResult?> OpenXMLPowerPoint_AddSlide(
        string url,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default) => await requestContext.WithExceptionCheck(async () =>
    {
        using var graphClient = await serviceProvider.GetOboGraphClient(requestContext.Server);
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();
        var fileItems = await downloadService.DownloadContentAsync(serviceProvider, requestContext.Server, url, cancellationToken);
        var fileItem = fileItems.FirstOrDefault();

        var newBinary = AddBlankSlide(fileItem?.Contents!);

        var uploaded = await graphClient.UploadBinaryDataAsync(url, newBinary, cancellationToken);

        return uploaded?.ToResourceLinkBlock(uploaded?.Name!).ToCallToolResult();
    });

    [Description("Create a new PowerPoint presentation")]
    [McpServerTool(ReadOnly = false, OpenWorld = false, Idempotent = false, Destructive = false)]
    public static async Task<CallToolResult?> OpenXMLPowerPoint_NewPresentation(
        [Description("Filename without .pptx extension")] string fileName,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default) => await requestContext.WithExceptionCheck(async () =>
    {
        using var graphClient = await serviceProvider.GetOboGraphClient(requestContext.Server);

        using var stream = new MemoryStream();
        CreatePresentation(stream);

        var uploaded = await graphClient.Upload($"{fileName}.pptx", await BinaryData.FromStreamAsync(stream, cancellationToken), cancellationToken);

        return uploaded?.ToCallToolResult();
    });

    // 1) SLIDE VERWIJDEREN
    [Description("Remove a slide (by zero-based index) from a PowerPoint presentation")]
    [McpServerTool(ReadOnly = false, OpenWorld = false, Idempotent = false, Destructive = true)]
    public static async Task<CallToolResult?> OpenXMLPowerPoint_RemoveSlide(
        string url,
        int slideIndex,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default) => await requestContext.WithExceptionCheck(async () =>
    {
        using var graphClient = await serviceProvider.GetOboGraphClient(requestContext.Server);

        var downloadService = serviceProvider.GetRequiredService<DownloadService>();
        var fileItems = await downloadService.DownloadContentAsync(serviceProvider, requestContext.Server, url, cancellationToken);
        var fileItem = fileItems.FirstOrDefault();
        if (fileItem == null) return null;

        var newBinary = RemoveSlideByIndex(fileItem.Contents, slideIndex);
        var uploaded = await graphClient.UploadBinaryDataAsync(url, newBinary, cancellationToken);

        return uploaded?.ToResourceLinkBlock(uploaded?.Name!).ToCallToolResult(); ;
    });

    // 2) SLIDE VERPLAATSEN (REORDER)
    [Description("Move a slide from one index to another (zero-based) in a PowerPoint presentation")]
    [McpServerTool(ReadOnly = false, OpenWorld = false, Idempotent = false, Destructive = true)]
    public static async Task<CallToolResult?> OpenXMLPowerPoint_MoveSlide(
        string url,
        int fromIndex,
        int toIndex,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default) => await requestContext.WithExceptionCheck(async () =>
    {
        using var graphClient = await serviceProvider.GetOboGraphClient(requestContext.Server);

        var downloadService = serviceProvider.GetRequiredService<DownloadService>();
        var fileItems = await downloadService.DownloadContentAsync(serviceProvider, requestContext.Server, url, cancellationToken);
        var fileItem = fileItems.FirstOrDefault();
        if (fileItem == null) return null;

        var newBinary = ReorderSlides(fileItem.Contents, fromIndex, toIndex);
        var uploaded = await graphClient.UploadBinaryDataAsync(url, newBinary, cancellationToken);

        return uploaded?.ToResourceLinkBlock(uploaded?.Name!).ToCallToolResult(); ;
    });

    private static BinaryData ReorderSlides(BinaryData pptx, int fromIndex, int toIndex)
    {
        using var inStream = new MemoryStream(pptx.ToArray());
        using var outStream = new MemoryStream();
        inStream.CopyTo(outStream);
        outStream.Position = 0;

        using (var presentation = PresentationDocument.Open(outStream, true))
        {
            var presentationPart = presentation.PresentationPart ?? throw new InvalidDataException("No presentation part found.");
            var slideIdList = presentationPart.Presentation.SlideIdList ?? throw new InvalidDataException("No SlideIdList found.");

            var count = slideIdList.ChildElements.OfType<SlideId>().Count();
            if (count == 0) return new BinaryData(outStream.ToArray());

            if (fromIndex < 0 || fromIndex >= count) throw new ArgumentOutOfRangeException(nameof(fromIndex), $"Valid range: 0..{count - 1}");
            if (toIndex < 0 || toIndex >= count) throw new ArgumentOutOfRangeException(nameof(toIndex), $"Valid range: 0..{count - 1}");
            if (fromIndex == toIndex) return new BinaryData(outStream.ToArray());

            // Pak het te verplaatsen element
            var moving = (SlideId)slideIdList.ChildElements[fromIndex];

            // Verwijder eerst; indexen schuiven dan op
            slideIdList.RemoveChild(moving);

            // Als je naar een hogere index verplaatst, is doelindex nu -1
            if (toIndex > fromIndex) toIndex--;

            slideIdList.InsertAt(moving, toIndex);

            presentationPart.Presentation.Save();
        }

        return new BinaryData(outStream.ToArray());
    }

    private static BinaryData RemoveSlideByIndex(BinaryData pptx, int slideIndex)
    {
        using var inStream = new MemoryStream(pptx.ToArray());
        using var outStream = new MemoryStream();
        inStream.CopyTo(outStream);
        outStream.Position = 0;

        using (var presentation = PresentationDocument.Open(outStream, true))
        {
            var presentationPart = presentation.PresentationPart ?? throw new InvalidDataException("No presentation part found.");
            var slideIdList = presentationPart.Presentation.SlideIdList ?? throw new InvalidDataException("No SlideIdList found.");

            var slideIds = slideIdList.ChildElements.OfType<SlideId>().ToList();
            if (slideIndex < 0 || slideIndex >= slideIds.Count)
                throw new ArgumentOutOfRangeException(nameof(slideIndex), $"Valid range: 0..{slideIds.Count - 1}");

            var targetSlideId = slideIds[slideIndex];
            var relId = targetSlideId.RelationshipId!;
            var slidePart = (SlidePart)presentationPart.GetPartById(relId!);

            // 1) verwijder SlideId uit de lijst (volgorde bepaalt show-volgorde)
            slideIdList.RemoveChild(targetSlideId);

            // 2) delete de SlidePart (OpenXML zorgt voor subparts)
            presentationPart.DeletePart(slidePart);

            presentationPart.Presentation.Save();
        }

        return new BinaryData(outStream.ToArray());
    }



    private static BinaryData AddBlankSlide(BinaryData original)
    {
        // werk in één MemoryStream (geen aparte in/out streams nodig)
        var buffer = original.ToArray();
        using var ms = new MemoryStream();
        ms.Write(buffer, 0, buffer.Length);
        ms.Position = 0;

        using (var presentation = PresentationDocument.Open(ms, true))
        {
            var presentationPart = presentation.PresentationPart
                ?? throw new InvalidDataException("No presentation part found in PPTX.");

            // zorg dat SlideIdList bestaat
            presentationPart.Presentation.SlideIdList ??= new SlideIdList();

            // pak eerste layout als template
            var slideMasterPart = presentationPart.SlideMasterParts.FirstOrDefault()
                ?? throw new InvalidDataException("No SlideMasterPart found.");
            var slideLayoutPart = slideMasterPart.SlideLayoutParts.FirstOrDefault()
                ?? throw new InvalidDataException("No SlideLayoutPart found.");

            // maak nieuwe slidepart
            var newSlidePart = presentationPart.AddNewPart<SlidePart>();
            newSlidePart.Slide = new P.Slide(
                new P.CommonSlideData(
                    new P.ShapeTree(
                        new P.NonVisualGroupShapeProperties(
                            new P.NonVisualDrawingProperties { Id = 1U, Name = "Slide" },
                            new P.NonVisualGroupShapeDrawingProperties(),
                            new P.ApplicationNonVisualDrawingProperties()),
                        new P.GroupShapeProperties(new A.TransformGroup())
                    )
                )
            );

            // koppel layout aan nieuwe slide
            newSlidePart.AddPart(slideLayoutPart);

            // append nieuwe SlideId in de lijst
            var slideIdList = presentationPart.Presentation.SlideIdList!;
            uint maxSlideId = slideIdList.ChildElements
                .OfType<SlideId>()
                .Select(s => s.Id?.Value ?? 255U)
                .DefaultIfEmpty(255U)
                .Max();
            uint newSlideId = maxSlideId + 1U;
            var relId = presentationPart.GetIdOfPart(newSlidePart);
            slideIdList.Append(new SlideId { Id = newSlideId, RelationshipId = relId });

            // save
            presentationPart.Presentation.Save();
        }

        return new BinaryData(ms.ToArray());
    }

    public static void CreatePresentation(Stream stream)
    {
        using var presentationDoc =
            PresentationDocument.Create(stream, PresentationDocumentType.Presentation);
        // add the PresentationPart
        var presentationPart = presentationDoc.AddPresentationPart();
        presentationPart.Presentation = new Presentation();

        // add a SlideMasterPart and a SlideLayoutPart (very minimal)
        var slideMasterPart = presentationPart.AddNewPart<SlideMasterPart>();
        slideMasterPart.SlideMaster = new SlideMaster(
            new CommonSlideData(new ShapeTree()),
            new SlideLayoutIdList(),
            new TextStyles());
        slideMasterPart.SlideMaster.Save();

        var slideLayoutPart = slideMasterPart.AddNewPart<SlideLayoutPart>();
        slideLayoutPart.SlideLayout = new SlideLayout(
            new CommonSlideData(new ShapeTree()));
        slideLayoutPart.SlideLayout.Save();

        // add a SlidePart using the layout
        var slidePart = presentationPart.AddNewPart<SlidePart>();
        slidePart.Slide = new Slide(
            new CommonSlideData(new ShapeTree()));
        slidePart.AddPart(slideLayoutPart);
        slidePart.Slide.Save();

        // wire slide into presentation
        presentationPart.Presentation.SlideIdList = new SlideIdList();
        var id = presentationPart.GetIdOfPart(slidePart);
        presentationPart.Presentation.SlideIdList.Append(
            new SlideId() { Id = 256U, RelationshipId = id });
        presentationPart.Presentation.Save();
    }

}
