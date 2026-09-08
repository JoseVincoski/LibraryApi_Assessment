using System.Text.Json.Nodes;
using LibrarySystem.Contracts.Common;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace LibrarySystem.Api.Extensions;

public static class OpenApiExtensions
{
    public static IServiceCollection AddOpenApiWithExamples(this IServiceCollection services)
    {
        services.AddOpenApi(options => options.AddOperationTransformer<DemoExamplesTransformer>());
        return services;
    }
}

internal sealed class DemoExamplesTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        foreach (var parameter in operation.Parameters ?? [])
        {
            if (parameter is not OpenApiParameter p)
            {
                continue;
            }

            p.Example = p.Name switch
            {
                "readerId" => JsonValue.Create(SampleIds.SampleReaderId),
                "bookId" => JsonValue.Create(SampleIds.SampleBookId),
                "loanId" => JsonValue.Create(SampleIds.SampleActiveLoanId),
                "daysLookback" => JsonValue.Create(30),
                "startDate" => JsonValue.Create("2025-01-01"),
                "endDate" => JsonValue.Create("2027-12-31"),
                "maxResults" => JsonValue.Create(
                    context.Description.RelativePath?.Contains("borrowing-patterns") == true ? 3 : 10),
                _ => p.Example
            };
        }

        if (operation.RequestBody?.Content is { } content &&
            content.TryGetValue("application/json", out var media) &&
            media is OpenApiMediaType json)
        {
            json.Examples = new Dictionary<string, IOpenApiExample>
            {
                ["available"] = new OpenApiExample
                {
                    Summary = "Sample book — free after seed",
                    Value = new JsonObject
                    {
                        ["bookId"] = SampleIds.SampleBookId,
                        ["readerId"] = SampleIds.SampleReaderId
                    }
                },
                ["alreadyOnLoan"] = new OpenApiExample
                {
                    Summary = "Sample loaned book — already on loan",
                    Value = new JsonObject
                    {
                        ["bookId"] = SampleIds.SampleLoanedBookId,
                        ["readerId"] = SampleIds.SampleReaderId
                    }
                }
            };
        }

        return Task.CompletedTask;
    }
}
