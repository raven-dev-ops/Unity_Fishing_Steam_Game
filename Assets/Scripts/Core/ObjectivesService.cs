using System;
using System.Collections.Generic;
using RavenDevOps.Fishing.Save;
using RavenDevOps.Fishing.UI;
using UnityEngine;

namespace RavenDevOps.Fishing.Core
{
    public enum ObjectiveType
    {
        CatchCount = 0,
        CompleteTrips = 1,
        CatchValueCopecs = 2
    }

    [Serializable]
    public sealed class ObjectiveDefinition
    {
        public string id = string.Empty;
        public string description = string.Empty;
        public ObjectiveType objectiveType = ObjectiveType.CatchCount;
        public int targetCount = 1;
        public int rewardCopecs = 100;
    }

    public sealed class ObjectivesService : MonoBehaviour
    {
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private bool _autoSeedDefaults = true;
        [SerializeField] private List<ObjectiveDefinition> _definitions = new List<ObjectiveDefinition>();

        public event Action<ObjectiveProgressEntry> ObjectiveUpdated;
        public event Action<ObjectiveProgressEntry> ObjectiveCompleted;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Register(this);
            SeedDefaultsIfNeeded();
            EnsureObjectiveEntries(saveIfChanged: true);
            UpdateObjectiveUi();
        }

        private void OnEnable()
        {
            if (_saveManager != null)
            {
                _saveManager.CatchRecorded += OnCatchRecorded;
                _saveManager.TripCompleted += OnTripCompleted;
                _saveManager.SaveDataChanged += OnSaveDataChanged;
            }

            UpdateObjectiveUi();
        }

        private void OnDisable()
        {
            if (_saveManager != null)
            {
                _saveManager.CatchRecorded -= OnCatchRecorded;
                _saveManager.TripCompleted -= OnTripCompleted;
                _saveManager.SaveDataChanged -= OnSaveDataChanged;
            }
        }

        private void OnDestroy()
        {
            RuntimeServiceRegistry.Unregister(this);
        }

        public ObjectiveProgressEntry GetActiveObjectiveSnapshot()
        {
            var save = _saveManager != null ? _saveManager.Current : null;
            if (save == null || save.objectiveProgress == null || save.objectiveProgress.entries == null)
            {
                return null;
            }

            for (var i = 0; i < _definitions.Count; i++)
            {
                var definition = _definitions[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.id))
                {
                    continue;
                }

                var entry = FindEntryById(save.objectiveProgress.entries, definition.id);
                if (entry != null && !entry.completed)
                {
                    return Clone(entry);
                }
            }

            return null;
        }

        public void ResetObjectiveProgressForQA()
        {
            if (_saveManager == null || _saveManager.Current == null)
            {
                return;
            }

            var progress = _saveManager.Current.objectiveProgress ?? new ObjectiveProgressData();
            progress.entries ??= new List<ObjectiveProgressEntry>();
            for (var i = 0; i < progress.entries.Count; i++)
            {
                var entry = progress.entries[i];
                if (entry == null)
                {
                    continue;
                }

                entry.currentCount = 0;
                entry.completed = false;
            }

            progress.completedObjectives = 0;
            _saveManager.Current.objectiveProgress = progress;
            _saveManager.Save();
            UpdateObjectiveUi();
        }

        public string BuildActiveObjectiveLabel()
        {
            var active = GetActiveObjectiveSnapshot();
            if (active == null)
            {
                return "Objective: All objectives complete";
            }

            return $"Objective: {active.description} ({Mathf.Clamp(active.currentCount, 0, active.targetCount)}/{active.targetCount}) +{active.rewardCopecs}c";
        }

        private void OnCatchRecorded(CatchLogEntry catchLogEntry)
        {
            if (_saveManager == null || _saveManager.Current == null)
            {
                return;
            }

            EnsureObjectiveEntries(saveIfChanged: false);
            var save = _saveManager.Current;
            if (save.stats == null)
            {
                return;
            }

            var changed = false;
            var completedEntries = new List<ObjectiveProgressEntry>();

            for (var i = 0; i < _definitions.Count; i++)
            {
                var definition = _definitions[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.id))
                {
                    continue;
                }

                var entry = FindEntryById(save.objectiveProgress.entries, definition.id);
                if (entry == null || entry.completed)
                {
                    continue;
                }

                if (definition.objectiveType == ObjectiveType.CatchCount)
                {
                    changed |= SetEntryProgress(entry, Mathf.Max(0, save.stats.totalFishCaught));
                }
                else if (definition.objectiveType == ObjectiveType.CatchValueCopecs)
                {
                    changed |= SetEntryProgress(entry, Mathf.Max(0, save.stats.totalCatchValueCopecs));
                }

                if (TryCompleteEntry(save, entry))
                {
                    changed = true;
                    completedEntries.Add(Clone(entry));
                }

                RaiseObjectiveUpdated(Clone(entry));
            }

            if (changed)
            {
                save.objectiveProgress.completedObjectives = CountCompleted(save.objectiveProgress.entries);
                _saveManager.Save();
            }

            for (var i = 0; i < completedEntries.Count; i++)
            {
                RaiseObjectiveCompleted(completedEntries[i]);
            }

            UpdateObjectiveUi();
        }

        private void OnTripCompleted(int totalTrips)
        {
            if (_saveManager == null || _saveManager.Current == null)
            {
                return;
            }

            EnsureObjectiveEntries(saveIfChanged: false);
            var save = _saveManager.Current;
            var changed = false;
            var completedEntries = new List<ObjectiveProgressEntry>();

            for (var i = 0; i < _definitions.Count; i++)
            {
                var definition = _definitions[i];
                if (definition == null || definition.objectiveType != ObjectiveType.CompleteTrips)
                {
                    continue;
                }

                var entry = FindEntryById(save.objectiveProgress.entries, definition.id);
                if (entry == null || entry.completed)
                {
                    continue;
                }

                changed |= SetEntryProgress(entry, Mathf.Max(0, totalTrips));
                if (TryCompleteEntry(save, entry))
                {
                    changed = true;
                    completedEntries.Add(Clone(entry));
                }

                RaiseObjectiveUpdated(Clone(entry));
            }

            if (changed)
            {
                save.objectiveProgress.completedObjectives = CountCompleted(save.objectiveProgress.entries);
                _saveManager.Save();
            }

            for (var i = 0; i < completedEntries.Count; i++)
            {
                RaiseObjectiveCompleted(completedEntries[i]);
            }

            UpdateObjectiveUi();
        }

        private void OnSaveDataChanged(SaveDataV1 data)
        {
            EnsureObjectiveEntries(saveIfChanged: false);
            UpdateObjectiveUi();
        }

        private void UpdateObjectiveUi()
        {
            var hud = RuntimeServiceRegistry.Get<HudOverlayController>();
            if (hud != null)
            {
                hud.SetObjectiveStatus(BuildActiveObjectiveLabel());
            }
        }

        private void SeedDefaultsIfNeeded()
        {
            if (!_autoSeedDefaults || _definitions.Count > 0)
            {
                return;
            }

            _definitions.Add(new ObjectiveDefinition
            {
                id = "obj_catch_3",
                description = "Catch 3 fish",
                objectiveType = ObjectiveType.CatchCount,
                targetCount = 3,
                rewardCopecs = 120
            });

            _definitions.Add(new ObjectiveDefinition
            {
                id = "obj_trip_2",
                description = "Complete 2 fishing trips",
                objectiveType = ObjectiveType.CompleteTrips,
                targetCount = 2,
                rewardCopecs = 90
            });

            _definitions.Add(new ObjectiveDefinition
            {
                id = "obj_value_250",
                description = "Land catch value of 250 copecs",
                objectiveType = ObjectiveType.CatchValueCopecs,
                targetCount = 250,
                rewardCopecs = 160
            });
        }

        private void EnsureObjectiveEntries(bool saveIfChanged)
        {
            if (_saveManager == null || _saveManager.Current == null)
            {
                return;
            }

            var save = _saveManager.Current;
            save.objectiveProgress ??= new ObjectiveProgressData();
            save.objectiveProgress.entries ??= new List<ObjectiveProgressEntry>();

            var changed = false;
            for (var i = 0; i < _definitions.Count; i++)
            {
                var definition = _definitions[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.id))
                {
                    continue;
                }

                var existing = FindEntryById(save.objectiveProgress.entries, definition.id);
                if (existing != null)
                {
                    existing.description = definition.description ?? string.Empty;
                    existing.targetCount = Mathf.Max(1, definition.targetCount);
                    existing.rewardCopecs = Mathf.Max(0, definition.rewardCopecs);
                    var metric = ResolveMetricValue(save, definition.objectiveType);
                    if (!existing.completed)
                    {
                        changed |= SetEntryProgress(existing, metric);
                        if (TryCompleteEntry(save, existing))
                        {
                            changed = true;
                        }
                    }

                    continue;
                }

                save.objectiveProgress.entries.Add(new ObjectiveProgressEntry
                {
                    id = definition.id,
                    description = definition.description ?? definition.id,
                    currentCount = 0,
                    targetCount = Mathf.Max(1, definition.targetCount),
                    rewardCopecs = Mathf.Max(0, definition.rewardCopecs),
                    completed = false
                });
                changed = true;
            }

            save.objectiveProgress.completedObjectives = CountCompleted(save.objectiveProgress.entries);
            if (changed && saveIfChanged)
            {
                _saveManager.Save();
            }
        }

        private static bool SetEntryProgress(ObjectiveProgressEntry entry, int targetProgress)
        {
            if (entry == null)
            {
                return false;
            }

            var nextValue = Mathf.Clamp(targetProgress, 0, Mathf.Max(1, entry.targetCount));
            if (entry.currentCount == nextValue)
            {
                return false;
            }

            entry.currentCount = nextValue;
            return true;
        }

        private static bool TryCompleteEntry(SaveDataV1 save, ObjectiveProgressEntry entry)
        {
            if (save == null || entry == null || entry.completed || entry.currentCount < Mathf.Max(1, entry.targetCount))
            {
                return false;
            }

            entry.completed = true;
            save.copecs += Mathf.Max(0, entry.rewardCopecs);
            return true;
        }

        private static int ResolveMetricValue(SaveDataV1 save, ObjectiveType objectiveType)
        {
            if (save == null || save.stats == null)
            {
                return 0;
            }

            switch (objectiveType)
            {
                case ObjectiveType.CatchCount:
                    return Mathf.Max(0, save.stats.totalFishCaught);
                case ObjectiveType.CompleteTrips:
                    return Mathf.Max(0, save.stats.totalTrips);
                case ObjectiveType.CatchValueCopecs:
                    return Mathf.Max(0, save.stats.totalCatchValueCopecs);
                default:
                    return 0;
            }
        }

        private static int CountCompleted(List<ObjectiveProgressEntry> entries)
        {
            if (entries == null)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].completed)
                {
                    count++;
                }
            }

            return count;
        }

        private static ObjectiveProgressEntry FindEntryById(List<ObjectiveProgressEntry> entries, string id)
        {
            if (entries == null || string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.id))
                {
                    continue;
                }

                if (string.Equals(entry.id, id, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        private static ObjectiveProgressEntry Clone(ObjectiveProgressEntry source)
        {
            if (source == null)
            {
                return null;
            }

            return new ObjectiveProgressEntry
            {
                id = source.id,
                description = source.description,
                currentCount = source.currentCount,
                targetCount = source.targetCount,
                rewardCopecs = source.rewardCopecs,
                completed = source.completed
            };
        }

        private void RaiseObjectiveUpdated(ObjectiveProgressEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            try
            {
                ObjectiveUpdated?.Invoke(entry);
            }
            catch (Exception ex)
            {
                Debug.LogError($"ObjectivesService: ObjectiveUpdated listener failed ({ex.Message}).");
            }
        }

        private void RaiseObjectiveCompleted(ObjectiveProgressEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            try
            {
                ObjectiveCompleted?.Invoke(entry);
            }
            catch (Exception ex)
            {
                Debug.LogError($"ObjectivesService: ObjectiveCompleted listener failed ({ex.Message}).");
            }
        }
    }
}
