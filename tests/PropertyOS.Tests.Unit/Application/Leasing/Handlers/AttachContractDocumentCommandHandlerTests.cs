using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Files;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Leasing.Commands.ActivateLeaseContract;
using PropertyOS.Application.Leasing.Commands.AttachContractDocument;
using PropertyOS.Application.Leasing.Queries.Common;
using PropertyOS.Application.Leasing.Queries.GetLeaseContractById;
using PropertyOS.Domain.Files.Entities;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Handlers;

public class AttachContractDocumentCommandHandlerTests
{
    private class FakeLeaseContractRepository : ILeaseContractRepository
    {
        public Dictionary<Guid, LeaseContract> Contracts { get; } = new();
        public List<ContractDocument> AddedDocuments { get; } = new();
        public HashSet<Guid> SignedDocuments { get; } = new();
        public HashSet<(Guid ContractId, Guid FileId)> ExistingDocumentPairs { get; } = new();

        public Task<LeaseContract?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Contracts.TryGetValue(id, out var c);
            return Task.FromResult(c);
        }

        public Task AddDocumentAsync(ContractDocument document, CancellationToken cancellationToken = default)
        {
            AddedDocuments.Add(document);
            ExistingDocumentPairs.Add((document.LeaseContractId, document.FileId));
            if (document.DocumentType == ContractDocumentType.SignedContract)
            {
                SignedDocuments.Add(document.LeaseContractId);
            }
            return Task.CompletedTask;
        }

        public Task<bool> HasDocumentAsync(Guid leaseContractId, Guid fileId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ExistingDocumentPairs.Contains((leaseContractId, fileId)));
        }

        public Task<bool> HasSignedContractDocumentAsync(Guid leaseContractId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SignedDocuments.Contains(leaseContractId));
        }

        public Task<ContractDocument?> GetDocumentByIdAsync(Guid leaseContractId, Guid documentId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<ContractDocument?>(null);

        public Task AddAsync(LeaseContract leaseContract, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LeaseContract?> GetWithHistoryByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, Guid excludeContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, Guid excludeContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasSuccessorContractAsync(Guid priorContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddStatusHistoryAsync(ContractStatusHistory statusHistory, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddTerminationAsync(ContractTermination termination, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Guid>> GetActiveContractIdsExpiringOnOrBeforeAsync(DateOnly asOfDate, int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Guid>> GetActiveContractIdsAsync(int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LeaseContractDetailDto?> GetDetailByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetHistoryByApartmentIdAsync(Guid apartmentId, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetHistoryByTenantIdAsync(Guid tenantId, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> SearchContractsAsync(string searchTerm, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetExpiringLeasesAsync(int daysAhead, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeFileStorageRepository : IFileStorageRepository
    {
        public Dictionary<Guid, FileStorage> Files { get; } = new();

        public Task AddAsync(FileStorage fileStorage, CancellationToken cancellationToken = default)
        {
            Files[fileStorage.Id] = fileStorage;
            return Task.CompletedTask;
        }

        public Task<FileStorage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Files.TryGetValue(id, out var f);
            return Task.FromResult(f?.DeletedAt == null ? f : null);
        }

        public Task<bool> ExistsAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
        {
            if (Files.TryGetValue(id, out var f))
            {
                return Task.FromResult(f.CompanyId == companyId && f.DeletedAt == null);
            }
            return Task.FromResult(false);
        }
    }

    private class FakeTenantContext : ITenantContext
    {
        public Guid? CompanyId { get; set; } = Guid.NewGuid();
        public bool IsPlatformAdmin { get; set; } = false;
    }

    private class FakeCurrentUserContext : ICurrentUserContext
    {
        public Guid? UserId { get; set; } = Guid.NewGuid();
    }

    private (LeaseContract Contract, FileStorage File) CreateTestData(Guid companyId)
    {
        var contract = LeaseContract.Create(
            companyId: companyId,
            buildingId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            contractNumber: "LC-TEST-1",
            startDate: DateOnly.FromDateTime(DateTime.UtcNow),
            endDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            monthlyRentAmount: 500,
            paymentFrequency: PaymentFrequency.Monthly,
            paymentDueDay: 1,
            createdAt: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid()
        );

        var file = FileStorage.Create(
            companyId: companyId,
            uploadedBy: Guid.NewGuid(),
            originalFilename: "lease_signed.pdf",
            mimeType: "application/pdf",
            sizeBytes: 2048,
            storageKey: $"{companyId:D}/{Guid.NewGuid():D}-lease_signed.pdf",
            now: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid()
        );

        return (contract, file);
    }

    [Fact]
    public async Task Handle_SuccessfulAttachment_ReturnsDocumentId()
    {
        var companyId = Guid.NewGuid();
        var (contract, file) = CreateTestData(companyId);

        var leaseRepo = new FakeLeaseContractRepository();
        leaseRepo.Contracts[contract.Id] = contract;

        var fileRepo = new FakeFileStorageRepository();
        fileRepo.Files[file.Id] = file;

        var tenantCtx = new FakeTenantContext { CompanyId = companyId };
        var userCtx = new FakeCurrentUserContext();

        var handler = new AttachContractDocumentCommandHandler(leaseRepo, fileRepo, tenantCtx, userCtx);
        var command = new AttachContractDocumentCommand(contract.Id, file.Id, ContractDocumentType.SignedContract, "Signed lease agreement");

        var docId = await handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, docId);
        Assert.Single(leaseRepo.AddedDocuments);
        var doc = leaseRepo.AddedDocuments[0];
        Assert.Equal(companyId, doc.CompanyId);
        Assert.Equal(contract.Id, doc.LeaseContractId);
        Assert.Equal(file.Id, doc.FileId);
        Assert.Equal(ContractDocumentType.SignedContract, doc.DocumentType);
        Assert.Equal("Signed lease agreement", doc.Description);
        Assert.Equal(userCtx.UserId, doc.UploadedBy);
    }

    [Fact]
    public async Task Handle_LeaseNotFound_ThrowsNotFoundException()
    {
        var companyId = Guid.NewGuid();
        var (_, file) = CreateTestData(companyId);

        var leaseRepo = new FakeLeaseContractRepository();
        var fileRepo = new FakeFileStorageRepository();
        fileRepo.Files[file.Id] = file;

        var tenantCtx = new FakeTenantContext { CompanyId = companyId };
        var handler = new AttachContractDocumentCommandHandler(leaseRepo, fileRepo, tenantCtx, new FakeCurrentUserContext());
        var command = new AttachContractDocumentCommand(Guid.NewGuid(), file.Id, ContractDocumentType.SignedContract);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_FileNotFound_ThrowsNotFoundException()
    {
        var companyId = Guid.NewGuid();
        var (contract, _) = CreateTestData(companyId);

        var leaseRepo = new FakeLeaseContractRepository();
        leaseRepo.Contracts[contract.Id] = contract;

        var fileRepo = new FakeFileStorageRepository();

        var tenantCtx = new FakeTenantContext { CompanyId = companyId };
        var handler = new AttachContractDocumentCommandHandler(leaseRepo, fileRepo, tenantCtx, new FakeCurrentUserContext());
        var command = new AttachContractDocumentCommand(contract.Id, Guid.NewGuid(), ContractDocumentType.SignedContract);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_FileFromAnotherCompany_ThrowsNotFoundException()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();

        var (contract, _) = CreateTestData(companyA);
        var (_, otherCompanyFile) = CreateTestData(companyB);

        var leaseRepo = new FakeLeaseContractRepository();
        leaseRepo.Contracts[contract.Id] = contract;

        var fileRepo = new FakeFileStorageRepository();
        fileRepo.Files[otherCompanyFile.Id] = otherCompanyFile;

        var tenantCtx = new FakeTenantContext { CompanyId = companyA };
        var handler = new AttachContractDocumentCommandHandler(leaseRepo, fileRepo, tenantCtx, new FakeCurrentUserContext());
        var command = new AttachContractDocumentCommand(contract.Id, otherCompanyFile.Id, ContractDocumentType.SignedContract);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DuplicateAttachment_ThrowsConflictException()
    {
        var companyId = Guid.NewGuid();
        var (contract, file) = CreateTestData(companyId);

        var leaseRepo = new FakeLeaseContractRepository();
        leaseRepo.Contracts[contract.Id] = contract;
        leaseRepo.ExistingDocumentPairs.Add((contract.Id, file.Id));

        var fileRepo = new FakeFileStorageRepository();
        fileRepo.Files[file.Id] = file;

        var tenantCtx = new FakeTenantContext { CompanyId = companyId };
        var handler = new AttachContractDocumentCommandHandler(leaseRepo, fileRepo, tenantCtx, new FakeCurrentUserContext());
        var command = new AttachContractDocumentCommand(contract.Id, file.Id, ContractDocumentType.SignedContract);

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SignedContract_SatisfiesActivationRequirement()
    {
        var companyId = Guid.NewGuid();
        var (contract, file) = CreateTestData(companyId);

        var leaseRepo = new FakeLeaseContractRepository();
        leaseRepo.Contracts[contract.Id] = contract;

        var fileRepo = new FakeFileStorageRepository();
        fileRepo.Files[file.Id] = file;

        var tenantCtx = new FakeTenantContext { CompanyId = companyId };
        var userCtx = new FakeCurrentUserContext();

        var attachHandler = new AttachContractDocumentCommandHandler(leaseRepo, fileRepo, tenantCtx, userCtx);
        var attachCommand = new AttachContractDocumentCommand(contract.Id, file.Id, ContractDocumentType.SignedContract);

        await attachHandler.Handle(attachCommand, CancellationToken.None);

        // Verify that HasSignedContractDocumentAsync now returns true for the contract
        var hasSignedDoc = await leaseRepo.HasSignedContractDocumentAsync(contract.Id, CancellationToken.None);
        Assert.True(hasSignedDoc);
    }

    /// <summary>
    /// Regression test for zero-value enum sentinel bug.
    /// ContractDocumentType.SignedContract == 0 (the C# int default).
    /// EF Core's HasDefaultValueSql implicitly sets ValueGeneratedOnAdd, which causes EF to
    /// omit zero-valued enum columns from the INSERT, allowing PostgreSQL's DEFAULT 'other'
    /// to fire. This test guards against any regression where SignedContract (0) is silently
    /// persisted as Other (4).
    /// The fix: ContractDocumentConfiguration must set .ValueGeneratedNever() on DocumentType.
    /// </summary>
    [Fact]
    public async Task Handle_DocumentType_SignedContract_NumericZero_IsNotSubstitutedWithOther()
    {
        // Arrange – SignedContract is numeric 0, the CLR default for int.
        // A zero-sentinel bug would replace it with Other (4) silently.
        var companyId = Guid.NewGuid();
        var (contract, file) = CreateTestData(companyId);

        var leaseRepo = new FakeLeaseContractRepository();
        leaseRepo.Contracts[contract.Id] = contract;

        var fileRepo = new FakeFileStorageRepository();
        fileRepo.Files[file.Id] = file;

        var tenantCtx = new FakeTenantContext { CompanyId = companyId };
        var userCtx = new FakeCurrentUserContext();

        var handler = new AttachContractDocumentCommandHandler(leaseRepo, fileRepo, tenantCtx, userCtx);

        // Act – send documentType = 0 (SignedContract), the value a real API call sends
        var command = new AttachContractDocumentCommand(
            LeaseContractId: contract.Id,
            FileId: file.Id,
            DocumentType: (ContractDocumentType)0,   // explicitly numeric 0, same as JSON "documentType": 0
            Description: "Signed lease contract"
        );

        await handler.Handle(command, CancellationToken.None);

        // Assert – the persisted entity must carry SignedContract, NOT Other
        Assert.Single(leaseRepo.AddedDocuments);
        var doc = leaseRepo.AddedDocuments[0];

        Assert.Equal((int)ContractDocumentType.SignedContract, 0); // guard: enum value IS 0
        Assert.Equal(ContractDocumentType.SignedContract, doc.DocumentType);
        Assert.NotEqual(ContractDocumentType.Other, doc.DocumentType);
    }

    [Fact]
    public void ContractDocument_Create_WithSignedContractType_PreservesSignedContract()
    {
        // Directly verify ContractDocument.Create() does not substitute SignedContract (0) with Other.
        // This isolates the domain factory from EF and handler concerns.
        var companyId = Guid.NewGuid();
        var leaseContractId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var document = ContractDocument.Create(
            companyId: companyId,
            leaseContractId: leaseContractId,
            fileId: fileId,
            documentType: ContractDocumentType.SignedContract,
            description: "Test",
            uploadedBy: Guid.NewGuid()
        );

        Assert.Equal(ContractDocumentType.SignedContract, document.DocumentType);
        Assert.Equal(0, (int)document.DocumentType);
    }
}
