using System;
using NUnit.Framework;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode
{
    /// <summary>
    /// Тесты совместимости пакетов после апгрейда на Unity 6.3.
    /// Проверяют доступность ключевых типов из InputSystem, UGS и игрового кода.
    /// </summary>
    [TestFixture]
    public class PackageCompatibilityTests
    {
        /// <summary>
        /// InputAction из Input System пакета доступен.
        /// </summary>
        [Test]
        public void InputSystemTypeExists()
        {
            var type = Type.GetType("UnityEngine.InputSystem.InputAction, Unity.InputSystem");
            Assert.IsNotNull(type,
                "InputAction из InputSystem пакета должен быть доступен");
        }

        /// <summary>
        /// IAuthenticationService из UGS Authentication пакета доступен.
        /// </summary>
        [Test]
        public void AuthenticationServiceTypeExists()
        {
            var type = Type.GetType(
                "Unity.Services.Authentication.IAuthenticationService, Unity.Services.Authentication");
            Assert.IsNotNull(type,
                "IAuthenticationService из UGS Auth должен быть доступен");
        }

        /// <summary>
        /// ILeaderboardsService из UGS Leaderboards пакета доступен.
        /// </summary>
        [Test]
        public void LeaderboardsServiceTypeExists()
        {
            var type = Type.GetType(
                "Unity.Services.Leaderboards.ILeaderboardsService, Unity.Services.Leaderboards");
            Assert.IsNotNull(type,
                "ILeaderboardsService из UGS Leaderboards должен быть доступен");
        }

        /// <summary>
        /// Основные игровые типы компилируются и доступны.
        /// </summary>
        [Test]
        public void CoreGameTypesExist()
        {
            Assert.IsNotNull(typeof(SelStrom.Asteroids.ApplicationEntry),
                "ApplicationEntry должен компилироваться");
            Assert.IsNotNull(typeof(MoveData),
                "MoveData должен компилироваться");
        }
    }
}
