namespace AirlineReservationSystem_Backend.Models
{
    public class PaymentRequest
    {
        public string PlaneId { get; set; }
        public string BookingId { get; set; }
        public List<string> SeatIds { get; set; }
        public string UserId { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime TravelDate { get; set; }
        public string TravelTime { get; set; }
        public List<Passenger> Passengers { get; set; }
    }
}
