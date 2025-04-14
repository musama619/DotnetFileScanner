using Microsoft.Extensions.Options;
using Models;
using Moq;
using Services.FileScanService;
using Services.FileValidationService;
using Services.FileUploadService;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace DotnetFileScanner.Test
{
    public class FileUploadServiceTests
    {
        private readonly Mock<IOptions<AttachmentConfiguration>> _mockAttachmentConfig;
        private readonly Mock<IFileScanService> _mockFileScanService;
        private readonly Mock<IFileValidatorService> _mockFileValidatorService;
        private readonly Mock<ILogger<FileUploadService>> _mockLogger;
        private readonly FileUploadService _fileUploadService;

        public FileUploadServiceTests()
        {
            _mockAttachmentConfig = new Mock<IOptions<AttachmentConfiguration>>();
            _mockFileScanService = new Mock<IFileScanService>();
            _mockFileValidatorService = new Mock<IFileValidatorService>();
            _mockLogger = new Mock<ILogger<FileUploadService>>();

            var attachmentConfig = new AttachmentConfiguration
            {
                AttachmentPath = "C:\\Temp",
                TemporaryPath = "C:\\Temp\\temp",
                MaxFileSize = 1048576,
                AllowedFileTypes = [".txt", ".jpg"]
            };

            _mockAttachmentConfig.Setup(x => x.Value).Returns(attachmentConfig);

            _fileUploadService = new FileUploadService(
                _mockAttachmentConfig.Object,
                _mockFileScanService.Object,
                _mockFileValidatorService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task UploadFile_ShouldThrowArgumentException_WhenFileIsEmpty()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(0);

            await Assert.ThrowsAsync<ArgumentException>(() => _fileUploadService.UploadFile(mockFile.Object));
        }

        [Fact]
        public async Task UploadFile_ShouldThrowInvalidOperationException_WhenFileExceedsMaxSize()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(2097152);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _fileUploadService.UploadFile(mockFile.Object));
        }

        [Fact]
        public async Task UploadFile_ShouldThrowInvalidOperationException_WhenFileTypeIsNotAllowed()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.FileName).Returns("file.exe");
            await Assert.ThrowsAsync<InvalidOperationException>(() => _fileUploadService.UploadFile(mockFile.Object));
        }

        [Fact]
        public async Task UploadFile_ShouldThrowInvalidOperationException_WhenFileValidationFails()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.FileName).Returns("file.txt");
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default)).Callback<Stream, CancellationToken>((stream, _) =>
            {
                using var writer = new StreamWriter(stream);
                writer.Write("Test content");
            });

            _mockFileValidatorService.Setup(v => v.Validate(It.IsAny<byte[]>(), It.IsAny<string>())).Returns(false);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _fileUploadService.UploadFile(mockFile.Object));
        }

        [Fact]
        public async Task UploadFile_ShouldThrowInvalidOperationException_WhenThreatIsFound()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.FileName).Returns("file.txt");
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default)).Callback<Stream, CancellationToken>((stream, _) =>
            {
                using var writer = new StreamWriter(stream);
                writer.Write("Test content");
            });

            _mockFileValidatorService.Setup(v => v.Validate(It.IsAny<byte[]>(), It.IsAny<string>())).Returns(true);
            _mockFileScanService.Setup(s => s.ScanAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(FileScanResult.ThreatFound);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _fileUploadService.UploadFile(mockFile.Object));
        }

        [Fact]
        public async Task UploadFile_ShouldReturnFileName_WhenNoThreatIsFound()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.FileName).Returns("file.txt");
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default)).Callback<Stream, CancellationToken>((stream, _) =>
            {
                using var writer = new StreamWriter(stream);
                writer.Write("Test content");
            });

            _mockFileValidatorService.Setup(v => v.Validate(It.IsAny<byte[]>(), It.IsAny<string>())).Returns(true);
            _mockFileScanService.Setup(s => s.ScanAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(FileScanResult.NoThreatFound);
            //_mockFileScanService.Setup(s => s.ScanAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(FileScanResult.NoThreatFound);

            var result = await _fileUploadService.UploadFile(mockFile.Object);

            Assert.NotNull(result);
            Assert.EndsWith(".txt", result);
        }
    }
}