using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AuctionService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuctionsController(AuctionDbContext context) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<AuctionDto>>> GetAuctions(string? date)
        {
            var query = context.Auctions.AsQueryable();

            if (!string.IsNullOrEmpty(date))
            {
                if (!DateTime.TryParse(date,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal
                    | System.Globalization.DateTimeStyles.AssumeUniversal,
                    out var parsedDate))
                {
                    return BadRequest("Invalid date format. Please use a valid date.");
                }

                query = query.Where(x => x.UpdatedAt > parsedDate);
            }

            var auctions = await query
                .OrderBy(x => x.Item.Make)
                .ThenBy(x => x.Item.Model)
                .ProjectToType<AuctionDto>()
                .ToListAsync();

            return auctions;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AuctionDto>> GetAuction(string id)
        {
            var auction = await context.Auctions
                .ProjectToType<AuctionDto>()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (auction == null) return NotFound();

            return auction;
        }

        [HttpPost]
        public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto createAuctionDto)
        {
            var auction = createAuctionDto.Adapt<Auction>();

            // TODO: add current user as seller
            auction.Seller = "TODO: seller";

            context.Auctions.Add(auction);
            await context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAuction),
                new { id = auction.Id }, auction.Adapt<AuctionDto>());
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAuction(string id, UpdateAuctionDto updateAuctionDto)
        {
            var auction = await context.Auctions.FindAsync(id);

            if (auction == null)
            {
                return NotFound();
            }
            if (auction.CurrentHighBid > 0)
            {
                return BadRequest("Cannot update auction with bids.");
            }

            auction.UpdatedAt = DateTime.UtcNow;

            updateAuctionDto.Adapt(auction.Item);

            await context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAuction(string id)
        {
            var auction = await context.Auctions.FindAsync(id);

            if (auction == null)
            {
                return NotFound();
            }
            if (auction.CurrentHighBid > 0)
            {
                return BadRequest("Cannot delete auction with bids.");
            }

            context.Auctions.Remove(auction);
            await context.SaveChangesAsync();
            return NoContent();
        }
    }
}
