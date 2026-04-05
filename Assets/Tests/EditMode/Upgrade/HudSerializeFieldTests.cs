using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace SelStrom.Asteroids.Tests.EditMode
{
    /// <summary>
    /// Регрессионный тест: все SerializeField в HudVisual должны быть привязаны
    /// к реальным объектам сцены (fileID != 0). Защищает от ситуации, когда
    /// ручное YAML-редактирование сцены оставляет ссылки неназначенными.
    /// </summary>
    [TestFixture]
    public class HudSerializeFieldTests
    {
        private static readonly string ScenePath = Path.Combine("Assets", "Scenes", "Main.unity");

        /// <summary>
        /// Все SerializeField HudVisual (_coordinates, _rotationAngle, _speed,
        /// _laserShootCount, _laserReloadTime, _rocketAmmoCount, _rocketReloadTime)
        /// должны ссылаться на существующие объекты (fileID != 0).
        /// </summary>
        [Test]
        public void HudVisual_AllSerializeFields_AreAssigned()
        {
            Assert.That(File.Exists(ScenePath), Is.True,
                $"Сцена не найдена: {ScenePath}");

            var sceneYaml = File.ReadAllText(ScenePath);

            // Ищем блок HudVisual по GUID скрипта (a547a8ff245d4c19bc62026c9b0f61f0)
            var hudVisualPattern = new Regex(
                @"m_Script:\s*\{fileID:\s*11500000,\s*guid:\s*a547a8ff245d4c19bc62026c9b0f61f0",
                RegexOptions.Multiline);

            Assert.That(hudVisualPattern.IsMatch(sceneYaml), Is.True,
                "HudVisual компонент не найден в сцене Main.unity");

            var requiredFields = new[]
            {
                "_coordinates",
                "_rotationAngle",
                "_speed",
                "_laserShootCount",
                "_laserReloadTime",
                "_rocketAmmoCount",
                "_rocketReloadTime"
            };

            foreach (var field in requiredFields)
            {
                // Паттерн: _fieldName: {fileID: NNNN} где NNNN != 0
                var fieldPattern = new Regex(
                    field + @":\s*\{fileID:\s*(\d+)\}",
                    RegexOptions.Multiline);

                var match = fieldPattern.Match(sceneYaml);
                Assert.That(match.Success, Is.True,
                    $"Поле {field} не найдено в HudVisual компоненте сцены");

                var fileId = match.Groups[1].Value;
                Assert.That(fileId, Is.Not.EqualTo("0"),
                    $"Поле {field} в HudVisual не привязано (fileID: 0). " +
                    "Назначьте TMP_Text объект в Unity Inspector.");
            }
        }

        /// <summary>
        /// Все fileID, на которые ссылаются SerializeField HudVisual,
        /// должны существовать как объекты в сцене.
        /// </summary>
        [Test]
        public void HudVisual_AllSerializeFieldReferences_ExistInScene()
        {
            Assert.That(File.Exists(ScenePath), Is.True,
                $"Сцена не найдена: {ScenePath}");

            var sceneYaml = File.ReadAllText(ScenePath);

            var fields = new[]
            {
                "_coordinates",
                "_rotationAngle",
                "_speed",
                "_laserShootCount",
                "_laserReloadTime",
                "_rocketAmmoCount",
                "_rocketReloadTime"
            };

            foreach (var field in fields)
            {
                var fieldPattern = new Regex(
                    field + @":\s*\{fileID:\s*(\d+)\}",
                    RegexOptions.Multiline);

                var match = fieldPattern.Match(sceneYaml);
                if (!match.Success || match.Groups[1].Value == "0")
                {
                    continue; // Проверяется в другом тесте
                }

                var fileId = match.Groups[1].Value;

                // Проверяем, что объект с этим fileID существует в сцене
                var objectPattern = new Regex(@"--- !u!\d+ &" + fileId);
                Assert.That(objectPattern.IsMatch(sceneYaml), Is.True,
                    $"Поле {field} ссылается на fileID {fileId}, " +
                    "но объект с таким ID не найден в сцене Main.unity. " +
                    "Пересохраните сцену в Unity Editor.");
            }
        }
    }
}
