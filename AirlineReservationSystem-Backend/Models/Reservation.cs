using AirlineReservationSystem_Backend.Models;
using System;
using System.Collections.Generic;

public class Reservation
{
    public int Id { get; set; }
    public DateTime ReservationTime { get; set; }
    public string UserId { get; set; }
    public User User { get; set; }
    public ICollection<ReservationSeat> ReservationSeats { get; set; }
}
