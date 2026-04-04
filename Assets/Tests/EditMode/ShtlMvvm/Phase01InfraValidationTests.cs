using System.IO;
using NUnit.Framework;

namespace SelStrom.Asteroids.Tests.EditMode.ShtlMvvm
{
    /// <summary>
    /// Инфраструктурные тесты Phase 01: проверка конфигурации проекта.
    /// Покрывают требования TOOL-01, TOOL-02, MVVM-06 через автоматические
    /// проверки файлов конфигурации.
    /// </summary>
    [TestFixture]
    public class Phase01InfraValidationTests
    {
        private string _projectRoot;

        [SetUp]
        public void SetUp()
        {
            // Application.dataPath = ".../Assets", поднимаемся на один уровень
            _projectRoot = Path.GetDirectoryName(UnityEngine.Application.dataPath);
        }

        /// <summary>
        /// TOOL-01: Unity-MCP пакет установлен в manifest.json.
        /// </summary>
        [Test]
        public void ManifestJson_ContainsUnityMcpPackage()
        {
            var manifestPath = Path.Combine(_projectRoot, "Packages", "manifest.json");
            Assert.That(File.Exists(manifestPath), Is.True,
                "Packages/manifest.json должен существовать");

            var content = File.ReadAllText(manifestPath);
            Assert.That(content, Does.Contain("com.ivanmurzak.unity.mcp"),
                "manifest.json должен содержать пакет Unity-MCP (TOOL-01)");
        }

        /// <summary>
        /// TOOL-02: EditMode test assembly definition существует и настроена.
        /// </summary>
        [Test]
        public void EditModeTestAssembly_ExistsAndConfigured()
        {
            var asmdefPath = Path.Combine(_projectRoot, "Assets", "Tests", "EditMode", "EditModeTests.asmdef");
            Assert.That(File.Exists(asmdefPath), Is.True,
                "Assets/Tests/EditMode/EditModeTests.asmdef должен существовать (TOOL-02)");

            var content = File.ReadAllText(asmdefPath);
            Assert.That(content, Does.Contain("\"name\": \"EditModeTests\""),
                "EditModeTests.asmdef должен иметь имя EditModeTests");
            Assert.That(content, Does.Contain("nunit.framework.dll"),
                "EditModeTests.asmdef должен ссылаться на NUnit");
            Assert.That(content, Does.Contain("UNITY_INCLUDE_TESTS"),
                "EditModeTests.asmdef должен иметь defineConstraint UNITY_INCLUDE_TESTS");
        }

        /// <summary>
        /// TOOL-02: PlayMode test assembly definition существует и настроена.
        /// </summary>
        [Test]
        public void PlayModeTestAssembly_ExistsAndConfigured()
        {
            var asmdefPath = Path.Combine(_projectRoot, "Assets", "Tests", "PlayMode", "PlayModeTests.asmdef");
            Assert.That(File.Exists(asmdefPath), Is.True,
                "Assets/Tests/PlayMode/PlayModeTests.asmdef должен существовать (TOOL-02)");

            var content = File.ReadAllText(asmdefPath);
            Assert.That(content, Does.Contain("\"name\": \"PlayModeTests\""),
                "PlayModeTests.asmdef должен иметь имя PlayModeTests");
            Assert.That(content, Does.Contain("nunit.framework.dll"),
                "PlayModeTests.asmdef должен ссылаться на NUnit");
        }

        /// <summary>
        /// MVVM-06: manifest.json ссылается на shtl-mvvm v1.1.0.
        /// </summary>
        [Test]
        public void ManifestJson_ReferencesShtlMvvmV110()
        {
            var manifestPath = Path.Combine(_projectRoot, "Packages", "manifest.json");
            Assert.That(File.Exists(manifestPath), Is.True,
                "Packages/manifest.json должен существовать");

            var content = File.ReadAllText(manifestPath);
            Assert.That(content, Does.Contain("shtl-mvvm.git#v1.1.0"),
                "manifest.json должен ссылаться на shtl-mvvm v1.1.0 (MVVM-06)");
        }
    }
}
