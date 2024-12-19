using System.Runtime.InteropServices;
using System.Text.Json;
using DictionaryBot.Core.Models;

namespace DictionaryBot.Core;

public class LocalStorageService
{
    private readonly string RootDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "AryaCore");
    private const string DataFolder = "DictionaryBot";
    private const string DataFile = "DictionaryBotConfig.json";

    public LocalStorage Data = new LocalStorage();

    private bool _initialLoadComplete = false;

    public void InitializeTaskModule(string moduleName, IEnumerable<string>? extras = null)
    {
        if (!_initialLoadComplete)
            throw new InvalidOperationException("LocalStorageService has not been initialized yet");

        Data.TaskModuleConfigs.TryAdd(moduleName, new TaskModuleConfig());

        if (extras == null) return;
        
        foreach (var extra in extras)
        {
            Data.TaskModuleConfigs[moduleName].Extras.TryAdd(extra, null);
        }
    }

    public void Load()
    {
        var dir = Path.Combine(RootDir, DataFolder);
        var localDataFile = Path.Combine(dir, DataFile);

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if (!File.Exists(localDataFile))
        {
            Data = new LocalStorage();
        }
        else
        {
            var raw = File.ReadAllText(localDataFile);
            Data = JsonSerializer.Deserialize<LocalStorage>(raw) ?? new LocalStorage();
        }
        
        if (!_initialLoadComplete)
        {
            Save();
            _initialLoadComplete = true;
        }
    }

    public void Save()
    {
        var dir = Path.Combine(RootDir, DataFolder);
        var localDataFile = Path.Combine(dir, DataFile);
        
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        
        File.WriteAllText(localDataFile, JsonSerializer.Serialize(Data));
    }
}