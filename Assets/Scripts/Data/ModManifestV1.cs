using System;
using System.Collections.Generic;

namespace RavenDevOps.Fishing.Tools
{
    [Serializable]
    public sealed class ModManifestV1
    {
        public string schemaVersion = "1.0";
        public string modId = string.Empty;
        public string modVersion = "1.0.0";
        public string displayName = string.Empty;
        public string author = string.Empty;
        public string description = string.Empty;
        public string minGameVersion = string.Empty;
        public string maxGameVersion = string.Empty;
        public List<string> dataCatalogs = new List<string>();
        public List<string> assetOverrides = new List<string>();
    }
}
