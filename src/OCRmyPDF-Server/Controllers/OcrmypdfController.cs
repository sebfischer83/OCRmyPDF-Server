using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using OCRmyPDF_Server.Services;
using System.Diagnostics;

namespace OCRmyPDF_Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OcrmypdfController : ControllerBase
    {
        private readonly ILogger<OcrmypdfController> _logger;
        private readonly ConverterService converterService;

        public OcrmypdfController(ILogger<OcrmypdfController> logger, ConverterService converterService)
        {
            _logger = logger;
            this.converterService = converterService;
        }

        [HttpPost(Name = "Convert")]
        public async Task<ConvertResponse> ConvertAsync([FromForm] ConvertRequest convertRequest)
        {
            string targetPath = Path.Combine("/var", "upload", Guid.NewGuid().ToString("N") + ".pdf");
            using (var memory = new MemoryStream())
            {
                await convertRequest.File.CopyToAsync(memory);

                System.IO.File.WriteAllBytes(targetPath, memory.ToArray());
            }

            var result = converterService.Convert(targetPath, convertRequest.File.FileName, convertRequest.Languages);

            ConvertResponse? convertResponse = null;
            if (result.Success)
            {

                convertResponse = new ConvertResponse()
                {
                    Content = System.IO.File.ReadAllText(result.ContentFile),
                    GeneratedPdf = System.IO.File.ReadAllBytes(result.GeneratedPdf),
                    Message = string.Empty,
                    Success = true
                };
            }
            else
            {
                convertResponse = new ConvertResponse()
                {
                    Content = string.Empty,
                    GeneratedPdf = null,
                    Message = result.ErrorMessage,
                    Success = false
                };
            }

            try
            {
                if (System.IO.File.Exists(targetPath))
                {
                    System.IO.File.Delete(targetPath);
                }
                if (System.IO.File.Exists(result.ContentFile))
                {
                    System.IO.File.Delete(result.ContentFile);
                }
                if (System.IO.File.Exists(result.GeneratedPdf))
                {
                    System.IO.File.Delete(result.GeneratedPdf);
                }
            }
            catch (Exception ex) 
            {
                this._logger.LogError(ex, "Error while cleaning up");
            }

            return convertResponse;
        }

        public record ConvertResponse
        {
            public required string Message { get; init; }

            public bool Success { get; init; }

            public required byte[]? GeneratedPdf { get; init; }

            public required string Content { get; init; }
        }
    }
}
