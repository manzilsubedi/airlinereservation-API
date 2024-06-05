using AirlineReservationSystem_Backend.Models;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using MongoDB.Bson;

public class SeatReservationService
{
    private readonly IMongoCollection<Seat> _seats;
    private readonly IMongoCollection<Booking> _bookings;
    private readonly IMongoCollection<FlightInstance> _flightInstances;
    private readonly IMongoCollection<Reservation> _reservations;

    public SeatReservationService(IMongoDatabase database)
    {
        _seats = database.GetCollection<Seat>("Seat");
        _bookings = database.GetCollection<Booking>("Booking");
        _flightInstances = database.GetCollection<FlightInstance>("FlightInstance");
        _reservations = database.GetCollection<Reservation>("Reservations");
    }

    public async Task<List<Seat>> GetSeatsAsync(string planeId, DateTime travelDate, string travelTime)
    {
        var allSeats = await _seats.Find(seat => seat.PlaneId == planeId).ToListAsync();

        var reservedSeats = await _reservations.Find(res => res.PlaneId == planeId && res.TravelDate == travelDate && res.TravelTime == travelTime && res.IsReserved).ToListAsync();

        foreach (var reserved in reservedSeats)
        {
            var seat = allSeats.FirstOrDefault(s => s.Id == reserved.SeatId);
            if (seat != null)
            {
                seat.IsReserved = true;
            }
        }

        return allSeats;
    }

    public async Task<List<Seat>> GetAllSeatsAsync()
    {
        return await _seats.Find(seat => true).ToListAsync();
    }

    public async Task<FlightInstance> GetOrCreateFlightInstanceAsync(string planeId, DateTime travelDate, string travelTime)
    {
        var filter = Builders<FlightInstance>.Filter.Where(f => f.PlaneId == planeId && f.TravelDate == travelDate && f.TravelTime == travelTime);
        var flightInstance = await _flightInstances.Find(filter).FirstOrDefaultAsync();

        if (flightInstance == null)
        {
            flightInstance = new FlightInstance
            {
                PlaneId = planeId,
                TravelDate = travelDate,
                TravelTime = travelTime
            };
            await _flightInstances.InsertOneAsync(flightInstance);
        }

        return flightInstance;
    }

    public async Task<bool> ReserveSeatsAsync(string planeId, List<string> seatIds, string userId, string userRole, DateTime travelDate, string travelTime)
    {
        var seats = await _seats.Find(s => seatIds.Contains(s.Id)).ToListAsync();

        foreach (var seatId in seatIds)
        {
            var reservation = new Reservation
            {
                Id = ObjectId.GenerateNewId().ToString(),
                PlaneId = planeId,
                SeatId = seatId,
                UserId = userId,
                TravelDate = travelDate,
                TravelTime = travelTime,
                IsReserved = true
            };

            await _reservations.InsertOneAsync(reservation);
        }

        var booking = new Booking
        {
            Id = ObjectId.GenerateNewId().ToString(),
            UserId = userId,
            PlaneId = planeId,
            Seats = seats,
            TotalPrice = seatIds.Count() * 100,
            BookingDate = DateTime.UtcNow,
            TravelDate = travelDate,
            TravelTime = travelTime,
            IsPaid = false
        };

        await _bookings.InsertOneAsync(booking);

        return true;
    }

    public async Task<bool> LockSeatsAsync(string planeId, List<string> seatIds, string userId, DateTime travelDate, string travelTime)
    {
        foreach (var seatId in seatIds)
        {
            var reservation = new Reservation
            {
                Id = ObjectId.GenerateNewId().ToString(),
                PlaneId = planeId,
                SeatId = seatId,
                UserId = userId,
                TravelDate = travelDate,
                TravelTime = travelTime,
                IsLocked = true
            };

            await _reservations.InsertOneAsync(reservation);
        }

        return true;
    }

    public async Task<bool> UnlockSeatsAsync(string planeId, List<string> seatIds, string userId, DateTime travelDate, string travelTime)
    {
        foreach (var seatId in seatIds)
        {
            var filter = Builders<Reservation>.Filter.Where(res => res.PlaneId == planeId && res.SeatId == seatId && res.UserId == userId && res.TravelDate == travelDate && res.TravelTime == travelTime && res.IsLocked);
            var update = Builders<Reservation>.Update.Set(res => res.IsLocked, false);

            await _reservations.UpdateOneAsync(filter, update);
        }

        return true;
    }

    public async Task<bool> UnreserveSeatsAsync(string planeId, List<string> seatIds, string userId, DateTime travelDate, string travelTime)
    {
        foreach (var seatId in seatIds)
        {
            var filter = Builders<Reservation>.Filter.Where(res => res.PlaneId == planeId && res.SeatId == seatId && res.UserId == userId && res.TravelDate == travelDate && res.TravelTime == travelTime && res.IsReserved);
            var update = Builders<Reservation>.Update.Set(res => res.IsReserved, false);

            await _reservations.UpdateOneAsync(filter, update);
        }

        return true;
    }

    public async Task<bool> UnlockAllSeatsAsync(string userId)
    {
        var filter = Builders<Reservation>.Filter.Where(res => res.UserId == userId && res.IsLocked);
        var update = Builders<Reservation>.Update.Set(res => res.IsLocked, false);

        var result = await _reservations.UpdateManyAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> CancelReservationAsync(string planeId, string seatId, string userId)
    {
        var seat = await _seats.Find(s => s.PlaneId == planeId && s.Id == seatId && s.IsReserved).FirstOrDefaultAsync();
        if (seat != null)
        {
            seat.IsReserved = false;
            seat.UserId = null;
            await _seats.ReplaceOneAsync(s => s.Id == seat.Id, seat);
            return true;
        }
        return false;
    }

    public async Task<bool> ProcessPaymentAsync(PaymentRequest paymentRequest)
    {
        // Simulate payment processing
        await Task.Delay(1000);

        var booking = await _bookings.Find(b => b.Id == paymentRequest.BookingId).FirstOrDefaultAsync();
        if (booking == null)
        {
            return false;
        }

        var filter = Builders<Booking>.Filter.Eq(b => b.Id, paymentRequest.BookingId);
        var update = Builders<Booking>.Update
            .Set(b => b.IsPaid, true)
            .Set(b => b.Passengers, paymentRequest.Passengers);  // Save passengers information

        var result = await _bookings.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> CancelBookingAsync(string bookingId, string userId)
    {
        var booking = await _bookings.Find(b => b.Id == bookingId && b.UserId == userId).FirstOrDefaultAsync();
        if (booking == null)
        {
            return false;
        }

        // Reset the IsReserved status and UserId for the seats associated with the booking
        var update = Builders<Seat>.Update.Set(s => s.IsReserved, false).Set(s => s.UserId, null).Set(s => s.IsLocked, false);
        var seatIds = booking.Seats.Select(s => s.Id).ToList();
        await _seats.UpdateManyAsync(s => seatIds.Contains(s.Id), update);

        // Delete the booking
        await _bookings.DeleteOneAsync(b => b.Id == bookingId);

        return true;
    }

    public async Task<bool> ConfirmPaymentAsync(string bookingId)
    {
        var filter = Builders<Booking>.Filter.Eq(b => b.Id, bookingId);
        var update = Builders<Booking>.Update.Set(b => b.IsPaid, true);
        var result = await _bookings.UpdateOneAsync(filter, update);

        return result.ModifiedCount > 0;
    }

    public async Task<List<Booking>> GetUserBookingsAsync(string userId)
    {
        return await _bookings.Find(b => b.UserId == userId).ToListAsync();
    }
}
