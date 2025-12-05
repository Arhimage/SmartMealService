using ConsoleSmsApp;
using ConsoleSmsApp.Data;
using SmsLib.Models;
using SmsLib.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;
using System.Text.Json;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}")
    .WriteTo.File(
        path: "logs/test-sms-console-app-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:dd-MM-yyyy HH:mm}] [{Level:u3}] {Message:lj}{NewLine}")
    .CreateLogger();

async Task<AppSettings> LoadSettingsAsync()
{
    var text = await File.ReadAllTextAsync("appsettings.json");
    return JsonSerializer.Deserialize<AppSettings>(text)!;
}

AppDbContext CreateDbContext(AppSettings settings)
{
    var builder = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(settings.ConnectionString);
    return new AppDbContext(builder.Options);
}

async Task<AppDbContext> EnsureDatabaseAsync(AppSettings settings)
{
    try
    {
        var db = CreateDbContext(settings);
        await db.Database.EnsureCreatedAsync();
        Log.Information("База данных инициализирована.");
        return db;
    }
    catch (NpgsqlException ex)
    {
        Log.Error("Ошибка подключения к PostgreSQL: {Message}", ex.Message);
        Console.WriteLine("Проверьте параметры подключения в appsettings.json.");
        Environment.Exit(1);
        return null!;
    }
    catch (Exception ex)
    {
        Log.Error("Невозможно инициализировать базу данных: {Message}", ex.Message);
        Console.WriteLine("Проверьте файл appsettings.json.");
        Environment.Exit(1);
        return null!;
    }
}

async Task<ClientService> EnsureServerConnectionAsync(AppSettings settings)
{
    var client = new ClientService(settings.ServerEndpoint, settings.BasicAuth.Username, settings.BasicAuth.Password);
    try
    {
        await client.GetMenuAsync(false);
        Log.Information("Подключение к REST‑серверу успешно.");
        return client;
    }
    catch (HttpRequestException ex)
    {
        Log.Error("REST‑сервер недоступен: {Message}", ex.Message);
        Console.WriteLine("Проверьте поле ServerEndpoint в appsettings.json и повторите запуск.");
        Environment.Exit(1);
        return null!;
    }
    catch (UnauthorizedAccessException)
    {
        Log.Error("Неверные логин или пароль REST‑сервера.");
        Console.WriteLine("Проверьте поля BasicAuth.Username и BasicAuth.Password в appsettings.json.");
        Environment.Exit(1);
        return null!;
    }
    catch (Exception ex)
    {
        Log.Error("Ошибка при подключении к REST‑серверу: {Message}", ex.Message);
        Console.WriteLine("Проверьте настройки REST‑сервера в appsettings.json.");
        Environment.Exit(1);
        return null!;
    }
}

try
{
    var settings = await LoadSettingsAsync();
    using var db = await EnsureDatabaseAsync(settings);
    var client = await EnsureServerConnectionAsync(settings);

    List<Dish> menu;
    try
    {
        menu = await client.GetMenuAsync(true);
        Log.Information("Получено {Count} блюд.", menu.Count);
    }
    catch (Exception ex)
    {
        Log.Error("Ошибка получения меню: {Message}", ex.Message);
        return;
    }

    db.Dishes.RemoveRange(db.Dishes);
    await db.SaveChangesAsync();

    db.Dishes.AddRange(menu.Select(m => new DishEntity
    {
        ExternalId = m.Id,
        Article = m.Article,
        Name = m.Name,
        Price = m.Price,
        FullPath = m.FullPath ?? "",
        IsWeighted = false
    }));
    await db.SaveChangesAsync();

    foreach (var d in db.Dishes)
        Console.WriteLine($"{d.Name} – {d.Article} – {d.Price}");

    Console.WriteLine("Введите список блюд (пример: A1004292:1;A1004293:0.4)");
    Console.Write("> ");

    while (true)
    {
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input) || !input.Contains(':'))
        {
            Console.WriteLine("Некорректный формат. Пример корректного ввода: A1004292:1;A1004293:0.4");
            Console.Write("> ");
            continue;
        }

        var pairs = input.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var items = new List<(string Article, decimal Qty)>();
        var hasError = false;

        foreach (var pair in pairs)
        {
            var parts = pair.Split(':');
            if (parts.Length != 2)
            {
                Console.WriteLine($"Ошибка в паре '{pair}': должен быть формат 'Артикул:Количество'");
                hasError = true;
                break;
            }

            var article = parts[0].Trim();
            if (string.IsNullOrWhiteSpace(article))
            {
                Console.WriteLine($"Ошибка в паре '{pair}': артикул не может быть пустым");
                hasError = true;
                break;
            }

            if (!decimal.TryParse(parts[1].Trim().Replace(',', '.'), System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var qty))
            {
                Console.WriteLine($"Ошибка в паре '{pair}': '{parts[1].Trim()}' не является числом");
                hasError = true;
                break;
            }

            if (qty <= 0)
            {
                Console.WriteLine($"Ошибка в паре '{pair}': количество должно быть больше нуля");
                hasError = true;
                break;
            }

            items.Add((article, qty));
        }

        if (hasError)
        {
            Console.Write("> ");
            continue;
        }

        var dishes = db.Dishes.ToList();
        var unknownArticles = items.Where(i => !dishes.Any(d => d.Article == i.Article)).ToList();

        if (unknownArticles.Any())
        {
            Console.WriteLine($"Ошибка: неизвестные артикулы: {string.Join(", ", unknownArticles.Select(x => x.Article))}");
            Console.Write("> ");
            continue;
        }

        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            MenuItems = items.Select(i => new OrderItem
            {
                Id = dishes.First(d => d.Article == i.Article).ExternalId,
                Quantity = i.Qty.ToString(System.Globalization.CultureInfo.InvariantCulture)
            }).ToList()
        };

        Log.Information("Отправка заказа...");
        try
        {
            var result = await client.SendOrderAsync(order);
            if (result) Console.WriteLine("УСПЕХ");
        }
        catch (Exception ex)
        {
            Log.Error("Ошибка отправки заказа: {Message}", ex.Message);
        }

        break;
    }

}
catch (Exception ex)
{
    Log.Error("Ошибка выполнения программы: {Message}", ex.Message);
}
finally
{
    Log.CloseAndFlush();
}
