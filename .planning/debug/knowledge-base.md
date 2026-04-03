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

