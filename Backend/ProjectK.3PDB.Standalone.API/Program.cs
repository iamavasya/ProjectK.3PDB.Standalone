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
        public static void Main(string[] args)
        {
            VelopackApp.Build().Run();

            var builder = WebApplication.CreateBuilder(args);

            var dbPath = Path.Combine(AppContext.BaseDirectory, "projectk_3pdb_standalone_v2.db");
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
                db.Database.EnsureCreated();
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
                    manager.Kill();
                    return Results.Ok();
                });
                var url = "http://localhost:5220";

                Task.Delay(1000).ContinueWith(t => OpenBrowser(url));
                app.Run("http://localhost:5220");
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
    }

    public class BrowserLifeTimeManager : IHostedService
    {
        private readonly IHostApplicationLifetime _lifetime;

        public BrowserLifeTimeManager(IHostApplicationLifetime lifetime)
        {
            _lifetime = lifetime;
        }

        public void Kill()
        {
            _lifetime.StopApplication();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Delay(TimeSpan.FromHours(24)).ContinueWith(_ => _lifetime.StopApplication());

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
