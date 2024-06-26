﻿using AirlineReservationSystem_Backend.Models;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<ActionResult<List<Seat>>> GetSeats(string planeId, [FromQuery] DateTime travelDate, [FromQuery] string travelTime)
        {
            var seats = await _seatReservationService.GetSeatsAsync(planeId, travelDate, travelTime);
            if (seats == null)
            {
                return NotFound();
            }
            return Ok(seats);
        }

        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<Seat>>> GetAllSeats()
        {
            var seats = await _seatReservationService.GetAllSeatsAsync();
            return Ok(seats);
        }

        [HttpPost("reserve")]
        public async Task<ActionResult<bool>> ReserveSeats([FromBody] ReserveSeatsRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.PlaneId) || request.SeatIds == null || string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.TravelTime))
            {
                return BadRequest("Invalid request data");
            }

            var result = await _seatReservationService.ReserveSeatsAsync(
                request.PlaneId, request.SeatIds, request.UserId, request.UserRole, request.TravelDate, request.TravelTime);

            return result ? Ok(true) : BadRequest("Failed to reserve seats.");
        }

        [HttpPost("lock")]
        public async Task<ActionResult<bool>> LockSeats([FromBody] LockSeatsRequest request)
        {
            var result = await _seatReservationService.LockSeatsAsync(request.PlaneId, request.SeatIds, request.UserId, request.TravelDate, request.TravelTime);
            return result ? Ok(true) : BadRequest(false);
        }

        [HttpPost("unlock")]
        public async Task<ActionResult<bool>> UnlockSeats([FromBody] UnlockSeatsRequest request)
        {
            var result = await _seatReservationService.UnlockSeatsAsync(request.PlaneId, request.SeatIds, request.UserId, request.TravelDate, request.TravelTime);
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
        public async Task<ActionResult<bool>> UnreserveSeats([FromQuery] string planeId, [FromBody] List<string> seatIds, [FromQuery] string userId, [FromQuery] DateTime travelDate, [FromQuery] string travelTime)
        {
            var result = await _seatReservationService.UnreserveSeatsAsync(planeId, seatIds, userId, travelDate, travelTime);
            return result ? Ok(true) : BadRequest("Failed to unreserve seats.");
        }

     
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

        [HttpPost("confirmPayment")]
        public async Task<ActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request)
        {
            var result = await _seatReservationService.ConfirmPaymentAsync(request.BookingId);
            if (result)
            {
                return Ok();
            }
            return BadRequest("Failed to confirm payment.");
        }

        [HttpPost("pay")]
        public async Task<ActionResult<bool>> PayForSeats([FromBody] PaymentRequest paymentRequest)
        {
            var result = await _seatReservationService.ProcessPaymentAsync(paymentRequest);
            return result ? Ok(true) : BadRequest("Payment failed.");
        }

        public class ConfirmPaymentRequest
        {
            public string BookingId { get; set; }
        }

        public class ReserveSeatsRequest
        {
            public string PlaneId { get; set; }
            public List<string> SeatIds { get; set; }
            public string UserId { get; set; }
            public string UserRole { get; set; }
            public DateTime TravelDate { get; set; }
            public string TravelTime { get; set; }
            
        }

        public class LockSeatsRequest
        {
            public string PlaneId { get; set; }
            public List<string> SeatIds { get; set; }
            public string UserId { get; set; }
            public DateTime TravelDate { get; set; }
            public string TravelTime { get; set; }
        }

        public class UnlockSeatsRequest
        {
            public string PlaneId { get; set; }
            public List<string> SeatIds { get; set; }
            public string UserId { get; set; }
            public DateTime TravelDate { get; set; }
            public string TravelTime { get; set; }
        }
    }
}
