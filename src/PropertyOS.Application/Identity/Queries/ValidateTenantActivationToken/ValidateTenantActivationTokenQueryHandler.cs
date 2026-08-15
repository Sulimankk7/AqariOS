using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.DTOs.Identity;

namespace PropertyOS.Application.Identity.Queries.ValidateTenantActivationToken;

public class ValidateTenantActivationTokenQueryHandler
    : IRequestHandler<ValidateTenantActivationTokenQuery, TenantActivationStatusDto>
{
    private readonly IApplicationDbContext _dbContext;

    public ValidateTenantActivationTokenQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<TenantActivationStatusDto> Handle(
        ValidateTenantActivationTokenQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return new TenantActivationStatusDto
            {
                Status = "INVALID",
                Message = "رابط التفعيل غير صالح أو مفقود."
            };
        }

        var hashedToken = HashToken(request.Token.Trim());

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.PasswordResetTokenHash == hashedToken && u.DeletedAt == null, cancellationToken);

        if (user == null)
        {
            return new TenantActivationStatusDto
            {
                Status = "ALREADY_USED",
                Message = "رابط التفعيل غير صالح أو تم استخدامه مسبقًا."
            };
        }

        if (user.PasswordResetExpiresAt.HasValue && user.PasswordResetExpiresAt.Value <= DateTimeOffset.UtcNow)
        {
            return new TenantActivationStatusDto
            {
                Status = "EXPIRED",
                ExpiresAt = user.PasswordResetExpiresAt,
                Message = "انتهت صلاحية رابط التفعيل. يرجى طلب رابط تفعيل جديد من إدارة العقار."
            };
        }

        if (!user.IsActive)
        {
            return new TenantActivationStatusDto
            {
                Status = "INVALID",
                Message = "الحساب المرتبط برابط التفعيل معطل."
            };
        }

        return new TenantActivationStatusDto
        {
            Status = "VALID",
            TenantName = user.FullName,
            ExpiresAt = user.PasswordResetExpiresAt,
            Message = "رابط التفعيل صالح ويمكنك الآن إنشاء كلمة المرور."
        };
    }

    private static string HashToken(string token)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(token.Trim());
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
