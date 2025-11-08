using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace WebexGuestApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GuestTokenController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        public record GuestRequest(string? DisplayName, int? TtlSeconds);
        public GuestTokenController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;

        }
        [HttpPost]
        public async Task<IActionResult> GetGuestToken([FromBody] GuestRequest request)
        {
            var token = _config["Webex:ServiceAppToken"];
            if (string.IsNullOrEmpty(token))
                return StatusCode(500, new { error = "Missing Service App Token" });


            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);


            var body = new
            {
                subject = $"guest-{Guid.NewGuid()}",
                displayName = request.DisplayName ?? "Web Guest",
                ttl = Math.Clamp(request.TtlSeconds ?? 3600, 300, 7200)
            };


            var response = await client.PostAsJsonAsync("https://webexapis.com/v2/guests/token", body);
            var content = await response.Content.ReadAsStringAsync();


            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, content);


            var json = JsonDocument.Parse(content).RootElement;
            return Ok(new
            {
                token = json.GetProperty("accessToken").GetString(),
                expiresIn = json.GetProperty("expiresIn").GetInt32(),
                displayName = body.displayName
            });
        }
    }
}
