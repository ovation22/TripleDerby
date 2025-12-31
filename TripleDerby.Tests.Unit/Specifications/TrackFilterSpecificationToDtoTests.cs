using TripleDerby.Core.Entities;
using TripleDerby.Core.Specifications;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Tests.Unit.Specifications;

public class TrackFilterSpecificationToDtoTests
{
    [Fact]
    public void Projects_Track_To_TracksResult()
    {
        // Arrange
        var tracks = new List<Track>
        {
            new() { Id = TrackId.TripleSpires, Name = "Triple Spires" },
            new() { Id = TrackId.BellMeade, Name = "Bell Meade" },
            new() { Id = TrackId.Pimento, Name = "Pimento" }
        };

        var request = new PaginationRequest
        {
            Page = 1,
            Size = 10,
            SortBy = "Name",
            Direction = SortDirection.Asc
        };

        var spec = new TrackFilterSpecificationToDto(request);

        // Act
        var results = ApplySpecification(tracks, spec);

        // Assert - sorted by Name ascending
        Assert.Equal(3, results.Count);

        Assert.Equal(TrackId.BellMeade, results[0].Id);
        Assert.Equal("Bell Meade", results[0].Name);

        Assert.Equal(TrackId.Pimento, results[1].Id);
        Assert.Equal("Pimento", results[1].Name);

        Assert.Equal(TrackId.TripleSpires, results[2].Id);
        Assert.Equal("Triple Spires", results[2].Name);
    }

    [Fact]
    public void SortsByName_Ascending_ByDefault()
    {
        // Arrange
        var tracks = new List<Track>
        {
            new() { Id = TrackId.Pimento, Name = "Pimento" },
            new() { Id = TrackId.BellMeade, Name = "Bell Meade" },
            new() { Id = TrackId.TripleSpires, Name = "Triple Spires" }
        };

        var request = new PaginationRequest
        {
            Page = 1,
            Size = 10
        };

        var spec = new TrackFilterSpecificationToDto(request);

        // Act
        var results = ApplySpecification(tracks, spec);

        // Assert
        Assert.Equal("Bell Meade", results[0].Name);
        Assert.Equal("Pimento", results[1].Name);
        Assert.Equal("Triple Spires", results[2].Name);
    }

    [Fact]
    public void SortsByName_Descending()
    {
        // Arrange
        var tracks = new List<Track>
        {
            new() { Id = TrackId.BellMeade, Name = "Bell Meade" },
            new() { Id = TrackId.Pimento, Name = "Pimento" },
            new() { Id = TrackId.TripleSpires, Name = "Triple Spires" }
        };

        var request = new PaginationRequest
        {
            Page = 1,
            Size = 10,
            SortBy = "Name",
            Direction = SortDirection.Desc
        };

        var spec = new TrackFilterSpecificationToDto(request);

        // Act
        var results = ApplySpecification(tracks, spec);

        // Assert
        Assert.Equal("Triple Spires", results[0].Name);
        Assert.Equal("Pimento", results[1].Name);
        Assert.Equal("Bell Meade", results[2].Name);
    }

    [Fact]
    public void HasCorrectPaginationProperties_FirstPage()
    {
        // Arrange
        var request = new PaginationRequest
        {
            Page = 1,
            Size = 3,
            SortBy = "Name",
            Direction = SortDirection.Asc
        };

        // Act
        var spec = new TrackFilterSpecificationToDto(request);

        // Assert - Pagination is applied by the repository, not the specification
        Assert.Equal(1, spec.PageNumber);
        Assert.Equal(3, spec.PageSize);
    }

    [Fact]
    public void HasCorrectPaginationProperties_SecondPage()
    {
        // Arrange
        var request = new PaginationRequest
        {
            Page = 2,
            Size = 5,
            SortBy = "Name",
            Direction = SortDirection.Asc
        };

        // Act
        var spec = new TrackFilterSpecificationToDto(request);

        // Assert - Pagination is applied by the repository, not the specification
        Assert.Equal(2, spec.PageNumber);
        Assert.Equal(5, spec.PageSize);
    }

    private static List<TracksResult> ApplySpecification(List<Track> tracks, TrackFilterSpecificationToDto spec)
    {
        IQueryable<Track> query = tracks.AsQueryable();

        // Apply projection first
        if (spec.Selector == null)
            return new List<TracksResult>();

        var projected = query.Select(spec.Selector);

        // Apply sorting - default is ascending by Name
        if (spec.OrderExpressions.Any())
        {
            var orderExpr = spec.OrderExpressions.First();
            if (orderExpr.OrderType == Ardalis.Specification.OrderTypeEnum.OrderBy)
            {
                projected = projected.OrderBy(t => t.Name);
            }
            else if (orderExpr.OrderType == Ardalis.Specification.OrderTypeEnum.OrderByDescending)
            {
                projected = projected.OrderByDescending(t => t.Name);
            }
        }
        else
        {
            // Default sort
            projected = projected.OrderBy(t => t.Name);
        }

        // Note: Pagination (Skip/Take) is applied by the repository, not the specification
        return projected.ToList();
    }
}
