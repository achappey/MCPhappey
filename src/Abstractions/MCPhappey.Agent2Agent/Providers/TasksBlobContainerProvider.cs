
using Azure.Storage.Blobs;

namespace MCPhappey.Agent2Agent.Providers;

public interface ITasksBlobContainerProvider
{
    BlobContainerClient Client { get; }
}


public class TasksBlobContainerProvider : ITasksBlobContainerProvider
{
    public BlobContainerClient Client { get; }
    public TasksBlobContainerProvider(BlobServiceClient blobServiceClient, string container)
    {
        Client = blobServiceClient.GetBlobContainerClient(container);
        Client.CreateIfNotExists();
    }
}
