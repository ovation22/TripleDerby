using Moq;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Services;
using TripleDerby.Core.Specifications;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Tests.Unit.Services;

public class TrackServiceTests
{
    [Fact]
    public async Task Filter_ReturnsPagedListOfTracks()
    {
        // Arrange
        var mockRepository = new Mock<ITripleDerbyRepository>();

        var tracks = new List<TracksResult>
        {
            new() { Id = TrackId.TripleSpires, Name = "Triple Spires" },
            new() { Id = TrackId.BellMeade, Name = "Bell Meade" },
            new() { Id = TrackId.Pimento, Name = "Pimento" }
        };

        var pagedList = new PagedList<TracksResult>(tracks, 3, 1, 10);

        mockRepository
            .Setup(r => r.ListAsync(It.IsAny<TrackFilterSpecificationToDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedList);

        var service = new TrackService(mockRepository.Object);

        var request = new PaginationRequest
        {
            Page = 1,
            Size = 10,
            SortBy = "Name",
            Direction = SortDirection.Asc
        };

        // Act
        var result = await service.Filter(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Total);
        Assert.Equal(3, result.Data.Count());
        Assert.Equal("Triple Spires", result.Data.First().Name);
    }

    [Fact]
    public async Task Filter_PassesCorrectSpecificationToRepository()
    {
        // Arrange
        var mockRepository = new Mock<ITripleDerbyRepository>();

        var pagedList = new PagedList<TracksResult>(new List<TracksResult>(), 0, 1, 10);

        mockRepository
            .Setup(r => r.ListAsync(It.IsAny<TrackFilterSpecificationToDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedList);

        var service = new TrackService(mockRepository.Object);

        var request = new PaginationRequest
        {
            Page = 2,
            Size = 20,
            SortBy = "Name",
            Direction = SortDirection.Desc
        };

        // Act
        await service.Filter(request);

        // Assert
        mockRepository.Verify(
            r => r.ListAsync(It.Is<TrackFilterSpecificationToDto>(spec => spec != null), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Filter_WithDefaultPagination_ReturnsFirstPage()
    {
        // Arrange
        var mockRepository = new Mock<ITripleDerbyRepository>();

        var tracks = new List<TracksResult>
        {
            new() { Id = TrackId.TripleSpires, Name = "Triple Spires" }
        };

        var pagedList = new PagedList<TracksResult>(tracks, 1, 1, 10);

        mockRepository
            .Setup(r => r.ListAsync(It.IsAny<TrackFilterSpecificationToDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedList);

        var service = new TrackService(mockRepository.Object);

        var request = new PaginationRequest
        {
            Page = 1,
            Size = 10
        };

        // Act
        var result = await service.Filter(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Total);
        Assert.Single(result.Data);
    }

    [Fact]
    public async Task Filter_WithCancellationToken_PassesToRepository()
    {
        // Arrange
        var mockRepository = new Mock<ITripleDerbyRepository>();
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var pagedList = new PagedList<TracksResult>(new List<TracksResult>(), 0, 1, 10);

        mockRepository
            .Setup(r => r.ListAsync(It.IsAny<TrackFilterSpecificationToDto>(), cancellationToken))
            .ReturnsAsync(pagedList);

        var service = new TrackService(mockRepository.Object);

        var request = new PaginationRequest
        {
            Page = 1,
            Size = 10
        };

        // Act
        await service.Filter(request, cancellationToken);

        // Assert
        mockRepository.Verify(
            r => r.ListAsync(It.IsAny<TrackFilterSpecificationToDto>(), cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Filter_ReturnsEmptyList_WhenNoTracksFound()
    {
        // Arrange
        var mockRepository = new Mock<ITripleDerbyRepository>();

        var pagedList = new PagedList<TracksResult>(new List<TracksResult>(), 0, 1, 10);

        mockRepository
            .Setup(r => r.ListAsync(It.IsAny<TrackFilterSpecificationToDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedList);

        var service = new TrackService(mockRepository.Object);

        var request = new PaginationRequest
        {
            Page = 1,
            Size = 10
        };

        // Act
        var result = await service.Filter(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.Total);
        Assert.Empty(result.Data);
    }
}
