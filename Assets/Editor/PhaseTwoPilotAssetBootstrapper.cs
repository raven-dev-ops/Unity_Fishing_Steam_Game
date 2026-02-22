#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RavenDevOps.Fishing.EditorTools
{
    public static class PhaseTwoPilotAssetBootstrapper
    {
        private const string PilotRootAssetPath = "Assets/Resources/Pilot";
        private const string AudioFolderAssetPath = PilotRootAssetPath + "/Audio";
        private const string EnvironmentFolderAssetPath = PilotRootAssetPath + "/Environment";
        private const string EnvironmentMaterialsAssetPath = EnvironmentFolderAssetPath + "/Materials";
        private const string SkyboxMaterialName = "fishing_skybox";
        private const string SkyboxMaterialAssetPath = EnvironmentMaterialsAssetPath + "/" + SkyboxMaterialName + ".mat";
        private const string AudioResourcesPath = "Pilot/Audio";
        private const string SkyboxResourcesPath = "Pilot/Environment/Materials/fishing_skybox";
        private const int SampleRateHz = 44100;

        private readonly struct PhaseTwoAudioSeed
        {
            public PhaseTwoAudioSeed(string key, float frequencyHz, float durationSeconds, float amplitude)
            {
                Key = key;
                FrequencyHz = frequencyHz;
                DurationSeconds = durationSeconds;
                Amplitude = amplitude;
            }

            public string Key { get; }
            public float FrequencyHz { get; }
            public float DurationSeconds { get; }
            public float Amplitude { get; }
        }

        private static readonly PhaseTwoAudioSeed[] AudioSeeds =
        {
            new PhaseTwoAudioSeed("menu_music_loop", 196.0f, 2.8f, 0.08f),
            new PhaseTwoAudioSeed("harbor_music_loop", 220.0f, 2.8f, 0.08f),
            new PhaseTwoAudioSeed("fishing_music_loop", 174.61f, 2.8f, 0.08f),
            new PhaseTwoAudioSeed("sfx_ui_navigate", 523.25f, 0.12f, 0.16f),
            new PhaseTwoAudioSeed("sfx_ui_select", 659.25f, 0.14f, 0.16f),
            new PhaseTwoAudioSeed("sfx_ui_cancel", 392.0f, 0.14f, 0.16f),
            new PhaseTwoAudioSeed("sfx_cast", 293.66f, 0.20f, 0.18f),
            new PhaseTwoAudioSeed("sfx_hooked", 261.63f, 0.24f, 0.18f),
            new PhaseTwoAudioSeed("sfx_catch", 329.63f, 0.22f, 0.18f),
            new PhaseTwoAudioSeed("sfx_sell", 440.0f, 0.16f, 0.16f),
            new PhaseTwoAudioSeed("sfx_purchase", 587.33f, 0.18f, 0.16f),
            new PhaseTwoAudioSeed("sfx_depart", 246.94f, 0.24f, 0.16f),
            new PhaseTwoAudioSeed("sfx_return", 349.23f, 0.22f, 0.16f)
        };

        [MenuItem("Raven/Bootstrap/Generate Phase-Two Pilot Assets")]
        public static void GenerateFromMenu()
        {
            if (GenerateInternal())
            {
                Debug.Log("PhaseTwoPilotAssetBootstrapper: phase-two pilot assets are ready.");
            }
        }

        public static void GenerateBatchMode()
        {
            var success = GenerateInternal();
            EditorApplication.Exit(success ? 0 : 1);
        }

        private static bool GenerateInternal()
        {
            try
            {
                EnsureFolder("Assets/Resources");
                EnsureFolder(PilotRootAssetPath);
                EnsureFolder(AudioFolderAssetPath);
                EnsureFolder(EnvironmentFolderAssetPath);
                EnsureFolder(EnvironmentMaterialsAssetPath);

                for (var i = 0; i < AudioSeeds.Length; i++)
                {
                    WriteWaveAsset(AudioSeeds[i]);
                }

                var shader = ResolveSkyboxShader();
                if (shader == null)
                {
                    throw new InvalidOperationException("Could not resolve a shader for fishing_skybox material.");
                }

                UpsertSkyboxMaterial(shader);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                var loadedAudio = Resources.LoadAll<AudioClip>(AudioResourcesPath);
                var loadedSkybox = Resources.Load<Material>(SkyboxResourcesPath);
                if (loadedSkybox == null)
                {
                    throw new InvalidOperationException("Resources load check failed for fishing_skybox.");
                }

                var loadedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < loadedAudio.Length; i++)
                {
                    if (loadedAudio[i] != null)
                    {
                        loadedNames.Add(loadedAudio[i].name);
                    }
                }

                for (var i = 0; i < AudioSeeds.Length; i++)
                {
                    if (!loadedNames.Contains(AudioSeeds[i].Key))
                    {
                        throw new InvalidOperationException($"Resources load check failed for audio key '{AudioSeeds[i].Key}'.");
                    }
                }

                Debug.Log(
                    $"PhaseTwoPilotAssetBootstrapper: generated {AudioSeeds.Length} audio clip assets under '{AudioFolderAssetPath}' and material '{SkyboxMaterialAssetPath}'.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"PhaseTwoPilotAssetBootstrapper: generation failed ({ex.Message}).");
                return false;
            }
        }

        private static void WriteWaveAsset(PhaseTwoAudioSeed seed)
        {
            var relativeAssetPath = AudioFolderAssetPath + "/" + seed.Key + ".wav";
            var absoluteFilePath = ToAbsolutePath(relativeAssetPath);
            var bytes = BuildWaveBytes(seed);

            var directory = Path.GetDirectoryName(absoluteFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(absoluteFilePath, bytes);
            AssetDatabase.ImportAsset(relativeAssetPath, ImportAssetOptions.ForceUpdate);
        }

        private static byte[] BuildWaveBytes(PhaseTwoAudioSeed seed)
        {
            var sampleCount = Mathf.Max(1, Mathf.RoundToInt(seed.DurationSeconds * SampleRateHz));
            const short channels = 1;
            const short bitsPerSample = 16;
            var bytesPerSample = bitsPerSample / 8;
            var byteRate = SampleRateHz * channels * bytesPerSample;
            var blockAlign = channels * bytesPerSample;
            var dataSize = sampleCount * blockAlign;

            using (var stream = new MemoryStream(44 + dataSize))
            using (var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataSize);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write(channels);
                writer.Write(SampleRateHz);
                writer.Write(byteRate);
                writer.Write((short)blockAlign);
                writer.Write(bitsPerSample);
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(dataSize);

                for (var i = 0; i < sampleCount; i++)
                {
                    var phase = i / (float)SampleRateHz;
                    var envelope = ComputeEnvelope(i, sampleCount);
                    var sample = Mathf.Sin(2f * Mathf.PI * seed.FrequencyHz * phase) * seed.Amplitude * envelope;
                    var packed = (short)Mathf.Clamp(sample * short.MaxValue, short.MinValue, short.MaxValue);
                    writer.Write(packed);
                }

                writer.Flush();
                return stream.ToArray();
            }
        }

        private static float ComputeEnvelope(int sampleIndex, int sampleCount)
        {
            if (sampleCount <= 1)
            {
                return 1f;
            }

            var t = sampleIndex / (float)(sampleCount - 1);
            var attack = Mathf.Clamp01(t / 0.06f);
            var release = Mathf.Clamp01((1f - t) / 0.10f);
            return Mathf.Min(attack, release);
        }

        private static void UpsertSkyboxMaterial(Shader shader)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(SkyboxMaterialAssetPath);
            if (material == null)
            {
                material = new Material(shader) { name = SkyboxMaterialName };
                AssetDatabase.CreateAsset(material, SkyboxMaterialAssetPath);
            }
            else
            {
                material.shader = shader;
                material.name = SkyboxMaterialName;
            }

            ConfigureSkyboxMaterial(material);
            EditorUtility.SetDirty(material);
            AssetDatabase.ImportAsset(SkyboxMaterialAssetPath, ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigureSkyboxMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_SkyTint"))
            {
                material.SetColor("_SkyTint", new Color(0.25f, 0.57f, 0.88f, 1f));
            }

            if (material.HasProperty("_GroundColor"))
            {
                material.SetColor("_GroundColor", new Color(0.14f, 0.20f, 0.28f, 1f));
            }

            if (material.HasProperty("_Tint"))
            {
                material.SetColor("_Tint", new Color(0.26f, 0.60f, 0.92f, 1f));
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", new Color(0.26f, 0.60f, 0.92f, 1f));
            }

            if (material.HasProperty("_Exposure"))
            {
                material.SetFloat("_Exposure", 1.05f);
            }
        }

        private static Shader ResolveSkyboxShader()
        {
            var candidates = new[]
            {
                "Skybox/Procedural",
                "Skybox/Panoramic",
                "Universal Render Pipeline/Unlit",
                "Unlit/Color",
                "Sprites/Default"
            };

            for (var i = 0; i < candidates.Length; i++)
            {
                var shader = Shader.Find(candidates[i]);
                if (shader != null)
                {
                    return shader;
                }
            }

            return null;
        }

        private static void EnsureFolder(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath))
            {
                return;
            }

            var parentPath = Path.GetDirectoryName(assetFolderPath)?.Replace("\\", "/");
            var folderName = Path.GetFileName(assetFolderPath);
            if (string.IsNullOrWhiteSpace(parentPath) || string.IsNullOrWhiteSpace(folderName))
            {
                throw new InvalidOperationException($"Invalid folder path '{assetFolderPath}'.");
            }

            EnsureFolder(parentPath);
            AssetDatabase.CreateFolder(parentPath, folderName);
        }

        private static string ToAbsolutePath(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                throw new InvalidOperationException("Unable to resolve Unity project root.");
            }

            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
#endif
