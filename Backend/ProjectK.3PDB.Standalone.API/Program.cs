using AutoMapper.EquivalencyExpression;
using Microsoft.EntityFrameworkCore;
using ProjectK._3PDB.Standalone.BL.Services;
using ProjectK._3PDB.Standalone.Infrastructure.Context;
using System.Diagnostics;
using Velopack;

namespace ProjectK._3PDB.Standalone.API
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            VelopackApp.Build().Run();

            var builder = WebApplication.CreateBuilder(args);

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(localAppData, "ProjectK3PDB", "data");
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }

            var dbPath = Path.Combine(appFolder, "projectk_3pdb_standalone_v2.db");

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));



            builder.Services.AddScoped<ParticipantService>();

            builder.Services.AddAutoMapper(cfg => { cfg.AddCollectionMappers(); }, typeof(BL.Maps.ParticipantMappingProfile));

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:4200")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            builder.Services.AddSingleton<BrowserLifeTimeManager>();
            builder.Services.AddSingleton<UpdateService>();

            builder.Services.AddHostedService(provider => provider.GetRequiredService<BrowserLifeTimeManager>());

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await EnsureMigrationHistoryAsync(db);

                await db.Database.MigrateAsync();
            }

            app.UseRouting();

            app.UseCors("AllowFrontend");

            app.MapControllers();

            app.UseSwagger();
            app.UseSwaggerUI();



            if (!app.Environment.IsDevelopment())
            {
                app.UseDefaultFiles();
                app.UseStaticFiles(new StaticFileOptions
                {
                    OnPrepareResponse = ctx =>
                    {
                        if (ctx.File.Name == "index.html")
                        {
                            ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
                            ctx.Context.Response.Headers.Append("Pragma", "no-cache");
                            ctx.Context.Response.Headers.Append("Expires", "0");
                        }
                    }
                });
                app.MapFallbackToFile("index.html");
                
                app.MapPost("/api/kill", (BrowserLifeTimeManager manager) =>
                {
                    manager.ScheduleShutdown();
                    return Results.Ok();
                });

                app.MapPost("/api/alive", (BrowserLifeTimeManager manager) =>
                {
                    manager.CancelShutdown();
                    return Results.Ok();
                });

                var url = "http://localhost:5220";

                if (!args.Contains("--restarted"))
                {
                    Task.Delay(1000).ContinueWith(t => OpenBrowser(url));
                    app.Run(url);
                }
                else
                {
                    Console.WriteLine("Restart detected. Skipping browser launch.");
                    app.Run(url);
                }
            }
            else
            {
                app.Run();
            }

        }

        private static void OpenBrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "msedge",
                    Arguments = $"--app={url}",
                    UseShellExecute = true
                });
            }
            catch
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "chrome",
                        Arguments = $"--app={url}",
                        UseShellExecute = true
                    });
                }
                catch
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
            }
        }

        /// <summary>
        /// Ensures the EF Core migration history table exists and is seeded for legacy databases
        /// that contain the <c>Participants</c> table but lack the EF migrations history metadata.
        /// </summary>
        /// <param name="db">The application database context with an openable SQLite connection.</param>
        /// <remarks>
        /// This method:
        /// <list type="bullet">
        /// <item><description>Checks for the presence of the legacy <c>Participants</c> table.</description></item>
        /// <item><description>Creates the <c>__EFMigrationsHistory</c> table if it is missing.</description></item>
        /// <item><description>Seeds the history table with the initial migration if it is empty.</description></item>
        /// </list>
        /// It is intended to bridge older databases into EF Core's migration tracking without
        /// rebuilding or losing data.
        /// </remarks>
        private static async Task EnsureMigrationHistoryAsync(AppDbContext db)
        {
            await db.Database.OpenConnectionAsync();
            try
            {
                await using var command = db.Database.GetDbConnection().CreateCommand();

                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Participants';";
                var participantsExists = (long)(await command.ExecuteScalarAsync() ?? 0) > 0;

                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory';";
                var historyTableExists = (long)(await command.ExecuteScalarAsync() ?? 0) > 0;

                if (participantsExists)
                {
                    if (!historyTableExists)
                    {
                        await db.Database.ExecuteSqlRawAsync(
                            "CREATE TABLE IF NOT EXISTS \"__EFMigrationsHistory\" (\"MigrationId\" TEXT NOT NULL CONSTRAINT \"PK___EFMigrationsHistory\" PRIMARY KEY, \"ProductVersion\" TEXT NOT NULL);");
                    }

                    command.CommandText = "SELECT COUNT(*) FROM \"__EFMigrationsHistory\";";
                    var historyCount = (long)(await command.ExecuteScalarAsync() ?? 0);

                    if (historyCount == 0)
                    {
                        await db.Database.ExecuteSqlRawAsync(
                            "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ('20251209143514_InitialMigration', '9.0.11');");
                    }
                }
            }
            finally
            {
                await db.Database.CloseConnectionAsync();
            }
        }
    }

    public class BrowserLifeTimeManager : IHostedService
    {
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ILogger<BrowserLifeTimeManager> _logger;
        private CancellationTokenSource? _shutdownCts;

        public BrowserLifeTimeManager(IHostApplicationLifetime lifetime, ILogger<BrowserLifeTimeManager> logger)
        {
            _lifetime = lifetime;
            _logger = logger;
        }

        public void ScheduleShutdown()
        {
            _shutdownCts?.Cancel();
            _shutdownCts = new CancellationTokenSource();
            var token = _shutdownCts.Token;

            _logger.LogWarning("Shutdown scheduled in 5 seconds...");

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(5000, token);

                    if (!token.IsCancellationRequested)
                    {
                        _logger.LogWarning("No heartbeat. Stopping application.");
                        _lifetime.StopApplication();
                    }
                }
                catch (TaskCanceledException) { }
            });
        }

        public void CancelShutdown()
        {
            if (_shutdownCts != null && !_shutdownCts.IsCancellationRequested)
            {
                _logger.LogInformation("Heartbeat received! Shutdown cancelled.");
                _shutdownCts.Cancel();
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Delay(TimeSpan.FromHours(24)).ContinueWith(_ => _lifetime.StopApplication());
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
