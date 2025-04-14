using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Services.FileScanService
{
    /// <summary>
    /// Service for scanning files for potential threats using an external scanner.
    /// </summary>
    public class FileScanService : IFileScanService
    {
        private readonly IOptions<ScannerConfiguration> _scannerConfiguration;
        private readonly ILogger<FileScanService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileScanService"/> class.
        /// </summary>
        /// <param name="scannerConfiguration">The scanner configuration options.</param>
        /// <param name="logger">The logger instance for logging operations.</param>
        /// <exception cref="ArgumentNullException">Thrown if any dependency is null.</exception>
        public FileScanService(IOptions<ScannerConfiguration> scannerConfiguration, ILogger<FileScanService> logger)
        {
            _scannerConfiguration = scannerConfiguration ?? throw new ArgumentNullException(nameof(scannerConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            ValidateConfiguration();
        }

        /// <summary>
        /// Scans a file asynchronously for potential threats.
        /// </summary>
        /// <param name="file">The path to the file to be scanned.</param>
        /// <param name="timeoutInMs">The timeout in milliseconds for the scanning process. Default is 30,000 ms.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A <see cref="FileScanResult"/> indicating the result of the scan.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the scanner executable or the file to scan is not found.</exception>
        public async Task<FileScanResult> ScanAsync(string file, int timeoutInMs = 30000, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting asynchronous file scan for: {File}", file);

            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
            {
                _logger.LogError("File to scan not found: {File}", file);
                return FileScanResult.FileNotFound;
            }

            if (!File.Exists(_scannerConfiguration.Value.ScannerPath))
            {
                _logger.LogError("Scanner executable not found at path: {Path}", _scannerConfiguration.Value.ScannerPath);
                throw new FileNotFoundException("Scanner executable not found.", _scannerConfiguration.Value.ScannerPath);
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = _scannerConfiguration.Value.ScannerPath,
                Arguments = _scannerConfiguration.Value.ScannerCommand.Replace("{FilePath}", new FileInfo(file).FullName),
                CreateNoWindow = true,
                ErrorDialog = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            // Create a timeout cancellation token source that is linked to the user's cancellation token
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeoutInMs);

            try
            {
                process.Start();
                _logger.LogInformation("Scanner process started for: {File}", file);

                await process.WaitForExitAsync(timeoutCts.Token);

                _logger.LogInformation("Scanner process exited for: {File} with ExitCode: {ExitCode}", file, process.ExitCode);

                return process.ExitCode switch
                {
                    0 => FileScanResult.NoThreatFound,
                    2 => FileScanResult.ThreatFound,
                    _ => FileScanResult.Error
                };
            }
            catch (OperationCanceledException)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("File scan operation was canceled by user for: {File}", file);
                    if (!process.HasExited)
                    {
                        process.Kill(true);
                    }
                    throw;
                }
                else
                {
                    _logger.LogWarning("Scanner process timed out after {Timeout}ms for: {File}", timeoutInMs, file);
                    if (!process.HasExited)
                    {
                        process.Kill(true);
                    }
                    return FileScanResult.Timeout;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while scanning the file: {File}", file);
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
                return FileScanResult.Error;
            }
        }

        /// <summary>
        /// Validates the scanner configuration to ensure all required values are set.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if any configuration value is invalid.</exception>
        private void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_scannerConfiguration.Value.ScannerPath))
            {
                throw new InvalidOperationException("ScannerPath is not configured.");
            }

            if (string.IsNullOrWhiteSpace(_scannerConfiguration.Value.ScannerCommand))
            {
                throw new InvalidOperationException("ScannerCommand is not configured.");
            }
        }
    }
}