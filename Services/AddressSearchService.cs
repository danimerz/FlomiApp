using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace FlomiApp.Services;

public partial class AddressSearchService : IAddressSearchService
{
    private readonly IHttpClientFactory _factory;

    public AddressSearchService(IHttpClientFactory factory) => _factory = factory;

    public async Task<List<AddressSuggestion>> SearchAsync(string query)
    {
        if (string.IsNullOrEmpty(query) || query.Length < 3) return new();

        var url = "https://api3.geo.admin.ch/rest/services/api/SearchServer"
                + $"?searchText={Uri.EscapeDataString(query)}"
                + "&type=locations&origins=address&sr=4326&lang=de&limit=8";

        try
        {
            var http     = _factory.CreateClient();
            var response = await http.GetFromJsonAsync<SwisstopoResponse>(url);

            return (response?.Results ?? new())
                .Select(r =>
                {
                    var cleanLabel = HtmlTagRegex().Replace(r.Attrs.Label ?? "", "");
                    var postalCode = r.Attrs.Postalcode?.ToString() ?? "";
                    var city       = r.Attrs.City ?? "";
                    var street     = cleanLabel;

                    if (!string.IsNullOrEmpty(postalCode) && !string.IsNullOrEmpty(city))
                    {
                        var suffix = postalCode + " " + city;
                        var idx    = cleanLabel.LastIndexOf(suffix, StringComparison.OrdinalIgnoreCase);
                        if (idx > 0) street = cleanLabel[..idx].Trim();
                    }

                    return new AddressSuggestion(cleanLabel, street, postalCode, city);
                })
                .ToList();
        }
        catch
        {
            return new();
        }
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();
}

file record SwisstopoResponse([property: JsonPropertyName("results")] List<SwisstopoResult> Results);
file record SwisstopoResult([property: JsonPropertyName("attrs")] SwisstopoAttrs Attrs);
file record SwisstopoAttrs(
    [property: JsonPropertyName("label")]      string? Label,
    [property: JsonPropertyName("postalcode")] int?    Postalcode,
    [property: JsonPropertyName("city")]       string? City
);
