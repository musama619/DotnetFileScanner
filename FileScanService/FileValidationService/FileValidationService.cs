namespace Services.FileValidationService
{
    //https://stackoverflow.com/a/73068336/13405106
    public class FileValidatorService : IFileValidatorService
    {
        private static readonly HashSet<string> ValidExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Image formats
            ".gif", ".jpg", ".jpeg", ".png", ".tif", ".tiff", ".bmp", ".dcm", ".ico",
            // Sound formats
            ".wav", ".mp3",
            // Document formats
            ".pdf", ".rtf", ".doc", ".xls", ".ppt", ".docx", ".xlsx", ".pptx", ".txt",
            // Special formats
            ".zip", ".rar", ".ico", ".dcm", ".dicom", ".xml"
        };

        private static readonly Dictionary<string, List<byte[]>> FileSignatures = new()
        {
            // Image Formats
            { ".gif", new List<byte[]> { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
            { ".jpg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".jpeg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47 } } },
            { ".tif", new List<byte[]> { new byte[] { 0x49, 0x49, 0x2A, 0x00 } } },
            { ".tiff", new List<byte[]> { new byte[] { 0x49, 0x49, 0x2A, 0x00 } } },
            { ".bmp", new List<byte[]> { new byte[] { 0x42, 0x4D } } },
            { ".dcm", new List<byte[]> { new byte[] { 0x44, 0x49, 0x43, 0x4D } } },
            { ".dicom", new List<byte[]> { new byte[] { 0x44, 0x49, 0x43, 0x4D } } },
            { ".ico", new List<byte[]> { new byte[] { 0x00, 0x00, 0x01, 0x00 } } },

            // Sound Formats
            { ".wav", new List<byte[]> { new byte[] { 0x52, 0x49, 0x46, 0x46 } } },
            { ".mp3", new List<byte[]> { new byte[] { 0x52, 0x49, 0x46, 0x46 } } },

            // Document Formats
            { ".pdf", new List<byte[]> { new byte[] { 0x25, 0x50, 0x44, 0x46 } } },
            { ".rtf", new List<byte[]> { new byte[] { 0x7B, 0x5C, 0x72, 0x74, 0x66 } } },
            { ".doc", new List<byte[]> { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } } },
            { ".xls", new List<byte[]> { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } } },
            { ".ppt", new List<byte[]> { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } } },
            { ".docx", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
            { ".xlsx", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
            { ".pptx", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
            { ".zip", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
            { ".txt", new List<byte[]> { 
                // UTF-8 BOM (uncommon for txt files but possible)
                new byte[] { 0xEF, 0xBB, 0xBF },
                // UTF-16 LE BOM
                new byte[] { 0xFF, 0xFE },
                // UTF-16 BE BOM
                new byte[] { 0xFE, 0xFF },
                // UTF-32 LE BOM
                new byte[] { 0xFF, 0xFE, 0x00, 0x00 },
                // UTF-32 BE BOM
                new byte[] { 0x00, 0x00, 0xFE, 0xFF },
                // No BOM (empty array to indicate no signature is required)
                new byte[] { }
            } },

            // Special Formats
            { ".rar", new List<byte[]> { new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00 } } },
            { ".xml", new List<byte[]> { new byte[] { 0x3C, 0x3F, 0x78, 0x6D, 0x6C } } }
        };

        public bool Validate(byte[] fileBytes, string fileName)
        {
            if (fileBytes.Length == 0 || string.IsNullOrEmpty(fileName))
                return false;

            string fileExtension = Path.GetExtension(fileName).ToLower();
            if (string.IsNullOrEmpty(fileExtension) || !ValidExtensions.Contains(fileExtension))
                return false;

            return IsValidFile(fileBytes, fileExtension);
        }


        private static bool IsValidFile(byte[] fileBytes, string expectedFileType)
        {
            if (!FileSignatures.ContainsKey(expectedFileType))
                throw new ArgumentException("Unsupported file type.");

            var signatures = FileSignatures[expectedFileType];

            return signatures.Any(signature =>
            {
                if (signature.Length == 0)
                    return true;

                if (fileBytes.Length < signature.Length)
                    return false;

                var fileHeader = fileBytes.Take(signature.Length).ToArray();
                return signature.SequenceEqual(fileHeader);
            });
        }

    }
}
