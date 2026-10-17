using Scalar.AspNetCore;
using Balenthiran.Tracks.WebApi;
using Balenthiran.Tracks.WebApi.ExceptionHandling;
using Balenthiran.Tracks.WebApi.Routes;
using Balenthiran.Tracks.Database;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBackendServices(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// A session is an HTTP-only, Secure cookie. SameSite=Lax (not Strict) is required: the OAuth
// redirect back from Strava/Spotify is a cross-site navigation, and Strict would hide the
// cookie from it. Lax still keeps it off cross-site POSTs. An API answers 401/403 rather than
// redirecting to a login page it does not have.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.Cookie.Name = "tracks.session";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        o.ExpireTimeSpan = TimeSpan.FromDays(30);
        o.SlidingExpiration = true;
        o.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
        o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
    });
builder.Services.AddAuthorization();

// Global exception handling: AppExceptionHandler maps thrown AppExceptions to RFC 7807
// ProblemDetails; AddProblemDetails supplies the writer + standard fields.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AppExceptionHandler>();

var app = builder.Build();

// Must sit at the top of the pipeline so it catches exceptions from everything below it.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/scalar/v1");
}

// When the build-time OpenAPI generator (GetDocument.Insider) loads the app purely to emit the
// OpenAPI document it runs this top-level code but never serves requests — it must not touch a
// database, or a Debug build would try to migrate against whatever connection string is configured.
var generatingOpenApiDocument =
    System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

if (!generatingOpenApiDocument)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetService<AppDbContext>();
    if (dbContext is null)
        app.Logger.LogWarning("Skipping database migration — no connection string configured.");
    else if (dbContext.Database.IsRelational())
        dbContext.Database.Migrate();
    else
        dbContext.Database.EnsureCreated(); // the in-memory provider in tests has no migrations
}

// The ingress routes /tracks/api here without stripping the prefix, so the app has
// to know it is hosted under one. Unset locally, where the API sits at the root.
var pathBase = builder.Configuration["PathBase"];
if (!string.IsNullOrWhiteSpace(pathBase))
    app.UsePathBase(pathBase);

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("/api")
    .MapStatusRoutes()
    .MapAuthRoutes()
    .MapStravaRoutes()
    .MapRunsRoutes()
    .WithOpenApi();

app.Run();

// Exposed so the test project can boot the real host in-process via
// WebApplicationFactory<Program> (top-level statements make Program internal by default).
public partial class Program;
