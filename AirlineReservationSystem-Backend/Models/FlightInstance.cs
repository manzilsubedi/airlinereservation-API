using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace AirlineReservationSystem_Backend.Models
{
    public class FlightInstance
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string PlaneId { get; set; }

        public DateTime TravelDate { get; set; }
        public string TravelTime { get; set; }
        public List<SeatReservation> SeatReservations { get; set; } = new List<SeatReservation>();
    }

    public class SeatReservation
    {
        public string SeatId { get; set; }
        public bool IsReserved { get; set; }
        public bool IsLocked { get; set; }
        public string UserId { get; set; }
    }
}
