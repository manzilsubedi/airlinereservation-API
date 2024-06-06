using AirlineReservationSystem_Backend.Models;
using MongoDB.Driver;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AirlineReservationSystem_Backend.Tests.Services
{
    public class SeatReservationServiceTests
    {
        private readonly Mock<IMongoCollection<Seat>> _seatsMock;
        private readonly Mock<IMongoCollection<Booking>> _bookingsMock;
        private readonly Mock<IMongoCollection<FlightInstance>> _flightInstancesMock;
        private readonly Mock<IMongoCollection<Reservation>> _reservationsMock;
        private readonly Mock<IMongoDatabase> _databaseMock;
        private readonly SeatReservationService _service;

        public SeatReservationServiceTests()
        {
            _seatsMock = new Mock<IMongoCollection<Seat>>();
            _bookingsMock = new Mock<IMongoCollection<Booking>>();
            _flightInstancesMock = new Mock<IMongoCollection<FlightInstance>>();
            _reservationsMock = new Mock<IMongoCollection<Reservation>>();
            _databaseMock = new Mock<IMongoDatabase>();

            // Setup mock database to return mock collections
            _databaseMock.Setup(db => db.GetCollection<Seat>("Seat", null)).Returns(_seatsMock.Object);
            _databaseMock.Setup(db => db.GetCollection<Booking>("Booking", null)).Returns(_bookingsMock.Object);
            _databaseMock.Setup(db => db.GetCollection<FlightInstance>("FlightInstance", null)).Returns(_flightInstancesMock.Object);
            _databaseMock.Setup(db => db.GetCollection<Reservation>("Reservations", null)).Returns(_reservationsMock.Object);

            // Create instance of the service using the mock database
            _service = new SeatReservationService(_databaseMock.Object);
        }

        private void MockFind<T>(Mock<IMongoCollection<T>> mockCollection, List<T> results) where T : class
        {
            var cursor = new Mock<IAsyncCursor<T>>();
            cursor.Setup(_ => _.Current).Returns(results);
            cursor.SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
            cursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true).ReturnsAsync(false);

            mockCollection.Setup(c => c.FindAsync(It.IsAny<FilterDefinition<T>>(), It.IsAny<FindOptions<T, T>>(), It.IsAny<CancellationToken>())).ReturnsAsync(cursor.Object);
        }

        [Fact]
        public async Task GetSeatsAsync_ReturnsSeatsWithReservationStatus()
        {
            // Arrange
            string planeId = "Plane1";
            DateTime travelDate = DateTime.Now;
            string travelTime = "10:00 AM";
            var seats = new List<Seat> { new Seat { Id = "1", PlaneId = planeId, IsReserved = false } };
            var reservations = new List<Reservation> { new Reservation { PlaneId = planeId, SeatId = "1", TravelDate = travelDate, TravelTime = travelTime, IsReserved = true } };

            MockFind(_seatsMock, seats);
            MockFind(_reservationsMock, reservations);

            // Act
            var result = await _service.GetSeatsAsync(planeId, travelDate, travelTime);

            // Assert
            Assert.Single(result);
            Assert.True(result.First().IsReserved);
        }

        [Fact]
        public async Task GetAllSeatsAsync_ReturnsAllSeats()
        {
            // Arrange
            var seats = new List<Seat> { new Seat { Id = "1" }, new Seat { Id = "2" } };

            MockFind(_seatsMock, seats);

            // Act
            var result = await _service.GetAllSeatsAsync();

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetOrCreateFlightInstanceAsync_ReturnsExistingInstance()
        {
            // Arrange
            string planeId = "Plane1";
            DateTime travelDate = DateTime.Now;
            string travelTime = "10:00 AM";
            var flightInstance = new FlightInstance { PlaneId = planeId, TravelDate = travelDate, TravelTime = travelTime };

            MockFind(_flightInstancesMock, new List<FlightInstance> { flightInstance });

            // Act
            var result = await _service.GetOrCreateFlightInstanceAsync(planeId, travelDate, travelTime);

            // Assert
            Assert.Equal(flightInstance, result);
        }

        [Fact]
        public async Task GetOrCreateFlightInstanceAsync_CreatesNewInstance_WhenNotFound()
        {
            // Arrange
            string planeId = "Plane1";
            DateTime travelDate = DateTime.Now;
            string travelTime = "10:00 AM";
            var flightInstance = new FlightInstance { PlaneId = planeId, TravelDate = travelDate, TravelTime = travelTime };

            MockFind(_flightInstancesMock, new List<FlightInstance>());

            _flightInstancesMock.Setup(c => c.InsertOneAsync(It.IsAny<FlightInstance>(), null, default)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.GetOrCreateFlightInstanceAsync(planeId, travelDate, travelTime);

            // Assert
            Assert.Equal(planeId, result.PlaneId);
            Assert.Equal(travelDate, result.TravelDate);
            Assert.Equal(travelTime, result.TravelTime);
        }

        [Fact]
        public async Task ReserveSeatsAsync_ReturnsTrue_WhenReservationSuccessful()
        {
            // Arrange
            string planeId = "Plane1";
            var seatIds = new List<string> { "1", "2" };
            string userId = "User1";
            string userRole = "User";
            DateTime travelDate = DateTime.Now;
            string travelTime = "10:00 AM";
            var seats = seatIds.Select(id => new Seat { Id = id, PlaneId = planeId }).ToList();

            MockFind(_seatsMock, seats);
            _reservationsMock.Setup(r => r.InsertOneAsync(It.IsAny<Reservation>(), null, default)).Returns(Task.CompletedTask);
            _bookingsMock.Setup(b => b.InsertOneAsync(It.IsAny<Booking>(), null, default)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.ReserveSeatsAsync(planeId, seatIds, userId, userRole, travelDate, travelTime);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task LockSeatsAsync_ReturnsTrue_WhenLockSuccessful()
        {
            // Arrange
            string planeId = "Plane1";
            var seatIds = new List<string> { "1", "2" };
            string userId = "User1";
            DateTime travelDate = DateTime.Now;
            string travelTime = "10:00 AM";

            _reservationsMock.Setup(r => r.InsertOneAsync(It.IsAny<Reservation>(), null, default)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.LockSeatsAsync(planeId, seatIds, userId, travelDate, travelTime);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UnlockSeatsAsync_ReturnsTrue_WhenUnlockSuccessful()
        {
            // Arrange
            string planeId = "Plane1";
            var seatIds = new List<string> { "1", "2" };
            string userId = "User1";
            DateTime travelDate = DateTime.Now;
            string travelTime = "10:00 AM";

            _reservationsMock.Setup(r => r.UpdateOneAsync(It.IsAny<FilterDefinition<Reservation>>(), It.IsAny<UpdateDefinition<Reservation>>(), null, default)).ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

            // Act
            var result = await _service.UnlockSeatsAsync(planeId, seatIds, userId, travelDate, travelTime);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UnreserveSeatsAsync_ReturnsTrue_WhenUnreserveSuccessful()
        {
            // Arrange
            string planeId = "Plane1";
            var seatIds = new List<string> { "1", "2" };
            string userId = "User1";
            DateTime travelDate = DateTime.Now;
            string travelTime = "10:00 AM";

            _reservationsMock.Setup(r => r.UpdateOneAsync(It.IsAny<FilterDefinition<Reservation>>(), It.IsAny<UpdateDefinition<Reservation>>(), null, default)).ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

            // Act
            var result = await _service.UnreserveSeatsAsync(planeId, seatIds, userId, travelDate, travelTime);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UnlockAllSeatsAsync_ReturnsTrue_WhenUnlockAllSuccessful()
        {
            // Arrange
            string userId = "User1";

            _reservationsMock.Setup(r => r.UpdateManyAsync(It.IsAny<FilterDefinition<Reservation>>(), It.IsAny<UpdateDefinition<Reservation>>(), null, default)).ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

            // Act
            var result = await _service.UnlockAllSeatsAsync(userId);

            // Assert
            Assert.True(result);
        }

     
        [Fact]
        public async Task ProcessPaymentAsync_ReturnsTrue_WhenPaymentSuccessful()
        {
            // Arrange
            var paymentRequest = new PaymentRequest { BookingId = "Booking1", Passengers = new List<Passenger> { new Passenger { Name = "John Doe" } } };
            var booking = new Booking { Id = "Booking1", IsPaid = false };

            MockFind(_bookingsMock, new List<Booking> { booking });

            _bookingsMock.Setup(b => b.UpdateOneAsync(It.IsAny<FilterDefinition<Booking>>(), It.IsAny<UpdateDefinition<Booking>>(), null, default)).ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

            // Act
            var result = await _service.ProcessPaymentAsync(paymentRequest);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CancelBookingAsync_ReturnsTrue_WhenCancellationSuccessful()
        {
            // Arrange
            string bookingId = "Booking1";
            string userId = "User1";
            var booking = new Booking { Id = bookingId, UserId = userId, Seats = new List<Seat> { new Seat { Id = "1" } } };

            MockFind(_bookingsMock, new List<Booking> { booking });

            _bookingsMock.Setup(b => b.DeleteOneAsync(It.IsAny<FilterDefinition<Booking>>(), default)).ReturnsAsync(new DeleteResult.Acknowledged(1));

            _reservationsMock.Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<Reservation>>(), default)).ReturnsAsync(new DeleteResult.Acknowledged(1));

            // Act
            var result = await _service.CancelBookingAsync(bookingId, userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ConfirmPaymentAsync_ReturnsTrue_WhenConfirmationSuccessful()
        {
            // Arrange
            string bookingId = "Booking1";

            _bookingsMock.Setup(b => b.UpdateOneAsync(It.IsAny<FilterDefinition<Booking>>(), It.IsAny<UpdateDefinition<Booking>>(), null, default)).ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

            // Act
            var result = await _service.ConfirmPaymentAsync(bookingId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetUserBookingsAsync_ReturnsUserBookings()
        {
            // Arrange
            string userId = "User1";
            var bookings = new List<Booking> { new Booking { Id = "Booking1", UserId = userId }, new Booking { Id = "Booking2", UserId = userId } };

            MockFind(_bookingsMock, bookings);

            // Act
            var result = await _service.GetUserBookingsAsync(userId);

            // Assert
            Assert.Equal(2, result.Count);
        }
    }
}
