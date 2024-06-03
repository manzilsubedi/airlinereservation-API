using AirlineReservationSystem_Backend.Models;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using System.Linq;

public class SeatReservationService
{
    private readonly IMongoCollection<Seat> _seats;

    public SeatReservationService(IMongoDatabase database)
    {
        _seats = database.GetCollection<Seat>("Seat");
    }

    public async Task<List<Seat>> GetSeatsAsync(string planeId)
    {
        return await _seats.Find(seat => seat.PlaneId == planeId).ToListAsync();
    }

    public async Task<List<Seat>> GetAllSeatsAsync()
    {
        return await _seats.Find(seat => true).ToListAsync();
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

    public async Task<bool> ReserveSeatsAsync(string planeId, List<string> seatIds, string userId, string userRole)
    {
        if (seatIds.Count > 6)
        {
            throw new InvalidOperationException("You cannot book more than six seats at once.");
        }

        var seats = await _seats.Find(s => s.PlaneId == planeId && seatIds.Contains(s.Id)).ToListAsync();
        if (userRole == "user")
        {
            var invalidSeats = seats.Where(seat =>
          (int.Parse(seat.Row) >= 4 && (seat.Column == "B" || seat.Column == "E"))).ToList();

            //var invalidSeats = seats.Where(seat =>
            //    (seat.Column == "B" || seat.Column == "E") && seatIds.Count == 1).ToList();

            if (invalidSeats.Count == 1 && seats.Count <2)
            {
                return false; // Validation failed
            }

            var rows = seats.GroupBy(s => s.Row).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var row in rows)
            {
                var columns = row.Value.Select(s => s.Column).OrderBy(c => c).ToList();
                for (int i = 0; i < columns.Count - 1; i++)
                {
                    var currentColumn = columns[i];
                    var nextColumn = columns[i + 1];

                    if (!IsAdjacent(currentColumn, nextColumn))
                    {
                        throw new InvalidOperationException("You can only book adjacent seats.");
                    }
                }
            }

            // Ensure seats are in the same row or adjacent rows
            var rowNumbers = rows.Keys.Select(int.Parse).OrderBy(r => r).ToList();
            for (int i = 0; i < rowNumbers.Count - 1; i++)
            {
                if (rowNumbers[i + 1] - rowNumbers[i] > 1)
                {
                    throw new InvalidOperationException("You can only book seats in the same row or adjacent rows.");
                }
            }
        }

        foreach (var seat in seats)
        {
            seat.IsReserved = true;
            seat.UserId = userId;
        }

        var updateTasks = seats.Select(seat =>
            _seats.ReplaceOneAsync(s => s.Id == seat.Id, seat)).ToArray();
        Task.WaitAll(updateTasks);

        return true;
    }


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
}
