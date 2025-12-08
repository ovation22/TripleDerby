using Ardalis.Specification;

namespace TripleDerby.Core.Specifications;

/// <summary>
/// Interface for a paginated specification.
/// </summary>
/// <typeparam name="T">The type of the entity.</typeparam>
public interface IPaginatedSpecification<T> : ISpecification<T>
{
    /// <summary>
    /// Gets the page number.
    /// </summary>
    int PageNumber { get; }

    /// <summary>
    /// Gets the page size.
    /// </summary>
    int PageSize { get; }
}

public interface IPaginatedSpecification<T, TResult> : ISpecification<T, TResult>
{
    int PageNumber { get; }
    int PageSize { get; }
}