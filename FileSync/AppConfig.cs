﻿using System;
using System.Configuration;
using System.IO;

namespace FileSync
{
    public class AppConfig : IAppConfig
    {
        public string Src { get; private set; }
        public string Dest { get; private set; }
        public string Log { get; private set; }

        public bool UseDeepFileComparer { get; private set; }

        public bool KeepRemovedFilesInDest { get; private set; }

        public AppConfig Initialize()
        {
            InitializeBasicConfigrations();
            InitializeComparerConfigurations();
            InitializeOperationConfigrations();

            return this;
        }

        private void InitializeBasicConfigrations()
        {
            Src = Path.GetFullPath(Get("APP_SRC"));
            Dest = Path.GetFullPath(Get("APP_DEST"));
            Log = Get("APP_LOG");

            if (Src == Dest) throw new Exception("dest should be different from src.");
        }

        private void InitializeComparerConfigurations()
        {
            UseDeepFileComparer = bool.Parse(Get("USE_DEEP_FILE_COMPARER"));
        }

        private void InitializeOperationConfigrations()
        {
            KeepRemovedFilesInDest = bool.Parse(Get("KEEP_REMOVED_FILES_IN_DEST"));
        }

        private static string Get(string key)
        {
            return ConfigurationManager.AppSettings[key] ?? throw new Exception($"App.config \"{key}\" should not be null.");
        }
    }
}