using Microsoft.JSInterop;
using RebelGame.Models;
using System.Text.Json;

namespace RebelGame.Services;

public class StorageService(IJSRuntime js)
{
    private const string StateKey = "game_state_v1";

    public async Task<GameState?> LoadStateAsync()
    {
        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", StateKey);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonSerializer.Deserialize<GameState>(json);
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveStateAsync(GameState state)
    {
        try
        {
            var json = JsonSerializer.Serialize(state);
            await js.InvokeVoidAsync("localStorage.setItem", StateKey, json);
        }
        catch { }
    }
}
