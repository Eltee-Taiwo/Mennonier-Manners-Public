using System.Text.Json;
using TaiwoTech.MennoniteManners.App.Domain.User;
using TaiwoTech.MennoniteManners.App.Models;
using TaiwoTech.MennoniteManners.App.Services.API;

namespace TaiwoTech.MennoniteManners.App.Services.Settings;

public class SettingsService : ISingletonService
{
    private const string UserNameKey = $"{nameof(SettingsService)}-{nameof(UserNameKey)}";
    private const string TimeToSetRollKey = $"{nameof(SettingsService)}-{nameof(TimeToSetRollKey)}";
    private const string GameSettingsKey = $"{nameof(SettingsService)}-{nameof(GameSettingsKey)}";
    private const string GameTypesKey = $"{nameof(SettingsService)}-{nameof(GameTypesKey)}";

    /// <summary>
    /// Get's the currently saved username
    /// </summary>
    /// <returns>The username or null if no username has been set up yet</returns>
    public UserName GetUserName()
    {
        var username = Preferences.Get(UserNameKey, null);
        return string.IsNullOrWhiteSpace(username) ? null : new UserName(username);
    }

    /// <summary>
    /// Get's the currently saved username surrounded by angle brackets '&lt; &gt;'
    /// </summary>
    /// <returns>The username or the default prompt <see cref="UserName.NoName"/></returns>
    public UserName GetUserNameForButton()
    {
        var username = Preferences.Get(UserNameKey, null);
        return string.IsNullOrWhiteSpace(username) ? UserName.NoName : new UserName($"< {username} >");
    }

    /// <summary>
    /// Saves the username
    /// </summary>
    /// <param name="userName"></param>
    public void SetUserName(UserName userName)
    {
        Preferences.Set(UserNameKey, userName.Value);
    }

    /// <summary>
    /// Set the amount of time to wait before forcing the next roll
    /// </summary>
    /// <param name="timeToForceRoll"></param>
    public void SetTimeToForceRoll(double timeToForceRoll)
    {
        Preferences.Set(TimeToSetRollKey, timeToForceRoll);
    }

    /// <summary>
    /// Set the amount of time to wait before forcing the next roll
    /// </summary>
    public double GetTimeToForceRoll()
    {
        var timeToRoll = Preferences.Get(TimeToSetRollKey, 3.0);
        return timeToRoll;
    }
    /// <summary>
    /// Get the preferred game settings
    /// </summary>
    /// <returns></returns>
    public GameSettingsDto GetGameSettings()
    {
        var gameSettingsJson = Preferences.Get(GameSettingsKey, null);
        return string.IsNullOrWhiteSpace(gameSettingsJson)
            ? new GameSettingsDto { MaxLevel = 20, GameTypeId = 1}
            : JsonSerializer.Deserialize<GameSettingsDto>(gameSettingsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    /// <summary>
    /// Save the preferred game settings
    /// </summary>
    /// <param name="dto"></param>
    public void SetGameSettings(GameSettingsDto dto)
    {
        var gameSettingsJson = JsonSerializer.Serialize(dto);
        Preferences.Set(GameSettingsKey, gameSettingsJson);
    }

    /// <summary>
    /// Get the known game types
    /// </summary>
    /// <returns></returns>
    public List<GameTypeDto> GetGameTypes()
    {
        var json = Preferences.Get(GameTypesKey, "[]");
        return JsonSerializer.Deserialize<IEnumerable<GameTypeDto>>(json).ToList();
    }

    /// <summary>
    /// Set the possible game types
    /// </summary>
    /// <param name="gameTypes"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void SetGameTypes(IEnumerable<GameTypeDto> gameTypes)
    {
        var json = JsonSerializer.Serialize(gameTypes);
        Preferences.Set(GameTypesKey, json);
    }
}
