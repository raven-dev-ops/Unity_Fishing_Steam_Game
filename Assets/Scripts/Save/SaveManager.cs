using System;
using System;
using System.IO;
using UnityEngine;

namespace RavenDevOps.Fishing.Save
{
    public sealed class SaveManager : MonoBehaviour
    {
        private const string FileName = "save_v1.json";

        [SerializeField] private SaveDataV1 _current = new SaveDataV1();

        public SaveDataV1 Current => _current;

        private string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public void LoadOrCreate()
        {
            if (!File.Exists(SavePath))
            {
                _current = new SaveDataV1();
                Save();
                return;
            }

            var json = File.ReadAllText(SavePath);
            _current = JsonUtility.FromJson<SaveDataV1>(json) ?? new SaveDataV1();
            _current.lastLoginLocalDate = DateTime.Now.ToString("yyyy-MM-dd");
        }

        public void Save()
        {
            var json = JsonUtility.ToJson(_current, true);
            File.WriteAllText(SavePath, json);
        }

        public void AddCopecs(int value)
        {
            _current.copecs += Mathf.Max(0, value);
            Save();
        }
    }
}
