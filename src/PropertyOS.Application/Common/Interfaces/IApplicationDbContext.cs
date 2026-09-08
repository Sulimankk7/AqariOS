using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using PropertyOS.Domain.Companies;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Domain.Audit.Entities;
using PropertyOS.Domain.Subscriptions;

namespace PropertyOS.Application.Common.Interfaces;

/// <summary>
/// Application database context interface exposing core entity DbSets and persistence operations.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Company> Companies { get; }
    DbSet<CompanySettings> CompanySettings { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserCompanyRole> UserCompanyRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<LoginHistory> LoginHistory { get; }
    DbSet<UserSystemRole> UserSystemRoles { get; }
    DbSet<LandlordRegistration> LandlordRegistrations { get; }
    DbSet<PropertyOS.Domain.PlatformAdministration.ContactRequest> ContactRequests { get; }
    DbSet<PasswordResetChallenge> PasswordResetChallenges => throw new NotSupportedException();
    DbSet<AuditLog> AuditLogs => throw new NotSupportedException();
    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<CompanySubscription> CompanySubscriptions { get; }
    DbSet<PlanChangeRequest> PlanChangeRequests { get; }
    DbSet<PaygUsagePeriod> PaygUsagePeriods => throw new NotSupportedException();
    DbSet<PaygLeaseUsage> PaygLeaseUsage => throw new NotSupportedException();

    DbSet<PropertyOS.Domain.Properties.Building> Buildings { get; }
    DbSet<PropertyOS.Domain.Properties.Floor> Floors => throw new NotSupportedException();
    DbSet<PropertyOS.Domain.Properties.Apartment> Apartments { get; }
    DbSet<PropertyOS.Domain.Leasing.LeaseContract> LeaseContracts { get; }
    DbSet<PropertyOS.Domain.Leasing.ContractStatusHistory> ContractStatusHistory => throw new NotSupportedException();
    DbSet<PropertyOS.Domain.Leasing.ContractTermination> ContractTerminations => throw new NotSupportedException();
    DbSet<PropertyOS.Domain.Leasing.Tenant> Tenants { get; }
    DbSet<PropertyOS.Domain.Financials.RentPayment> RentPayments { get; }
    DbSet<PropertyOS.Domain.Financials.PaymentAllocation> PaymentAllocations { get; }
    DbSet<PropertyOS.Domain.Financials.PaymentSubmission> PaymentSubmissions { get; }
    DbSet<PropertyOS.Domain.Financials.RentPaymentReceipt> RentPaymentReceipts { get; }
    DbSet<PropertyOS.Domain.Financials.Expense> Expenses { get; }
    DbSet<PropertyOS.Domain.UtilityBills.UtilityAccount> UtilityAccounts { get; }
    DbSet<PropertyOS.Domain.UtilityBills.UtilityBill> UtilityBills { get; }


    DatabaseFacade Database { get; }
    bool SupportsAtomicOperations => false;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task<TResult> ExecuteInTransactionAsync<TResult>(System.Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(System.Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
}
