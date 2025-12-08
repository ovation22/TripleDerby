using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;

namespace TripleDerby.Core.Extensions;

public static class JsonPatchDocumentExtensions
{
    public static JsonPatchDocument<TOut> Map<TIn, TOut>(this JsonPatchDocument<TIn> instance)
        where TIn : class, new()
        where TOut : class, new()
    {
        return new(instance.Operations.Select(x => x.Map<TOut>()).ToList(), instance.ContractResolver);
    }

    public static Operation<TOut> Map<TOut>(this Operation instance) where TOut : class, new()
    {
        return new(instance.op, instance.path, instance.from, instance.value);
    }
}