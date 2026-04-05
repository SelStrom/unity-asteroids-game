using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace SelStrom.Asteroids.Tests.EditMode
{
    /// <summary>
    /// Регрессионный тест: все SerializeField в RocketVisual должны быть привязаны
    /// в prefab (fileID != 0). Защищает от ситуации, когда дочерний ParticleSystem
    /// существует на prefab, но не назначен в _trailEffect.
    /// </summary>
    [TestFixture]
    public class RocketPrefabSerializeFieldTests
    {
        private static readonly string PrefabPath =
            Path.Combine("Assets", "Media", "prefabs", "rocket.prefab");

        /// <summary>
        /// RocketVisual GUID скрипта: e629bb4e10eaf4f0db91271f84a82a8f.
        /// Поля _collider и _trailEffect должны ссылаться на существующие
        /// компоненты (fileID != 0).
        /// </summary>
        [Test]
        public void RocketVisual_AllSerializeFields_AreAssigned()
        {
            Assert.That(File.Exists(PrefabPath), Is.True,
                $"Prefab не найден: {PrefabPath}");

            var prefabYaml = File.ReadAllText(PrefabPath);

            // Ищем блок RocketVisual по GUID скрипта
            var rocketVisualPattern = new Regex(
                @"m_Script:\s*\{fileID:\s*11500000,\s*guid:\s*e629bb4e10eaf4f0db91271f84a82a8f",
                RegexOptions.Multiline);

            Assert.That(rocketVisualPattern.IsMatch(prefabYaml), Is.True,
                "RocketVisual компонент не найден в rocket.prefab");

            var requiredFields = new[]
            {
                "_collider",
                "_trailEffect"
            };

            foreach (var field in requiredFields)
            {
                // Паттерн: _fieldName: {fileID: NNNN} где NNNN != 0
                var fieldPattern = new Regex(
                    field + @":\s*\{fileID:\s*(\d+)",
                    RegexOptions.Multiline);

                var match = fieldPattern.Match(prefabYaml);
                Assert.That(match.Success, Is.True,
                    $"Поле {field} не найдено в RocketVisual компоненте prefab");

                var fileId = match.Groups[1].Value;
                Assert.That(fileId, Is.Not.EqualTo("0"),
                    $"Поле {field} в RocketVisual не привязано (fileID: 0). " +
                    "Назначьте компонент в Unity Inspector.");
            }
        }

        /// <summary>
        /// Все fileID, на которые ссылаются SerializeField RocketVisual,
        /// должны существовать как объекты в prefab.
        /// </summary>
        [Test]
        public void RocketVisual_AllSerializeFieldReferences_ExistInPrefab()
        {
            Assert.That(File.Exists(PrefabPath), Is.True,
                $"Prefab не найден: {PrefabPath}");

            var prefabYaml = File.ReadAllText(PrefabPath);

            var fields = new[]
            {
                "_collider",
                "_trailEffect"
            };

            foreach (var field in fields)
            {
                var fieldPattern = new Regex(
                    field + @":\s*\{fileID:\s*(\d+)",
                    RegexOptions.Multiline);

                var match = fieldPattern.Match(prefabYaml);
                if (!match.Success || match.Groups[1].Value == "0")
                {
                    continue; // Проверяется в другом тесте
                }

                var fileId = match.Groups[1].Value;

                // Проверяем, что объект с этим fileID существует в prefab
                var objectPattern = new Regex(@"--- !u!\d+ &" + fileId);
                Assert.That(objectPattern.IsMatch(prefabYaml), Is.True,
                    $"Поле {field} ссылается на fileID {fileId}, " +
                    "но объект с таким ID не найден в rocket.prefab.");
            }
        }
    }
}
