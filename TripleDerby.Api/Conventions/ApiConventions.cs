using Microsoft.AspNetCore.Mvc;

namespace TripleDerby.Api.Conventions;

public static class ApiConventions
{
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static void Default()
    {
    }
}
