using System;
using System.Collections.Generic;
using System.Configuration;

namespace FileSync
{
    public class AppConfig
    {
        public IReadOnlyList<string> IgnoringFiles { get; private set; }
        public string Src { get; private set; }
        public string Dest { get; private set; }
        public bool ShouldRemoveFileInDest { get; private set; }

        public AppConfig()
        {
        }

        public AppConfig Initialize()
        {
            IgnoringFiles = ConfigurationManager.AppSettings["APP_IGNORING_FILES"]?.Split(';') ??
                            throw new Exception("App.config \"APP_IGNORING_FILES\" should not be null.");

            Src = ConfigurationManager.AppSettings["APP_SRC"] ??
                  throw new Exception("App.config \"APP_SRC\" should not be null.");

            Dest = ConfigurationManager.AppSettings["APP_DEST"] ??
                   throw new Exception("App.config \"APP_DEST\" should not be null.");

            var shouldRemoveFileInDestStr = ConfigurationManager.AppSettings["SHOULD_REMOVE_FILE_IN_DEST"] ??
                                            throw new Exception("App.config \"SHOULD_REMOVE_FILE_IN_DEST\" should not be null.");
            ShouldRemoveFileInDest = bool.Parse(shouldRemoveFileInDestStr);
            
            if (Src == Dest) throw new Exception("dest should be different from src.");

            return this;
        }
    }
}