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
        Task<(Stream data, string fileType)> DownloadFileAsync(string varType, int id, string blobFilename);
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

        private async Task<BlobContainerClient> GetContainerClient(string varType, int id)
        {
            string containerName = GetContainerName(varType, id);
            BlobContainerClient containerClient = new BlobContainerClient(_storageConnectionString, containerName);
            if (!containerClient.Exists())
            {
                await CreateContainerAsync(varType, id);
                containerClient = new BlobContainerClient(_storageConnectionString, containerName);
            }
            return containerClient;
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
            var containerClient = await GetContainerClient(varType, id);
            await containerClient.DeleteAsync();
        }

        public async Task UploadFileAsync(IFormFile blob, string varType, int id)
        {
            var containerClient = await GetContainerClient(varType, id);
            var blobClient = containerClient.GetBlobClient(blob.FileName);
            int i = 0;
            string fileName = SplitFileName(blob.FileName).name;
            while (blobClient.Exists())
            {
                i += 1;
                fileName = SplitFileName(blob.FileName).name + i + "." + SplitFileName(blob.FileName).type;
                blobClient = containerClient.GetBlobClient(fileName);
            }
            await using (Stream? data = blob.OpenReadStream())
            {
                await blobClient.UploadAsync(data);
            }
        }

        public async Task<List<string>> ListFilesAsync(string varType, int id)
        {
            var containerClient = await GetContainerClient(varType, id);
            List<string> fileNames = new List<string>();
            fileNames.Add(id.ToString());
            await foreach (BlobItem file in containerClient.GetBlobsAsync())
            {
                fileNames.Add(file.Name);
            }
            return fileNames;
        }

        public async Task<(Stream, string)> DownloadFileAsync(string varType, int id, string blobName)
        {
            var containerClient = await GetContainerClient(varType, id);
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            if (!blobClient.Exists())
            {
                var newContainerClient = new BlobContainerClient(_storageConnectionString, "default");
                blobClient = newContainerClient.GetBlobClient("error.png");
            }
            Stream data = await blobClient.OpenReadAsync();
            string fileType = SplitFileName(blobName).type;
            return (data, fileType);
        }

        public async Task DeleteFileAsync(string varType, int id, string blobName)
        {
            var containerClient = await GetContainerClient(varType, id);
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            if (blobClient.Exists())
            {
                await blobClient.DeleteAsync();
            }
        }

        private (string name, string type) SplitFileName(string fileName)
        {
            //return Path.GetExtension(fileName);
            int fileTypeIndexStart = fileName.LastIndexOfAny(new char[] {'.'});
            return (fileName.Substring(0, fileTypeIndexStart), fileName.Substring(fileTypeIndexStart + 1));
        }
    }
}