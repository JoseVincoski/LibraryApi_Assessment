using Carter;
using LibrarySystem.Api.Shared.Validations;

namespace LibrarySystem.Api.Extensions;

public static class EndpointExtensions
{
    public static void AddCarter(this WebApplicationBuilder builder)
    {
        var types = new DependencyContextAssemblyCatalog().GetAssemblies().SelectMany(x => x.GetTypes());
        var modules = types
                    .Where(t =>
                        !t.IsAbstract &&
                        typeof(ICarterModule).IsAssignableFrom(t)
                        && (t.IsPublic || t.IsNestedPublic)
                    ).ToList();

        builder.Services.AddCarter(configurator: c =>
        {
            c.WithModules(modules.ToArray());
        });
    }

    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter<ValidationFilter<T>>();
    }
}
