using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using AzureBlobStorage.WebApi.Models;

namespace BricksAndHearts.Services
{
    public interface IAzureStorage
    {
        Task<BlobResponseDto> UploadAsync(IFormFile file);
        //Task<BlobDto> DownloadAsync(string blobFilename);
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
            _storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=bnhpropertyimages;AccountKey=6kjyiLjh7FBBr/G+vCf9mWiGDdbsAuHjCXfj7mrsl06ai5H7uRMhPMHlxqx6JxknsBfqY8pMyKI4+AStAOwVHQ==;EndpointSuffix=core.windows.net";
            _storageContainerName = "test";
            _logger = logger;
        }
        
        public async Task<BlobResponseDto> UploadAsync(IFormFile blob)
        {
            BlobResponseDto response = new();
            BlobContainerClient container = new BlobContainerClient(_storageConnectionString, _storageContainerName);
            try
            {
                BlobClient client = container.GetBlobClient(blob.FileName);
                await using (Stream? data = blob.OpenReadStream())
                {
                    await client.UploadAsync(data);
                }
            }
            // If the file already exists, we catch the exception and do not upload it
            catch (RequestFailedException ex)
                when (ex.ErrorCode == BlobErrorCode.BlobAlreadyExists)
            {
                //response.Status = $"File with name {blob.FileName} already exists. Please use another name to store your file.";
                //response.Error = true;
                return response;
            }
            return response;
        }
    }
}