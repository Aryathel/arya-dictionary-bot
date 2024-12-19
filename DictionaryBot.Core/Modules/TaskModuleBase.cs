using System;
using DictionaryBot.Core.Models;
using DictionaryBot.Core.Services;
using Discord;
using Microsoft.Extensions.DependencyInjection;

namespace DictionaryBot.Core.Modules;

public abstract class TaskModuleBase
{
    protected IServiceProvider _services;
    protected LogService _logger;
    protected LocalStorageService _localStorage;

    protected TaskRunType RunType;
    protected IEnumerable<string> Extras;

    public TaskModuleBase(TaskRunType runType = TaskRunType.ScheduledTime, IEnumerable<string>? extras = null)
    {
        RunType = runType;
        Extras = extras ?? Array.Empty<string>();
    }
    
    private bool _initialized = false;

    public string Name { get => GetType().Name; }
    
    protected TaskModuleConfig Config
    {
        get => _initialized ? _localStorage.Data.TaskModuleConfigs[Name] : null;
    }

    public virtual async Task InitializeAsync(IServiceProvider services)
    {
        _services = services;
        _logger = _services.GetRequiredService<LogService>();
        _localStorage = _services.GetRequiredService<LocalStorageService>();
        
        _localStorage.InitializeTaskModule(Name, Extras);
        _localStorage.Save();
        
        await _logger.LogAsync(Name, "Initialized task module.");
        
        _initialized = true;
    }

    public abstract Task ExecuteAsync(Dictionary<string, object>? args = null);
    
    public virtual async Task<(bool, Dictionary<string, object>?)> IsTaskReady()
    {
        bool run;
        switch (RunType)
        {
            case TaskRunType.Other:
                run = true;
                break;
            case TaskRunType.TimeInterval:
                if (!Config.RunInterval.HasValue) run = false;
                else if (Config.LastRunUTC == null) run = true;
                else run = Config.LastRunUTC + Config.RunInterval < DateTime.UtcNow;
                break;
            case TaskRunType.ScheduledTime:
            default:
                if (!Config.RunTimeUTC.HasValue) run = false;
                else if (Config.LastRunUTC == null &&
                         (TimeOnly.FromDateTime(DateTime.UtcNow) - Config.RunTimeUTC)?.TotalMinutes < 60) run = true;
                else
                    run = (DateTime.UtcNow - Config.LastRunUTC)?.TotalMinutes >= 1425 &&
                          (TimeOnly.FromDateTime(DateTime.UtcNow) - Config.RunTimeUTC)?.TotalMinutes < 60;
                break;
        }

        return (run, null);
    }
    
    public virtual async Task PreExecuteAsync(Dictionary<string, object>? args = null)
    {
        await _logger.LogAsync(LogSeverity.Debug, Name, $"Running {Name} task module.");
    }

    public virtual async Task PostExecuteAsync(Dictionary<string, object>? args = null)
    {
        _localStorage.Data.TaskModuleConfigs[Name].LastRunUTC = DateTime.UtcNow;
        _localStorage.Save();
        
        await _logger.LogAsync(LogSeverity.Debug, Name, $"Finished running {Name} task module.");
    }

    protected async Task LogAsync(string message)
    {
        await _logger.LogAsync(Name, message);
    }
}