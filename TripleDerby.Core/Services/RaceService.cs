using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Specifications;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Services;

public class RaceService : IRaceService
{
    private readonly ITripleDerbyRepository _repository;

    public RaceService(
        ITripleDerbyRepository repository
    )
    {
        _repository = repository;
    }

    public async Task<RaceResult> Get(byte id)
    {
        var race = await _repository.SingleOrDefaultAsync(new RaceSpecification(id));

        return new RaceResult
        {
            Id = race.Id,
            Name = race.Name,
            Description = race.Description,
            Furlongs = race.Furlongs,
            SurfaceId = race.SurfaceId,
            Surface = race.Surface.Name,
            TrackId = race.TrackId,
            Track = race.Track.Name
        };
    }

    public async Task<IEnumerable<RacesResult>> GetAll()
    {
        var races = await _repository.ListAsync(new RaceSpecification());

        return races.Select(x => new RacesResult
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            Furlongs = x.Furlongs,
            SurfaceId = x.SurfaceId,
            Surface = x.Surface.Name,
            TrackId = x.TrackId,
            Track = x.Track.Name
        });
    }

    public async Task<RaceRunResult> Race(byte raceId, Guid horseId)
    {
        var race = await _repository.SingleOrDefaultAsync(new RaceSpecification(raceId));
        var racers = await _repository.ListAsync(new HorseRandomRacerSpecification());

        var raceRunHorses = new List<RaceRunHorse> { new RaceRunHorse { HorseId = horseId, Lane = 1 } };
        raceRunHorses.AddRange(racers.Select(racer => new RaceRunHorse { HorseId = racer.Id, Lane = 2 })); // TODO: lane

        var raceRunTicks = CreateRaceRunTicks(horseId, race.Furlongs);

        var raceRun = new RaceRun
        {
            RaceId = raceId,
            ConditionId = ConditionId.Fast, // TODO: random condition
            Horses = raceRunHorses.OrderBy(x => Guid.NewGuid()).ToList(),
            Purse = 30000, // TODO
            RaceRunTicks = raceRunTicks
        };

        await _repository.CreateAsync(raceRun);

        return new RaceRunResult
        {
            Id = Guid.NewGuid(),
            RaceId = raceId,
            WinHorse = "Winner!",
            PlaceHorse = "Place",
            ShowHorse = "Show",
            PlayByPlay = raceRun.RaceRunTicks.Select(x => x.Note).ToList()
        };
    }

    private static List<RaceRunTick> CreateRaceRunTicks(Guid horseId, decimal raceFurlongs)
    {
        var distanceTraveled = 0M;
        var raceDistance = raceFurlongs;
        var raceRunTicks = new List<RaceRunTick>();

        for (decimal i = 1M, tickDistance; distanceTraveled < raceDistance; i++, distanceTraveled += tickDistance)
        {
            byte lane = 1;
            byte distance = 1;
            tickDistance = 1M;

            raceRunTicks.Add(new RaceRunTick
            {
                Tick = (byte)i,
                Note = "Horse 1 is strong out of the gate!",
                RaceRunTickHorses = new List<RaceRunTickHorse>
                {
                    new() {HorseId = horseId, Lane = lane, Distance = distance}
                }
            });
        }

        return raceRunTicks;
    }
}
