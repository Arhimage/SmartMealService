using SmsLib.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SmsLib.Services
{
    public class ClientService
    {
        private readonly HttpClient _client;
        private readonly string _endpoint;

        public ClientService(string endpoint, string username, string password)
        {
            _endpoint = endpoint;
            _client = new HttpClient();
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
        }

        public async Task<List<Dish>> GetMenuAsync(bool withPrice)
        {
            var body = new { Command = "GetMenu", CommandParameters = new { WithPrice = withPrice } };
            var json = JsonSerializer.Serialize(body);
            var response = await _client.PostAsync(_endpoint, new StringContent(json, Encoding.UTF8, "application/json"));
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ServerResponse<MenuData>>(content);
            if (result == null || !result.Success) throw new Exception(result?.ErrorMessage ?? "Invalid response");
            return result.Data.MenuItems;
        }

        public async Task<bool> SendOrderAsync(Order order)
        {
            var body = new { Command = "SendOrder", CommandParameters = order };
            var json = JsonSerializer.Serialize(body);
            var response = await _client.PostAsync(_endpoint, new StringContent(json, Encoding.UTF8, "application/json"));
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ServerResponse<object>>(content);
            if (result == null || !result.Success) throw new Exception(result?.ErrorMessage ?? "Invalid response");
            return true;
        }
    }
}
