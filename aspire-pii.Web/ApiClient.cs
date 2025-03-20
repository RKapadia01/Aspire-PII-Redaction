namespace aspire_pii.Web;

public class ApiClient(HttpClient httpClient)
{
    public async Task PostPiiAsync(PiiData input)
    {
        var response = await httpClient.PostAsJsonAsync("/pii", input);
        response.EnsureSuccessStatusCode();
    }
}

public record PiiData()
{
    public string? Text { get; set; }
}