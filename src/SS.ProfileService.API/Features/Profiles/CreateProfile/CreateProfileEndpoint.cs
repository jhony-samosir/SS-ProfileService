using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SS.ProfileService.API.Features.Profiles.CreateProfile;

public static class CreateProfileEndpoint
{
    public static void MapCreateProfileEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/profiles", async (CreateProfileRequest request, ISender sender) =>
        {
            var command = new CreateProfileCommand(
                request.UserId,
                request.UserPublicId,
                request.FullName,
                request.PhoneNumber,
                request.AvatarUrl,
                request.Bio,
                request.Gender,
                request.DateOfBirth);

            return await sender.Send(command);
        })
        .WithName("CreateProfile")
        .WithTags("Profiles");
    }
}
