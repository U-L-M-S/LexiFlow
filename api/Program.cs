using System.Text;
using LexiFlow.Api.Data;
using LexiFlow.Api.Infrastructure.Options;
using LexiFlow.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

ConfigureOptions(builder);

var connectionString = BuildConnectionString(builder.Configuration);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(static options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "LexiFlow API", Version = "v1" });
    options.SupportNonNullableReferenceTypes();
});

builder.Services.AddHttpClient<OcrClient>();
builder.Services.AddHttpClient<LexOfficeClient>();
builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var origins = builder.Configuration["FRONTEND_ALLOWED_ORIGINS"]?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                      ?? new[] { $"http://localhost:{builder.Configuration["FRONTEND_PORT"] ?? "8080"}" };
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (!string.IsNullOrWhiteSpace(builder.Configuration["JWT_SECRET"]))
{
    jwtOptions.Secret = builder.Configuration["JWT_SECRET"]!;
}
if (!string.IsNullOrWhiteSpace(builder.Configuration["JWT_ISSUER"]))
{
    jwtOptions.Issuer = builder.Configuration["JWT_ISSUER"]!;
}
if (!string.IsNullOrWhiteSpace(builder.Configuration["JWT_AUDIENCE"]))
{
    jwtOptions.Audience = builder.Configuration["JWT_AUDIENCE"]!;
}
if (int.TryParse(builder.Configuration["JWT_EXPIRY_MINUTES"], out var expiry))
{
    jwtOptions.ExpiryMinutes = expiry;
}

builder.Services.Configure<JwtOptions>(options => CopyJwt(jwtOptions, options));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret))
        };
    });

builder.Services.AddAuthorization();

var seedOnly = args.Contains("--seed");

var app = builder.Build();

await DatabaseSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (seedOnly)
{
    return;
}

app.Run();

static string BuildConnectionString(IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        return connectionString;
    }

    var host = configuration["POSTGRES_HOST"] ?? "db";
    var port = configuration["POSTGRES_PORT"] ?? "5432";
    var database = configuration["POSTGRES_DB"] ?? "lexiflow";
    var user = configuration["POSTGRES_USER"] ?? "lexiflow";
    var password = configuration["POSTGRES_PASSWORD"] ?? "lexiflow";
    return $"Host={host};Port={port};Database={database};Username={user};Password={password}";
}

static void ConfigureOptions(WebApplicationBuilder builder)
{
    builder.Services.Configure<LexOfficeOptions>(options =>
    {
        builder.Configuration.GetSection(LexOfficeOptions.SectionName).Bind(options);
        options.ApiBase = builder.Configuration["LEXOFFICE_API_BASE"] ?? options.ApiBase;
        options.ApiKey = builder.Configuration["LEXOFFICE_API_KEY"] ?? options.ApiKey;
    });

    builder.Services.Configure<OcrOptions>(options =>
    {
        builder.Configuration.GetSection(OcrOptions.SectionName).Bind(options);
        options.ApiBase = builder.Configuration["OCR_API_BASE"] ?? options.ApiBase;
    });

    builder.Services.Configure<StorageOptions>(options =>
    {
        builder.Configuration.GetSection(StorageOptions.SectionName).Bind(options);
        options.UploadsPath = builder.Configuration["UPLOADS_PATH"] ?? options.UploadsPath;
    });
}

static void CopyJwt(JwtOptions source, JwtOptions destination)
{
    destination.Secret = source.Secret;
    destination.Issuer = source.Issuer;
    destination.Audience = source.Audience;
    destination.ExpiryMinutes = source.ExpiryMinutes;
}
