using System.IO;
using System.Linq;
using NUnit.Framework;

namespace SelStrom.Asteroids.Tests.EditMode
{
    /// <summary>
    /// Тесты валидации апгрейда: проверяют отсутствие deprecated API
    /// и корректность ссылок на TMP в asmdef файлах.
    /// </summary>
    [TestFixture]
    public class UpgradeValidationTests
    {
        private static readonly string ScriptsPath = Path.Combine("Assets", "Scripts");

        private string[] GetAllCsFiles()
        {
            if (!Directory.Exists(ScriptsPath))
            {
                return new string[0];
            }

            return Directory.GetFiles(ScriptsPath, "*.cs", SearchOption.AllDirectories);
        }

        /// <summary>
        /// Ни один .cs файл в Assets/Scripts/ не содержит deprecated FindObjectsOfType/FindObjectOfType.
        /// В Unity 6.3 следует использовать FindObjectsByType/FindFirstObjectByType.
        /// </summary>
        [Test]
        public void NoDeprecatedFindObjectsOfType()
        {
            var csFiles = GetAllCsFiles();
            Assert.That(csFiles.Length, Is.GreaterThan(0),
                "Должны существовать .cs файлы в Assets/Scripts/");

            var filesWithDeprecated = csFiles
                .Where(file =>
                {
                    var content = File.ReadAllText(file);
                    return content.Contains("FindObjectsOfType") || content.Contains("FindObjectOfType");
                })
                .Select(Path.GetFileName)
                .ToArray();

            Assert.That(filesWithDeprecated, Is.Empty,
                "Файлы содержат deprecated FindObjectsOfType/FindObjectOfType: " +
                string.Join(", ", filesWithDeprecated));
        }

        /// <summary>
        /// Ни один .cs файл в Assets/Scripts/ не использует deprecated SendMessage/BroadcastMessage.
        /// </summary>
        [Test]
        public void NoDeprecatedSendMessage()
        {
            var csFiles = GetAllCsFiles();
            Assert.That(csFiles.Length, Is.GreaterThan(0),
                "Должны существовать .cs файлы в Assets/Scripts/");

            var filesWithDeprecated = csFiles
                .Where(file =>
                {
                    var content = File.ReadAllText(file);
                    return content.Contains("SendMessage(") || content.Contains("BroadcastMessage(");
                })
                .Select(Path.GetFileName)
                .ToArray();

            Assert.That(filesWithDeprecated, Is.Empty,
                "Файлы содержат deprecated SendMessage/BroadcastMessage: " +
                string.Join(", ", filesWithDeprecated));
        }

        /// <summary>
        /// Все asmdef файлы в Assets/ не содержат строковых ссылок "Unity.TextMeshPro".
        /// После миграции на Unity 6.3 TMP встроен в движок, ссылки должны быть через GUID.
        /// </summary>
        [Test]
        public void NoStringTmpReferencesInAsmdef()
        {
            var asmdefFiles = Directory.GetFiles("Assets", "*.asmdef", SearchOption.AllDirectories);
            Assert.That(asmdefFiles.Length, Is.GreaterThan(0),
                "Должны существовать .asmdef файлы в Assets/");

            var filesWithStringRef = asmdefFiles
                .Where(file =>
                {
                    var content = File.ReadAllText(file);
                    return content.Contains("\"Unity.TextMeshPro\"");
                })
                .Select(Path.GetFileName)
                .ToArray();

            Assert.That(filesWithStringRef, Is.Empty,
                "Asmdef файлы содержат строковую ссылку на Unity.TextMeshPro (должна быть GUID): " +
                string.Join(", ", filesWithStringRef));
        }
    }
}
