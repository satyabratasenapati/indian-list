// MunicipalityTax.API/Program.cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting; // Ensure this is imported for Host.CreateDefaultBuilder
using Microsoft.Extensions.DependencyInjection; // For CreateScope and GetRequiredService
using Microsoft.EntityFrameworkCore; // For Migrate
using MunicipalityTax.Infrastructure.Data; // For ApplicationDbContext

namespace MunicipalityTax.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            // Apply migrations on startup (still a good idea here for development)
            // This ensures the database is ready when the API starts,
            // regardless of whether it's self-hosted or via IIS.
            using (var scope = host.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.Migrate();
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>(); // Use the new Startup class
                    // You can still add .UseUrls() here if you want to define specific URLs
                    // for when this project runs as a standalone web app/API.
                    // webBuilder.UseUrls("http://localhost:5000", "https://localhost:5001");
                });
    }
}
