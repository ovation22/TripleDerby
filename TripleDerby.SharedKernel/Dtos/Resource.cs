namespace TripleDerby.SharedKernel.Dtos;

public record Resource<T>(T Data, IEnumerable<Link>? Links = null);
