using System;
using System.Linq;
using NUnit.Framework;

namespace SelStrom.Asteroids.Tests.EditMode.ShtlMvvm
{
    /// <summary>
    /// Тесты совместимости shtl-mvvm с TextMeshPro.
    /// Проверяют доступность TMP-типов и binding-методов после замены
    /// зависимости com.unity.textmeshpro на com.unity.ugui.
    /// </summary>
    [TestFixture]
    public class TmpCompatibilityTests
    {
        [Test]
        public void TmpText_Type_IsAccessible()
        {
            // TMP_Text должен быть доступен через assembly Unity.TextMeshPro
            // В Unity 2022.3 -- реальная assembly, в Unity 6 -- через forwarding
            var tmpTextType = Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
            Assert.That(tmpTextType, Is.Not.Null,
                "TMP_Text должен быть доступен через assembly Unity.TextMeshPro");
        }

        [Test]
        public void TextMeshProUGUI_Type_IsAccessible()
        {
            // TextMeshProUGUI -- основной компонент для UI текста
            var tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            Assert.That(tmpType, Is.Not.Null,
                "TextMeshProUGUI должен быть доступен через assembly Unity.TextMeshPro");
        }

        [Test]
        public void ShtlMvvm_ViewModelToUIBindings_TmpMethodsExist()
        {
            // ViewModelToUIEventBindingsExtensions должен содержать методы To()
            // с параметром TMP_Text -- они используются для binding текстовых полей
            var extensionsType = typeof(global::Shtl.Mvvm.ViewModelToUIEventBindingsExtensions);
            var methods = extensionsType.GetMethods();
            var toMethods = methods.Where(m => m.Name == "To").ToArray();
            Assert.That(toMethods.Length, Is.GreaterThanOrEqualTo(4),
                "ViewModelToUIEventBindingsExtensions должен содержать минимум 4 метода To() " +
                "(string, int, long, Color для TMP_Text)");
        }

        [Test]
        public void ShtlMvvm_ViewModelToUIBindings_StringToTmpMethod_HasCorrectSignature()
        {
            // Проверяем конкретную сигнатуру: To(BindFrom<ReactiveValue<string>>, TMP_Text)
            var extensionsType = typeof(global::Shtl.Mvvm.ViewModelToUIEventBindingsExtensions);
            var tmpTextType = Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
            Assert.That(tmpTextType, Is.Not.Null, "TMP_Text тип должен быть доступен");

            var toMethods = extensionsType.GetMethods()
                .Where(m => m.Name == "To")
                .Where(m =>
                {
                    var parameters = m.GetParameters();
                    return parameters.Length == 2 && parameters[1].ParameterType == tmpTextType;
                })
                .ToArray();

            Assert.That(toMethods.Length, Is.GreaterThanOrEqualTo(1),
                "Должен существовать метод To() с параметром TMP_Text");
        }
    }
}
