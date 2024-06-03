using AirlineReservationSystem_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AirlineReservationSystem_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeatReservationsController : ControllerBase
    {
        private readonly SeatReservationService _seatReservationService;

        public SeatReservationsController(SeatReservationService seatReservationService)
        {
            _seatReservationService = seatReservationService;
        }

        [HttpGet("{planeId}")]
        public async Task<ActionResult<IEnumerable<Seat>>> GetSeats(string planeId)
        {
            var seats = await _seatReservationService.GetSeatsAsync(planeId);
            return Ok(seats);
        }

        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<Seat>>> GetAllSeats()
        {
            var seats = await _seatReservationService.GetAllSeatsAsync();
            return Ok(seats);
        }

        [HttpPost("reserve")]
        public async Task<ActionResult<bool>> ReserveSeats(string planeId, [FromBody] List<string> seatIds, [FromQuery] string userId, [FromQuery] string userRole)
        {
            var result = await  _seatReservationService.ReserveSeatsAsync(planeId, seatIds, userId, userRole);
            if (result)
            {
                return Ok(true);
            }
            return BadRequest("These seats cannot be booked. Please try selecting multiple seats together.");
        
        }

        [HttpPost("lock")]
        public async Task<ActionResult<bool>> LockSeats(string planeId, [FromBody] List<string> seatIds, [FromQuery] string userId)
        {
            var result = await _seatReservationService.LockSeatsAsync(planeId, seatIds, userId);
            return result ? Ok(true) : BadRequest(false);
        }

        [HttpPost("unlock")]
        public async Task<ActionResult<bool>> UnlockSeats(string planeId, [FromBody] List<string> seatIds, [FromQuery] string userId)
        {
            var result = await _seatReservationService.UnlockSeatsAsync(planeId, seatIds, userId);
            return result ? Ok(true) : BadRequest(false);
        }

        [HttpPost("unlockAll")]
        public async Task<ActionResult<bool>> UnlockAllSeats([FromBody] UnlockAllRequest request)
        {
            var result = await _seatReservationService.UnlockAllSeatsAsync(request.UserId);
            return result ? Ok(true) : BadRequest(false);
        }

        public class UnlockAllRequest
        {
            public string UserId { get; set; }
        }

        [HttpPost("cancel")]
        public async Task<ActionResult<bool>> CancelReservation(string planeId, string seatId, [FromQuery] string userId)
        {
            var result = await _seatReservationService.CancelReservationAsync(planeId, seatId, userId);
            return result ? Ok(true) : BadRequest(false);
        }

        [HttpPost("unreserve")]
        public async Task<ActionResult<bool>> UnreserveSeats(string planeId, [FromBody] List<string> seatIds, [FromQuery] string userId, [FromQuery] string userRole)
        {
            if (userRole != "staff" && userRole != "management")
            {
                return Forbid("Only staff and management can unreserve seats.");
            }

            var result = await _seatReservationService.UnreserveSeatsAsync(planeId, seatIds, userId);
            return result ? Ok(true) : BadRequest("Failed to unreserve seats.");
        }

    }
}
