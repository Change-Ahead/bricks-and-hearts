using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BricksAndHearts.ViewModels;

namespace BricksAndHearts.Services
{
    public interface IAzureStorage
    {
        Task DeleteContainer(string varType, int id);
        Task<string> UploadFile(IFormFile file, string varType, int id);
        Task<ImageListViewModel> ListFiles(string varType, int id);
        Task<(Stream? data, string? fileType)> DownloadFile(string varType, int id, string blobFilename);
        Task DeleteFile(string varType, int id, string blobFilename);
        public bool IsImage(string fileName);
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

        public async Task<string> UploadFile(IFormFile blob, string varType, int id)
        {
            var containerClient = await GetOrCreateContainerClient(varType, id);
            var blobClient = containerClient.GetBlobClient(blob.FileName);
            string fileName = blob.FileName;
            int i = 0;
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

            if (blob.FileName != fileName)
            {
                return $"Successfully uploaded {blob.FileName} with name {fileName}";
            }

            return $"Successfully uploaded {blob.FileName}";
        }

        public async Task<ImageListViewModel> ListFiles(string varType, int id)
        {
            var containerClient = await GetOrCreateContainerClient(varType, id);
            List<string> fileNames = new List<string>();
            await foreach (BlobItem file in containerClient.GetBlobsAsync())
            {
                fileNames.Add(file.Name);
            }
            ImageListViewModel imageList = new ImageListViewModel()
            {
                PropertyId = id,
                FileList = fileNames
            };
            return imageList;
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
        
        public bool IsImage(string fileName)
        {
            string fileType = SplitFileName(fileName).type;
            List<string> imageExtensions = new List<string> { "jpg", "jpeg", "png", "bmp", "gif" };
            if (imageExtensions.Contains(fileType))
            {
                return true;
            }
            return false;
        }

        private (string name, string type) SplitFileName(string fileName)
        {
            int fileTypeIndexStart = fileName.LastIndexOfAny(new char[] {'.'});
            return (fileName.Substring(0, fileTypeIndexStart), fileName.Substring(fileTypeIndexStart + 1));
        }
    }
}