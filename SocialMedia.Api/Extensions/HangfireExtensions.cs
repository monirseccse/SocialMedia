using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using SocialMedia.Application.Services.Interfaces.Services;
using SocialMedia.Infrastructure.Backgroudjobs;

namespace SocialMedia.Api.Extensions
{
        public static class HangfireExtensions
        {
            public static IServiceCollection AddHangfireServices(
                this IServiceCollection services,
                IConfiguration configuration)
            {
                services.AddHangfire(config => config
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UsePostgreSqlStorage(options =>
                        options.UseNpgsqlConnection(
                            configuration.GetConnectionString("DefaultConnection")),
                        new PostgreSqlStorageOptions
                        {
                            DistributedLockTimeout = TimeSpan.FromSeconds(30)
                        }));

                services.AddHangfireServer(options =>
                {
                    options.Queues = new[] { "counts", "default" };
                    options.WorkerCount = 1;
                });

                // Register job
                services.AddScoped<IFeedSyncJob, FeedSyncJob>();

                return services;
            }

            public static IApplicationBuilder UseHangfireServices(
                this IApplicationBuilder app)
            {
                app.UseHangfireDashboard("/hangfire", new DashboardOptions
                {
                    Authorization = new[] { new HangfireAuthFilter() }
                });

                // Register recurring job — every 20s
                RecurringJob.AddOrUpdate<IFeedSyncJob>(
                    recurringJobId: "sync-all-counts",
                    methodCall: job => job.SyncAllCountsAsync(),
                    cronExpression: "*/20 * * * * *",   // every 20s
                    options: new RecurringJobOptions
                    {
                        TimeZone = TimeZoneInfo.Utc
                    });

                return app;
            }
        }

        public class HangfireAuthFilter : IDashboardAuthorizationFilter
        {
            public bool Authorize(DashboardContext context)
            {
                var http = context.GetHttpContext();
                return http.User.Identity?.IsAuthenticated == true
                    && http.User.IsInRole("Admin");
            }
        }
    }
