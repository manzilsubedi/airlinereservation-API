using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AirlineReservationSystem_Backend.Models
{
    public class ReservationSeat
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string SeatId { get; set; }
        public string ReservationId { get; set; }
    }
}
