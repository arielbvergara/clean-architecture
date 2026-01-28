using Application.Interfaces;
using Application.Services;
using Application.UseCases.User;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        // User services
        services.AddScoped<IUserOwnershipService, UserOwnershipService>();

        // User use cases
        services.AddScoped<CreateUserUseCase>();
        services.AddScoped<GetUserByIdUseCase>();
        services.AddScoped<GetUserByEmailUseCase>();
        services.AddScoped<UpdateUserNameUseCase>();
        services.AddScoped<DeleteUserUseCase>();
        services.AddScoped<GetUserByExternalAuthIdUseCase>();
        services.AddScoped<GetUsersUseCase>();

        return services;
    }
}
