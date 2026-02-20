using System;
using System.Collections.Generic;
using System.IO;
using RavenDevOps.Fishing.Core;
using UnityEngine;

namespace RavenDevOps.Fishing.Save
{
    public sealed class SaveManager : MonoBehaviour
    {
        private const string FileName = "save_v1.json";
        private const string TempFileSuffix = ".tmp";
        private const string BackupFileSuffix = ".bak";
        private const int MaxCatchLogEntries = 200;

        private static SaveManager _instance;

        [SerializeField] private SaveDataV1 _current = new SaveDataV1();
        [SerializeField] private string _sessionId = string.Empty;

        public static SaveManager Instance => _instance;
        public SaveDataV1 Current => _current;
        public event Action<SaveDataV1> SaveDataChanged;

        private string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _sessionId = Guid.NewGuid().ToString("N");
            RuntimeServiceRegistry.Register(this);
            LoadOrCreate();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Save();
            }
        }

        private void OnApplicationQuit()
        {
            Save();
        }

        public void LoadOrCreate()
        {
            if (!TryLoadExisting(out var loaded))
            {
                _current = CreateNewSaveData();
                Save();
                return;
            }

            _current = loaded;
            NormalizeLoadedData(_current);
            _current.lastLoginLocalDate = CurrentLocalDate();
            Save();
        }

        public void Save()
        {
            try
            {
                var saveDir = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrWhiteSpace(saveDir))
                {
                    Directory.CreateDirectory(saveDir);
                }

                var tmpPath = SavePath + TempFileSuffix;
                var backupPath = SavePath + BackupFileSuffix;
                var json = JsonUtility.ToJson(_current, true);

                File.WriteAllText(tmpPath, json);

                if (File.Exists(SavePath))
                {
                    AtomicReplace(tmpPath, SavePath, backupPath);
                }
                else
                {
                    File.Move(tmpPath, SavePath);
                }

                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                if (File.Exists(tmpPath))
                {
                    File.Delete(tmpPath);
                }

                try
                {
                    SaveDataChanged?.Invoke(_current);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"SaveManager: SaveDataChanged listener failed ({ex.Message}).");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"SaveManager: failed to save profile atomically ({ex.Message}).");
            }
        }

        public void AddCopecs(int value)
        {
            var clamped = Mathf.Max(0, value);
            if (clamped == 0)
            {
                return;
            }

            _current.copecs += clamped;
            Save();
        }

        public void MarkTripCompleted()
        {
            _current.stats.totalTrips += 1;
            Save();
        }

        public void SetTutorialSeen(bool tutorialSeen)
        {
            _current.tutorialFlags ??= new TutorialFlags();
            if (_current.tutorialFlags.tutorialSeen == tutorialSeen)
            {
                return;
            }

            _current.tutorialFlags.tutorialSeen = tutorialSeen;
            Save();
        }

        public void ResetProfileStats()
        {
            _current.stats ??= new SaveStats();
            _current.copecs = 0;
            _current.stats.totalFishCaught = 0;
            _current.stats.farthestDistanceTier = 0;
            _current.stats.totalTrips = 0;
            Save();
        }

        public void ClearFishInventory()
        {
            _current.fishInventory ??= new List<FishInventoryEntry>();
            if (_current.fishInventory.Count == 0)
            {
                return;
            }

            _current.fishInventory.Clear();
            Save();
        }

        public void EnsureStarterOwnership()
        {
            _current.ownedShips ??= new List<string>();
            _current.ownedHooks ??= new List<string>();

            var changed = false;
            if (!_current.ownedShips.Contains("ship_lv1"))
            {
                _current.ownedShips.Add("ship_lv1");
                changed = true;
            }

            if (!_current.ownedHooks.Contains("hook_lv1"))
            {
                _current.ownedHooks.Add("hook_lv1");
                changed = true;
            }

            if (changed)
            {
                Save();
            }
        }

        public void RecordCatch(string fishId, int distanceTier, float weightKg = 0f, int valueCopecs = 0)
        {
            if (string.IsNullOrWhiteSpace(fishId))
            {
                return;
            }

            _current.fishInventory ??= new List<FishInventoryEntry>();
            _current.stats ??= new SaveStats();
            var clampedDistanceTier = Mathf.Max(1, distanceTier);

            var existing = _current.fishInventory.Find(x =>
                x != null &&
                string.Equals(x.fishId, fishId, StringComparison.Ordinal) &&
                x.distanceTier == clampedDistanceTier);

            if (existing == null)
            {
                _current.fishInventory.Add(new FishInventoryEntry
                {
                    fishId = fishId,
                    distanceTier = clampedDistanceTier,
                    count = 1
                });
            }
            else
            {
                existing.count += 1;
            }

            _current.stats.totalFishCaught += 1;
            _current.stats.farthestDistanceTier = Mathf.Max(_current.stats.farthestDistanceTier, clampedDistanceTier);
            AppendCatchLog(fishId, clampedDistanceTier, landed: true, weightKg, valueCopecs, failReason: string.Empty);
            Save();
        }

        public void RecordCatchFailure(string fishId, int distanceTier, string failReason)
        {
            var clampedDistanceTier = Mathf.Max(1, distanceTier);
            AppendCatchLog(fishId, clampedDistanceTier, landed: false, 0f, 0, failReason ?? string.Empty);
            Save();
        }

        public List<CatchLogEntry> GetRecentCatchLogSnapshot(int maxEntries)
        {
            _current.catchLog ??= new List<CatchLogEntry>();
            var count = Mathf.Max(1, maxEntries);
            var start = Mathf.Max(0, _current.catchLog.Count - count);
            return _current.catchLog.GetRange(start, _current.catchLog.Count - start);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            RuntimeServiceRegistry.Unregister(this);
        }

        private bool TryLoadExisting(out SaveDataV1 loaded)
        {
            loaded = null;
            if (!File.Exists(SavePath))
            {
                return false;
            }

            try
            {
                var json = File.ReadAllText(SavePath);
                loaded = JsonUtility.FromJson<SaveDataV1>(json);
            }
            catch (Exception ex)
            {
                BackupCorruptSaveFile($"read/deserialize exception: {ex.Message}");
                return false;
            }

            if (loaded == null)
            {
                BackupCorruptSaveFile("deserialize produced null save");
                return false;
            }

            return true;
        }

        private static SaveDataV1 CreateNewSaveData()
        {
            var now = CurrentLocalDate();
            return new SaveDataV1
            {
                careerStartLocalDate = now,
                lastLoginLocalDate = now
            };
        }

        private static void NormalizeLoadedData(SaveDataV1 save)
        {
            if (save == null)
            {
                return;
            }

            save.ownedShips ??= new List<string>();
            save.ownedHooks ??= new List<string>();
            save.fishInventory ??= new List<FishInventoryEntry>();
            save.catchLog ??= new List<CatchLogEntry>();
            save.tutorialFlags ??= new TutorialFlags();
            save.stats ??= new SaveStats();

            if (save.ownedShips.Count == 0)
            {
                save.ownedShips.Add("ship_lv1");
            }

            if (save.ownedHooks.Count == 0)
            {
                save.ownedHooks.Add("hook_lv1");
            }

            if (string.IsNullOrWhiteSpace(save.equippedShipId))
            {
                save.equippedShipId = save.ownedShips[0];
            }

            if (string.IsNullOrWhiteSpace(save.equippedHookId))
            {
                save.equippedHookId = save.ownedHooks[0];
            }

            if (string.IsNullOrWhiteSpace(save.careerStartLocalDate))
            {
                save.careerStartLocalDate = CurrentLocalDate();
            }

            if (string.IsNullOrWhiteSpace(save.lastLoginLocalDate))
            {
                save.lastLoginLocalDate = save.careerStartLocalDate;
            }

            TrimCatchLog(save.catchLog);
        }

        private void BackupCorruptSaveFile(string reason)
        {
            if (!File.Exists(SavePath))
            {
                return;
            }

            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var corruptPath = SavePath + $".corrupt_{timestamp}";
                File.Copy(SavePath, corruptPath, overwrite: true);
                Debug.LogWarning($"SaveManager: detected corrupt save, copied to '{corruptPath}' ({reason}).");
            }
            catch (Exception ex)
            {
                Debug.LogError($"SaveManager: failed to back up corrupt save ({ex.Message}).");
            }
        }

        private static void AtomicReplace(string tempPath, string destinationPath, string backupPath)
        {
            try
            {
                File.Replace(tempPath, destinationPath, backupPath, ignoreMetadataErrors: true);
            }
            catch (PlatformNotSupportedException)
            {
                File.Copy(tempPath, destinationPath, overwrite: true);
                File.Delete(tempPath);
            }
            catch (IOException)
            {
                File.Copy(tempPath, destinationPath, overwrite: true);
                File.Delete(tempPath);
            }
        }

        private static string CurrentLocalDate()
        {
            return DateTime.Now.ToString("yyyy-MM-dd");
        }

        private void AppendCatchLog(string fishId, int distanceTier, bool landed, float weightKg, int valueCopecs, string failReason)
        {
            _current.catchLog ??= new List<CatchLogEntry>();
            _current.catchLog.Add(new CatchLogEntry
            {
                fishId = fishId ?? string.Empty,
                distanceTier = Mathf.Max(1, distanceTier),
                weightKg = Mathf.Max(0f, weightKg),
                valueCopecs = Mathf.Max(0, valueCopecs),
                timestampUtc = DateTime.UtcNow.ToString("O"),
                sessionId = _sessionId,
                landed = landed,
                failReason = failReason ?? string.Empty
            });

            TrimCatchLog(_current.catchLog);
        }

        private static void TrimCatchLog(List<CatchLogEntry> log)
        {
            if (log == null)
            {
                return;
            }

            if (log.Count <= MaxCatchLogEntries)
            {
                return;
            }

            var removeCount = log.Count - MaxCatchLogEntries;
            log.RemoveRange(0, removeCount);
        }
    }
}
