using Roi_ocr.Helpers; // Add this to reference DatabaseHelper
using Roi_ocr.Services; // Add this to reference ImageProcessor

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAllOrigins",
                policy => policy.AllowAnyOrigin()
                                .AllowAnyMethod()
                                .AllowAnyHeader());
        });

        // Register services
        builder.Services.AddSingleton<DatabaseHelper>();
        builder.Services.AddTransient<ImageProcessor>(); 
        builder.Services.AddScoped<ImageProcessingService>();

        // Add controllers
        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseCors("AllowAllOrigins");

        app.MapControllers();

        app.Run();
    }
}