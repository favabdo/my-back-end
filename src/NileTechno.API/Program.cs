using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NileTechno.API.Auth;
using NileTechno.API.Middleware;
using NileTechno.Application;
using NileTechno.Infrastructure;
using NileTechno.Infrastructure.Configuration;

EnvFile.Load();

// Render (and many Linux hosts) cap inotify at 128. Default config file
// watchers exceed that and crash the process with IOException / exit 139.
Environment.SetEnvironmentVariable("DOTNET_HOSTBUILDER_RELOADCONFIGONCHANGE", "false");
Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "true");

var builder = WebApplication.CreateBuilder(args);

var listenPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(listenPort))
    builder.WebHost.UseUrls($"http://0.0.0.0:{listenPort}");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "NileTechno API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Bearer {token}"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key مش موجود في appsettings.json");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };

    // Convenience for hand-tested calls: on the address endpoints the JWT may also arrive as
    // `accessToken` in the JSON body when no Authorization header is present. Validation is unchanged.
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = async context =>
        {
            if (context.Request.Headers.Authorization.Count > 0) return;
            if (!BodyAccessTokenReader.AppliesTo(context.Request)) return;

            var fromBody = await BodyAccessTokenReader.ReadAsync(context.Request);
            if (!string.IsNullOrWhiteSpace(fromBody))
                context.Token = fromBody;
        }
    };
});

builder.Services.AddAuthorization();

const string CorsPolicy = "AllowFrontend";
var corsOrigins = (Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS") ?? "")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Select(origin => origin.TrimEnd('/'))
    .Where(origin => origin.Length > 0)
    .ToArray();
if (corsOrigins.Length == 0)
    corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        // Browser calls come from Vercel (and preview URLs that change every deploy).
        // Auth uses Authorization header, not cookies, so credentials are not required.
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var schema = scope.ServiceProvider.GetRequiredService<NileTechno.Application.Common.Interfaces.ISqlSchemaBootstrapper>();
    await schema.EnsureAsync();
}

// Swagger app: serves the bundled OpenAPI spec at /api/swagger in every environment
var openapiSpecPath = Path.Combine(AppContext.BaseDirectory, "openapi", "niletechno-openapi.yaml");
app.MapGet("/openapi/niletechno-openapi.yaml", () =>
    Results.Content(File.ReadAllText(openapiSpecPath), "application/yaml"));

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/openapi/niletechno-openapi.yaml", "NileTechno API");
    c.RoutePrefix = "api/swagger";
});

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
