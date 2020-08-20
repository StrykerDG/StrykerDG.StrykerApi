using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StrykerDG.StrykerActors.GitHub;
using StrykerDG.StrykerApi.Configuration;
using StrykerDG.StrykerServices.GitHubService;
using StrykerDG.StrykerServices.Interfaces;

namespace StrykerDG.StrykerApi
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        IActorRef GitHubActor { get; set; }
        readonly string AllowedOrigins = "CorsPolicy";

        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var settings = new ApiSettings();
            Configuration.GetSection("ApiSettings").Bind(settings);

            // Access Settings via DI
            services.Configure<ApiSettings>(Configuration.GetSection("ApiSettings"));

            // Access Services via DI
            services.AddHttpClient();
            services.AddTransient<IStrykerService>((provider) =>
            {
                var clientFactory = provider.GetService<IHttpClientFactory>();
                return new GitHubService(
                    clientFactory, 
                    settings.SecuritySettings.GitHubToken, 
                    settings.SecuritySettings.GitHubUserAgent
                );
            });

            // Add Akka.net
            services.AddSingleton((provider) =>
            {
                // Create the actor system
                var actorSystem = ActorSystem.Create("StrykerDG");

                // Register the actors
                var serviceScopeFactory = provider.GetService<IServiceScopeFactory>();
                GitHubActor = actorSystem.ActorOf(Props.Create(() => new GitHubActor(serviceScopeFactory)), "GitHubActor");

                return actorSystem;
            });

            // Access Actors via DI
            services.AddSingleton(_ => GitHubActor);

            // Setup CORS
            if(settings.CORS != null)
            {
                services.AddCors(options =>
                {
                    options.AddPolicy(
                        AllowedOrigins,
                        builder => 
                            builder.WithOrigins(settings.CORS)
                            .AllowAnyMethod()
                            .AllowAnyHeader());
                });
            }

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseCors(AllowedOrigins);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            lifetime.ApplicationStarted.Register(() =>
            {
                // Start Akka.net
                app.ApplicationServices.GetService<ActorSystem>();
            });

            lifetime.ApplicationStopping.Register(() =>
            {
                // Stop Akka.net
                app.ApplicationServices.GetService<ActorSystem>().Terminate().Wait();
            });
        }
    }
}
