using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using log4net;
using log4net.Core;
using log4net.Config;
using log4net.Appender;

namespace ModernMinas.Update.Api
{
    public static class Log
    {
        private static ILog _log;

        public static void Init()
        {
            _log = LogManager.GetLogger("AppDomain");

            BasicConfigurator.Configure(
                new FileAppender(new log4net.Layout.SimpleLayout(), Path.Combine(Environment.CurrentDirectory, "update.log")),
                new ConsoleAppender()
            );

            AppDomain.CurrentDomain.AssemblyLoad += (sender, e) =>
            {
                _log.DebugFormat("Assembly load: {0}", e.LoadedAssembly.FullName);
            };
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                _log.Info("Process exiting.");
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                _log.ErrorFormat("Unhandled exception: {0}", e.ExceptionObject.ToString());
            };
        }
    }
}
