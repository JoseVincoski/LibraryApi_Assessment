using FluentValidation;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace LibrarySystem.Service.Infrastructure.Interceptors;

public sealed class ValidationInterceptor(IServiceProvider serviceProvider) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var validator = serviceProvider.GetService<IValidator<TRequest>>();

        if (validator is not null)
        {
            var result = await validator.ValidateAsync(request, context.CancellationToken);

            if (!result.IsValid)
            {
                var message = string.Join(" ", result.Errors.Select(failure => failure.ErrorMessage));
                throw new RpcException(new Status(StatusCode.InvalidArgument, message));
            }
        }

        return await continuation(request, context);
    }
}
