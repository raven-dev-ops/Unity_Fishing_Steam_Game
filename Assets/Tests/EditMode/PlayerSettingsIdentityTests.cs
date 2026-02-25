#if UNITY_EDITOR
using NUnit.Framework;
using RavenDevOps.Fishing.Save;
using UnityEditor;

namespace RavenDevOps.Fishing.Tests.EditMode
{
    public sealed class PlayerSettingsIdentityTests
    {
        [Test]
        public void CompanyAndProductNames_AreFinalizedForRelease()
        {
            Assert.That(PlayerSettings.companyName, Is.EqualTo("Raven DevOps"));
            Assert.That(PlayerSettings.productName, Is.EqualTo("Raven DevOps Fishing"));
            Assert.That(PlayerSettings.companyName, Is.Not.EqualTo("DefaultCompany"));
            Assert.That(PlayerSettings.productName, Is.Not.EqualTo("Unity_Fishing_Steam_Game"));
        }

        [Test]
        public void SaveFileSystemPersistentDataPath_ContainsFinalizedIdentifiers()
        {
            var fileSystem = new SaveFileSystem();
            var persistentDataPath = fileSystem.PersistentDataPath.Replace('\\', '/');

            Assert.That(persistentDataPath, Does.Contain(PlayerSettings.companyName));
            Assert.That(persistentDataPath, Does.Contain(PlayerSettings.productName));
            Assert.That(persistentDataPath, Does.Not.Contain("DefaultCompany"));
            Assert.That(persistentDataPath, Does.Not.Contain("Unity_Fishing_Steam_Game"));
        }
    }
}
#endif
