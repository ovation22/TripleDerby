using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TripleDerby.Infrastructure.Data.Repositories;

namespace TripleDerby.Tests.Unit.Infrastructure.Data.Repositories;

internal class TestRepository(DbContext dbContext, ILogger<EFRepository> logger) : EFRepository(dbContext, logger);