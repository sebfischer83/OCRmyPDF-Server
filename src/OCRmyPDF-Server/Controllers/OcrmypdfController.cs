using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using OCRmyPDF_Server.Services;
using System.Diagnostics;
using System.Net;
using static OCRmyPDF_Server.Controllers.OcrmypdfController;

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

        [HttpGet()]
        [Route("GetLanguages")]
        public ActionResult GetLanguages()
        {
            var list = converterService.GetInstalledLanguages();

            return Ok(list);
        }

        [HttpPost()]
        [Route("EnsureLanguages")]
        public ActionResult EnsureLanguages(string[] languages)
        {
            if (languages.Length == 0)
            {
                return BadRequest();
            }

            var success = converterService.InstallLanguages(languages);

            if (!success)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return Ok();
        }


        [HttpPost]
        [Route("Convert")]
        public async Task<IActionResult> Convert([FromForm] ConvertRequest convertRequest)
        {
            string targetPath = Path.Combine("/var", "upload", Guid.NewGuid().ToString("N") + ".pdf");
            using (var memory = new MemoryStream())
            {
                await convertRequest.File.CopyToAsync(memory);

                System.IO.File.WriteAllBytes(targetPath, memory.ToArray());
            }

            ConverterResult? result = null;
            try
            {
                result = converterService.Convert(targetPath, convertRequest.File.FileName, convertRequest.Languages);

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
                    return Ok(convertResponse);
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
                    return StatusCode(StatusCodes.Status500InternalServerError, convertResponse);
                }
            }
            finally
            {
                try
                {
                    if (System.IO.File.Exists(targetPath))
                    {
                        System.IO.File.Delete(targetPath);
                    }
                    if (System.IO.File.Exists(result?.ContentFile))
                    {
                        System.IO.File.Delete(result.ContentFile);
                    }
                    if (System.IO.File.Exists(result?.GeneratedPdf))
                    {
                        System.IO.File.Delete(result.GeneratedPdf);
                    }
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "Error while cleaning up");
                }
            }          
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
