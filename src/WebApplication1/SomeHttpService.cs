namespace WebApplication1;

public record Response(string Username, string Email);

public class SomeHttpService
{
    // Inject http factory or typed http client here
    public async Task<Response?> GetUserAsync(string username, CancellationToken token)
    {
        if (username == "fail")
            throw new ApplicationException("Request failed.");

        await Task.Delay(300, token);
        return new Response(username, $"{username}@gmail.com");
    }
}
