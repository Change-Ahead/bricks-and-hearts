using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using AzureBlobStorage.WebApi.Models;

namespace BricksAndHearts.Services
{
    public interface IAzureStorage
    {
        Task CreateContainerAsync(string varType, int id);
        Task UploadFileAsync(IFormFile file, string varType, int id);
        Task<BlobDto> DownloadFileAsync(string blobFilename);
        //Task<BlobResponseDto> DeleteAsync(string blobFilename);
        //Task<List<BlobDto>> ListAsync();
    }

    public class AzureStorage : IAzureStorage
    {
        private readonly string _storageConnectionString;
        private readonly string _storageContainerName;
        private readonly ILogger<AzureStorage> _logger;

        public AzureStorage(IConfiguration configuration, ILogger<AzureStorage> logger)
        {
            _storageConnectionString = configuration.GetValue<string>("BlobConnectionString");
            _storageContainerName = configuration.GetValue<string>("BlobContainerName");
            _logger = logger;
        }

        private string GetContainerName(string varType, int id)
        {
            return varType + id.ToString();
        }

        public async Task CreateContainerAsync(string varType, int id)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(_storageConnectionString);
            string containerName = GetContainerName(varType, id); // + Guid.NewGuid();
            await blobServiceClient.CreateBlobContainerAsync(containerName);
        }

        public async Task UploadFileAsync(IFormFile blob, string varType, int id)
        {
            string containerName = GetContainerName(varType, id);
            BlobContainerClient container = new BlobContainerClient(_storageConnectionString, containerName);
            try
            {
                BlobClient client = container.GetBlobClient(blob.FileName);
                await using (Stream? data = blob.OpenReadStream())
                {
                    await client.UploadAsync(data);
                }
            }
            // If the file already exists, we catch the exception and do not upload it
            //TODO deal with duplicate file names
            catch (RequestFailedException ex)
                when (ex.ErrorCode == BlobErrorCode.BlobAlreadyExists)
            {
                //response.Status = $"File with name {blob.FileName} already exists. Please use another name to store your file.";
            }
        }
        
        public async Task<BlobDto> DownloadFileAsync(string blobFilename)
        {
            string containerName = GetContainerName("property", 0);
            BlobContainerClient client = new BlobContainerClient(_storageConnectionString, "test");
            try
            {
                BlobClient file = client.GetBlobClient("jpegtest.jfif");
                if (await file.ExistsAsync())
                {
                    var data = await file.OpenReadAsync();
                    Stream blobContent = data;
                    var content = await file.DownloadContentAsync();
                    string name = blobFilename;
                    string contentType = content.Value.Details.ContentType;
                    return new BlobDto { Content = blobContent, Name = name, ContentType = contentType };
                }
            }
            catch (RequestFailedException ex)
                when(ex.ErrorCode == BlobErrorCode.BlobNotFound)
            {
                // Log error to console
                //_logger.LogError($"File {blobFilename} was not found.");
            }
            // File does not exist, return null and handle that in requesting method
            return null;
        }
    }
}