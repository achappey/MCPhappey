
using Azure.Storage.Blobs;

namespace MCPhappey.Agent2Agent.Providers;

public interface IContextsBlobContainerProvider
{
    BlobContainerClient Client { get; }
}

public class ContextsBlobContainerProvider : IContextsBlobContainerProvider
{
    public BlobContainerClient Client { get; }
    public ContextsBlobContainerProvider(BlobServiceClient blobServiceClient, string container)
    {
        Client = blobServiceClient.GetBlobContainerClient(container);
        Client.CreateIfNotExists();
    }
}
