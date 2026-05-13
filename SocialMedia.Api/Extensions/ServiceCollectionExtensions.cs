using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SocialMedia.Application.Services.Implementations;
using SocialMedia.Application.Services.Interfaces.Common;
using SocialMedia.Application.Services.Interfaces.Repositories;
using SocialMedia.Application.Services.Interfaces.Services;
using SocialMedia.Application.Settings;
using SocialMedia.Infrastructure.Common;
using SocialMedia.Infrastructure.DbContexts;
using SocialMedia.Infrastructure.Repositories;
using SocialMedia.Infrastructure.Security;
using SocialMedia.Infrastructure.Services;
using StackExchange.Redis;
using System.Text;

namespace SocialMedia.Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
        {
            var redisConnection = configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("Redis connection string is not configured.");

            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnection));

            services.AddStackExchangeRedisCache(options =>
                options.Configuration = redisConnection);

            services.AddSingleton<ICacheService, CacheService>();
            services.Configure<CacheSettings>(configuration.GetSection("CacheSettings"));
            services.Configure<FeedSyncSettings>(configuration.GetSection("FeedSyncSettings"));

            return services;
        }

        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContextPool<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            });

            services.AddDbContextPool<ReadOnlyDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("ReadReplicaConnection"));
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            });

            return services;
        }

        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("Jwt");
            var secretKey = jwtSettings["SecretKey"]
                ?? throw new InvalidOperationException("JWT SecretKey is not configured.");

            services
                .AddAuthentication(options =>
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
                        ValidIssuer = jwtSettings["Issuer"],
                        ValidAudience = jwtSettings["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                        ClockSkew = TimeSpan.Zero
                    };
                });

            services.AddAuthorization();

            return services;
        }

        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped(typeof(IReadRepository<>), typeof(ReadRepository<>));
            services.AddScoped<IPostRepository, PostRepository>();
            services.AddScoped<ICommentRepository, CommentRepository>();
            services.AddScoped<ILikeRepository, LikeRepository>();
            services.AddScoped<IPostReadRepository, PostReadRepository>();
            services.AddScoped<ICommentReadRepository, CommentReadRepository>();
            services.AddScoped<ILikeReadRepository, LikeReadRepository>();

            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IFileService, CloudinaryService>();
            services.AddScoped<IPostService, PostService>();
            services.AddScoped<IFeedService, FeedService>();

            return services;
        }

        public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services, IWebHostEnvironment env)
        {
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = $"Social Media APIs - {env.EnvironmentName}",
                    Version = "v1"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Enter your JWT token (without 'Bearer ' prefix — Swagger adds it automatically)",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        []
                    }
                });

                c.EnableAnnotations();
                c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
                c.SupportNonNullableReferenceTypes();
            });

            return services;
        }

        public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
        {
            services.AddCors(options =>
                options.AddPolicy("AllowFrontend", policy =>
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

            return services;
        }
    }
}
