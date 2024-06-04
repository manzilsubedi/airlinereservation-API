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
            if (planeId == "plane1")
            {
                planeId = "66587ad30cef16eb96e7fedc";

            }
            else if (planeId == "plane2")
            {
                planeId = "66587ae20cef16eb96e81a6b";
            }
            else
            {
                planeId = "66587ad30cef16eb96e7fedc";
            }
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
        public async Task<ActionResult<bool>> ReserveSeats(string planeId, [FromBody] List<string> seatIds, [FromQuery] string userId, [FromQuery] string userRole, [FromQuery] DateTime travelDate, [FromQuery] string travelTime)
        {
            var result = await  _seatReservationService.ReserveSeatsAsync(planeId, seatIds, userId, userRole,travelDate,travelTime);
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

        [HttpPost("pay")]
        public async Task<ActionResult<bool>> PayForSeats([FromBody] PaymentRequest paymentRequest)
        {
            var result = await _seatReservationService.ProcessPaymentAsync(paymentRequest);
            return result ? Ok(true) : BadRequest("Payment failed.");
        }
        // Add this method to your SeatReservationsController

        [HttpGet("bookings")]
        public async Task<ActionResult<IEnumerable<Booking>>> GetUserBookings([FromQuery] string userId)
        {
            var bookings = await _seatReservationService.GetUserBookingsAsync(userId);
            return Ok(bookings);
        }

        [HttpPost("cancelBooking")]
        public async Task<ActionResult<bool>> CancelBooking([FromBody] CancelBookingRequest request)
        {
            var result = await _seatReservationService.CancelBookingAsync(request.BookingId, request.UserId);
            return result ? Ok(true) : BadRequest(false);
        }

        public class CancelBookingRequest
        {
            public string BookingId { get; set; }
            public string UserId { get; set; }
        }



        public class PaymentRequest
        {
            public string PlaneId { get; set; }
            public List<string> SeatIds { get; set; }
            public string UserId { get; set; }
            public decimal TotalAmount { get; set; }
        }

    }
}
