using Microsoft.AspNetCore.JsonPatch;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Extensions;
using TripleDerby.Core.Specifications;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Core.Services;

public class HorseService(ITripleDerbyRepository repository) : IHorseService
{
    public async Task<HorseResult> Get(Guid id)
    {
        var horse = await repository.SingleOrDefaultAsync(new HorseSpecification(id));

        if (horse is null)
        {
            throw new KeyNotFoundException($"Horse with ID '{id}' was not found.");
        }

        return new HorseResult
        {
            Id = horse.Id,
            Name = horse.Name,
            Color = horse.Color.Name,
            Earnings = horse.Earnings,
            RacePlace = horse.RacePlace,
            RaceShow = horse.RaceShow,
            RaceStarts = horse.RaceStarts,
            RaceWins = horse.RaceWins,
            Sire = horse.Sire?.Name,
            Dam = horse.Dam?.Name
        };
    }

    public async Task<PagedList<HorseResult>> Filter(PaginationRequest request, CancellationToken cancellationToken)
    {
        //return await FilterWithManualMapping(request, cancellationToken);
        
        return await FilterWithSpecMapping(request, cancellationToken);
    }

    private async Task<PagedList<HorseResult>> FilterWithManualMapping(PaginationRequest request, CancellationToken cancellationToken)
    {
        var spec = new HorseFilterSpecification(request);
        var pagedHorses = await repository.ListAsync(spec, cancellationToken);

        var horses = pagedHorses.Data.Select(x => new HorseResult
        {
            Id = x.Id,
            Name = x.Name,
            IsMale = x.IsMale,
            Color = x.Color.Name,
            Earnings = x.Earnings,
            RacePlace = x.RacePlace,
            RaceShow = x.RaceShow,
            RaceStarts = x.RaceStarts,
            RaceWins = x.RaceWins,
            Sire = x.Sire?.Name,
            Dam = x.Dam?.Name,
            Created = x.CreatedDate,
            Owner = x.Owner.Username
        }).ToList();

        return new PagedList<HorseResult>(horses, pagedHorses.Total, pagedHorses.PageNumber, pagedHorses.Size);
    }

    private async Task<PagedList<HorseResult>> FilterWithSpecMapping(PaginationRequest request, CancellationToken cancellationToken)
    {
        var spec = new HorseSearchSpecificationToDto(request);

        var pagedHorseResults = await repository.ListAsync(spec, cancellationToken);

        return pagedHorseResults;
    }

    public async Task Update(Guid id, JsonPatchDocument<HorsePatch> patch)
    {
        var horse = await repository.FindAsync<Horse>(id);

        if (horse is null)
        {
            throw new KeyNotFoundException($"Horse with ID '{id}' was not found.");
        }

        patch.Map<HorsePatch, Horse>().ApplyTo(horse);

        await repository.UpdateAsync(horse);
    }
}
