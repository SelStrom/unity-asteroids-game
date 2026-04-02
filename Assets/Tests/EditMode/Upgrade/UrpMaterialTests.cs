using NUnit.Framework;
using UnityEngine;

namespace SelStrom.Asteroids.Tests.EditMode
{
    /// <summary>
    /// Тесты материалов URP: материалы используют URP шейдеры, prefabs ссылаются на URP материалы.
    /// Покрывает требование URP-03.
    /// </summary>
    [TestFixture]
    public class UrpMaterialTests
    {
        /// <summary>
        /// Laser-URP.mat должен существовать и использовать URP шейдер.
        /// </summary>
        [Test]
        public void LaserMaterialExistsAndUsesUrpShader()
        {
            var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Media/materials/Laser-URP.mat");
            Assert.IsNotNull(mat, "Laser-URP.mat должен существовать");
            Assert.IsTrue(mat.shader.name.Contains("Universal"),
                $"Laser материал должен использовать URP шейдер, текущий: {mat.shader.name}");
        }

        /// <summary>
        /// Particle-URP.mat должен существовать и использовать URP Particles шейдер.
        /// </summary>
        [Test]
        public void ParticleMaterialExistsAndUsesUrpShader()
        {
            var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Media/materials/Particle-URP.mat");
            Assert.IsNotNull(mat, "Particle-URP.mat должен существовать");
            Assert.IsTrue(mat.shader.name.Contains("Particles"),
                $"Particle материал должен использовать URP Particles шейдер, текущий: {mat.shader.name}");
        }

        /// <summary>
        /// lazer.prefab LineRenderer должен использовать URP материал.
        /// </summary>
        [Test]
        public void LazerPrefabUsesUrpMaterial()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Media/effects/lazer.prefab");
            Assert.IsNotNull(prefab, "lazer.prefab должен существовать");
            var lineRenderer = prefab.GetComponent<LineRenderer>();
            Assert.IsNotNull(lineRenderer, "lazer.prefab должен иметь LineRenderer");
            Assert.IsNotNull(lineRenderer.sharedMaterial, "LineRenderer должен иметь материал");
            Assert.IsTrue(lineRenderer.sharedMaterial.shader.name.Contains("Universal"),
                $"LineRenderer должен использовать URP шейдер, текущий: {lineRenderer.sharedMaterial.shader.name}");
        }

        /// <summary>
        /// vfx_blow.prefab ParticleSystemRenderer должен использовать URP материал.
        /// </summary>
        [Test]
        public void VfxBlowPrefabUsesUrpMaterial()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Media/effects/vfx_blow.prefab");
            Assert.IsNotNull(prefab, "vfx_blow.prefab должен существовать");
            var particleRenderer = prefab.GetComponent<ParticleSystemRenderer>();
            Assert.IsNotNull(particleRenderer, "vfx_blow.prefab должен иметь ParticleSystemRenderer");
            Assert.IsNotNull(particleRenderer.sharedMaterial,
                "ParticleSystemRenderer должен иметь материал");
            Assert.IsTrue(particleRenderer.sharedMaterial.shader.name.Contains("Particles"),
                $"ParticleSystem должен использовать URP Particles шейдер, текущий: {particleRenderer.sharedMaterial.shader.name}");
        }

        /// <summary>
        /// vfx_blow.prefab ParticleSystem stopAction должен быть Callback (для возврата в пул).
        /// </summary>
        [Test]
        public void VfxBlowPrefabStopActionIsCallback()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Media/effects/vfx_blow.prefab");
            var ps = prefab.GetComponent<ParticleSystem>();
            Assert.IsNotNull(ps, "vfx_blow.prefab должен иметь ParticleSystem");
            var main = ps.main;
            Assert.AreEqual(ParticleSystemStopAction.Callback, main.stopAction,
                "ParticleSystem stopAction должен быть Callback (для возврата в пул)");
        }
    }
}
