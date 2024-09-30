namespace OCRmyPDF_Server.Services
{
    public record ConverterResult
    {
        public required string OriginalFileName { get; init; }

        public required string GeneratedPdf { get; init; }

        public required string ContentFile { get; init; }

        public required string ErrorMessage { get; init; }

        public bool Success { get; init; }
    }
}