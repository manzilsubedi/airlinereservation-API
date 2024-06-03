using AirlineReservationSystem_Backend.Models;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

public class PlaneService
{
    private readonly IMongoCollection<Plane> _planes;

    public PlaneService(IMongoDatabase database)
    {
        _planes = database.GetCollection<Plane>("Planes");
    }

    public async Task<List<Plane>> GetPlanesAsync()
    {
        return await _planes.Find(plane => true).ToListAsync();
    }

    public async Task<Plane> GetPlaneByIdAsync(string planeId)
    {
        return await _planes.Find<Plane>(plane => plane.Id == planeId).FirstOrDefaultAsync();
    }

    public async Task CreatePlaneAsync(Plane plane)
    {
        await _planes.InsertOneAsync(plane);
    }

    public async Task UpdatePlaneAsync(Plane plane)
    {
        await _planes.ReplaceOneAsync(p => p.Id == plane.Id, plane);
    }

    public async Task DeletePlaneAsync(string planeId)
    {
        await _planes.DeleteOneAsync(plane => plane.Id == planeId);
    }
}
