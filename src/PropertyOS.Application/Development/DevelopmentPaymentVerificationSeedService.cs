using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Files;
using PropertyOS.Application.Files.Services;
using PropertyOS.Application.Identity;
using PropertyOS.Application.Leasing.Commands.ActivateLeaseContract;
using PropertyOS.Application.Properties;
using PropertyOS.Domain.Common.Enums;
using PropertyOS.Domain.Files.Entities;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Application.Development;

public class DevelopmentPaymentVerificationSeedService : IDevelopmentPaymentVerificationSeedService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IStorageProvider _storageProvider;
    private readonly IFileStorageRepository _fileStorageRepository;
    private readonly IFloorRepository _floorRepository;
    private readonly ISender _sender;

    public DevelopmentPaymentVerificationSeedService(
        IApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        IStorageProvider storageProvider,
        IFileStorageRepository fileStorageRepository,
        IFloorRepository floorRepository,
        ISender sender)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
        _fileStorageRepository = fileStorageRepository ?? throw new ArgumentNullException(nameof(fileStorageRepository));
        _floorRepository = floorRepository ?? throw new ArgumentNullException(nameof(floorRepository));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public async Task<SeedResult> SeedAsync(Guid companyId, Guid currentOwnerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ExecuteInTransactionAsync(async (ct) =>
        {
            var now = DateTimeOffset.UtcNow;
            var referenceNumber = "DEMO-VERIFY-001";

            // 1. Idempotency Check
            var existingPayment = await _dbContext.RentPayments
                .Include(rp => rp.Submissions)
                .FirstOrDefaultAsync(rp => rp.CompanyId == companyId && rp.Submissions.Any(s => s.ReferenceNumber == referenceNumber), ct);

            if (existingPayment != null)
            {
                var submission = existingPayment.Submissions.First(s => s.ReferenceNumber == referenceNumber);
                return new SeedResult(
                    CompanyId: companyId,
                    TenantUserId: submission.SubmittedBy,
                    TenantId: existingPayment.TenantId,
                    LeaseContractId: existingPayment.LeaseContractId,
                    RentPaymentId: existingPayment.Id,
                    PaymentSubmissionId: submission.Id,
                    ProofFileId: submission.ProofFileId ?? Guid.Empty,
                    ReferenceNumber: submission.ReferenceNumber!,
                    SubmissionStatus: submission.Status.ToString(),
                    DueDateStatus: existingPayment.DueDateStatus.ToString(),
                    AmountPaid: existingPayment.AmountPaid);
            }

            // 2. Tenant User
            var tenantEmail = "tenant.demo@aqarios.test";
            var tenantUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == tenantEmail, ct);
            if (tenantUser == null)
            {
                tenantUser = new User
                {
                    Id = Guid.CreateVersion7(),
                    Email = tenantEmail,
                    FullName = "Verification Demo Tenant User",
                    PasswordHash = _passwordHasher.HashPassword("Demo@123!"),
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _dbContext.Users.Add(tenantUser);
            }

            // 3. Hierarchy Fixtures
            var buildingName = "Demo Verification Building";
            var building = await _dbContext.Buildings.FirstOrDefaultAsync(b => b.CompanyId == companyId && b.Name == buildingName, ct);
            if (building == null)
            {
                building = Building.Create(
                    companyId: companyId,
                    name: buildingName,
                    totalFloors: (short)1,
                    createdAt: now,
                    createdBy: currentOwnerId,
                    id: Guid.CreateVersion7());
                _dbContext.Buildings.Add(building);
                await _dbContext.SaveChangesAsync(ct);
            }

            if (building.Id == Guid.Empty)
            {
                throw new InvalidOperationException("Building.Id must not be empty after building resolution.");
            }

            var floors = await _floorRepository.ListByBuildingIdAsync(building.Id, ct);
            var floor = floors.FirstOrDefault();
            if (floor == null)
            {
                floor = Floor.Create(
                    companyId: companyId,
                    buildingId: building.Id,
                    floorNumber: (short)1,
                    floorLabel: "First Floor",
                    createdAt: now,
                    createdBy: currentOwnerId);
                await _floorRepository.AddAsync(floor, ct);
                await _dbContext.SaveChangesAsync(ct);
            }

            if (floor.Id == Guid.Empty)
            {
                throw new InvalidOperationException("Floor.Id must not be empty after floor resolution.");
            }

            var unitNumber = "101";
            var apartment = await _dbContext.Apartments.FirstOrDefaultAsync(a => a.BuildingId == building.Id && a.UnitNumber == unitNumber, ct);
            if (apartment == null)
            {
                apartment = Apartment.Create(
                    companyId: companyId,
                    buildingId: building.Id,
                    floorId: floor.Id,
                    unitNumber: unitNumber,
                    areaSqm: 100m,
                    createdAt: now,
                    createdBy: currentOwnerId,
                    ownershipStatus: OwnershipStatus.CompanyOwned,
                    externalOwnerName: null,
                    externalOwnerPhone: null,
                    bedrooms: (short)2,
                    bathrooms: (short)2,
                    baseRentAmount: 500m,
                    baseRentCurrency: "JOD");
                _dbContext.Apartments.Add(apartment);
                await _dbContext.SaveChangesAsync(ct);
            }

            var tenantName = "Verification Demo Tenant";
            var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.CompanyId == companyId && t.Name == tenantName, ct);
            if (tenant == null)
            {
                tenant = Tenant.Create(
                    companyId: companyId,
                    name: tenantName,
                    nationalId: "1234567890",
                    phone: "+962790000000",
                    createdAt: now,
                    createdBy: currentOwnerId,
                    occupation: null,
                    employer: null,
                    userId: tenantUser.Id);
                _dbContext.Tenants.Add(tenant);
            }

            var leaseNumber = "DEMO-LC-001";
            var lease = await _dbContext.LeaseContracts.FirstOrDefaultAsync(lc => lc.CompanyId == companyId && lc.ContractNumber == leaseNumber, ct);
            if (lease == null)
            {
                lease = LeaseContract.Create(
                    companyId: companyId,
                    buildingId: building.Id,
                    apartmentId: apartment.Id,
                    tenantId: tenant.Id,
                    contractNumber: leaseNumber,
                    startDate: DateOnly.FromDateTime(now.AddDays(-10).DateTime),
                    endDate: DateOnly.FromDateTime(now.AddYears(1).DateTime),
                    monthlyRentAmount: 500m,
                    paymentFrequency: PaymentFrequency.Monthly,
                    paymentDueDay: (short)1,
                    createdAt: now,
                    createdBy: currentOwnerId,
                    legalRegime: LegalRegime.Standard,
                    tenantType: TenantType.Personal,
                    securityDepositAmount: 500m,
                    status: ContractStatus.Draft,
                    notes: "Demo Seed Lease");

                _dbContext.LeaseContracts.Add(lease);

                // Save context before activating lease to ensure entities exist in DB
                await _dbContext.SaveChangesAsync(ct);

                // Activate Lease via Orchestrator Command
                await _sender.Send(new ActivateLeaseContractCommand(lease.Id), ct);
            }

            // 4. Rent Payment Obligation
            var rentPayment = RentPayment.Create(
                companyId: companyId,
                leaseContractId: lease.Id,
                tenantId: tenant.Id,
                buildingId: building.Id,
                apartmentId: apartment.Id,
                purpose: PaymentPurpose.ScheduledInstallment,
                amountDue: 500m,
                currency: "JOD",
                billingPeriodStart: DateOnly.FromDateTime(now.AddDays(-10).DateTime),
                billingPeriodEnd: DateOnly.FromDateTime(now.AddDays(20).DateTime),
                dueDate: DateOnly.FromDateTime(now.AddDays(-5).DateTime),
                createdAt: now,
                createdBy: currentOwnerId,
                notes: null);

            _dbContext.RentPayments.Add(rentPayment);

            // 5. File Storage Fixture (Dummy PDF)
            var pdfBytes = Encoding.UTF8.GetBytes("%PDF-1.4\n%Demo PDF Fixture\n");
            using var pdfStream = new MemoryStream(pdfBytes);
            var storageKey = $"development/proofs/demo-proof-{Guid.NewGuid()}.pdf";
            await _storageProvider.SaveAsync(storageKey, pdfStream, "application/pdf", ct);

            var fileStorage = FileStorage.Create(
                companyId: companyId,
                uploadedBy: tenantUser.Id,
                originalFilename: "demo-proof.pdf",
                mimeType: "application/pdf",
                sizeBytes: pdfBytes.Length,
                storageKey: storageKey,
                now: now,
                createdBy: tenantUser.Id);
            await _fileStorageRepository.AddAsync(fileStorage, ct);

            // Need to save so fileStorage gets tracked properly for SubmitForVerification
            await _dbContext.SaveChangesAsync(ct);

            // 6. Submit for Verification
            rentPayment.SubmitForVerification(PaymentMethod.BankTransfer, referenceNumber, fileStorage.Id, tenantUser.Id, now);
            await _dbContext.SaveChangesAsync(ct);

            var newSubmission = rentPayment.Submissions.First(s => s.ReferenceNumber == referenceNumber);

            return new SeedResult(
                CompanyId: companyId,
                TenantUserId: tenantUser.Id,
                TenantId: tenant.Id,
                LeaseContractId: lease.Id,
                RentPaymentId: rentPayment.Id,
                PaymentSubmissionId: newSubmission.Id,
                ProofFileId: fileStorage.Id,
                ReferenceNumber: newSubmission.ReferenceNumber!,
                SubmissionStatus: newSubmission.Status.ToString(),
                DueDateStatus: rentPayment.DueDateStatus.ToString(),
                AmountPaid: rentPayment.AmountPaid);

        }, cancellationToken);
    }
}
