using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace FileSync
{
    public class AppConfig
    {
        public IReadOnlyList<string> IgnoringFileNames { get; private set; }
        public string Src { get; private set; }
        public string Dest { get; private set; }
        public bool ShouldKeepRemovedFilesInDest { get; private set; }

        public AppConfig Initialize()
        {
            IgnoringFileNames = ConfigurationManager.AppSettings["APP_IGNORING_FILE_NAMES"]?.Split(';') ??
                                throw new Exception("App.config \"APP_IGNORING_FILE_NAMES\" should not be null.");

            Src = ConfigurationManager.AppSettings["APP_SRC"] ??
                  throw new Exception("App.config \"APP_SRC\" should not be null.");
            Src = Path.GetFullPath(Src);

            Dest = ConfigurationManager.AppSettings["APP_DEST"] ??
                   throw new Exception("App.config \"APP_DEST\" should not be null.");
            Dest = Path.GetFullPath(Dest);

            var shouldRemoveFileInDestStr = ConfigurationManager.AppSettings["SHOULD_KEEP_REMOVED_FILES_IN_DEST"] ??
                                            throw new Exception("App.config \"SHOULD_KEEP_REMOVED_FILES_IN_DEST\" should not be null.");
            ShouldKeepRemovedFilesInDest = bool.Parse(shouldRemoveFileInDestStr);

            if (Src == Dest) throw new Exception("dest should be different from src.");

            return this;
        }
    }
}