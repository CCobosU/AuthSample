using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Microsoft.OpenApi.Models;
using Infrastructure;
using Infrastructure.Settings;
using Application.Validators;
using FluentValidation.AspNetCore;
using FluentValidation;
using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using System.Linq;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .ReadFrom.Configuration(ctx.Configuration));

// Configuration
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();

// Infrastructure
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddInfrastructure(conn);

// MediatR, AutoMapper, FluentValidation
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Application.Commands.RegisterCommand).Assembly));
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(Application.Mappings.MappingProfile).Assembly));
builder.Services.AddControllers();
// FluentValidation: register automatic validation and validators
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterValidator>();

// Health checks
builder.Services.AddHealthChecks();

// HealthChecks UI storage (in-memory) - used by the optional UI middleware
builder.Services.AddHealthChecksUI().AddInMemoryStorage();

// JWT
builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options => {
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
    };
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement{
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            new string[] {}
        }
    });
});

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseMiddleware<WebApi.Middlewares.ExceptionMiddleware>();

// Apply EF Core migrations at startup (useful for local/dev in docker-compose)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while migrating or initializing the database.");
        throw;
    }
}

// OpenAPI (Swagger) conditional by configuration
var openApiEnabled = builder.Configuration.GetValue<bool?>("OpenApi:Enabled") ?? app.Environment.IsDevelopment();
if (openApiEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Serve a simple static welcome page in all environments (environments can still enable/disable OpenAPI)
app.UseDefaultFiles();
app.UseStaticFiles();

// Small endpoint to expose environment name to static welcome page
app.MapGet("/env", (Microsoft.AspNetCore.Hosting.IWebHostEnvironment env) => Results.Text(env.EnvironmentName));

// Root endpoint that returns a simple HTML welcome with injected metadata (env, version, commit)
app.MapGet("/", async (Microsoft.AspNetCore.Hosting.IWebHostEnvironment env, Microsoft.AspNetCore.Http.HttpContext http) =>
{
    string environment = env.EnvironmentName;

    // Try to read build info from optional file BUILD_INFO.json placed in content root
    string contentRoot = env.ContentRootPath;
    string buildInfoPath = System.IO.Path.Combine(contentRoot, "BUILD_INFO.json");
    string version = System.Reflection.Assembly.GetEntryAssembly()?
        .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?
        .InformationalVersion ?? System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";
    string commit = "";
    if (System.IO.File.Exists(buildInfoPath))
    {
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(buildInfoPath));
            if (json.RootElement.TryGetProperty("version", out var v)) version = v.GetString() ?? version;
            if (json.RootElement.TryGetProperty("commit", out var c)) commit = c.GetString() ?? "";
        }
        catch { /* ignore parse errors */ }
    }

    // Build a simple HTML response
    var encodedEnv = System.Net.WebUtility.HtmlEncode(environment);
    var encodedVersion = System.Net.WebUtility.HtmlEncode(version);
    var encodedCommit = System.Net.WebUtility.HtmlEncode(commit);
    var builtTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");

    var html = "<!doctype html><html><head><meta charset=\"utf-8\"/><title>Welcome</title>" +
        "<style>body{font-family:Arial;background:#f7f9fc;color:#222;margin:0;padding:40px}" +
        ".container{max-width:760px;margin:40px auto;background:#fff;border-radius:8px;padding:28px;box-shadow:0 4px 14px rgba(0,0,0,0.1)}" +
        "h1{margin-top:0}p.lead{color:#555}footer{margin-top:24px;font-size:13px;color:#888}</style>" +
        "</head><body><div class=\"container\">" +
        "<h1>AuthSample API</h1>" +
        "<p class=\"lead\">This is the welcome page for the AuthSample Web API.</p>" +
        "<p>Available endpoints: <a href=\"/swagger/index.html\">Swagger UI</a> (if enabled)</p>" +
        "<ul>" +
        $"<li>Environment: <strong>{encodedEnv}</strong></li>" +
        $"<li>Version: <strong>{encodedVersion}</strong></li>" +
        $"<li>Commit: <strong>{encodedCommit}</strong></li>" +
        "</ul>" +
        $"<footer>Built: {builtTime} UTC</footer>" +
        "</div></body></html>";

    http.Response.ContentType = "text/html; charset=utf-8";
    await http.Response.WriteAsync(html);
});

// Liveness endpoint (alive check)
app.MapGet("/health/live", () => Results.Ok(new { status = "Alive" }));

// Readiness endpoint using health checks with detailed JSON response and optional API key protection
app.Map("/health/ready", branch =>
{
    branch.Use(async (ctx, next) =>
    {
        var config = ctx.RequestServices.GetService<IConfiguration>();
        var apiKey = config?["Health:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            if (!ctx.Request.Headers.TryGetValue("X-Health-Key", out var provided) || provided != apiKey)
            {
                ctx.Response.StatusCode = 401;
                await ctx.Response.WriteAsync("Unauthorized");
                return;
            }
        }
        await next();
    });

    branch.UseHealthChecks("/health/ready", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json; charset=utf-8";

            // Try to read build info
            var env = context.RequestServices.GetService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            string contentRoot = env?.ContentRootPath ?? string.Empty;
            string buildInfoPath = System.IO.Path.Combine(contentRoot, "BUILD_INFO.json");
            string version = System.Reflection.Assembly.GetEntryAssembly()?
                .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";
            string commit = "";
            if (System.IO.File.Exists(buildInfoPath))
            {
                try
                {
                    var json = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(buildInfoPath));
                    if (json.RootElement.TryGetProperty("version", out var v)) version = v.GetString() ?? version;
                    if (json.RootElement.TryGetProperty("commit", out var c)) commit = c.GetString() ?? "";
                }
                catch { }
            }

            var checks = report.Entries.Select(kvp => new {
                name = kvp.Key,
                status = kvp.Value.Status.ToString(),
                description = kvp.Value.Description,
                duration = kvp.Value.Duration.ToString(),
                exception = kvp.Value.Exception?.Message
            });

            var result = new {
                status = report.Status.ToString(),
                totalDuration = report.TotalDuration.ToString(),
                checks,
                version,
                commit,
                timestamp = DateTime.UtcNow
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
    });
});

// HealthChecks UI (optional) - provides a simple UI at /healthchecks-ui
app.UseHealthChecksUI(options => { options.UIPath = "/healthchecks-ui"; });

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
