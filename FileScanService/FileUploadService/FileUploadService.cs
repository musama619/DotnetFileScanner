using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Services.FileScanService;
using Services.FileValidationService;

namespace Services.FileUploadService
{
    /// <summary>
    /// Service for handling file uploads, including validation, scanning, and storage.
    /// </summary>
    public class FileUploadService : IFileUploadService
    {
        private readonly IOptions<AttachmentConfiguration> _attachmentConfig;
        private readonly IFileScanService _fileScanService;
        private readonly IFileValidatorService _fileValidatorService;
        private readonly ILogger<FileUploadService> _logger;

        public FileUploadService(
            IOptions<AttachmentConfiguration> attachmentConfig,
            IFileScanService fileScanService,
            IFileValidatorService fileValidatorService,
            ILogger<FileUploadService> logger)
        {
            _attachmentConfig = attachmentConfig ?? throw new ArgumentNullException(nameof(attachmentConfig));
            _fileScanService = fileScanService ?? throw new ArgumentNullException(nameof(fileScanService));
            _fileValidatorService = fileValidatorService ?? throw new ArgumentNullException(nameof(fileValidatorService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            ValidateConfiguration();
        }

        public async Task<string> UploadFile(IFormFile file, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting file upload process for file: {FileName}, Size: {FileSize}",
                file.FileName, file.Length);

            ValidateFile(file);

            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            var uniqueFileName = Guid.NewGuid() + fileExtension;
            var tempPath = Path.Combine(_attachmentConfig.Value.TemporaryPath, uniqueFileName);

            try
            {
                await ValidateFileContent(file, tempPath, cancellationToken);

                await SaveFileToTemporaryLocation(file, tempPath, cancellationToken);

                var scanResult = await _fileScanService.ScanAsync(tempPath, cancellationToken: cancellationToken);
                return HandleScanResult(scanResult, tempPath, uniqueFileName);
            }
            finally
            {
                CleanupTemporaryFile(tempPath);
            }
        }

        private void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_attachmentConfig.Value.TemporaryPath))
                throw new InvalidOperationException("TemporaryPath is not configured.");

            if (string.IsNullOrWhiteSpace(_attachmentConfig.Value.AttachmentPath))
                throw new InvalidOperationException("AttachmentPath is not configured.");
        }

        private void ValidateFile(IFormFile file)
        {
            if (file.Length == 0)
                throw new ArgumentException("The uploaded file is empty.");

            if (file.Length > _attachmentConfig.Value.MaxFileSize)
                throw new InvalidOperationException("File exceeds maximum allowed size.");

            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!_attachmentConfig.Value.AllowedFileTypes.Contains(fileExtension))
                throw new InvalidOperationException("The file type is not allowed for upload.");

        }

        private async Task ValidateFileContent(IFormFile file, string tempPath, CancellationToken cancellationToken)
        {
            byte[] fileBytes;

            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms, cancellationToken);
                fileBytes = ms.ToArray();
            }

            if (!_fileValidatorService.Validate(fileBytes, file.FileName))
            {
                _logger.LogWarning("File content validation failed for: {FileName}", file.FileName);
                throw new InvalidOperationException("The file content does not match its extension or is potentially malicious.");
            }

            _logger.LogInformation("File content validation passed for: {FileName}", file.FileName);
        }

        private async Task SaveFileToTemporaryLocation(IFormFile file, string tempPath, CancellationToken cancellationToken)
        {
            await using var tempFileStream = new FileStream(tempPath, FileMode.Create);
            await file.CopyToAsync(tempFileStream, cancellationToken);
        }

        private string HandleScanResult(FileScanResult scanResult, string tempPath, string uniqueFileName)
        {
            switch (scanResult)
            {
                case FileScanResult.NoThreatFound:
                    var permanentPath = Path.Combine(_attachmentConfig.Value.AttachmentPath, uniqueFileName);
                    File.Move(tempPath, permanentPath);
                    return uniqueFileName;

                case FileScanResult.ThreatFound:
                    throw new InvalidOperationException("The uploaded file contains a potential security threat.");

                case FileScanResult.FileNotFound:
                    throw new FileNotFoundException("File not found during scanning.", uniqueFileName);

                default:
                    throw new InvalidOperationException("An unexpected error occurred while processing the file.");
            }
        }

        private void CleanupTemporaryFile(string tempPath)
        {
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary file: {TempPath}", tempPath);
                }
            }
        }
       
    }

}
