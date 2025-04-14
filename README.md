# DotnetFileScanner
A robust and secure .NET file scanning solution that provides protection against malicious file uploads using **Windows Defender** with multi-layered validation.
## Features

- **Secure File Upload:** Handles file uploads with comprehensive validation and security checks
- **File Validation:** Verifies file integrity by checking file signatures against their extensions to prevent file type spoofing
- **Malware Scanning:** Utilizes Windows Defender to scan uploaded files for potential threats before processing
- **Configurable:** Easily customize allowed file types, size limits, and scanner configurations

## Getting Started
### Prerequisites

- .NET 8.0 or later
- Windows Defender (exe path configured in appsettings.json)

### Installation

1. Clone the repository:
`git clone https://github.com/musama619/DotnetFileScanner.git`

2. Navigate to the project directory:
`cd DotnetFileScanner`

3. Restore dependencies:
`dotnet restore`

4. Update the `TemporaryPath` and `AttachmentPath` configuration in appsettings.json:
````json { 
  "AttachmentConfiguration": {
    "TemporaryPath": "C:/temp/uploads",
    "AttachmentPath": "C:/uploads",
    "MaxFileSize": 10485760,
    "AllowedFileTypes": [".pdf", ".jpg", ".png", ".docx"]
  },
  "ScannerConfiguration": {
    "ScannerPath": "C:/Program Files/Windows Defender/MpCmdRun.exe",
    "ScannerCommand": "-Scan -ScanType 3 -File \"{FilePath}\" -DisableRemediation"
  }
}
````

5. Run the application:
`dotnet run`


## Usage
### API Endpoints
**Upload File**

`POST /upload`

**Request:**

- Form data with a file field

**Response:**

- 200 OK: File uploaded successfully
```json
{
  "fileName": "guid-filename.extension"
}
```

- 400 Bad Request: No file provided or validation error
- 500 Internal Server Error: Processing error

## Architecture
The application consists of three primary services:

**FileUploadService:** Orchestrates the upload process, managing temporary storage, and integrating with validation and scanning services

**FileValidatorService:** Validates file content by checking file signatures against known patterns to ensure the file extension matches its content

**FileScanService:** Leverages Windows Defender to check uploaded files for malware and other threats
