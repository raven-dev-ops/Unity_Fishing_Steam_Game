using System;
using System.IO;
using UnityEngine;

namespace RavenDevOps.Fishing.Save
{
    public interface ISaveFileSystem
    {
        string PersistentDataPath { get; }
        bool FileExists(string path);
        string ReadAllText(string path);
        void WriteAllText(string path, string content);
        void DeleteFile(string path);
        void MoveFile(string sourcePath, string destinationPath);
        void CopyFile(string sourcePath, string destinationPath, bool overwrite);
        void ReplaceFile(string sourcePath, string destinationPath, string backupPath);
        void EnsureDirectory(string path);
    }

    public interface ITimeProvider
    {
        DateTime LocalNow { get; }
        DateTime UtcNow { get; }
        float RealtimeSinceStartup { get; }
    }

    public sealed class SaveFileSystem : ISaveFileSystem
    {
        public string PersistentDataPath => Application.persistentDataPath;

        public bool FileExists(string path) => File.Exists(path);

        public string ReadAllText(string path) => File.ReadAllText(path);

        public void WriteAllText(string path, string content) => File.WriteAllText(path, content);

        public void DeleteFile(string path) => File.Delete(path);

        public void MoveFile(string sourcePath, string destinationPath) => File.Move(sourcePath, destinationPath);

        public void CopyFile(string sourcePath, string destinationPath, bool overwrite) => File.Copy(sourcePath, destinationPath, overwrite);

        public void ReplaceFile(string sourcePath, string destinationPath, string backupPath) =>
            File.Replace(sourcePath, destinationPath, backupPath, ignoreMetadataErrors: true);

        public void EnsureDirectory(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }

    public sealed class UnityTimeProvider : ITimeProvider
    {
        public DateTime LocalNow => DateTime.Now;
        public DateTime UtcNow => DateTime.UtcNow;
        public float RealtimeSinceStartup => Time.realtimeSinceStartup;
    }
}
