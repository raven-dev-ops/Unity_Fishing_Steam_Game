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

    public sealed class SaveManager : MonoBehaviour, ISaveDataView
    {
        private const string FileName = "save_v1.json";
        private const int MaxCatchLogEntries = 200;
        private const int MaxFishSaleHistoryEntries = 160;

        private static SaveManager _instance;

        [SerializeField] private SaveDataV1 _current = new SaveDataV1();
        [SerializeField] private string _sessionId = string.Empty;
        [SerializeField] private List<int> _levelXpThresholds = new List<int>(ProgressionRules.Defaults);
        [SerializeField] private List<ProgressionUnlockDefinition> _progressionUnlocks = new List<ProgressionUnlockDefinition>();
        [SerializeField] private float _minimumSaveIntervalSeconds = 1f;

        [NonSerialized] private ISaveFileSystem _fileSystem;
        [NonSerialized] private ITimeProvider _timeProvider;
        [NonSerialized] private SaveWriteThrottle _saveWriteThrottle;
        [NonSerialized] private ISavePersistenceAdapter _savePersistenceAdapter;
        [NonSerialized] private ISaveMigrationLoadCoordinator _saveLoadCoordinator;
        [NonSerialized] private SaveProgressionService _progressionService;
        [NonSerialized] private SaveDomainMutationService _mutationService;

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

        private ISaveFileSystem FileSystem => _fileSystem ??= new SaveFileSystem();
        private ITimeProvider TimeProvider => _timeProvider ??= new UnityTimeProvider();
        private SaveWriteThrottle WriteThrottle => _saveWriteThrottle ??= new SaveWriteThrottle(_minimumSaveIntervalSeconds);
        private ISavePersistenceAdapter PersistenceAdapter => _savePersistenceAdapter ??= new AtomicJsonSavePersistenceAdapter();
        private ISaveMigrationLoadCoordinator LoadCoordinator => _saveLoadCoordinator ??= new SaveMigrationLoadCoordinator();
        private SaveProgressionService ProgressionService => _progressionService ??= new SaveProgressionService();
        private SaveDomainMutationService MutationService => _mutationService ??= new SaveDomainMutationService();
        private string SavePath => Path.Combine(FileSystem.PersistentDataPath, FileName);

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _saveWriteThrottle = new SaveWriteThrottle(_minimumSaveIntervalSeconds);
            _sessionId = Guid.NewGuid().ToString("N");
            NormalizeProgressionConfig();
            RuntimeServiceRegistry.Register(this);
            LoadOrCreate();
        }

        private void OnValidate()
        {
            _minimumSaveIntervalSeconds = Mathf.Max(0f, _minimumSaveIntervalSeconds);
            _saveWriteThrottle = new SaveWriteThrottle(_minimumSaveIntervalSeconds);
        }

        private void Update()
        {
            TryFlushPendingSave();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Save(forceImmediate: true);
            }
        }

        private void OnApplicationQuit()
        {
            Save(forceImmediate: true);
        }

        public void LoadOrCreate()
        {
            if (!TryLoadExisting(out var loaded))
            {
                _current = CreateNewSaveData();
                NormalizeCurrentData();
                Save(forceImmediate: true);
                return;
            }

            _current = loaded;
            NormalizeCurrentData();
            _current.lastLoginLocalDate = CurrentLocalDate();
            Save(forceImmediate: true);
        }

        public void Save()
        {
            Save(forceImmediate: false);
        }

        public void Save(bool forceImmediate)
        {
            if (forceImmediate)
            {
                PersistNow();
                return;
            }

            var now = TimeProvider.RealtimeSinceStartup;
            if (WriteThrottle.Request(now, forceImmediate: false))
            {
                PersistNow();
            }
        }

        internal void ConfigureRuntimeDependencies(
            ISaveFileSystem fileSystem,
            ITimeProvider timeProvider,
            ISavePersistenceAdapter persistenceAdapter = null,
            ISaveMigrationLoadCoordinator loadCoordinator = null)
        {
            _fileSystem = fileSystem ?? new SaveFileSystem();
            _timeProvider = timeProvider ?? new UnityTimeProvider();
            _saveWriteThrottle = new SaveWriteThrottle(_minimumSaveIntervalSeconds);
            _savePersistenceAdapter = persistenceAdapter ?? new AtomicJsonSavePersistenceAdapter();
            _saveLoadCoordinator = loadCoordinator ?? new SaveMigrationLoadCoordinator();
        }

        private void TryFlushPendingSave()
        {
            if (!WriteThrottle.TryFlush(TimeProvider.RealtimeSinceStartup))
            {
                return;
            }

            PersistNow();
        }

        private void PersistNow()
        {
            if (!PersistenceAdapter.TryPersist(SavePath, _current, FileSystem, out var failureReason))
            {
                Debug.LogError($"SaveManager: failed to save profile atomically ({failureReason}).");
                WriteThrottle.MarkPending();
                return;
            }

            WriteThrottle.MarkPersisted(TimeProvider.RealtimeSinceStartup);
            NotifySaveDataChanged();
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
            if (!MutationService.SetTutorialSeen(_current, tutorialSeen))
            {
                return;
            }

            Save();
        }

        public bool ShouldRunIntroTutorial()
        {
            return MutationService.ShouldRunIntroTutorial(_current);
        }

        public void RequestIntroTutorialReplay()
        {
            if (MutationService.RequestIntroTutorialReplay(_current))
            {
                Save();
            }
        }

        public void MarkIntroTutorialStarted()
        {
            if (MutationService.MarkIntroTutorialStarted(_current))
            {
                Save();
            }
        }

        public bool ShouldRunFishingLoopTutorial()
        {
            return MutationService.ShouldRunFishingLoopTutorial(_current);
        }

        public void RequestFishingLoopTutorialReplay()
        {
            if (MutationService.RequestFishingLoopTutorialReplay(_current))
            {
                Save();
            }
        }

        public void MarkFishingLoopTutorialStarted()
        {
            if (MutationService.MarkFishingLoopTutorialStarted(_current))
            {
                Save();
            }
        }

        public void CompleteFishingLoopTutorial(bool skipped)
        {
            if (MutationService.CompleteFishingLoopTutorial(_current, skipped))
            {
                Save();
            }
        }

        public void ResetProfileStats()
        {
            if (MutationService.ResetProfileStats(_current, _levelXpThresholds))
            {
                Save();
            }
        }

        public void ClearFishInventory()
        {
            if (!MutationService.ClearFishInventory(_current))
            {
                return;
            }

            Save();
        }

        public int GetFishInventoryCount()
        {
            return SaveDomainMutationService.CountFishInventory(_current != null ? _current.fishInventory : null);
        }

        public void EnsureStarterOwnership()
        {
            if (MutationService.EnsureStarterOwnership(_current))
            {
                Save();
            }
        }

        public void RecordCatch(string fishId, int distanceTier, float weightKg = 0f, int valueCopecs = 0, float depthMeters = 0f)
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

            var entry = AppendCatchLog(
                fishId,
                clampedDistanceTier,
                landed: true,
                depthMeters,
                weightKg,
                valueCopecs,
                failReason: string.Empty);
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
            AppendCatchLog(fishId, clampedDistanceTier, landed: false, 0f, 0f, 0, failReason ?? string.Empty);
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
            return ProgressionService.GetNextUnlockDescription(_current, _progressionUnlocks);
        }

        public bool IsContentUnlocked(string contentId)
        {
            return ProgressionService.IsContentUnlocked(_current, _progressionUnlocks, contentId);
        }

        public int GetUnlockLevel(string contentId)
        {
            return ProgressionService.GetUnlockLevel(_progressionUnlocks, contentId);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                Save(forceImmediate: true);
                _instance = null;
            }

            RuntimeServiceRegistry.Unregister(this);
        }

        private bool TryLoadExisting(out SaveDataV1 loaded)
        {
            return LoadCoordinator.TryLoad(SavePath, FileSystem, TimeProvider, out loaded, out _);
        }

        private SaveDataV1 CreateNewSaveData()
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

            MutationService.NormalizeCurrentDataEnvelope(
                _current,
                CurrentLocalDate,
                MaxCatchLogEntries,
                MaxFishSaleHistoryEntries);
            NormalizeProgressionData();
        }

        private void NormalizeProgressionData()
        {
            ProgressionService.NormalizeProgressionData(_current, _levelXpThresholds, _progressionUnlocks);
        }

        private void NormalizeProgressionConfig()
        {
            _levelXpThresholds ??= new List<int>();
            _progressionUnlocks ??= new List<ProgressionUnlockDefinition>();
            ProgressionService.NormalizeConfig(_levelXpThresholds, _progressionUnlocks);
        }

        private bool ApplyProgressionXp(int xpAmount, out int previousLevel, out int newLevel)
        {
            return ProgressionService.ApplyProgressionXp(
                _current,
                xpAmount,
                _levelXpThresholds,
                _progressionUnlocks,
                out previousLevel,
                out newLevel);
        }

        private string CurrentLocalDate()
        {
            return DateTimeUtility.ToLocalDateString(TimeProvider.LocalNow);
        }

        private CatchLogEntry AppendCatchLog(string fishId, int distanceTier, bool landed, float depthMeters, float weightKg, int valueCopecs, string failReason)
        {
            return MutationService.AppendCatchLog(
                _current,
                MaxCatchLogEntries,
                fishId,
                distanceTier,
                landed,
                depthMeters,
                weightKg,
                valueCopecs,
                failReason,
                _sessionId,
                TimeProvider.UtcNow);
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

        private void NotifySaveDataChanged()
        {
            try
            {
                SaveDataChanged?.Invoke(_current);
            }
            catch (Exception ex)
            {
                Debug.LogError($"SaveManager: SaveDataChanged listener failed ({ex.Message}).");
            }
        }
    }
}
