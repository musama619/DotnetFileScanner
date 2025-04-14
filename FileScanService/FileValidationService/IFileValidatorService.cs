namespace Services.FileValidationService
{
    public interface IFileValidatorService
    {
        bool Validate(byte[] fileBytes, string fileName);
    }
}
