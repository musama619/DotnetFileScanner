using Models;

namespace Services.FileScanService
{
    public interface IFileScanService
    {
        Task<FileScanResult> ScanAsync(string file, int timeoutInMs = 30000, CancellationToken cancellationToken = default);
    }
}
