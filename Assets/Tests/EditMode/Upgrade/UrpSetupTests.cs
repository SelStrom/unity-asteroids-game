using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SelStrom.Asteroids.Tests.EditMode
{
    /// <summary>
    /// Тесты настройки URP: Pipeline Asset существует и назначен в Graphics/Quality Settings.
    /// Покрывает требования URP-01, URP-02.
    /// </summary>
    [TestFixture]
    public class UrpSetupTests
    {
        /// <summary>
        /// URP Pipeline Asset должен существовать в Assets/Settings/URP-2D-Asset.asset.
        /// </summary>
        [Test]
        public void UrpPipelineAssetExists()
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                "Assets/Settings/URP-2D-Asset.asset");
            Assert.IsNotNull(asset,
                "URP Pipeline Asset должен существовать в Assets/Settings/URP-2D-Asset.asset");
        }

        /// <summary>
        /// 2D Renderer Data Asset должен существовать в Assets/Settings/URP-2D-Renderer.asset.
        /// </summary>
        [Test]
        public void RendererDataAssetExists()
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(
                "Assets/Settings/URP-2D-Renderer.asset");
            Assert.IsNotNull(asset,
                "2D Renderer Data должен существовать в Assets/Settings/URP-2D-Renderer.asset");
        }

        /// <summary>
        /// GraphicsSettings.currentRenderPipeline должен быть UniversalRenderPipelineAsset.
        /// </summary>
        [Test]
        public void GraphicsSettingsUsesUrp()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            Assert.IsNotNull(pipeline,
                "GraphicsSettings.currentRenderPipeline должен быть назначен (не null)");
            Assert.IsInstanceOf<UniversalRenderPipelineAsset>(pipeline,
                "currentRenderPipeline должен быть UniversalRenderPipelineAsset");
        }

        /// <summary>
        /// Все Quality Level должны иметь назначенный URP Asset.
        /// </summary>
        [Test]
        public void QualitySettingsAllLevelsHaveUrp()
        {
            var qualityLevelCount = QualitySettings.names.Length;
            var currentLevel = QualitySettings.GetQualityLevel();

            for (int i = 0; i < qualityLevelCount; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                var pipeline = QualitySettings.renderPipeline;
                Assert.IsNotNull(pipeline,
                    $"Quality Level {i} ({QualitySettings.names[i]}) должен иметь назначенный URP Asset");
            }

            QualitySettings.SetQualityLevel(currentLevel, false);
        }
    }
}
