using AirlineReservationSystem_Backend.Models;
using System;
using System.Collections.Generic;

public class Reservation
{
    public string Id { get; set; }
    public string PlaneId { get; set; }
    public string SeatId { get; set; }
    public string UserId { get; set; }
    public DateTime TravelDate { get; set; }
    public string TravelTime { get; set; }
    public bool IsReserved { get; set; }
    public bool IsLocked { get; set; }
}
