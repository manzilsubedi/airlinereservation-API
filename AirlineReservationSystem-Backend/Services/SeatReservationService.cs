using AirlineReservationSystem_Backend.Models;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using System.Linq;
using static AirlineReservationSystem_Backend.Controllers.SeatReservationsController;
using System.Numerics;
using Microsoft.EntityFrameworkCore;

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

    //public async Task<List<Seat>> GetSeatsAsync(string planeId)
    //{
    //    return await _seats.Find(seat => seat.PlaneId == planeId).ToListAsync();
    //}
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
    //public async Task<bool> ReserveSeatsAsync(string planeId, List<string> seatIds, string userId)
    //{
    //    var seats = await _seats.Find(s => s.PlaneId == planeId && seatIds.Contains(s.Id)).ToListAsync();
    //    foreach (var seat in seats)
    //    {
    //        seat.IsReserved = true;
    //        seat.UserId = userId;
    //        await _seats.ReplaceOneAsync(s => s.Id == seat.Id, seat);
    //    }
    //    return true;
    //}


    //public async Task<bool> ReserveSeatsAsync(string planeId, List<string> seatIds, string userId, string userRole, DateTime travelDate, string travelTime)
    //{
    //    if (seatIds.Count > 6)
    //    {
    //        throw new InvalidOperationException("You cannot book more than six seats at once.");
    //    }

    //    //var seats = await _seats.Find(s => s.PlaneId == planeId && seatIds.Contains(s.Id)).ToListAsync();
    //    var seats = await _seats.Find(s => s.PlaneId == planeId && seatIds.Contains(s.Id) && s.TravelDate == travelDate && s.TravelTime == travelTime).ToListAsync();
    //    if (userRole == "user")
    //    {
    //        var invalidSeats = seats.Where(seat =>
    //      (int.Parse(seat.Row) >= 4 && (seat.Column == "B" || seat.Column == "E"))).ToList();

    //        if (invalidSeats.Count == 1 && seats.Count < 2)
    //        {
    //            return false; // Validation failed
    //        }

    //        var rows = seats.GroupBy(s => s.Row).ToDictionary(g => g.Key, g => g.ToList());

    //        foreach (var row in rows)
    //        {
    //            var columns = row.Value.Select(s => s.Column).OrderBy(c => c).ToList();
    //            for (int i = 0; i < columns.Count - 1; i++)
    //            {
    //                var currentColumn = columns[i];
    //                var nextColumn = columns[i + 1];

    //                if (!IsAdjacent(currentColumn, nextColumn))
    //                {
    //                    throw new InvalidOperationException("You can only book adjacent seats.");
    //                }
    //            }
    //        }

    //        // Ensure seats are in the same row or adjacent rows
    //        var rowNumbers = rows.Keys.Select(int.Parse).OrderBy(r => r).ToList();
    //        for (int i = 0; i < rowNumbers.Count - 1; i++)
    //        {
    //            if (rowNumbers[i + 1] - rowNumbers[i] > 1)
    //            {
    //                throw new InvalidOperationException("You can only book seats in the same row or adjacent rows.");
    //            }
    //        }
    //    }

    //    foreach (var seat in seats)
    //    {
    //        seat.IsReserved = true;
    //        seat.UserId = userId;
    //        seat.TravelDate = travelDate;
    //        seat.TravelTime = travelTime;
    //    }

    //    var totalPrice = seats.Sum(s => (double)s.Price);
    //    var booking = new Booking
    //    {
    //        Id = ObjectId.GenerateNewId().ToString(), // Generate a new ObjectId
    //        UserId = userId,
    //        PlaneId = planeId,
    //        Seats = seats,
    //        TotalPrice = totalPrice,
    //        BookingDate = DateTime.UtcNow,
    //        TravelDate = travelDate,
    //        TravelTime = travelTime,
    //        IsPaid = false // Mark the booking as paid
    //    };

    //    using (var session = await _seats.Database.Client.StartSessionAsync())
    //    {
    //        session.StartTransaction();

    //        try
    //        {
    //            await _bookings.InsertOneAsync(session, booking);

    //            var updateTasks = seats.Select(seat =>
    //                _seats.ReplaceOneAsync(session, s => s.Id == seat.Id, seat)).ToArray();

    //            await Task.WhenAll(updateTasks);

    //            await session.CommitTransactionAsync();
    //            return true;
    //        }
    //        catch
    //        {
    //            await session.AbortTransactionAsync();
    //            throw;
    //        }
    //    }
    //}


    //public async Task<bool> ReserveSeatsAsync(string planeId, List<string> seatIds, string userId, string userRole, DateTime travelDate, string travelTime)
    //{
    //    var flightInstance = await GetOrCreateFlightInstanceAsync(planeId, travelDate, travelTime);

    //    if (seatIds.Count > 6)
    //    {
    //        throw new InvalidOperationException("You cannot book more than six seats at once.");
    //    }

    //    var seats = await _seats.Find(s => s.PlaneId == planeId && seatIds.Contains(s.Id)).ToListAsync();

    //    if (userRole == "user")
    //    {
    //        var invalidSeats = seats.Where(seat =>
    //            int.Parse(seat.Row) >= 4 && (seat.Column == "B" || seat.Column == "E")).ToList();

    //        if (invalidSeats.Count == 1 && seats.Count < 2)
    //        {
    //            return false; // Validation failed
    //        }

    //        var rows = seats.GroupBy(s => s.Row).ToDictionary(g => g.Key, g => g.ToList());

    //        foreach (var row in rows)
    //        {
    //            var columns = row.Value.Select(s => s.Column).OrderBy(c => c).ToList();
    //            for (int i = 0; i < columns.Count - 1; i++)
    //            {
    //                var currentColumn = columns[i];
    //                var nextColumn = columns[i + 1];

    //                if (!IsAdjacent(currentColumn, nextColumn))
    //                {
    //                    throw new InvalidOperationException("You can only book adjacent seats.");
    //                }
    //            }
    //        }

    //        var rowNumbers = rows.Keys.Select(int.Parse).OrderBy(r => r).ToList();
    //        for (int i = 0; i < rowNumbers.Count - 1; i++)
    //        {
    //            if (rowNumbers[i + 1] - rowNumbers[i] > 1)
    //            {
    //                throw new InvalidOperationException("You can only book seats in the same row or adjacent rows.");
    //            }
    //        }
    //    }

    //    var seatReservations = flightInstance.SeatReservations.Where(sr => seatIds.Contains(sr.SeatId)).ToList();

    //    foreach (var seatReservation in seatReservations)
    //    {
    //        seatReservation.IsReserved = true;
    //        seatReservation.UserId = userId;
    //    }

    //    var totalPrice = seats.Sum(s => (double)s.Price);
    //    var booking = new Booking
    //    {
    //        Id = ObjectId.GenerateNewId().ToString(),
    //        UserId = userId,
    //        PlaneId = planeId,
    //        Seats = seats,
    //        TotalPrice = totalPrice,
    //        BookingDate = DateTime.UtcNow,
    //        TravelDate = travelDate,
    //        TravelTime = travelTime
    //    };

    //    using (var session = await _seats.Database.Client.StartSessionAsync())
    //    {
    //        session.StartTransaction();

    //        try
    //        {
    //            await _flightInstances.ReplaceOneAsync(session, fi => fi.Id == flightInstance.Id, flightInstance);
    //            await _bookings.InsertOneAsync(session, booking);
    //            await session.CommitTransactionAsync();
    //            return true;
    //        }
    //        catch
    //        {
    //            await session.AbortTransactionAsync();
    //            throw;
    //        }
    //    }
    //}

    private bool IsAdjacent(string currentColumn, string nextColumn)
    {
        // Define your seat column order
        var columnOrder = new List<string> { "A", "B", "C", "D", "E", "F" };
        var currentIndex = columnOrder.IndexOf(currentColumn);
        var nextIndex = columnOrder.IndexOf(nextColumn);
        return nextIndex - currentIndex == 1;
    }


    public async Task<bool> UnreserveSeatsAsync(string planeId, List<string> seatIds, string userId)
{

        var seats = await _seats.Find(s => s.PlaneId == planeId && seatIds.Contains(s.Id)).ToListAsync();

        foreach (var seat in seats)
    {
        seat.IsReserved = false;
        seat.UserId = null;
        await _seats.ReplaceOneAsync(s => s.Id == seat.Id, seat);
    }

    return true;
}



    public async Task<bool> LockSeatsAsync(string planeId, List<string> seatIds, string userId)
    {
        var seats = await _seats.Find(s => s.PlaneId == planeId && seatIds.Contains(s.Id)).ToListAsync();
        foreach (var seat in seats)
        {
            seat.IsLocked = true;
            seat.UserId = userId;
            await _seats.ReplaceOneAsync(s => s.Id == seat.Id, seat);
        }
        return true;
    }

    public async Task<bool> UnlockSeatsAsync(string planeId, List<string> seatIds, string userId)
    {
        var seats = await _seats.Find(s => seatIds.Contains(s.Id) && s.PlaneId == planeId).ToListAsync();
        foreach (var seat in seats)
        {
            seat.IsLocked = false;
            seat.UserId = null;
            await _seats.ReplaceOneAsync(s => s.Id == seat.Id, seat);
        }
        return true;
    }

    public async Task<bool> UnlockAllSeatsAsync(string userId)
    {
        var seats = await _seats.Find(s => s.UserId == userId && s.IsLocked).ToListAsync();
        foreach (var seat in seats)
        {
            seat.IsLocked = false;
            seat.UserId = null;
            await _seats.ReplaceOneAsync(s => s.Id == seat.Id, seat);
        }
        return true;
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

        //var planeObjectId = new ObjectId(paymentRequest.PlaneId);
        var seatObjectIds = paymentRequest.SeatIds.Select(id => (id)).ToList();
        var seats = _seats.Find(s => s.PlaneId == paymentRequest.PlaneId && seatObjectIds.Contains(s.Id)).ToList();

        foreach (var seat in seats)
        {
            seat.IsReserved = true;
            seat.UserId = paymentRequest.UserId;
            _seats.ReplaceOne(s => s.Id == seat.Id, seat);
        }

        return true;
    }

    // Add this method to your SeatReservationService


    public async Task<bool> CancelBookingAsync(string bookingId, string userId)
    {
        var booking = await _bookings.Find(b => b.Id == bookingId && b.UserId == userId).FirstOrDefaultAsync();
        if (booking == null)
        {
            return false;
        }

        // Reset the IsReserved status and UserId for the seats associated with the booking
        var update = Builders<Seat>.Update.Set(s => s.IsReserved, false).Set(s => s.UserId, null).Set(s=> s.IsLocked, false);
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
