using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AirlineReservationSystem_Backend.Models
{
    public class Seat
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Row { get; set; }
        public string Column { get; set; }
        public bool IsReserved { get; set; }
        public bool IsLocked { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string PlaneId { get; set; }
        public string? UserId { get; set; } = string.Empty;
    }
}
