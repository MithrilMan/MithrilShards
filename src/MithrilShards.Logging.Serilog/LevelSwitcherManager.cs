using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace MithrilShards.Logging.Serilog;

public class LevelSwitcherManager
{
   readonly Dictionary<string, LoggingLevelSwitch> _logLevels = new(StringComparer.InvariantCultureIgnoreCase);

   public LevelSwitcherManager() { }

   internal Dictionary<string, LoggingLevelSwitch> LoadLoggingLevelSwitches(IConfigurationRoot configuration)
   {
      _logLevels.Clear();

      //Set default log level
      if (configuration.GetSection("Serilog:MinimumLevel:Default").Exists())
      {
         _logLevels.Add("Default", new LoggingLevelSwitch((LogEventLevel)Enum.Parse(typeof(LogEventLevel), configuration.GetValue<string>("Serilog:MinimumLevel:Default"))));
      }

      //Set log level(s) overrides
      if (configuration.GetSection("Serilog:MinimumLevel:Override").Exists())
      {
         foreach (IConfigurationSection levelOverride in configuration.GetSection("Serilog:MinimumLevel:Override").GetChildren())
         {
            _logLevels.Add(levelOverride.Key, new LoggingLevelSwitch((LogEventLevel)Enum.Parse(typeof(LogEventLevel), levelOverride.Value)));
         }
      }
      return _logLevels;
   }

   internal bool SetLevel(string context, LogEventLevel level)
   {
      if (_logLevels.TryGetValue(context, out LoggingLevelSwitch? logLevel))
      {
         logLevel.MinimumLevel = level;
         return true;
      }

      return false;
   }

   internal object GetCurrentLevels()
   {
      return _logLevels;
   }

   internal LoggingLevelSwitch? GetCurrentLevel(string context)
   {
      _logLevels.TryGetValue(context, out LoggingLevelSwitch? logLevel);
      return logLevel;
   }

   /// <summary>
   /// Binds the log level switches.
   /// </summary>
   /// <param name="loggerConfiguration">The logger configuration.</param>
   internal void BindLevelSwitches(LoggerConfiguration loggerConfiguration)
   {
      foreach (string name in _logLevels.Keys)
      {
         if (string.Equals(name, "Default", StringComparison.InvariantCultureIgnoreCase))
         {
            loggerConfiguration.MinimumLevel.ControlledBy(_logLevels[name]);
         }
         else
         {
            loggerConfiguration.MinimumLevel.Override(name, _logLevels[name]);
         }
      }
   }
}
