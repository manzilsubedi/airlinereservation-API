using AirlineReservationSystem_Backend.Controllers;
using AirlineReservationSystem_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AirlineReservationSystem_Backend.Tests.Controllers
{
    public class SeatReservationsControllerTests
    {
        private readonly Mock<IMongoCollection<Seat>> _seatsMock;
        private readonly Mock<IMongoCollection<Booking>> _bookingsMock;
        private readonly Mock<IMongoCollection<FlightInstance>> _flightInstancesMock;
        private readonly Mock<IMongoCollection<Reservation>> _reservationsMock;
        private readonly Mock<IMongoDatabase> _databaseMock;
        private readonly SeatReservationService _seatReservationService;
        private readonly SeatReservationsController _controller;

        public SeatReservationsControllerTests()
        {
            _seatsMock = new Mock<IMongoCollection<Seat>>();
            _bookingsMock = new Mock<IMongoCollection<Booking>>();
            _flightInstancesMock = new Mock<IMongoCollection<FlightInstance>>();
            _reservationsMock = new Mock<IMongoCollection<Reservation>>();
            _databaseMock = new Mock<IMongoDatabase>();

            // Setup mock database to return mock collections
            _databaseMock.Setup(db => db.GetCollection<Seat>("Seat", It.IsAny<MongoCollectionSettings>())).Returns(_seatsMock.Object);
            _databaseMock.Setup(db => db.GetCollection<Booking>("Booking", It.IsAny<MongoCollectionSettings>())).Returns(_bookingsMock.Object);
            _databaseMock.Setup(db => db.GetCollection<FlightInstance>("FlightInstance", It.IsAny<MongoCollectionSettings>())).Returns(_flightInstancesMock.Object);
            _databaseMock.Setup(db => db.GetCollection<Reservation>("Reservations", It.IsAny<MongoCollectionSettings>())).Returns(_reservationsMock.Object);

            // Create instance of the service using the mock database
            _seatReservationService = new SeatReservationService(_databaseMock.Object);

            // Pass the service instance to the controller
            _controller = new SeatReservationsController(_seatReservationService);
        }

        private void MockFind<T>(Mock<IMongoCollection<T>> mockCollection, List<T> results) where T : class
        {
            var cursor = new Mock<IAsyncCursor<T>>();
            cursor.Setup(_ => _.Current).Returns(results);
            cursor.SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
            cursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true).ReturnsAsync(false);

            Expression<Func<IMongoCollection<T>, IAsyncCursor<T>>> expression = c => c.FindSync(It.IsAny<FilterDefinition<T>>(), It.IsAny<FindOptions<T, T>>(), It.IsAny<CancellationToken>());
            mockCollection.Setup(expression).Returns(cursor.Object);

            Expression<Func<IMongoCollection<T>, Task<IAsyncCursor<T>>>> expressionAsync = c => c.FindAsync(It.IsAny<FilterDefinition<T>>(), It.IsAny<FindOptions<T, T>>(), It.IsAny<CancellationToken>());
            mockCollection.Setup(expressionAsync).ReturnsAsync(cursor.Object);
        }



        [Fact]
        public async Task ReserveSeats_ReturnsOkResult_WhenReservationSuccessful()
        {
            // Arrange
            var request = new SeatReservationsController.ReserveSeatsRequest
            {
                PlaneId = "Plane1",
                SeatIds = new List<string> { "1", "2" },
                UserId = "User1",
                UserRole = "User",
                TravelDate = DateTime.Now,
                TravelTime = "10:00 AM"
            };

            var seats = new List<Seat> { new Seat { Id = "1", PlaneId = "Plane1" }, new Seat { Id = "2", PlaneId = "Plane1" } };
            MockFind(_seatsMock, seats);

            _reservationsMock.Setup(r => r.InsertOneAsync(It.IsAny<Reservation>(), 
                It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _bookingsMock.Setup(b => b.InsertOneAsync(It.IsAny<Booking>(), It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.ReserveSeats(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.True((bool)okResult.Value);
        }

        [Fact]
        public async Task ReserveSeats_ReturnsBadRequest_WhenRequestInvalid()
        {
            // Arrange
            var request = new SeatReservationsController.ReserveSeatsRequest
            {
                PlaneId = "Plane1",
                SeatIds = null, // Invalid data
                UserId = "User1",
                UserRole = "User",
                TravelDate = DateTime.Now,
                TravelTime = "10:00 AM"
            };

            // Act
            var result = await _controller.ReserveSeats(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Invalid request data", badRequestResult.Value);
        }

        [Fact]
        public async Task LockSeats_ReturnsOkResult_WhenLockSuccessful()
        {
            // Arrange
            var request = new SeatReservationsController.LockSeatsRequest
            {
                PlaneId = "Plane1",
                SeatIds = new List<string> { "1", "2" },
                UserId = "User1",
                TravelDate = DateTime.Now,
                TravelTime = "10:00 AM"
            };
            _reservationsMock.Setup(r => r.InsertOneAsync(It.IsAny<Reservation>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.LockSeats(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.True((bool)okResult.Value);
        }

   
        [Fact]
        public async Task CancelReservation_ReturnsOkResult_WhenCancellationSuccessful()
        {
            // Arrange
            string planeId = "Plane1";
            string seatId = "1";
            string userId = "User1";
            var seat = new Seat { Id = seatId, PlaneId = planeId, IsReserved = true };
            MockFind(_seatsMock, new List<Seat> { seat });
            _seatsMock.Setup(s => s.ReplaceOneAsync(It.IsAny<FilterDefinition<Seat>>(), It.IsAny<Seat>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, seatId));

            // Act
            var result = await _controller.CancelReservation(planeId, seatId, userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.True((bool)okResult.Value);
        }

        [Fact]
        public async Task CancelReservation_ReturnsBadRequest_WhenCancellationFails()
        {
            // Arrange
            string planeId = "Plane1";
            string seatId = "1";
            string userId = "User1";
            MockFind(_seatsMock, new List<Seat>());

            // Act
            var result = await _controller.CancelReservation(planeId, seatId, userId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.False((bool)badRequestResult.Value);
        }
    }
}
