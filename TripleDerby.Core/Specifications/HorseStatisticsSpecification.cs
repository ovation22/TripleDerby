using System;
using Ardalis.Specification;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Specifications;

public sealed class HorseStatisticsSpecification : Specification<HorseStatistic>
{
    public HorseStatisticsSpecification(Guid horseId, StatisticId statisticId)
    {
        Query.Where(x => x.HorseId == horseId && x.StatisticId == statisticId);
    }
}
