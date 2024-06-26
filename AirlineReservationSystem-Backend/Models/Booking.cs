﻿using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace AirlineReservationSystem_Backend.Models
{
    // Booking.cs
    public class Booking
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string PlaneId { get; set; }
        public List<Seat> Seats { get; set; }
        public List<Passenger> Passengers { get; set; }
        public double TotalPrice { get; set; }
        public DateTime BookingDate { get; set; }
        public DateTime TravelDate { get; set; }
        public string TravelTime { get; set; }
        public bool IsPaid { get; set; }
    }

    public class Passenger
    {
        public string Name { get; set; }
        public string PassportNo { get; set; }
        public int Age { get; set; }
    }
}
