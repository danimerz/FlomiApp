// Endpoints/SeoEndpoints.cs
using FlomiApp.Helpers;

namespace FlomiApp.Endpoints;

/// <summary>
/// robots.txt und sitemap.xml mit der Domain, über die die App aufgerufen wird
/// (z. B. https://pfadiflomi.ch). Öffentlich, ohne Login.
/// </summary>
public static class SeoEndpoints
{
    public static IEndpointRouteBuilder MapSeo(this IEndpointRouteBuilder app)
    {
        app.MapGet("/robots.txt", (HttpContext http) =>
                Results.Text(SeoHelper.BuildRobotsTxt(BaseUrl(http)), "text/plain; charset=utf-8"))
            .AllowAnonymous();

        app.MapGet("/sitemap.xml", (HttpContext http) =>
                Results.Text(SeoHelper.BuildSitemap(BaseUrl(http)), "application/xml; charset=utf-8"))
            .AllowAnonymous();

        return app;
    }

    private static string BaseUrl(HttpContext http) =>
        $"{http.Request.Scheme}://{http.Request.Host}{http.Request.PathBase}/";
}
