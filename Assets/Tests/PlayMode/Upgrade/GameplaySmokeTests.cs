using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SelStrom.Asteroids.Tests.PlayMode
{
    /// <summary>
    /// Smoke-тесты загрузки сцены после апгрейда на Unity 6.3.
    /// Проверяют базовую работоспособность: загрузка сцены, наличие
    /// ключевых объектов (ApplicationEntry, Camera).
    /// </summary>
    [TestFixture]
    public class GameplaySmokeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("Main");
            yield return null; // ждем 1 кадр для инициализации
            yield return null; // ещё 1 кадр для Awake/Start
        }

        /// <summary>
        /// Сцена Main загружается без ошибок.
        /// </summary>
        [UnityTest]
        public IEnumerator SceneLoadsSuccessfully()
        {
            var scene = SceneManager.GetActiveScene();
            Assert.AreEqual("Main", scene.name,
                "Сцена Main должна быть активной");
            Assert.IsTrue(scene.isLoaded,
                "Сцена должна быть загружена");
            yield return null;
        }

        /// <summary>
        /// ApplicationEntry существует в сцене Main.
        /// Это единственная точка входа игрового кода.
        /// </summary>
        [UnityTest]
        public IEnumerator ApplicationEntryExists()
        {
            var entry = Object.FindFirstObjectByType<ApplicationEntry>();
            Assert.IsNotNull(entry,
                "ApplicationEntry должен присутствовать в сцене Main");
            yield return null;
        }

        /// <summary>
        /// Main Camera существует и настроена как ортографическая (2D игра).
        /// </summary>
        [UnityTest]
        public IEnumerator CameraExists()
        {
            var camera = Camera.main;
            Assert.IsNotNull(camera,
                "Main Camera должна существовать");
            Assert.IsTrue(camera.orthographic,
                "Камера должна быть ортографической (2D игра)");
            yield return null;
        }
    }
}
