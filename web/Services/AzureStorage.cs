using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace BricksAndHearts.Services
{
    public interface IAzureStorage
    {
        Task CreateContainerAsync(string varType, int id);
        Task DeleteContainerAsync(string varType, int id);
        Task UploadFileAsync(IFormFile file, string varType, int id);
        Task<List<string>> ListFilesAsync(string varType, int id);
        Task<Stream> DownloadFileAsync(string varType, int id, string blobFilename);
        Task DeleteFileAsync(string varType, int id, string blobFilename);
    }

    public class AzureStorage : IAzureStorage
    {
        private readonly string _storageConnectionString;
        private readonly string _baseContainerName;
        private readonly ILogger<AzureStorage> _logger;

        public AzureStorage(IConfiguration configuration, ILogger<AzureStorage> logger)
        {
            _storageConnectionString = configuration.GetValue<string>("BlobConnectionString");
            _baseContainerName = configuration.GetValue<string>("BlobContainerName");
            _logger = logger;
        }

        private string GetContainerName(string varType, int id)
        {
            return _baseContainerName + varType + id.ToString();
        }

        private BlobContainerClient GetContainerClient(string containerName)
        {
            BlobContainerClient containerClient = new BlobContainerClient(_storageConnectionString, containerName);
            if (!containerClient.Exists())
            {
                throw new Exception($"Container {containerName} does not exist");
            }
            return containerClient;
        }

        private BlobClient GetBlobClient(BlobContainerClient containerClient, string blobName)
        {
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            if (!blobClient.Exists())
            {
                throw new Exception($"Blob {blobName} does not exist");
            }
            return blobClient;
        }
        
        public async Task CreateContainerAsync(string varType, int id)
        {
            string containerName = GetContainerName(varType, id);
            BlobServiceClient blobServiceClient = new BlobServiceClient(_storageConnectionString);
            var container = blobServiceClient.GetBlobContainerClient(containerName);
            if (container.Exists())
            {
                await DeleteContainerAsync(varType, id);
            }
            await blobServiceClient.CreateBlobContainerAsync(containerName);
        }
        
        public async Task DeleteContainerAsync(string varType, int id)
        {
            string containerName = GetContainerName(varType, id);
            var containerClient = GetContainerClient(containerName);
            await containerClient.DeleteAsync();
        }

        public async Task UploadFileAsync(IFormFile blob, string varType, int id)
        {
            string containerName = GetContainerName(varType, id);
            var containerClient = GetContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blob.FileName);
            if (blobClient.Exists())
            {
                await DeleteFileAsync(varType, id, blob.FileName);
            }
            /*while (blobClient.Exists())
            {
                await using (var stream = File.OpenRead(blob.FileName))
                {
                    FormFile newBlob = new FormFile(stream, 0, stream.Length, null, blob.FileName + "0")
                    {
                        Headers = new HeaderDictionary(),
                        ContentType = "image/jpeg"
                    };
                    blob = newBlob;
                    blobClient = containerClient.GetBlobClient(newBlob.FileName);
                }
            }*/
            await using (Stream? data = blob.OpenReadStream())
            {
                await blobClient.UploadAsync(data);
            }
        }

        public async Task<List<string>> ListFilesAsync(string varType, int id)
        {
            string containerName = GetContainerName(varType, id);
            var containerClient = GetContainerClient(containerName);
            List<string> fileNames = new List<string>();
            fileNames.Add(id.ToString());
            await foreach (BlobItem file in containerClient.GetBlobsAsync())
            {
                fileNames.Add(file.Name);
            }
            return fileNames;
        }

        public async Task<Stream> DownloadFileAsync(string varType, int id, string blobName)
        {
            string containerName = GetContainerName(varType, id);
            var containerClient = GetContainerClient(containerName);
            var blobClient = GetBlobClient(containerClient, blobName);
            Stream data = await blobClient.OpenReadAsync();
            return data;
        }

        public async Task DeleteFileAsync(string varType, int id, string blobName)
        {
            string containerName = GetContainerName(varType, id);
            var containerClient = GetContainerClient(containerName);
            var blobClient = GetBlobClient(containerClient, blobName);
            await blobClient.DeleteAsync();
        }
    }
}