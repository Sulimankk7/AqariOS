using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PropertyOS.Application.Marketplace.Commands.AddListingImage;
using PropertyOS.Application.Marketplace.Commands.ArchiveMarketplaceListing;
using PropertyOS.Application.Marketplace.Commands.CreateMarketplaceListing;
using PropertyOS.Application.Marketplace.Commands.CreateViewingRequest;
using PropertyOS.Application.Marketplace.Commands.DeleteMarketplaceListing;
using PropertyOS.Application.Marketplace.Commands.PublishMarketplaceListing;
using PropertyOS.Application.Marketplace.Commands.RemoveListingImage;
using PropertyOS.Application.Marketplace.Commands.ReorderListingImages;
using PropertyOS.Application.Marketplace.Commands.SetListingCoverImage;
using PropertyOS.Application.Marketplace.Commands.UpdateMarketplaceListing;
using PropertyOS.Application.Marketplace.Commands.UpdateViewingRequestStatus;
using PropertyOS.Application.Marketplace.Queries.Common;
using PropertyOS.Application.Marketplace.Queries.GetCompanyListings;
using PropertyOS.Application.Marketplace.Queries.GetListingDetails;
using PropertyOS.Application.Marketplace.Queries.GetPublicListings;
using PropertyOS.Application.Marketplace.Queries.GetViewingRequests;
using PropertyOS.Domain.Marketplace.Enums;

namespace PropertyOS.Api.Marketplace;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/marketplace")]
[Authorize]
public class MarketplaceController : ControllerBase
{
    private readonly ISender _mediator;

    public MarketplaceController(ISender mediator)
    {
        _mediator = mediator;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Listing Management (Staff authenticated)
    // ─────────────────────────────────────────────────────────────────────────

    [HttpPost("listings")]
    public async Task<ActionResult<Guid>> CreateListing([FromBody] CreateMarketplaceListingCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetListingDetails), new { id }, id);
    }

    [HttpPut("listings/{id:guid}")]
    public async Task<ActionResult> UpdateListing(Guid id, [FromBody] UpdateMarketplaceListingCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID in route does not match ID in body.");

        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPost("listings/{id:guid}/publish")]
    public async Task<ActionResult> PublishListing(Guid id)
    {
        await _mediator.Send(new PublishMarketplaceListingCommand(id));
        return NoContent();
    }

    [HttpPost("listings/{id:guid}/archive")]
    public async Task<ActionResult> ArchiveListing(Guid id)
    {
        await _mediator.Send(new ArchiveMarketplaceListingCommand(id));
        return NoContent();
    }

    [HttpDelete("listings/{id:guid}")]
    public async Task<ActionResult> DeleteListing(Guid id)
    {
        await _mediator.Send(new DeleteMarketplaceListingCommand(id));
        return NoContent();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Image Management (Staff authenticated)
    // ─────────────────────────────────────────────────────────────────────────

    [HttpPost("listings/{id:guid}/images")]
    public async Task<ActionResult<Guid>> AddListingImage(Guid id, [FromBody] AddListingImageCommand command)
    {
        if (id != command.ListingId)
            return BadRequest("Listing ID in route does not match listing ID in body.");

        var imageId = await _mediator.Send(command);
        return Ok(imageId);
    }

    [HttpDelete("listings/{id:guid}/images/{imageId:guid}")]
    public async Task<ActionResult> RemoveListingImage(Guid id, Guid imageId)
    {
        await _mediator.Send(new RemoveListingImageCommand(id, imageId));
        return NoContent();
    }

    [HttpPost("listings/{id:guid}/images/{imageId:guid}/cover")]
    public async Task<ActionResult> SetListingCoverImage(Guid id, Guid imageId)
    {
        await _mediator.Send(new SetListingCoverImageCommand(id, imageId));
        return NoContent();
    }

    [HttpPost("listings/{id:guid}/images/reorder")]
    public async Task<ActionResult> ReorderListingImages(Guid id, [FromBody] List<Guid> imageIds)
    {
        await _mediator.Send(new ReorderListingImagesCommand(id, imageIds));
        return NoContent();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public Listing Retrieval (Anonymous access)
    // ─────────────────────────────────────────────────────────────────────────

    [HttpGet("listings")]
    [AllowAnonymous]
    public async Task<ActionResult<List<PublicListingSummaryDto>>> GetPublicListings([FromQuery] GetPublicListingsQuery query)
    {
        var listings = await _mediator.Send(query);
        return Ok(listings);
    }

    [HttpGet("listings/{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ListingDetailDto>> GetListingDetails(Guid id)
    {
        var listing = await _mediator.Send(new GetListingDetailsQuery(id));
        if (listing == null)
            return NotFound();

        return Ok(listing);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Staff Listing Retrieval (Staff authenticated)
    // ─────────────────────────────────────────────────────────────────────────

    [HttpGet("listings/company")]
    public async Task<ActionResult<List<CompanyListingSummaryDto>>> GetCompanyListings([FromQuery] GetCompanyListingsQuery query)
    {
        var listings = await _mediator.Send(query);
        return Ok(listings);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Viewing Requests (Public submissions + Staff worklist)
    // ─────────────────────────────────────────────────────────────────────────

    [HttpPost("viewing-requests")]
    [AllowAnonymous]
    [EnableRateLimiting("ViewingRequestLimit")]
    [RequestSizeLimit(1024)] // Strict request size limit to prevent DOS
    public async Task<ActionResult<Guid>> CreateViewingRequest([FromBody] CreateViewingRequestCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpGet("viewing-requests")]
    public async Task<ActionResult<List<ViewingRequestSummaryDto>>> GetViewingRequests([FromQuery] GetViewingRequestsQuery query)
    {
        var requests = await _mediator.Send(query);
        return Ok(requests);
    }

    [HttpPost("viewing-requests/{id:guid}/status")]
    public async Task<ActionResult> UpdateViewingRequestStatus(Guid id, [FromBody] UpdateViewingRequestStatusCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID in route does not match ID in body.");

        await _mediator.Send(command);
        return NoContent();
    }
}
