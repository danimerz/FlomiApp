// Endpoints/NewsImageEndpoints.cs
using FlomiApp.Security;
using FlomiApp.Services;

namespace FlomiApp.Endpoints;

/// <summary>
/// Liefert News-Bilder als normale Bild-URLs aus (<c>/news-images/{id}?thumb=true</c>).
/// Bilder veröffentlichter Meldungen sind öffentlich, die von Entwürfen sieht nur der Admin.
/// </summary>
public static class NewsImageEndpoints
{
    public static IEndpointRouteBuilder MapNewsImages(this IEndpointRouteBuilder app)
    {
        app.MapGet("/news-images/{id:int}", async (int id, bool? thumb, INewsService service, HttpContext http) =>
            {
                var isAdmin = http.User.IsInRole(Roles.Admin);
                var image   = await service.GetImageContentAsync(id, thumb == true, includeUnpublished: isAdmin);
                if (image == null) return Results.NotFound();

                // Bilder ändern sich nie (neues Bild = neue Id); Entwürfe nicht in geteilten Caches
                http.Response.Headers.CacheControl = isAdmin ? "private, max-age=86400" : "public, max-age=86400";
                return Results.File(image.Value.Data, image.Value.ContentType);
            })
            .AllowAnonymous();

        return app;
    }
}
