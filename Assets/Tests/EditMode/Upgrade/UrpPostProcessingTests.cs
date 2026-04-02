using NUnit.Framework;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SelStrom.Asteroids.Tests.EditMode
{
    /// <summary>
    /// Тесты Post-Processing: Volume Profile существует и содержит Bloom и Vignette.
    /// Покрывает требование URP-04.
    /// </summary>
    [TestFixture]
    public class UrpPostProcessingTests
    {
        /// <summary>
        /// Volume Profile должен существовать в Assets/Settings/PostProcessing-Profile.asset.
        /// </summary>
        [Test]
        public void VolumeProfileExists()
        {
            var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                "Assets/Settings/PostProcessing-Profile.asset");
            Assert.IsNotNull(profile,
                "Volume Profile должен существовать в Assets/Settings/PostProcessing-Profile.asset");
        }

        /// <summary>
        /// Volume Profile должен содержать Bloom override.
        /// </summary>
        [Test]
        public void VolumeProfileHasBloom()
        {
            var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                "Assets/Settings/PostProcessing-Profile.asset");
            Assert.IsNotNull(profile);
            Assert.IsTrue(profile.Has<Bloom>(),
                "Volume Profile должен содержать Bloom override");
        }

        /// <summary>
        /// Volume Profile должен содержать Vignette override.
        /// </summary>
        [Test]
        public void VolumeProfileHasVignette()
        {
            var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                "Assets/Settings/PostProcessing-Profile.asset");
            Assert.IsNotNull(profile);
            Assert.IsTrue(profile.Has<Vignette>(),
                "Volume Profile должен содержать Vignette override");
        }
    }
}
