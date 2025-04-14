using Microsoft.AspNetCore.Http;

namespace Services.FileUploadService
{
    public interface IFileUploadService
    {
        Task<string> UploadFile(IFormFile file, CancellationToken cancellationToken);
    }
}
