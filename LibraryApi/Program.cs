using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LibraryApi.Persistance;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LibraryApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateWebHostBuilder(args).Build();

            // Migrate database
            host.Services.GetRequiredService<DatabaseContext>().MigrateDatabase();

            host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var env = aspNetCoreEnvironment != null ? aspNetCoreEnvironment.ToLower() : "local";

            var config = BuildConfiguration(args, Directory.GetCurrentDirectory(), env);
            var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";

            return new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseConfiguration(config)
                .UseStartup<Startup>()
                .UseUrls($"http://0.0.0.0:{port}");
        }

        private static IConfigurationRoot BuildConfiguration(string[] args, string basePath, string env)
        {
            var buildConfiguration = new ConfigurationBuilder()
                .SetBasePath(basePath);

            return buildConfiguration
                .AddJsonFile("appsettings.json", optional: false)
                 .AddJsonFile($"appsettings.{env.ToLower()}.json", optional: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();
        }
    }

    public static class DatabaseMigrationExtensions
    {
        public static void MigrateDatabase<TDbContext>(this TDbContext dbContext) where TDbContext : DbContext
        {
            var db = dbContext.Database;
            if (db.IsInMemory())
            {
                return;
            }

            var migrate = false;
            foreach (var pendingMigration in db.GetPendingMigrations())
            {
                migrate = true;
                Console.WriteLine($"Pending DB migration: {pendingMigration}.");
            }

            if (migrate)
            {
                Console.WriteLine("Applying DB migrations ...");
                db.SetCommandTimeout(3 * 60);
                db.Migrate();
            }
            else
            {
                Console.WriteLine("No pending DB migrations.");
            }
        }
    }
}
