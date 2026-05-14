using Microsoft.EntityFrameworkCore;
using SocialMedia.Application.ClientModels.RequestModel;
using SocialMedia.Application.ClientModels.ResponseModel;
using SocialMedia.Application.Services.Interfaces.Common;
using SocialMedia.Domain.Entities;
using SocialMedia.Infrastructure.DbContexts;
using System;
using System.Security.Claims;

namespace SocialMedia.Infrastructure.Security
{
    public class AuthService : IAuthService
    {
        private readonly ITokenService _jwt;
        private readonly IPasswordHasher _hasher;
        private readonly ApplicationDbContext _db;

        public AuthService(ITokenService jwt, IPasswordHasher hasher, ApplicationDbContext db)
        {
            _jwt = jwt;
            _hasher = hasher;
            _db = db;
        }

        public async Task<AuthResponse> Login(string email, string password)
        {
            var user = await _db.Users
                .Include(x => x.RefreshTokens)
                .FirstOrDefaultAsync(x => x.Email == email);

            if (user == null || !_hasher.Verify(user.PasswordHash, password))
                throw new Exception("Invalid credentials");

            var accessToken = _jwt.GenerateAccessToken(user);
            var refreshToken = _jwt.GenerateRefreshToken();

            user.RefreshTokens.Add(refreshToken);
            await _db.SaveChangesAsync();

            return new AuthResponse { AccessToken = accessToken, RefreshToken = refreshToken.Token, FullName = $"{user.FirstName} {user.LastName}" };
        }

        public async Task<AuthResponse> RefreshTokenAsync(string expiredToken, string refreshToken)
        {
            var principal = _jwt.GetPrincipalFromExpiredToken(expiredToken);
            if (principal == null) throw new Exception("Invalid token");

            var userId = int.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var user = await _db.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var existingToken = user?.RefreshTokens.SingleOrDefault(x => x.Token == refreshToken);

            if (existingToken == null || existingToken.IsRevoked || existingToken.ExpiresAt <= DateTime.UtcNow)
                throw new Exception("Invalid or expired refresh token");

            existingToken.IsRevoked = true;
            return await CreateAuthResponseAndSave(user!);
        }

        private async Task<AuthResponse> CreateAuthResponseAndSave(User user)
        {
            var accessToken = _jwt.GenerateAccessToken(user);
            var newRefreshToken = _jwt.GenerateRefreshToken();
            user.RefreshTokens.Add(newRefreshToken);
            var tokensToRemove = user.RefreshTokens
                .Where(t => t.IsRevoked || t.ExpiresAt < DateTime.UtcNow.AddDays(-7))
                .ToList();

            foreach (var oldToken in tokensToRemove)
            {
                _db.Entry(oldToken).State = EntityState.Deleted;
            }
            await _db.SaveChangesAsync();

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken.Token,
                FullName = $"{user.FirstName} {user.LastName}"
            };
        }


        public async Task<AuthResponse> RegisterAsync(RegisterRequest model)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var existingUser = await _db.Users
                    .AnyAsync(x => x.Email == model.Email);

                if (existingUser)
                    throw new Exception("Email already exists");

                var user = new User
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    PasswordHash = _hasher.Hash(model.Password),
                    CreatedAt = DateTime.UtcNow
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                var accessToken = _jwt.GenerateAccessToken(user);
                var refreshToken = _jwt.GenerateRefreshToken();

                user.RefreshTokens.Add(refreshToken);

                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                return new AuthResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken.Token,
                    FullName = $"{user.FirstName} {user.LastName}"
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
