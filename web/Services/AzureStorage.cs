using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace BricksAndHearts.Services
{
    public interface IAzureStorage
    {
        Task DeleteContainer(string varType, int id);
        Task UploadFile(IFormFile file, string varType, int id);
        Task<List<string>> ListFiles(string varType, int id);
        Task<(Stream? data, string? fileType)> DownloadFile(string varType, int id, string blobFilename);
        Task DeleteFile(string varType, int id, string blobFilename);
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

        private async Task<BlobContainerClient> GetOrCreateContainerClient(string varType, int id)
        {
            string containerName = GetContainerName(varType, id);
            BlobServiceClient blobServiceClient = new BlobServiceClient(_storageConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            if (!containerClient.Exists())
            {
                await blobServiceClient.CreateBlobContainerAsync(containerName);
                containerClient = new BlobContainerClient(_storageConnectionString, containerName);
            }
            return containerClient;
        }

        public async Task DeleteContainer(string varType, int id)
        {
            var containerClient = await GetOrCreateContainerClient(varType, id);
            await containerClient.DeleteAsync();
        }

        public async Task UploadFile(IFormFile blob, string varType, int id)
        {
            var containerClient = await GetOrCreateContainerClient(varType, id);
            var blobClient = containerClient.GetBlobClient(blob.FileName);
            int i = 0;
            while (blobClient.Exists())
            {
                i += 1;
                string fileName = SplitFileName(blob.FileName).name + i + "." + SplitFileName(blob.FileName).type;
                blobClient = containerClient.GetBlobClient(fileName);
            }
            await using (Stream? data = blob.OpenReadStream())
            {
                await blobClient.UploadAsync(data);
            }
        }

        public async Task<List<string>> ListFiles(string varType, int id)
        {
            var containerClient = await GetOrCreateContainerClient(varType, id);
            List<string> fileNames = new List<string>();
            fileNames.Add(id.ToString());
            await foreach (BlobItem file in containerClient.GetBlobsAsync())
            {
                fileNames.Add(file.Name);
            }
            return fileNames;
        }

        public async Task<(Stream?, string?)> DownloadFile(string varType, int id, string blobName)
        {
            var containerClient = await GetOrCreateContainerClient(varType, id);
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            if (!blobClient.Exists())
            {
                return (null, null);
            }
            Stream data = await blobClient.OpenReadAsync();
            string fileType = SplitFileName(blobName).type;
            return (data, fileType);
        }

        public async Task DeleteFile(string varType, int id, string blobName)
        {
            var containerClient = await GetOrCreateContainerClient(varType, id);
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            if (blobClient.Exists())
            {
                await blobClient.DeleteAsync();
            }
        }

        private (string name, string type) SplitFileName(string fileName)
        {
            int fileTypeIndexStart = fileName.LastIndexOfAny(new char[] {'.'});
            return (fileName.Substring(0, fileTypeIndexStart), fileName.Substring(fileTypeIndexStart + 1));
        }
    }
}