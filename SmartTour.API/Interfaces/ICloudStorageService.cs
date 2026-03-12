namespace SmartTour.API.Interfaces;

public interface ICloudStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
    Task<string> UploadBytesAsync(byte[] data, string fileName, string contentType);
}
