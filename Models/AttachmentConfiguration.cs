namespace Models
{
    public class AttachmentConfiguration
    {
        public required string AttachmentPath { get; set; }
        public required string TemporaryPath { get; set; }
        public long MaxFileSize { get; set; }
        public required string[] AllowedFileTypes { get; set; } 
    }
}
