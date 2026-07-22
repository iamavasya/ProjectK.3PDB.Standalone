using AutoMapper.EquivalencyExpression;
using Microsoft.EntityFrameworkCore;
using ProjectK._3PDB.Standalone.BL.Interfaces;
using ProjectK._3PDB.Standalone.BL.Services;
using ProjectK._3PDB.Standalone.Infrastructure.Context;
using ProjectK._3PDB.Standalone.Infrastructure.Paths;
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

            // Run modes (driven by environment variables / configuration):
            //  - containerMode: serve the SPA + API headless (no browser launch, no Velopack,
            //    no heartbeat auto-shutdown) so the app can run inside a Docker container.
            //  - mockUpdate: swap the real UpdateService for a controllable mock and expose the
            //    test-control endpoints, for deterministic update/changelog E2E testing.
            var containerMode = builder.Configuration.GetValue<bool>("PROJECTK_CONTAINER");
            var mockUpdate = builder.Configuration.GetValue<bool>("PROJECTK_MOCK_UPDATE");

            var appPaths = new AppPaths();
            appPaths.EnsureDirectories();

            // Apply a staged raw-db restore (if any) before the database is opened.
            ApplyPendingRestore(appPaths);

            builder.Services.AddSingleton(appPaths);

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={appPaths.DbPath}"));

            builder.Services.AddScoped<IParticipantService, ParticipantService>();
            builder.Services.AddScoped<IConfigService, ConfigService>();
            builder.Services.AddScoped<IBackupService, BackupService>();
            builder.Services.AddHostedService<AutoBackupHostedService>();

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

            if (mockUpdate)
            {
                builder.Services.AddSingleton<MockUpdateService>();
                builder.Services.AddSingleton<IUpdateService>(sp => sp.GetRequiredService<MockUpdateService>());
                builder.Services.AddSingleton<IMockUpdateControl>(sp => sp.GetRequiredService<MockUpdateService>());
            }
            else
            {
                builder.Services.AddSingleton<IUpdateService, UpdateService>();
            }

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



            // Development desktop: API only; the Angular dev server is served separately (ng serve).
            if (app.Environment.IsDevelopment() && !containerMode)
            {
                app.Run();
                return;
            }

            // Both container and packaged-desktop modes serve the built Angular SPA.
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

            if (containerMode)
            {
                // Headless container: no browser launch and no heartbeat-driven shutdown, so the
                // container stays up for as long as the orchestrator wants it. Heartbeat endpoints
                // are accepted as no-ops for frontend compatibility.
                app.MapPost("/api/kill", () => Results.Ok());
                app.MapPost("/api/alive", () => Results.Ok());

                var containerUrl = "http://0.0.0.0:5220";
                Console.WriteLine($"Container mode. Listening on {containerUrl}. MockUpdate={mockUpdate}");
                app.Run(containerUrl);
                return;
            }

            // Packaged desktop: heartbeat-driven lifetime + a real browser window.
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
                Task.Delay(100).ContinueWith(t => OpenBrowser(url));
            }
            else
            {
                Console.WriteLine("Restart detected. Skipping browser launch.");
            }

            app.Run(url);
        }

        /// <summary>
        /// If a restore was staged (an uploaded <c>.db</c> awaiting swap), backs up the
        /// current database and replaces it with the staged file. Runs at startup, before
        /// any database connection is opened, so the live file is not locked.
        /// </summary>
        private static void ApplyPendingRestore(AppPaths paths)
        {
            if (!File.Exists(paths.PendingRestorePath))
            {
                return;
            }

            try
            {
                paths.EnsureDirectories();

                if (File.Exists(paths.DbPath))
                {
                    var safety = Path.Combine(paths.BackupsFolder, $"pre-swap_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.db");
                    File.Copy(paths.DbPath, safety, overwrite: true);
                }

                // Remove stale SQLite sidecar files so they don't shadow the new database.
                foreach (var sidecar in new[] { "-wal", "-shm", "-journal" })
                {
                    var sidecarPath = paths.DbPath + sidecar;
                    if (File.Exists(sidecarPath))
                    {
                        File.Delete(sidecarPath);
                    }
                }

                File.Copy(paths.PendingRestorePath, paths.DbPath, overwrite: true);
                File.Delete(paths.PendingRestorePath);

                Console.WriteLine("Staged database restore applied.");
            }
            catch (Exception ex)
            {
                // Leave the pending file in place for a later retry rather than crashing startup.
                Console.WriteLine($"Failed to apply staged restore: {ex.Message}");
            }
        }

        private static void OpenBrowser(string url)
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

            _logger.LogWarning("Shutdown scheduled in 15 seconds...");

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(15000, token);

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
