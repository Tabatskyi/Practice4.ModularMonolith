using Modules.Core.Domain;

namespace Modules.Core.Tests.Domain;

public class CoreDomainTests
{
    [Fact]
    public void Ctor_WithEmptyTitle_ThrowsArgumentException()
    {
        var listingId = ListingId.New();

        Assert.Throws<ArgumentException>(() => new Listing(listingId, Guid.NewGuid(), "   ", 100));
    }

    [Fact]
    public void Ctor_WithNonPositivePrice_ThrowsArgumentOutOfRangeException()
    {
        var listingId = ListingId.New();

        Assert.Throws<ArgumentOutOfRangeException>(() => new Listing(listingId, Guid.NewGuid(), "ASUS P8Z68-V", 0));
    }

    [Fact]
    public void ChangeStatus_WithInvalidTransition_ThrowsInvalidOperationException()
    {
        var listing = new Listing(ListingId.New(), Guid.NewGuid(), "ASUS G817JZN", 350);

        Assert.Throws<InvalidOperationException>(() => listing.ChangeStatus(ListingStatus.Sold));
    }

    [Fact]
    public void UpdateDetails_WhenSold_ThrowsInvalidOperationException()
    {
        var listing = new Listing(ListingId.New(), Guid.NewGuid(), "AMD Radeon RX 580", 500);
        listing.ChangeStatus(ListingStatus.Published);
        listing.ChangeStatus(ListingStatus.Sold);

        Assert.Throws<InvalidOperationException>(() => listing.UpdateDetails("AMD Radeon RX 580 Nitro+ 8GB", 450));
    }
}