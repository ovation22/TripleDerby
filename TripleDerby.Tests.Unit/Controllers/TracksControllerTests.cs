using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TripleDerby.Api.Controllers;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Tests.Unit.Controllers;

public class TracksControllerTests
{
    [Fact]
    public async Task Filter_ReturnsOkWithPagedTracks()
    {
        // Arrange
        var mockTrackService = new Mock<ITrackService>();

        var tracks = new List<TracksResult>
        {
            new() { Id = TrackId.TripleSpires, Name = "Triple Spires" },
            new() { Id = TrackId.BellMeade, Name = "Bell Meade" },
            new() { Id = TrackId.Pimento, Name = "Pimento" }
        };

        var pagedList = new PagedList<TracksResult>(tracks, 3, 1, 10);

        mockTrackService
            .Setup(s => s.Filter(It.IsAny<PaginationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedList);

        var controller = new TracksController(mockTrackService.Object);

        var request = new PaginationRequest
        {
            Page = 1,
            Size = 10,
            SortBy = "Name",
            Direction = SortDirection.Asc
        };

        // Act
        var result = await controller.Filter(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedPagedList = Assert.IsType<PagedList<TracksResult>>(okResult.Value);

        Assert.Equal(3, returnedPagedList.Total);
        Assert.Equal(3, returnedPagedList.Data.Count());
        Assert.Equal("Triple Spires", returnedPagedList.Data.First().Name);
    }

    [Fact]
    public async Task Filter_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var mockTrackService = new Mock<ITrackService>();

        var pagedList = new PagedList<TracksResult>(new List<TracksResult>(), 0, 1, 10);

        mockTrackService
            .Setup(s => s.Filter(It.IsAny<PaginationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedList);

        var controller = new TracksController(mockTrackService.Object);

        var request = new PaginationRequest
        {
            Page = 2,
            Size = 20,
            SortBy = "Name",
            Direction = SortDirection.Desc
        };

        // Act
        await controller.Filter(request, CancellationToken.None);

        // Assert
        mockTrackService.Verify(
            s => s.Filter(
                It.Is<PaginationRequest>(r =>
                    r.Page == 2 &&
                    r.Size == 20 &&
                    r.SortBy == "Name" &&
                    r.Direction == SortDirection.Desc),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Filter_WithSortByName_Ascending_ReturnsSorted()
    {
        // Arrange
        var mockTrackService = new Mock<ITrackService>();

        var tracks = new List<TracksResult>
        {
            new() { Id = TrackId.BellMeade, Name = "Bell Meade" },
            new() { Id = TrackId.EmeraldDowns, Name = "Emerald Downs" },
            new() { Id = TrackId.Pimento, Name = "Pimento" }
        };

        var pagedList = new PagedList<TracksResult>(tracks, 3, 1, 10);

        mockTrackService
            .Setup(s => s.Filter(It.Is<PaginationRequest>(r =>
                r.SortBy == "Name" && r.Direction == SortDirection.Asc),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedList);

        var controller = new TracksController(mockTrackService.Object);

        var request = new PaginationRequest
        {
            Page = 1,
            Size = 10,
            SortBy = "Name",
            Direction = SortDirection.Asc
        };

        // Act
        var result = await controller.Filter(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedPagedList = Assert.IsType<PagedList<TracksResult>>(okResult.Value);

        var tracksList = returnedPagedList.Data.ToList();
        Assert.Equal("Bell Meade", tracksList[0].Name);
        Assert.Equal("Emerald Downs", tracksList[1].Name);
        Assert.Equal("Pimento", tracksList[2].Name);
    }

    [Fact]
    public async Task Filter_WithSortByName_Descending_ReturnsSorted()
    {
        // Arrange
        var mockTrackService = new Mock<ITrackService>();

        var tracks = new List<TracksResult>
        {
            new() { Id = TrackId.Pimento, Name = "Pimento" },
            new() { Id = TrackId.EmeraldDowns, Name = "Emerald Downs" },
            new() { Id = TrackId.BellMeade, Name = "Bell Meade" }
        };

        var pagedList = new PagedList<TracksResult>(tracks, 3, 1, 10);

        mockTrackService
            .Setup(s => s.Filter(It.Is<PaginationRequest>(r =>
                r.SortBy == "Name" && r.Direction == SortDirection.Desc),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedList);

        var controller = new TracksController(mockTrackService.Object);

        var request = new PaginationRequest
        {
            Page = 1,
            Size = 10,
            SortBy = "Name",
            Direction = SortDirection.Desc
        };

        // Act
        var result = await controller.Filter(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedPagedList = Assert.IsType<PagedList<TracksResult>>(okResult.Value);

        var tracksList = returnedPagedList.Data.ToList();
        Assert.Equal("Pimento", tracksList[0].Name);
        Assert.Equal("Emerald Downs", tracksList[1].Name);
        Assert.Equal("Bell Meade", tracksList[2].Name);
    }

    [Fact]
    public async Task Filter_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var mockTrackService = new Mock<ITrackService>();

        var tracks = new List<TracksResult>
        {
            new() { Id = TrackId.Pimento, Name = "Pimento" },
            new() { Id = TrackId.TripleSpires, Name = "Triple Spires" }
        };

        var pagedList = new PagedList<TracksResult>(tracks, 8, 2, 3); // Total 8, page 2, size 3

        mockTrackService
            .Setup(s => s.Filter(It.Is<PaginationRequest>(r =>
                r.Page == 2 && r.Size == 3),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedList);

        var controller = new TracksController(mockTrackService.Object);

        var request = new PaginationRequest
        {
            Page = 2,
            Size = 3
        };

        // Act
        var result = await controller.Filter(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedPagedList = Assert.IsType<PagedList<TracksResult>>(okResult.Value);

        Assert.Equal(8, returnedPagedList.Total);
        Assert.Equal(2, returnedPagedList.Data.Count());
    }

    [Fact]
    public async Task Filter_WithEmptyResults_ReturnsOkWithEmptyList()
    {
        // Arrange
        var mockTrackService = new Mock<ITrackService>();

        var pagedList = new PagedList<TracksResult>(new List<TracksResult>(), 0, 1, 10);

        mockTrackService
            .Setup(s => s.Filter(It.IsAny<PaginationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedList);

        var controller = new TracksController(mockTrackService.Object);

        var request = new PaginationRequest
        {
            Page = 1,
            Size = 10
        };

        // Act
        var result = await controller.Filter(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedPagedList = Assert.IsType<PagedList<TracksResult>>(okResult.Value);

        Assert.Equal(0, returnedPagedList.Total);
        Assert.Empty(returnedPagedList.Data);
    }

    [Fact]
    public async Task Filter_WithCancellationToken_PassesToService()
    {
        // Arrange
        var mockTrackService = new Mock<ITrackService>();
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var pagedList = new PagedList<TracksResult>(new List<TracksResult>(), 0, 1, 10);

        mockTrackService
            .Setup(s => s.Filter(It.IsAny<PaginationRequest>(), cancellationToken))
            .ReturnsAsync(pagedList);

        var controller = new TracksController(mockTrackService.Object);

        var request = new PaginationRequest
        {
            Page = 1,
            Size = 10
        };

        // Act
        await controller.Filter(request, cancellationToken);

        // Assert
        mockTrackService.Verify(
            s => s.Filter(It.IsAny<PaginationRequest>(), cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Filter_ReturnsAllTracks()
    {
        // Arrange
        var mockTrackService = new Mock<ITrackService>();

        var tracks = new List<TracksResult>
        {
            new() { Id = TrackId.TripleSpires, Name = "Triple Spires" },
            new() { Id = TrackId.BellMeade, Name = "Bell Meade" },
            new() { Id = TrackId.Pimento, Name = "Pimento" },
            new() { Id = TrackId.EmeraldDowns, Name = "Emerald Downs" },
            new() { Id = TrackId.GoldenGateFields, Name = "Golden Gate Fields" },
            new() { Id = TrackId.FairGrounds, Name = "Fair Grounds" },
            new() { Id = TrackId.OaklawnPark, Name = "Oaklawn Park" },
            new() { Id = TrackId.GulfstreamPark, Name = "Gulfstream Park" }
        };

        var pagedList = new PagedList<TracksResult>(tracks, 8, 1, 10);

        mockTrackService
            .Setup(s => s.Filter(It.IsAny<PaginationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedList);

        var controller = new TracksController(mockTrackService.Object);

        var request = new PaginationRequest
        {
            Page = 1,
            Size = 10
        };

        // Act
        var result = await controller.Filter(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedPagedList = Assert.IsType<PagedList<TracksResult>>(okResult.Value);

        Assert.Equal(8, returnedPagedList.Total);
        Assert.Equal(8, returnedPagedList.Data.Count());
    }
}
