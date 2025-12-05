using System.Net;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

Console.OutputEncoding = Encoding.UTF8;

app.MapPost("/api", async (HttpContext ctx) =>
{
    if (!ctx.Request.Headers.TryGetValue("Authorization", out var authHeader) ||
        !authHeader.ToString().StartsWith("Basic "))
    {
        ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        await ctx.Response.WriteAsync("{\"Success\":false,\"ErrorMessage\":\"Нет авторизации.\"}");
        return;
    }

    var encoded = authHeader.ToString()["Basic ".Length..];
    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
    var parts = decoded.Split(':');
    var username = parts.ElementAtOrDefault(0);
    var password = parts.ElementAtOrDefault(1);

    if (username != "test" || password != "test")
    {
        ctx.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        await ctx.Response.WriteAsync("{\"Success\":false,\"ErrorMessage\":\"Неверный логин или пароль.\"}");
        return;
    }

    string body = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
    var json = JsonDocument.Parse(body);
    var command = json.RootElement.GetProperty("Command").GetString();

    object response;

    switch (command)
    {
        case "GetMenu":
            response = new
            {
                Command = "GetMenu",
                Success = true,
                ErrorMessage = "",
                Data = new
                {
                    MenuItems = new[]
                    {
                        new { Id = "5979224", Article = "A1004292", Name = "Каша гречневая", Price = 50 },
                        new { Id = "5979225", Article = "A1004293", Name = "Конфеты Коровка", Price = 300 }
                    }
                }
            };
            break;

        case "SendOrder":
            Console.WriteLine("Заказ получен:");
            Console.WriteLine(body);
            response = new { Command = "SendOrder", Success = true, ErrorMessage = "" };
            break;

        default:
            response = new
            {
                Command = command,
                Success = false,
                ErrorMessage = $"Неизвестная команда: {command}"
            };
            break;
    }

    ctx.Response.ContentType = "application/json; charset=utf-8";
    await ctx.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
});

app.Run("http://localhost:5000");
