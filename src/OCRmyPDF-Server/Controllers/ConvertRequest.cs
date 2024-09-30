using System.Diagnostics.CodeAnalysis;

namespace OCRmyPDF_Server.Controllers
{
    public class ConvertRequest
    {
        [NotNull]
        public IFormFile File { get; set; } = null!;

        [NotNull]
        public string[] Languages { get; set; } = ["deu", "eng"];
    }
}