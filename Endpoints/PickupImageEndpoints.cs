// Endpoints/PickupImageEndpoints.cs
using FlomiApp.Security;
using FlomiApp.Services;
using Microsoft.AspNetCore.Authorization;

namespace FlomiApp.Endpoints;

/// <summary>
/// Liefert Möbelfotos als normale Bild-URLs aus, statt sie als Base64 durch die
/// Blazor-Verbindung zu schieben. Der Browser kann sie so cachen und lädt nur,
/// was gerade sichtbar ist. Zugriff wie die Admin-Seite der Möbelabholung.
/// </summary>
public static class PickupImageEndpoints
{
    public static IEndpointRouteBuilder MapPickupImages(this IEndpointRouteBuilder app)
    {
        app.MapGet("/pickup-images/{id:int}", async (int id, bool? thumb,
                IFurniturePickupService service, HttpContext http) =>
            {
                var image = await service.GetImageContentAsync(id, thumb == true);
                if (image == null) return Results.NotFound();

                // Personenbezogene Fotos: nur im Browser-Cache, nie in geteilten Caches
                http.Response.Headers.CacheControl = "private, max-age=86400";
                return Results.File(image.Value.Data, image.Value.ContentType);
            })
            .RequireAuthorization(
                new AuthorizeAttribute { Roles  = Roles.AdminOrMoebel },
                new AuthorizeAttribute { Policy = Roles.RequireTwoFactorForAdmins });

        return app;
    }
}
