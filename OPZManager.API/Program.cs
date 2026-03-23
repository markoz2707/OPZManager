using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using OPZManager.API.Data;
using OPZManager.API.Middleware;
using OPZManager.API.Services;
using OPZManager.API.Services.Embeddings;
using OPZManager.API.Services.LLM;

var builder = WebApplication.CreateBuilder(args);

// Add controllers with JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Configure Entity Framework with PostgreSQL + pgvector
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.UseVector()));

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Add Authorization
builder.Services.AddAuthorization();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "http://localhost")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Register application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPdfProcessingService, PdfProcessingService>();
builder.Services.AddScoped<IPllumIntegrationService, PllumIntegrationService>();
builder.Services.AddScoped<IEquipmentMatchingService, EquipmentMatchingService>();
builder.Services.AddScoped<IOPZGenerationService, OPZGenerationService>();
builder.Services.AddScoped<ITrainingDataService, TrainingDataService>();
builder.Services.AddScoped<IOPZVerificationService, OPZVerificationService>();
builder.Services.AddScoped<ILeadCaptureService, LeadCaptureService>();
builder.Services.AddScoped<PythonPdfProcessingService>();
builder.Services.AddScoped<IFolderImportService, FolderImportService>();

// Register SystemConfigService
builder.Services.AddScoped<ISystemConfigService, SystemConfigService>();

// Register Analysis Progress tracking (singleton — shared across scopes)
builder.Services.AddSingleton<IAnalysisProgressService, AnalysisProgressService>();

// Register named HttpClients for provider factories
builder.Services.AddHttpClient("LlmProvider", client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
});
builder.Services.AddHttpClient("EmbeddingProvider");

// Register provider factories and resolve providers per-request from DB config
builder.Services.AddScoped<ILlmProviderFactory, LlmProviderFactory>();
builder.Services.AddScoped<IEmbeddingProviderFactory, EmbeddingProviderFactory>();
builder.Services.AddScoped<ILlmProvider>(sp =>
{
    var inner = sp.GetRequiredService<ILlmProviderFactory>().GetProvider();
    return new LoggingLlmProviderDecorator(
        inner,
        sp.GetRequiredService<ApplicationDbContext>(),
        sp.GetRequiredService<ILogger<LoggingLlmProviderDecorator>>());
});
builder.Services.AddScoped<IEmbeddingProvider>(sp => sp.GetRequiredService<IEmbeddingProviderFactory>().GetProvider());

// Register document processing queue (singleton background service)
builder.Services.AddSingleton<DocumentProcessingQueue>();
builder.Services.AddSingleton<IDocumentProcessingQueue>(sp => sp.GetRequiredService<DocumentProcessingQueue>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<DocumentProcessingQueue>());

// Register Knowledge Base service
builder.Services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();

// Register DOCX Export service
builder.Services.AddScoped<IDocxExportService, DocxExportService>();

// Register Analytics service
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

// Register SWZ Generation service
builder.Services.AddScoped<ISWZGenerationService, SWZGenerationService>();

// Register Folder Import service
builder.Services.AddScoped<IFolderImportService, FolderImportService>();

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("anonymous", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.RejectionStatusCode = 429;
});

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OPZ Manager API",
        Version = "v1",
        Description = "API do zarządzania dokumentami OPZ i katalogiem sprzętu"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Middleware pipeline (order matters)
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "OPZ Manager API v1");
    });
}

// Use CORS
app.UseCors("AllowReactApp");

// Use Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();

// Rate limiting
app.UseRateLimiter();

app.MapControllers();

// Apply pending migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

app.Run();
