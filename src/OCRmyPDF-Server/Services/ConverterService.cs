using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OCRmyPDF_Server.Services
{
    public class ConverterService
    {
        private readonly ILogger<ConverterService> logger;

        public ConverterService(ILogger<ConverterService> logger)
        {
            this.logger = logger;
        }

        public List<string> GetInstalledLanguages()
        {
            List<string> list = new();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "tesseract";
            startInfo.Arguments = $"--list-langs";
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            using (Process process = new Process())
            {
                process.StartInfo = startInfo;

                // Start the process
                process.Start();

                // Read the output
                string output = process.StandardOutput.ReadToEnd();
                string errors = process.StandardError.ReadToEnd();

                // Wait for the process to exit
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    logger.LogError(errors);
                    return list;
                }

                var parts = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

                list.AddRange(parts.Skip(1));

                logger.LogDebug(errors);

            }

            return list;
        }

        public bool InstallLanguages(string[] languages)
        {
            bool error = false;
            foreach (var item in languages)
            {
                //  -y install tesseract-ocr-deu
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "apt-get";
                startInfo.Arguments = $"-y install tesseract-ocr-{item}";
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;

                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;

                    // Start the process
                    process.Start();

                    // Read the output
                    string output = process.StandardOutput.ReadToEnd();
                    string errors = process.StandardError.ReadToEnd();

                    // Wait for the process to exit
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        error = true;
                        logger.LogError(errors);
                    }

                    logger.LogDebug(errors);

                }
            }

            return error;
        }

        public ConverterResult Convert(string file, string orgFileName, string[] languages)
        {
            logger.LogInformation($"Start processing file {file} - {orgFileName}");
            var stopwatch = Stopwatch.StartNew();
            string command = "ocrmypdf";
            var outputFileNames = GetOutputFileNames(file);

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = command;
            startInfo.Arguments = $"-l {string.Join('+', languages)} --redo-ocr --sidecar {outputFileNames.Txt} {file} {outputFileNames.Pdf}";
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;

                // Start the process
                process.Start();

                // Read the output
                string output = process.StandardOutput.ReadToEnd();
                string errors = process.StandardError.ReadToEnd();

                // Wait for the process to exit
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    logger.LogError(errors);
                    return new ConverterResult()
                    { ContentFile = string.Empty, ErrorMessage = errors, GeneratedPdf = "", OriginalFileName = file, Success = false };
                }

                logger.LogDebug(errors);

                stopwatch.Stop();
                logger.LogInformation($"End processing file {file} - {orgFileName} after {stopwatch.Elapsed.TotalSeconds} seconds");
            }
            return new ConverterResult()
            { ContentFile = outputFileNames.Txt, ErrorMessage = string.Empty, GeneratedPdf = outputFileNames.Pdf, OriginalFileName = file, Success = true };
        }

        private (string Pdf, string Txt) GetOutputFileNames(string file)
        {
            string pdf = Path.GetFileName(file);
            pdf = Path.Combine("/var", "working", pdf);
            string txt = Path.ChangeExtension(pdf, "txt");
            return new(pdf, txt);
        }
    }
}
