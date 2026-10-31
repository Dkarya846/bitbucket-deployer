using BitbucketDeployer.Api.Auth.Clients;
using BitbucketDeployer.Api.Auth.Configuration;
using BitbucketDeployer.Api.Auth.Services;
using BitbucketDeployer.Api.Deployment.Clients;
using BitbucketDeployer.Api.Deployment.Configuration;
using BitbucketDeployer.Api.Deployment.Persistence;
using BitbucketDeployer.Api.Deployment.Services;
using BitbucketDeployer.Api.ErrorHandling;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuration ----
builder.Services
    .AddOptions<BitbucketOptions>()
    .Bind(builder.Configuration.GetSection(BitbucketOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(o => !string.IsNullOrWhiteSpace(o.Workspace), "Bitbucket:Workspace must be configured.")
    .ValidateOnStart();

builder.Services
    .AddOptions<BitbucketOAuthOptions>()
    .Bind(builder.Configuration.GetSection(BitbucketOAuthOptions.SectionName))
    .Validate(
        o => !string.IsNullOrWhiteSpace(o.ClientId) && !string.IsNullOrWhiteSpace(o.ClientSecret),
        "Bitbucket:OAuth:ClientId and Bitbucket:OAuth:ClientSecret must be configured.")
    .ValidateOnStart();

builder.Services
    .AddOptions<FrontendOptions>()
    .Bind(builder.Configuration.GetSection(FrontendOptions.SectionName));

builder.Services
    .AddOptions<AuthorizationOptions>()
    .Bind(builder.Configuration.GetSection(AuthorizationOptions.SectionName));

builder.Services.Configure<List<DeploymentEnvironmentOptions>>(
    builder.Configuration.GetSection(DeploymentEnvironmentOptions.SectionName));

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
Console.WriteLine($"CORS allowed origins: {string.Join(", ", corsOrigins)}");
// ---- Services ----
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (corsOrigins.Length > 0)
        {
            // AllowCredentials is required for the session cookie to flow on cross-origin
            // (localhost:4200 -> localhost:5149) requests — only valid with explicit origins,
            // never AllowAnyOrigin, which WithOrigins already guarantees here.
            policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddDataProtection();
builder.Services.AddHttpClient();

builder.Services.AddSingleton<IBitbucketOAuthClient, BitbucketOAuthClient>();
builder.Services.AddScoped<IOAuthSessionStore, OAuthSessionStore>();
builder.Services.AddScoped<ICurrentSessionAccessor, CurrentSessionAccessor>();

builder.Services.AddTransient<BitbucketAuthHandler>();
builder.Services
    .AddHttpClient<IBitbucketClient, BitbucketClient>((sp, client) =>
    {
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BitbucketOptions>>().Value;
        // A missing trailing slash would make HttpClient silently drop the "/2.0" path segment
        // on every relative request (RFC 3986 URI-combining behavior) — guard against that here.
        var baseUrl = options.BaseUrl.EndsWith('/') ? options.BaseUrl : options.BaseUrl + "/";
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    })
    .AddHttpMessageHandler<BitbucketAuthHandler>();

builder.Services.AddScoped<IBitbucketService, BitbucketService>();
builder.Services.AddScoped<IDeploymentValidationService, DeploymentValidationService>();
builder.Services.AddScoped<IDeploymentService, DeploymentService>();
builder.Services.AddScoped<IDeploymentAuthorizationService, DeploymentAuthorizationService>();

// SQLite-backed deployment history — survives backend restarts, unlike the earlier in-memory
// store (InMemoryDeploymentStore is kept around for unit tests). Scoped, not singleton: the
// DbContext is scoped, and persistence itself (not a shared in-process object) is what now
// survives across requests.
var deploymentsConnectionString = builder.Configuration.GetConnectionString("Deployments")
    ?? throw new InvalidOperationException("ConnectionStrings:Deployments must be configured.");
var deploymentsDbDirectory = Path.GetDirectoryName(
    Path.GetFullPath(new SqliteConnectionStringBuilder(deploymentsConnectionString).DataSource));
if (!string.IsNullOrEmpty(deploymentsDbDirectory))
{
    Directory.CreateDirectory(deploymentsDbDirectory);
}

builder.Services.AddDbContext<DeploymentDbContext>(options => options.UseSqlite(deploymentsConnectionString));
builder.Services.AddScoped<IDeploymentStore, SqliteDeploymentStore>();

builder.Services.AddExceptionHandler<BitbucketApiExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// No EF migrations for a schema this small — just ensure the file/tables exist.
using (var startupScope = app.Services.CreateScope())
{
    startupScope.ServiceProvider.GetRequiredService<DeploymentDbContext>().Database.EnsureCreated();
}

// ---- Pipeline ----
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();

// Skipped in Development: the Angular dev server calls the API over plain HTTP by design
// (see environment.development.ts). Redirecting to HTTPS here would bounce the frontend's
// request to a different origin/port before UseCors runs, which the browser reports as a CORS
// failure even though the real cause is the redirect. Re-enable behind a reverse proxy in prod
// if TLS isn't already terminated there.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
