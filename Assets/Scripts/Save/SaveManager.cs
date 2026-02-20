using System;
using System.Collections.Generic;
using System.IO;
using RavenDevOps.Fishing.Core;
using UnityEngine;

namespace RavenDevOps.Fishing.Save
{
    public enum ProgressionUnlockType
    {
        Content = 0,
        Ship = 1,
        Hook = 2
    }

    [Serializable]
    public sealed class ProgressionUnlockDefinition
    {
        public int level = 2;
        public ProgressionUnlockType unlockType = ProgressionUnlockType.Content;
        public string unlockId = string.Empty;
        public string displayName = string.Empty;
    }

    public sealed class SaveManager : MonoBehaviour
    {
        private const string FileName = "save_v1.json";
        private const string TempFileSuffix = ".tmp";
        private const string BackupFileSuffix = ".bak";
        private const int MaxCatchLogEntries = 200;

        private static SaveManager _instance;

        [SerializeField] private SaveDataV1 _current = new SaveDataV1();
        [SerializeField] private string _sessionId = string.Empty;
        [SerializeField] private List<int> _levelXpThresholds = new List<int>(ProgressionRules.Defaults);
        [SerializeField] private List<ProgressionUnlockDefinition> _progressionUnlocks = new List<ProgressionUnlockDefinition>();

        public static SaveManager Instance => _instance;
        public SaveDataV1 Current => _current;
        public int CurrentLevel => _current != null && _current.progression != null ? Mathf.Max(1, _current.progression.level) : 1;
        public int CurrentTotalXp => _current != null && _current.progression != null ? Mathf.Max(0, _current.progression.totalXp) : 0;
        public int CurrentXpIntoLevel => _current != null && _current.progression != null ? Mathf.Max(0, _current.progression.xpIntoLevel) : 0;
        public int CurrentXpToNextLevel => _current != null && _current.progression != null ? Mathf.Max(0, _current.progression.xpToNextLevel) : 0;
        public string SaveFilePath => SavePath;

        public event Action<SaveDataV1> SaveDataChanged;
        public event Action<CatchLogEntry> CatchRecorded;
        public event Action<int, int> LevelChanged;
        public event Action<int> TripCompleted;
        public event Action<string, int, int> PurchaseRecorded;

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
            NormalizeProgressionConfig();
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
                NormalizeCurrentData();
                Save();
                return;
            }

            _current = loaded;
            NormalizeCurrentData();
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
            InvokeTripCompleted(_current.stats.totalTrips);
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

        public bool ShouldRunFishingLoopTutorial()
        {
            _current.tutorialFlags ??= new TutorialFlags();
            return !_current.tutorialFlags.fishingLoopTutorialCompleted || _current.tutorialFlags.fishingLoopTutorialReplayRequested;
        }

        public void RequestFishingLoopTutorialReplay()
        {
            _current.tutorialFlags ??= new TutorialFlags();
            _current.tutorialFlags.fishingLoopTutorialCompleted = false;
            _current.tutorialFlags.fishingLoopTutorialSkipped = false;
            _current.tutorialFlags.fishingLoopTutorialReplayRequested = true;
            Save();
        }

        public void MarkFishingLoopTutorialStarted()
        {
            _current.tutorialFlags ??= new TutorialFlags();
            if (_current.tutorialFlags.fishingLoopTutorialReplayRequested)
            {
                _current.tutorialFlags.fishingLoopTutorialReplayRequested = false;
                Save();
            }
        }

        public void CompleteFishingLoopTutorial(bool skipped)
        {
            _current.tutorialFlags ??= new TutorialFlags();
            _current.tutorialFlags.fishingLoopTutorialCompleted = true;
            _current.tutorialFlags.fishingLoopTutorialSkipped = skipped;
            _current.tutorialFlags.fishingLoopTutorialReplayRequested = false;
            Save();
        }

        public void ResetProfileStats()
        {
            _current.stats ??= new SaveStats();
            _current.progression ??= new ProgressionData();
            _current.copecs = 0;
            _current.stats.totalFishCaught = 0;
            _current.stats.farthestDistanceTier = 0;
            _current.stats.totalTrips = 0;
            _current.stats.totalPurchases = 0;
            _current.stats.totalCatchValueCopecs = 0;
            _current.progression.totalXp = 0;
            _current.progression.level = 1;
            _current.progression.xpIntoLevel = 0;
            _current.progression.xpToNextLevel = _levelXpThresholds.Count > 1 ? _levelXpThresholds[1] : 0;
            _current.progression.unlockedContentIds ??= new List<string>();
            _current.progression.unlockedContentIds.Clear();
            _current.progression.lastUnlockId = string.Empty;
            _current.objectiveProgress ??= new ObjectiveProgressData();
            _current.objectiveProgress.entries ??= new List<ObjectiveProgressEntry>();
            for (var i = 0; i < _current.objectiveProgress.entries.Count; i++)
            {
                var entry = _current.objectiveProgress.entries[i];
                if (entry == null)
                {
                    continue;
                }

                entry.currentCount = 0;
                entry.completed = false;
            }

            _current.objectiveProgress.completedObjectives = 0;
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
            _current.stats.totalCatchValueCopecs += Mathf.Max(0, valueCopecs);

            var xpEarned = ProgressionRules.CalculateCatchXp(clampedDistanceTier, weightKg, valueCopecs);
            var didLevelUp = ApplyProgressionXp(xpEarned, out var previousLevel, out var newLevel);

            var entry = AppendCatchLog(fishId, clampedDistanceTier, landed: true, weightKg, valueCopecs, failReason: string.Empty);
            Save();

            if (didLevelUp)
            {
                InvokeLevelChanged(previousLevel, newLevel);
            }

            InvokeCatchRecorded(entry);
        }

        public void RecordPurchase(string itemId, int priceCopecs, bool saveAfterRecord = true)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return;
            }

            _current.stats ??= new SaveStats();
            _current.stats.totalPurchases += 1;
            if (saveAfterRecord)
            {
                Save();
            }

            InvokePurchaseRecorded(itemId, Mathf.Max(0, priceCopecs), _current.stats.totalPurchases);
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

        public string GetNextUnlockDescription()
        {
            _current.progression ??= new ProgressionData();
            _current.progression.unlockedContentIds ??= new List<string>();

            for (var i = 0; i < _progressionUnlocks.Count; i++)
            {
                var unlock = _progressionUnlocks[i];
                if (unlock == null || string.IsNullOrWhiteSpace(unlock.unlockId))
                {
                    continue;
                }

                if (_current.progression.unlockedContentIds.Contains(unlock.unlockId))
                {
                    continue;
                }

                var label = string.IsNullOrWhiteSpace(unlock.displayName) ? unlock.unlockId : unlock.displayName;
                return $"Level {Mathf.Max(1, unlock.level)}: {label}";
            }

            return "All configured unlocks claimed";
        }

        public bool IsContentUnlocked(string contentId)
        {
            if (string.IsNullOrWhiteSpace(contentId))
            {
                return false;
            }

            _current.progression ??= new ProgressionData();
            _current.progression.unlockedContentIds ??= new List<string>();

            var isTrackedUnlock = false;
            for (var i = 0; i < _progressionUnlocks.Count; i++)
            {
                var unlock = _progressionUnlocks[i];
                if (unlock == null || string.IsNullOrWhiteSpace(unlock.unlockId))
                {
                    continue;
                }

                if (!string.Equals(unlock.unlockId, contentId, StringComparison.Ordinal))
                {
                    continue;
                }

                isTrackedUnlock = true;
                if (_current.progression.unlockedContentIds.Contains(unlock.unlockId))
                {
                    return true;
                }
            }

            return !isTrackedUnlock;
        }

        public int GetUnlockLevel(string contentId)
        {
            if (string.IsNullOrWhiteSpace(contentId))
            {
                return 1;
            }

            for (var i = 0; i < _progressionUnlocks.Count; i++)
            {
                var unlock = _progressionUnlocks[i];
                if (unlock == null || string.IsNullOrWhiteSpace(unlock.unlockId))
                {
                    continue;
                }

                if (string.Equals(unlock.unlockId, contentId, StringComparison.Ordinal))
                {
                    return Mathf.Max(1, unlock.level);
                }
            }

            return 1;
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

        private void NormalizeCurrentData()
        {
            if (_current == null)
            {
                _current = CreateNewSaveData();
            }

            _current.ownedShips ??= new List<string>();
            _current.ownedHooks ??= new List<string>();
            _current.fishInventory ??= new List<FishInventoryEntry>();
            _current.catchLog ??= new List<CatchLogEntry>();
            _current.tutorialFlags ??= new TutorialFlags();
            _current.stats ??= new SaveStats();
            _current.progression ??= new ProgressionData();
            _current.progression.unlockedContentIds ??= new List<string>();
            _current.objectiveProgress ??= new ObjectiveProgressData();
            _current.objectiveProgress.entries ??= new List<ObjectiveProgressEntry>();

            if (_current.ownedShips.Count == 0)
            {
                _current.ownedShips.Add("ship_lv1");
            }

            if (_current.ownedHooks.Count == 0)
            {
                _current.ownedHooks.Add("hook_lv1");
            }

            if (string.IsNullOrWhiteSpace(_current.equippedShipId))
            {
                _current.equippedShipId = _current.ownedShips[0];
            }

            if (string.IsNullOrWhiteSpace(_current.equippedHookId))
            {
                _current.equippedHookId = _current.ownedHooks[0];
            }

            if (string.IsNullOrWhiteSpace(_current.careerStartLocalDate))
            {
                _current.careerStartLocalDate = CurrentLocalDate();
            }

            if (string.IsNullOrWhiteSpace(_current.lastLoginLocalDate))
            {
                _current.lastLoginLocalDate = _current.careerStartLocalDate;
            }

            TrimCatchLog(_current.catchLog);
            NormalizeProgressionData();
        }

        private void NormalizeProgressionData()
        {
            var progression = _current.progression ?? new ProgressionData();
            progression.totalXp = Mathf.Max(0, progression.totalXp);
            ProgressionRules.ResolveXpProgress(
                progression.totalXp,
                _levelXpThresholds,
                out var resolvedLevel,
                out var xpIntoLevel,
                out var xpToNextLevel);

            progression.level = resolvedLevel;
            progression.xpIntoLevel = xpIntoLevel;
            progression.xpToNextLevel = xpToNextLevel;
            _current.progression = progression;
            ApplyProgressionUnlocks(minLevelInclusive: 1, maxLevelInclusive: progression.level);
        }

        private void NormalizeProgressionConfig()
        {
            _levelXpThresholds ??= new List<int>();
            if (_levelXpThresholds.Count == 0)
            {
                _levelXpThresholds.AddRange(ProgressionRules.Defaults);
            }

            for (var i = 0; i < _levelXpThresholds.Count; i++)
            {
                _levelXpThresholds[i] = Mathf.Max(0, _levelXpThresholds[i]);
            }

            _levelXpThresholds.Sort();
            for (var i = _levelXpThresholds.Count - 1; i > 0; i--)
            {
                if (_levelXpThresholds[i] == _levelXpThresholds[i - 1])
                {
                    _levelXpThresholds.RemoveAt(i);
                }
            }

            if (_levelXpThresholds.Count == 0 || _levelXpThresholds[0] != 0)
            {
                _levelXpThresholds.Insert(0, 0);
            }

            _progressionUnlocks ??= new List<ProgressionUnlockDefinition>();
            if (_progressionUnlocks.Count == 0)
            {
                SeedDefaultProgressionUnlocks();
            }

            _progressionUnlocks.RemoveAll(IsInvalidUnlockDefinition);
            _progressionUnlocks.Sort((a, b) => a.level.CompareTo(b.level));
        }

        private void SeedDefaultProgressionUnlocks()
        {
            _progressionUnlocks.Add(new ProgressionUnlockDefinition
            {
                level = 2,
                unlockType = ProgressionUnlockType.Hook,
                unlockId = "hook_lv2",
                displayName = "Hook Lv2"
            });
            _progressionUnlocks.Add(new ProgressionUnlockDefinition
            {
                level = 3,
                unlockType = ProgressionUnlockType.Ship,
                unlockId = "ship_lv2",
                displayName = "Ship Lv2"
            });
            _progressionUnlocks.Add(new ProgressionUnlockDefinition
            {
                level = 4,
                unlockType = ProgressionUnlockType.Hook,
                unlockId = "hook_lv3",
                displayName = "Hook Lv3"
            });
            _progressionUnlocks.Add(new ProgressionUnlockDefinition
            {
                level = 5,
                unlockType = ProgressionUnlockType.Ship,
                unlockId = "ship_lv3",
                displayName = "Ship Lv3"
            });
        }

        private static bool IsInvalidUnlockDefinition(ProgressionUnlockDefinition unlock)
        {
            return unlock == null || unlock.level < 2 || string.IsNullOrWhiteSpace(unlock.unlockId);
        }

        private bool ApplyProgressionXp(int xpAmount, out int previousLevel, out int newLevel)
        {
            previousLevel = CurrentLevel;
            newLevel = previousLevel;
            if (xpAmount <= 0)
            {
                return false;
            }

            _current.progression ??= new ProgressionData();
            _current.progression.totalXp = Mathf.Max(0, _current.progression.totalXp + xpAmount);
            ProgressionRules.ResolveXpProgress(
                _current.progression.totalXp,
                _levelXpThresholds,
                out var resolvedLevel,
                out var xpIntoLevel,
                out var xpToNextLevel);

            _current.progression.level = resolvedLevel;
            _current.progression.xpIntoLevel = xpIntoLevel;
            _current.progression.xpToNextLevel = xpToNextLevel;
            newLevel = resolvedLevel;

            if (resolvedLevel > previousLevel)
            {
                ApplyProgressionUnlocks(previousLevel + 1, resolvedLevel);
                return true;
            }

            return false;
        }

        private void ApplyProgressionUnlocks(int minLevelInclusive, int maxLevelInclusive)
        {
            _current.ownedShips ??= new List<string>();
            _current.ownedHooks ??= new List<string>();
            _current.progression ??= new ProgressionData();
            _current.progression.unlockedContentIds ??= new List<string>();

            for (var i = 0; i < _progressionUnlocks.Count; i++)
            {
                var unlock = _progressionUnlocks[i];
                if (unlock == null || unlock.level < minLevelInclusive || unlock.level > maxLevelInclusive)
                {
                    continue;
                }

                if (!_current.progression.unlockedContentIds.Contains(unlock.unlockId))
                {
                    _current.progression.unlockedContentIds.Add(unlock.unlockId);
                    _current.progression.lastUnlockId = unlock.unlockId;
                }

                ApplyUnlockOwnership(unlock);
            }
        }

        private void ApplyUnlockOwnership(ProgressionUnlockDefinition unlock)
        {
            if (unlock == null || string.IsNullOrWhiteSpace(unlock.unlockId))
            {
                return;
            }

            if (unlock.unlockType == ProgressionUnlockType.Ship)
            {
                if (!_current.ownedShips.Contains(unlock.unlockId))
                {
                    _current.ownedShips.Add(unlock.unlockId);
                }
            }
            else if (unlock.unlockType == ProgressionUnlockType.Hook)
            {
                if (!_current.ownedHooks.Contains(unlock.unlockId))
                {
                    _current.ownedHooks.Add(unlock.unlockId);
                }
            }
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

        private CatchLogEntry AppendCatchLog(string fishId, int distanceTier, bool landed, float weightKg, int valueCopecs, string failReason)
        {
            _current.catchLog ??= new List<CatchLogEntry>();
            var entry = new CatchLogEntry
            {
                fishId = fishId ?? string.Empty,
                distanceTier = Mathf.Max(1, distanceTier),
                weightKg = Mathf.Max(0f, weightKg),
                valueCopecs = Mathf.Max(0, valueCopecs),
                timestampUtc = DateTime.UtcNow.ToString("O"),
                sessionId = _sessionId,
                landed = landed,
                failReason = failReason ?? string.Empty
            };

            _current.catchLog.Add(entry);
            TrimCatchLog(_current.catchLog);
            return entry;
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

        private void InvokeCatchRecorded(CatchLogEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            try
            {
                CatchRecorded?.Invoke(entry);
            }
            catch (Exception ex)
            {
                Debug.LogError($"SaveManager: CatchRecorded listener failed ({ex.Message}).");
            }
        }

        private void InvokeLevelChanged(int previousLevel, int newLevel)
        {
            if (newLevel <= previousLevel)
            {
                return;
            }

            try
            {
                LevelChanged?.Invoke(previousLevel, newLevel);
            }
            catch (Exception ex)
            {
                Debug.LogError($"SaveManager: LevelChanged listener failed ({ex.Message}).");
            }
        }

        private void InvokeTripCompleted(int totalTrips)
        {
            try
            {
                TripCompleted?.Invoke(Mathf.Max(0, totalTrips));
            }
            catch (Exception ex)
            {
                Debug.LogError($"SaveManager: TripCompleted listener failed ({ex.Message}).");
            }
        }

        private void InvokePurchaseRecorded(string itemId, int priceCopecs, int totalPurchases)
        {
            try
            {
                PurchaseRecorded?.Invoke(itemId, Mathf.Max(0, priceCopecs), Mathf.Max(0, totalPurchases));
            }
            catch (Exception ex)
            {
                Debug.LogError($"SaveManager: PurchaseRecorded listener failed ({ex.Message}).");
            }
        }
    }
}
