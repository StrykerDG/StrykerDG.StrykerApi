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
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StrykerDG.StrykerActors.GitHub;
using StrykerDG.StrykerActors.Twitch;
using StrykerDG.StrykerApi.Configuration;
using StrykerDG.StrykerServices.GitHubService;
using StrykerDG.StrykerServices.Interfaces;
using StrykerDG.StrykerServices.TwitchService;

namespace StrykerDG.StrykerApi
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        readonly string AllowedOrigins = "CorsPolicy";

        public IActorRef GitHubActor { get; set; }
        public IActorRef TwitchActor { get; set; }

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

            services.AddTransient<IStrykerService>((provider) =>
            {
                var clientFactory = provider.GetService<IHttpClientFactory>();
                return new TwitchService(clientFactory);
            });

            // Add Akka.net
            services.AddSingleton((provider) =>
            {
                // Create the actor system
                var actorSystem = ActorSystem.Create("StrykerDG");

                // Register the actors
                var serviceScopeFactory = provider.GetService<IServiceScopeFactory>();
                var twitchId = settings.SecuritySettings.TwitchClientId;
                var twitchSecret = settings.SecuritySettings.TwitchClientSecret;

                GitHubActor = actorSystem.ActorOf(
                    Props.Create(() => new GitHubActor(serviceScopeFactory)), 
                    "GitHubActor"
                );
                TwitchActor = actorSystem.ActorOf(
                    Props.Create(() => new TwitchActor(serviceScopeFactory, twitchId, twitchSecret)), 
                    "TwitchActor"
                );

                return actorSystem;
            });

            // Access Actors via DI
            services.AddSingleton(_ => GitHubActor);
            services.AddSingleton(_ => TwitchActor);

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
