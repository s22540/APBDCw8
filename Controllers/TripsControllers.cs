using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TripsAPI.Models;

namespace TripsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly TripsContext _context;

        public TripsController(TripsContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetTrips([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var trips = await _context.Trips
                .OrderByDescending(t => t.DateFrom)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new
                {
                    t.Name,
                    t.Description,
                    t.DateFrom,
                    t.DateTo,
                    t.MaxPeople,
                    Countries = t.CountryTrips.Select(ct => new { ct.Country.Name }).ToList(),
                    Clients = t.ClientTrips.Select(ct => new { ct.Client.FirstName, ct.Client.LastName }).ToList()
                })
                .ToListAsync();

            var totalTrips = await _context.Trips.CountAsync();
            var totalPages = (int)Math.Ceiling(totalTrips / (double)pageSize);

            return Ok(new
            {
                pageNum = page,
                pageSize = pageSize,
                allPages = totalPages,
                trips = trips
            });
        }

        [HttpPost("{idTrip}/clients")]
        public async Task<IActionResult> AddClientToTrip(int idTrip, [FromBody] Client client)
        {
            if (_context.Clients.Any(c => c.Pesel == client.Pesel))
            {
                return BadRequest("Client with this PESEL already exists.");
            }

            if (_context.ClientTrips.Any(ct => ct.IdClient == client.IdClient && ct.IdTrip == idTrip))
            {
                return BadRequest("Client is already registered for this trip.");
            }

            var trip = await _context.Trips.FindAsync(idTrip);
            if (trip == null || trip.DateFrom <= DateTime.Now)
            {
                return BadRequest("Trip does not exist or has already started.");
            }

            client.IdClient = 0;
            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            var clientTrip = new ClientTrip
            {
                IdClient = client.IdClient,
                IdTrip = idTrip,
                RegisteredAt = DateTime.Now,
                PaymentDate = client.ClientTrips.FirstOrDefault()?.PaymentDate
            };
            _context.ClientTrips.Add(clientTrip);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("clients/{idClient}")]
        public async Task<IActionResult> DeleteClient(int idClient)
        {
            var client = await _context.Clients.FindAsync(idClient);
            if (client == null)
            {
                return NotFound("Client not found.");
            }

            if (_context.ClientTrips.Any(ct => ct.IdClient == idClient))
            {
                return BadRequest("Client cannot be deleted because they are assigned to a trip.");
            }

            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}