using Serilog;
using SocialMedia.Api.Extensions;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services));

builder.Services
    .AddRedisCache(builder.Configuration)
    .AddDatabase(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddRepositories()
    .AddApplicationServices()
    .AddSwaggerWithJwt(builder.Environment)
    .AddCorsPolicy()
    .AddHangfireServices(builder.Configuration);

var app = builder.Build();

app.UseApplicationPipeline();

try
{
    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
