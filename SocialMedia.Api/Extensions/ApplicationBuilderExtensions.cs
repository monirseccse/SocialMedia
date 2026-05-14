using Serilog;
using SocialMedia.Api.Middleware;

namespace SocialMedia.Api.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static WebApplication UseApplicationPipeline(this WebApplication app)
        {
            app.UseMiddleware<GlobalExceptionMiddleware>();
            app.UseSerilogRequestLogging();

            app.UseSwagger();
            app.UseSwaggerUI(options =>
                options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None));

            app.UseCors("AllowFrontend");

            if (!app.Environment.IsDevelopment())
                app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseHangfireServices();
            app.MapControllers();

            return app;
        }
    }
}
