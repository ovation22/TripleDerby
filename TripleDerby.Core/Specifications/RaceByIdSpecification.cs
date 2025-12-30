using Ardalis.Specification;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel;

namespace TripleDerby.Core.Specifications;

/// <summary>
/// Specification for fetching a single race by ID with projection to RaceResult.
/// </summary>
public sealed class RaceByIdSpecification : Specification<Race, RaceResult>, ISingleResultSpecification<Race, RaceResult>
{
    public RaceByIdSpecification(byte raceId)
    {
        Query.Where(r => r.Id == raceId);

        Query.Select(r => new RaceResult
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            Furlongs = r.Furlongs,
            SurfaceId = r.SurfaceId,
            Surface = r.Surface.Name,
            TrackId = r.TrackId,
            Track = r.Track.Name,
            RaceClassId = r.RaceClassId,
            RaceClass = r.RaceClass.Name,
            Purse = r.Purse
        });
    }
}
