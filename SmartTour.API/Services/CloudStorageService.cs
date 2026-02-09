using Google.Cloud.Storage.V1;
using Google.Apis.Auth.OAuth2;

namespace SmartTour.API.Services;

public interface ICloudStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
    Task<string> UploadBytesAsync(byte[] data, string fileName, string contentType);
}

public class CloudStorageService : ICloudStorageService
{
    private readonly StorageClient _storageClient;
    private readonly string _bucketName;

    public CloudStorageService(IConfiguration config, IWebHostEnvironment env)
    {
        _bucketName = config["GOOGLE_STORAGE_BUCKET"] ?? "smart-tour-storage";
        var credentialPath = Path.Combine(env.ContentRootPath, "google-credentials.json");
        
        if (File.Exists(credentialPath))
        {
            Console.WriteLine($"[GCS] Using credential file at: {credentialPath}");
            using var stream = new System.IO.FileStream(credentialPath, FileMode.Open, FileAccess.Read);
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var credential = GoogleCredential.FromJson(json);
            _storageClient = StorageClient.Create(credential);
        }
        else
        {
            Console.WriteLine("[GCS] WARNING: google-credentials.json not found. Using Application Default Credentials.");
            _storageClient = StorageClient.Create();
        }
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        try {
            Console.WriteLine($"[GCS] Starting upload: {fileName} into bucket {_bucketName} (Type: {contentType})");
            var data = await _storageClient.UploadObjectAsync(_bucketName, fileName, contentType, fileStream);
            var url = $"https://storage.googleapis.com/{_bucketName}/{fileName}";
            Console.WriteLine($"[GCS] Upload success: {url}");
            return url;
        } catch (Exception ex) {
            Console.WriteLine($"[GCS] UPLOAD ERROR (File): {ex.Message}");
            if (ex.InnerException != null) Console.WriteLine($"[GCS] Inner Error: {ex.InnerException.Message}");
            throw;
        }
    }

    public async Task<string> UploadBytesAsync(byte[] data, string fileName, string contentType)
    {
        try {
            Console.WriteLine($"[GCS] Starting byte upload: {fileName} into bucket {_bucketName} (Size: {data.Length} bytes)");
            using var stream = new MemoryStream(data);
            await _storageClient.UploadObjectAsync(_bucketName, fileName, contentType, stream);
            var url = $"https://storage.googleapis.com/{_bucketName}/{fileName}";
            Console.WriteLine($"[GCS] Byte upload success: {url}");
            return url;
        } catch (Exception ex) {
            Console.WriteLine($"[GCS] UPLOAD ERROR (Bytes): {ex.Message}");
            if (ex.InnerException != null) Console.WriteLine($"[GCS] Inner Error: {ex.InnerException.Message}");
            throw;
        }
    }
}
