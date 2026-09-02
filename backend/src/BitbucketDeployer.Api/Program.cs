using BitbucketDeployer.Api.Deployment.Clients;
using BitbucketDeployer.Api.Deployment.Configuration;
using BitbucketDeployer.Api.Deployment.Services;
using BitbucketDeployer.Api.ErrorHandling;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuration ----
builder.Services
    .AddOptions<BitbucketOptions>()
    .Bind(builder.Configuration.GetSection(BitbucketOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(o => !string.IsNullOrWhiteSpace(o.Workspace), "Bitbucket:Workspace must be configured.")
    .ValidateOnStart();

builder.Services
    .AddOptions<AuthorizationOptions>()
    .Bind(builder.Configuration.GetSection(AuthorizationOptions.SectionName));

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
            policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

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

// Singleton: this is the in-memory deployment store, and its state must survive across requests
// within the process (see IDeploymentStore for why persistence isn't used yet).
builder.Services.AddSingleton<IDeploymentStore, InMemoryDeploymentStore>();

builder.Services.AddExceptionHandler<BitbucketApiExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

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
