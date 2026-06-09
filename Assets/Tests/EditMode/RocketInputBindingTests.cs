using System.Linq;
using NUnit.Framework;

namespace SelStrom.Asteroids.Tests.EditMode
{
    public class RocketInputBindingTests
    {
        [Test]
        public void RocketAction_Exists_AndBoundToRKey()
        {
            var actions = new PlayerActions();
            var rocket = actions.PlayerControls.Rocket;

            Assert.IsNotNull(rocket, "Действие Rocket должно существовать в PlayerControls");
            Assert.IsTrue(
                rocket.bindings.Any(b => b.path == "<Keyboard>/r"),
                "Действие Rocket должно быть привязано к клавише R");

            // Dispose не вызывается: PlayerActions.Dispose использует Object.Destroy,
            // запрещённый в EditMode
            UnityEngine.Object.DestroyImmediate(actions.asset);
        }
    }
}
