using Discord;
using Serilog.Events;

namespace DictionaryBot.Core.Services;

public class LogService
{
    public void Log(LogMessage msg)
    {
        var level = msg.Severity switch
        {
            LogSeverity.Critical => LogEventLevel.Fatal,
            LogSeverity.Error => LogEventLevel.Error,
            LogSeverity.Warning => LogEventLevel.Warning,
            LogSeverity.Info => LogEventLevel.Information,
            LogSeverity.Verbose => LogEventLevel.Verbose,
            LogSeverity.Debug => LogEventLevel.Debug,
            _ => LogEventLevel.Information
        };
        
        Serilog.Log.Write(level, msg.Exception, "[{Source}] {Message}", msg.Source, msg.Message);
    }
    public void Log(string source, string message) => Log(new LogMessage(LogSeverity.Info, source, message));
    public void Log(LogSeverity severity, string source, string message, Exception exception = null) => Log(new LogMessage(severity, source, message, exception));
    
    public void Debug(string source, string message) => Log(new LogMessage(LogSeverity.Debug, source, message));
    public void Info(string source, string message) => Log(new LogMessage(LogSeverity.Info, source, message));
    public void Warn(string source, string message) => Log(new LogMessage(LogSeverity.Warning, source, message));
    public void Error(string source, string message, Exception? ex = null) => Log(new LogMessage(LogSeverity.Debug, source, message, ex));
    
    public Task LogAsync(LogMessage msg)
    {
        Log(msg);
        return Task.CompletedTask;
    }
    public Task LogAsync(string source, string message) => LogAsync(new LogMessage(LogSeverity.Info, source, message));
    public Task LogAsync(LogSeverity severity, string source, string message, Exception exception = null) => LogAsync(new LogMessage(severity, source, message, exception)); 
    
    public Task DebugAsync(string source, string message) => LogAsync(new LogMessage(LogSeverity.Debug, source, message));
    public Task InfoAsync(string source, string message) => LogAsync(new LogMessage(LogSeverity.Info, source, message));
    public Task WarnAsync(string source, string message) => LogAsync(new LogMessage(LogSeverity.Warning, source, message));
    public Task ErrorAsync(string source, string message, Exception? ex = null) => LogAsync(new LogMessage(LogSeverity.Debug, source, message, ex));
}