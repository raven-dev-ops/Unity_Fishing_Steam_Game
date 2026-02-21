using System;

namespace RavenDevOps.Fishing.Save
{
    public interface ISaveDataView
    {
        SaveDataV1 Current { get; }
        event Action<SaveDataV1> SaveDataChanged;
    }
}
