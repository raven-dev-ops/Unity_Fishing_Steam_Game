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

        private static SaveManager _instance;

        [SerializeField] private SaveDataV1 _current = new SaveDataV1();

        public static SaveManager Instance => _instance;
        public SaveDataV1 Current => _current;

        private string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
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
            }
            catch (Exception ex)
            {
                Debug.LogError($"SaveManager: failed to save profile atomically ({ex.Message}).");
            }
        }

        public void AddCopecs(int value)
        {
            _current.copecs += Mathf.Max(0, value);
            Save();
        }

        public void MarkTripCompleted()
        {
            _current.stats.totalTrips += 1;
            Save();
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
    }
}
