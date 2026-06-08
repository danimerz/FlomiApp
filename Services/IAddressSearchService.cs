namespace FlomiApp.Services;

public record AddressSuggestion(string Label, string Street, string PostalCode, string City);

public interface IAddressSearchService
{
    Task<List<AddressSuggestion>> SearchAsync(string query);
}
