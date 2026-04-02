using System;
using NUnit.Framework;

namespace SelStrom.Asteroids.Tests.EditMode
{
    /// <summary>
    /// Тесты интеграции TextMeshPro в Unity 6.3.
    /// Проверяют доступность ключевых TMP-типов через reflection,
    /// гарантируя корректную работу assembly forwarding.
    /// </summary>
    [TestFixture]
    public class TmpIntegrationTests
    {
        /// <summary>
        /// TMP_InputField доступен через assembly Unity.TextMeshPro.
        /// Используется в ScoreVisual.cs для ввода имени игрока.
        /// </summary>
        [Test]
        public void TmpInputFieldTypeExists()
        {
            var type = Type.GetType("TMPro.TMP_InputField, Unity.TextMeshPro");
            Assert.IsNotNull(type,
                "TMP_InputField должен быть доступен через Unity.TextMeshPro assembly");
        }

        /// <summary>
        /// TextMeshProUGUI доступен через assembly Unity.TextMeshPro.
        /// Используется в LeaderboardPrefabCreator.cs и других UI-компонентах.
        /// </summary>
        [Test]
        public void TextMeshProUguiTypeExists()
        {
            var type = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            Assert.IsNotNull(type,
                "TextMeshProUGUI должен быть доступен через Unity.TextMeshPro assembly");
        }

        /// <summary>
        /// TMP_FontAsset доступен через assembly Unity.TextMeshPro.
        /// Необходим для корректного рендеринга шрифтов.
        /// </summary>
        [Test]
        public void TmpFontAssetTypeExists()
        {
            var type = Type.GetType("TMPro.TMP_FontAsset, Unity.TextMeshPro");
            Assert.IsNotNull(type,
                "TMP_FontAsset должен быть доступен для рендеринга шрифтов");
        }
    }
}
