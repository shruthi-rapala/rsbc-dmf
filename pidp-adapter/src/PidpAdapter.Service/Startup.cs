﻿using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NodaTime;
using PidpAdapter.Extensions;
using PidpAdapter.Infrastructure.Auth;
using PidpAdapter.Infrastructure.HttpClients;
using PidpAdapter.Services;
using Serilog;
using Swashbuckle.AspNetCore.Filters;
using System.Reflection;
using System.Text;
using System.Text.Json;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace PidpAdapter;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration) => this.Configuration = configuration;
    public void ConfigureServices(IServiceCollection services)
    {
        var config = this.InitializeConfiguration(services);

        services
          .AddHttpClients(config)
          .AddSingleton<ILogger>(svc => svc.GetRequiredService<ILogger<Startup>>());

        if (!string.IsNullOrEmpty(Configuration["JWT_TOKEN_KEY"]))
        {
            byte[] key = Encoding.UTF8.GetBytes(Configuration["JWT_TOKEN_KEY"]);
            Array.Resize(ref key, 32);

            // Configure JWT authentication
            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(o =>
            {
                o.SaveToken = true;
                o.RequireHttpsMetadata = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    RequireExpirationTime = false,
                    ValidIssuer = Configuration["JWT_VALID_ISSUER"],
                    ValidAudience = Configuration["JWT_VALID_AUDIENCE"],
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });
        }
        else
        {
            services.AddAuthentication();
        }

        services.AddAuthorization();

        services.AddHttpClient();

        services.AddSingleton<IAuthorizationHandler, RealmAccessRoleHandler>();
        services.AddTransient<IClaimsTransformation, KeycloakClaimTransformer>();
        services.AddHttpContextAccessor();
        services.AddTransient(s => s.GetService<IHttpContextAccessor>().HttpContext.User);

        services.AddGrpc(opts =>
        {
            opts.EnableDetailedErrors = true;
        });
        services.AddGrpcReflection();
        services.AddDistributedMemoryCache();

        services.AddHealthChecks()
            .AddCheck("liveliness", () => HealthCheckResult.Healthy());
    }

    private Configuration InitializeConfiguration(IServiceCollection services)
    {
        var config = new Configuration();
        this.Configuration.Bind(config);
        services.AddSingleton(config);

        Log.Logger.Information("### App Version:{0} ###", Assembly.GetExecutingAssembly().GetName().Version);
        Log.Logger.Information("### Pidp Adapter Configuration:{0} ###", JsonSerializer.Serialize(config));

        return config;
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
         
        }

        app.UseSerilogRequestLogging(options => options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            var userId = httpContext.User.GetUserId();
            if (!userId.Equals(Guid.Empty))
            {
                diagnosticContext.Set("User", userId);
            }
        });
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<PidpService>();
            endpoints.MapGet("/", async context =>
            {
                await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client");
            });
            if (env.IsDevelopment())
            {
                endpoints.MapGrpcReflectionService();
            }
        });

    }
}