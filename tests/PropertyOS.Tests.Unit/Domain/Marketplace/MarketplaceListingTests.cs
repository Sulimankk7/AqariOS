using System;
using System.Linq;
using System.Reflection;
using PropertyOS.Domain.Common.Enums;
using PropertyOS.Domain.Common.ValueObjects;
using PropertyOS.Domain.Marketplace;
using PropertyOS.Domain.Marketplace.Enums;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Domain.Marketplace;

public class MarketplaceListingTests
{
    private static void SetListingId(MarketplaceListing listing, Guid id)
    {
        var property = typeof(MarketplaceListing).GetProperty("Id", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property == null)
            throw new InvalidOperationException("Could not find Id property on MarketplaceListing");
        property.SetValue(listing, id);
    }

    private static void SetApartmentId(Apartment apartment, Guid id)
    {
        var property = typeof(Apartment).GetProperty("Id", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property == null)
            throw new InvalidOperationException("Could not find Id property on Apartment");
        property.SetValue(apartment, id);
    }

    private static void SetImageId(ListingImage image, Guid id)
    {
        var property = typeof(ListingImage).GetProperty("Id", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property == null)
            throw new InvalidOperationException("Could not find Id property on ListingImage");
        property.SetValue(image, id);
    }

    [Fact]
    public void Create_WithValidParameters_Succeeds()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var buildingId = Guid.NewGuid();
        var apartmentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();

        // Act
        var listing = MarketplaceListing.Create(
            companyId: companyId,
            buildingId: buildingId,
            apartmentId: apartmentId,
            listingTitle: "Cozy Studio",
            listingDescription: "Fully furnished studio near downtown",
            monthlyRent: 450m,
            securityDeposit: 150m,
            currency: CurrencyCode.JOD,
            contactPhone: new PhoneNumber("+962791234567"),
            contactWhatsapp: new PhoneNumber("+962791234567"),
            expirationDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            isFeatured: true,
            now: now,
            createdBy: userId
        );

        // Assert
        Assert.Equal(companyId, listing.CompanyId);
        Assert.Equal(buildingId, listing.BuildingId);
        Assert.Equal(apartmentId, listing.ApartmentId);
        Assert.Equal("Cozy Studio", listing.ListingTitle);
        Assert.Equal("Fully furnished studio near downtown", listing.ListingDescription);
        Assert.Equal(450m, listing.MonthlyRent);
        Assert.Equal(150m, listing.SecurityDeposit);
        Assert.Equal(CurrencyCode.JOD, listing.Currency);
        Assert.Equal(ListingStatus.Draft, listing.Status);
        Assert.Equal(now, listing.CreatedAt);
        Assert.Equal(userId, listing.CreatedBy);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithBlankTitle_ThrowsArgumentException(string invalidTitle)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => MarketplaceListing.Create(
            companyId: Guid.NewGuid(),
            buildingId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            listingTitle: invalidTitle!,
            listingDescription: "Some desc",
            monthlyRent: 300m,
            securityDeposit: 100m,
            currency: CurrencyCode.JOD,
            contactPhone: new PhoneNumber("+962791234567"),
            contactWhatsapp: null,
            expirationDate: null,
            isFeatured: false,
            now: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid()
        ));
    }

    [Fact]
    public void Create_WithNegativeMonthlyRent_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => MarketplaceListing.Create(
            companyId: Guid.NewGuid(),
            buildingId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            listingTitle: "Valid Title",
            listingDescription: "Valid Description",
            monthlyRent: -50m, // Negative
            securityDeposit: 100m,
            currency: CurrencyCode.JOD,
            contactPhone: new PhoneNumber("+962791234567"),
            contactWhatsapp: null,
            expirationDate: null,
            isFeatured: false,
            now: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid()
        ));
    }

    [Fact]
    public void Update_WhenPublished_ThrowsInvalidOperationException()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var buildingId = Guid.NewGuid();
        var apartmentId = Guid.NewGuid();
        var listing = MarketplaceListing.Create(
            companyId: companyId,
            buildingId: buildingId,
            apartmentId: apartmentId,
            listingTitle: "Title",
            listingDescription: "Desc",
            monthlyRent: 400m,
            securityDeposit: null,
            currency: CurrencyCode.JOD,
            contactPhone: new PhoneNumber("+962791234567"),
            contactWhatsapp: null,
            expirationDate: null,
            isFeatured: false,
            now: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid()
        );

        SetListingId(listing, Guid.NewGuid());

        var apartment = Apartment.Create(
            companyId: companyId,
            buildingId: buildingId,
            floorId: Guid.NewGuid(),
            unitNumber: "101",
            areaSqm: 100m,
            createdAt: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid()
        );
        SetApartmentId(apartment, apartmentId);

        // Add a cover image so publish invariant succeeds
        var img = listing.AddImage(Guid.NewGuid(), true, Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid());
        SetImageId(img, Guid.NewGuid());

        listing.Publish(apartment, DateTimeOffset.UtcNow, Guid.NewGuid()); // Transition to Published

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => listing.UpdateDetails(
            listingTitle: "New Title",
            listingDescription: "New Desc",
            monthlyRent: 450m,
            securityDeposit: 200m,
            currency: CurrencyCode.JOD,
            contactPhone: new PhoneNumber("+962791234567"),
            contactWhatsapp: null,
            expirationDate: null,
            isFeatured: false,
            now: DateTimeOffset.UtcNow,
            updatedBy: Guid.NewGuid()
        ));
    }

    [Fact]
    public void AddImage_WhenImagesLimitExceeded_ThrowsInvalidOperationException()
    {
        // Arrange
        var listing = MarketplaceListing.Create(
            companyId: Guid.NewGuid(),
            buildingId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            listingTitle: "Title",
            listingDescription: "Desc",
            monthlyRent: 400m,
            securityDeposit: null,
            currency: CurrencyCode.JOD,
            contactPhone: new PhoneNumber("+962791234567"),
            contactWhatsapp: null,
            expirationDate: null,
            isFeatured: false,
            now: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid()
        );

        SetListingId(listing, Guid.NewGuid());

        // Add 10 images
        for (int i = 0; i < 10; i++)
        {
            var img = listing.AddImage(Guid.NewGuid(), false, Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid());
            SetImageId(img, Guid.NewGuid());
        }

        // Act & Assert (11th image throws)
        Assert.Throws<InvalidOperationException>(() =>
            listing.AddImage(Guid.NewGuid(), false, Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid())
        );
    }

    [Fact]
    public void SetCover_UpdatesCoverImageContiguously()
    {
        // Arrange
        var listing = MarketplaceListing.Create(
            companyId: Guid.NewGuid(),
            buildingId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            listingTitle: "Title",
            listingDescription: "Desc",
            monthlyRent: 400m,
            securityDeposit: null,
            currency: CurrencyCode.JOD,
            contactPhone: new PhoneNumber("+962791234567"),
            contactWhatsapp: null,
            expirationDate: null,
            isFeatured: false,
            now: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid()
        );

        SetListingId(listing, Guid.NewGuid());

        var img1 = listing.AddImage(Guid.NewGuid(), true, Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid()); // Cover
        SetImageId(img1, Guid.NewGuid());
        var img2 = listing.AddImage(Guid.NewGuid(), false, Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid());
        SetImageId(img2, Guid.NewGuid());

        // Act
        listing.SetCoverImage(img2.Id, DateTimeOffset.UtcNow, Guid.NewGuid());

        // Assert
        Assert.True(listing.Images.First(i => i.Id == img2.Id).IsCover);
        Assert.False(listing.Images.First(i => i.Id == img1.Id).IsCover);
    }
}
