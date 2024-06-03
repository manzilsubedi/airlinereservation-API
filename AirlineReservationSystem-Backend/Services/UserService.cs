using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using AirlineReservationSystem_Backend.Models;
using System.Linq;
using System;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class UserService
{
    private readonly IMongoCollection<User> _users;
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserService(IConfiguration configuration, IPasswordHasher<User> passwordHasher)
    {
        var client = new MongoClient(configuration.GetConnectionString("MongoDb"));
        var database = client.GetDatabase("AirlineReservation");
        _users = database.GetCollection<User>("Users");
        _configuration = configuration;
        _passwordHasher = passwordHasher;
    }

    public User Authenticate(string email, string password)
    {
        var user = _users.Find(u => u.Email == email).SingleOrDefault();

        if (user == null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.Failed)
            return null;

        return user;
    }

    public User Register(string name, string email, string password, string role)
    {
        if (_users.Find(u => u.Email == email).Any())
            throw new Exception("User with this email already exists");

        var user = new User
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = name,
            Email = email,
            Role = role,
            PasswordHash = _passwordHasher.HashPassword(null, password)
        };

        _users.InsertOne(user);

        return user;
    }

    public string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
