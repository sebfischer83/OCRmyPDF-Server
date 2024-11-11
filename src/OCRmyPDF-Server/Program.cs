
using Microsoft.AspNetCore.ResponseCompression;
using OCRmyPDF_Server.Services;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace OCRmyPDF_Server
{
    /*
     * 
     * USER root
RUN apt-get -y update && apt -y install ocrmypdf
RUN apt-get -y install tesseract-ocr-deu
RUN apt-get -y install tesseract-ocr-all

     * 
     * */
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddTransient<ConverterService>();
            builder.Configuration.AddEnvironmentVariables("OCRMYPDFSERVER__");

            string? telemtry = builder.Configuration["Telemetry:Server"];
            string? telemtryHeaderKey = builder.Configuration["Telemetry:HeaderKey"];
            string? telemtryHeaderValue = builder.Configuration["Telemetry:HeaderValue"];
            builder.Host.UseSerilog((context, configuration) =>
            {
                configuration.ReadFrom.Configuration(context.Configuration);
                if (!string.IsNullOrWhiteSpace(telemtry) && Uri.IsWellFormedUriString(telemtry, UriKind.Absolute))
                    configuration.WriteTo.OpenTelemetry(conf =>
                    {
                        conf.Endpoint = telemtry;
                        if (!string.IsNullOrWhiteSpace(telemtryHeaderKey) && !string.IsNullOrWhiteSpace(telemtryHeaderValue))
                        {
                            conf.Headers.Add(telemtryHeaderKey, telemtryHeaderValue);
                        }
                    });
            });

            builder.Services.AddRequestTimeouts(c =>
            {
                c.DefaultPolicy = new Microsoft.AspNetCore.Http.Timeouts.RequestTimeoutPolicy()
                {
                    Timeout = TimeSpan.FromMinutes(3)
                };
            });
            builder.Services.AddMetrics();
            builder.Services.AddHealthChecks();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddResponseCompression(c =>
            {
                c.EnableForHttps = true;
                c.Providers.Add<GzipCompressionProvider>();
            });

            ConfigureOpenTelemetry(builder, telemtry, telemtryHeaderKey, telemtryHeaderValue);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.UseSerilogRequestLogging(options =>
            {
                options.IncludeQueryInRequestPath = true;
            });
            app.MapHealthChecks("/healthz");
            app.MapControllers();

            app.Run();
        }

        private static void ConfigureOpenTelemetry(WebApplicationBuilder builder, string? telemtry, string? telemtryHeaderKey, string? telemtryHeaderValue)
        {
            return;
            string appName = "OCRmyPDF-Server";
            if (!string.IsNullOrWhiteSpace(telemtry) && Uri.IsWellFormedUriString(telemtry, UriKind.Absolute))
            {
                builder.Logging.AddOpenTelemetry(logging =>
                {
                    var resourceBuilder = ResourceBuilder.CreateDefault().AddService($"{appName}");
                    logging.SetResourceBuilder(resourceBuilder).AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri(telemtry);
                        if (!string.IsNullOrWhiteSpace(telemtryHeaderKey) && !string.IsNullOrWhiteSpace(telemtryHeaderValue))
                        {
                            o.Headers = $"{telemtryHeaderKey}={telemtryHeaderValue}";
                        }
                    });
                });

                builder.Services.AddOpenTelemetry()
                .WithMetrics(builder =>
                {
                    builder.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService($"{appName}")).AddAspNetCoreInstrumentation().AddRuntimeInstrumentation()
                    .AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri(telemtry);
                        if (!string.IsNullOrWhiteSpace(telemtryHeaderKey) && !string.IsNullOrWhiteSpace(telemtryHeaderValue))
                        {
                            o.Headers = $"{telemtryHeaderKey}={telemtryHeaderValue}";
                        }
                    });

                }).WithTracing(builder =>
                {
                    builder.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService($"{appName}")).AddAspNetCoreInstrumentation()
                    .AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri(telemtry);
                        if (!string.IsNullOrWhiteSpace(telemtryHeaderKey) && !string.IsNullOrWhiteSpace(telemtryHeaderValue))
                        {
                            o.Headers = $"{telemtryHeaderKey}={telemtryHeaderValue}";
                        }
                    });
                });
            }
            else if (builder.Environment.IsDevelopment())
            {
                builder.Logging.AddOpenTelemetry(logging =>
                {
                    var resourceBuilder = ResourceBuilder.CreateDefault().AddService($"{appName}");
                    logging.SetResourceBuilder(resourceBuilder).AddConsoleExporter();
                });

                builder.Services.AddOpenTelemetry()
                .WithMetrics(builder =>
                {
                    builder.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService($"{appName}")).AddAspNetCoreInstrumentation().AddRuntimeInstrumentation().
                    AddConsoleExporter();

                }).WithTracing(builder =>
                {
                    builder.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService($"{appName}")).AddAspNetCoreInstrumentation()
                    .AddConsoleExporter();
                });
            }
        }
    }
}
