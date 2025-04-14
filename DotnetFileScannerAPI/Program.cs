using DotnetFileScannerAPI.Middleware;
using Models;
using Serilog;
using Services.FileScanService;
using Services.FileUploadService;
using Services.FileValidationService;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<IFileScanService, FileScanService>();
builder.Services.AddScoped<IFileValidatorService, FileValidatorService>();

builder.Services.Configure<AttachmentConfiguration>(builder.Configuration.GetSection("AttachmentConfiguration"));
builder.Services.Configure<ScannerConfiguration>(builder.Configuration.GetSection("ScannerConfiguration"));

var app = builder.Build();


app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/upload", async (
    IFormFile file,
    IFileUploadService fileUploadService,
     HttpContext httpContext) =>
{
    if (file == null)
    {
        return Results.BadRequest("No file was provided in the request.");
    }

    try
    {
        string uploadedFileName = await fileUploadService.UploadFile(file, httpContext.RequestAborted);
        return Results.Ok(new { FileName = uploadedFileName });
    }
    catch
    {
        throw;
    }
}).DisableAntiforgery();

app.Run();
