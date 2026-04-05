# GSD Debug Knowledge Base

Resolved debug sessions. Used by `gsd-debugger` to surface known-pattern hypotheses at the start of new investigations.

---

## ufo-asteroid-collision-and-thrust-sprite --- UFO и астероиды не коллизили, thrust sprite не менялся
- **Date:** 2026-04-04
- **Error patterns:** UFO, asteroid, collision, OnCollisionEnter2D, OnCollision, wiring, thrust, sprite, ThrustSprite, null guard
- **Root cause:** AsteroidVisual не имел OnCollisionEnter2D, UfoViewModel.OnCollision был Action без параметра Collision2D, EntitiesCatalog не подключал OnCollision для астероидов и UFO. Thrust sprite -- guard на null пропускал обновление при отсутствии назначенного спрайта.
- **Fix:** Добавлены OnCollisionEnter2D в AsteroidVisual, изменён тип OnCollision на Action<Collision2D> в UfoViewModel, добавлен wiring в EntitiesCatalog для asteroid/UFO. Thrust sprite -- проверка конфигурации GameData Inspector.
- **Files changed:** Assets/Scripts/View/AsteroidVisual.cs, Assets/Scripts/View/UfoVisual.cs, Assets/Scripts/Application/EntitiesCatalog.cs, Assets/Tests/EditMode/ECS/ObservableBridgeSystemTests.cs, Assets/Tests/EditMode/ECS/EcsBridgeRegressionTests.cs
---

## hudvisual-nullref-onconnected --- NullReferenceException в HudVisual.OnConnected() для rocket SerializeField
- **Date:** 2026-04-06
- **Error patterns:** NullReferenceException, HudVisual, OnConnected, SerializeField, null, _rocketReloadTime, _rocketAmmoCount, YAML, PrefabInstance, deserialization
- **Root cause:** SerializeField _rocketReloadTime и _rocketAmmoCount = null в runtime, потому что PrefabInstance были добавлены вручную через YAML-редактирование сцены и Unity Editor не подхватил изменения без пересохранения. Строка 40 обращалась к .gameObject без null-check.
- **Fix:** Null-guard в HudVisual.OnConnected() с Debug.LogWarning для rocket SerializeField. Регрессионный тест HudSerializeFieldTests. Пересохранение сцены в Unity Editor.
- **Files changed:** Assets/Scripts/View/HudVisual.cs, Assets/Tests/EditMode/Upgrade/HudSerializeFieldTests.cs
---

