using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace AirlineReservationSystem_Backend.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public ICollection<Reservation> Reservations { get; set; }
    }

}
