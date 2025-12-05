using System.IO;
using System.Text.Json;
using WpfSmsApp.Models;

namespace WpfSmsApp.Services;

public class DataItemService
{
    public static DataItemService Instance { get => _instance.Value; }
    private readonly static Lazy<DataItemService> _instance = new(() => new DataItemService());

    private readonly string _configPath;
    private readonly LoggerService _loggerService = LoggerService.Instance;
    private CancellationTokenSource _saveKeysTokenSource = new();
    private List<string>? _cachedKeys;
    private const string EnvPrefix = "APP_DATA_";

    private static readonly List<DataItem> DefaultItems = new()
    {
        new DataItem { Name = "DefaultKey1", Value = "DefaultValue1", Comment = "Первое дефолтное значение" },
        new DataItem { Name = "DefaultKey2", Value = "DefaultValue2", Comment = "Второе дефолтное значение" }
    };

    private DataItemService()
    {
        _configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
    }

    public async Task<List<DataItem>> GetAllAsync()
    {
        var keys = await GetAllKeysAsync();
        var result = new List<DataItem>();
        var keysToRemove = new List<string>();

        foreach (var key in keys)
        {
            var item = GetDataByKey(key);
            if (item != null)
                result.Add(item);
            else
                keysToRemove.Add(key);
        }

        if (keysToRemove.Count > 0)
        {
            foreach (var key in keysToRemove)
                keys.Remove(key);

            SaveAllKeys(keys);
        }

        return result;
    }

    public async Task CreateAsync(DataItem item)
    {
        var keys = await GetAllKeysAsync();
        if (keys.Contains(item.Name))
            throw new ArgumentException($"Поле '{item.Name}' уже существует (если не внести изменений, то будет сохранен последний вариант)");

        await SetEnvVarAsync(item);
        keys.Add(item.Name);
        SaveAllKeys(keys);

        _loggerService.LogCreateEvent(item);
    }

    public async Task UpdateAsync(DataItem oldItem, DataItem newItem)
    {
        var keys = await GetAllKeysAsync();

        if (oldItem.Name != newItem.Name)
        {
            await Task.Run(() =>
                Environment.SetEnvironmentVariable($"{EnvPrefix}{oldItem.Name}", null, EnvironmentVariableTarget.User)
            );

            if (keys.Contains(newItem.Name))
                throw new ArgumentException($"Поле '{newItem.Name}' уже существует");

            keys.Remove(oldItem.Name);
            keys.Add(newItem.Name);

            await SetEnvVarAsync(newItem);
            SaveAllKeys(keys);

        }
        else if (oldItem.Value != newItem.Value || oldItem.Comment != newItem.Comment)
        {
            await SetEnvVarAsync(newItem);
        }
        _loggerService.LogUpdateEvent(oldItem, newItem);
    }


    public async Task DeleteAsync(DataItem item)
    {
        var keys = await GetAllKeysAsync();
        if (!keys.Remove(item.Name))
            return;

        await Task.Run(() =>
            Environment.SetEnvironmentVariable($"{EnvPrefix}{item.Name}", null, EnvironmentVariableTarget.User)
        );

        SaveAllKeys(keys);
    }

    private async Task InitializeDefaultsAsync()
    {
        var keys = new List<string>();

        foreach (var defaultItem in DefaultItems)
            await CreateInternalAsync(defaultItem, keys);

        SaveAllKeys(keys);
    }


    private async Task CreateInternalAsync(DataItem item, List<string> existingKeys)
    {
        if (existingKeys.Contains(item.Name))
            return;

        await SetEnvVarAsync(item);
        existingKeys.Add(item.Name);
        _loggerService.LogCreateEvent(item);
    }


    private async Task<List<string>> GetAllKeysAsync()
    {
        if (_cachedKeys != null)
            return _cachedKeys;

        if (!File.Exists(_configPath))
        {
            await InitializeDefaultsAsync();
            _cachedKeys = DefaultItems.Select(x => x.Name).ToList();
            return _cachedKeys;
        }

        var json = await File.ReadAllTextAsync(_configPath);
        using var doc = JsonDocument.Parse(json);

        List<string> keys;
        if (doc.RootElement.TryGetProperty("DataKeys", out var arr))
            keys = arr.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrEmpty(x)).ToList()!;
        else
            keys = new List<string>();

        await EnsureDefaultKeysExistAsync(keys);

        _cachedKeys = keys;
        return _cachedKeys;
    }

    private async Task EnsureDefaultKeysExistAsync(List<string> existingKeys)
    {
        bool keysChanged = false;

        foreach (var defaultItem in DefaultItems)
        {
            if (!existingKeys.Contains(defaultItem.Name))
            {
                await CreateInternalAsync(defaultItem, existingKeys);
                keysChanged = true;
            }
            else
            {
                var envValue = Environment.GetEnvironmentVariable($"{EnvPrefix}{defaultItem.Name}", EnvironmentVariableTarget.User);
                if (string.IsNullOrEmpty(envValue))
                {
                    await SetEnvVarAsync(defaultItem);
                    _loggerService.LogCreateEvent(defaultItem);
                }
            }
        }

        if (keysChanged)
            SaveAllKeys(existingKeys);
    }


    private static async Task SetEnvVarAsync(DataItem item)
    {
        var json = JsonSerializer.Serialize(item);
        await Task.Run(() => Environment.SetEnvironmentVariable($"{EnvPrefix}{item.Name}", json, EnvironmentVariableTarget.User));
    }

    private DataItem? GetDataByKey(string key)
    {
        var envValue = Environment.GetEnvironmentVariable($"{EnvPrefix}{key}", EnvironmentVariableTarget.User);
        return string.IsNullOrEmpty(envValue) ? null : JsonSerializer.Deserialize<DataItem>(envValue);
    }
    private void SaveAllKeys(List<string> keys)
    {
        _cachedKeys = keys;

        Delay.Debounce(async () =>
        {
            var obj = new { DataKeys = keys };
            var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_configPath, json);
        }, ref _saveKeysTokenSource);
    }
}
