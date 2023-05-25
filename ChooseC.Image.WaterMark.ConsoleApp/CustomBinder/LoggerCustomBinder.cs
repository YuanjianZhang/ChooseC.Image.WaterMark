using Serilog;
using System.CommandLine.Binding;

namespace ChooseC.Image.WaterMark.ConsoleApp.CustomBinder
{
    public class LoggerCustomBinder : BinderBase<ILogger>
    {
        protected override ILogger GetBoundValue(
         BindingContext bindingContext) => GetLogger(bindingContext);

        ILogger GetLogger(BindingContext bindingContext)
        {
            var logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Debug)
                .Enrich.FromLogContext()
                .WriteTo.Console(
                theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code,
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine} {Exception}"
                )
                .WriteTo.File(
                path: "Logs/WatermarkDebug.log",
                shared: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 10
                )
                .CreateLogger();
            return logger;
        }
    }
}
