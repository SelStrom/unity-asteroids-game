---
status: resolved
trigger: "UFO и астероиды не уничтожаются при столкновении; спрайт корабля не меняется при thrust"
created: 2026-04-04T00:00:00Z
updated: 2026-04-04T00:15:00Z
---

## Current Focus

hypothesis: Баг 1 исправлен (wiring). Баг 2 — код корректен, возможно проблема конфигурации (ThrustSprite не назначен в Inspector)
test: Пользователь проверяет Play Mode
expecting: Астероиды и UFO уничтожаются при столкновении; thrust sprite переключается при W
next_action: Ожидание подтверждения от пользователя

## Symptoms

expected:
  1. UFO и астероиды уничтожают друг друга при столкновении
  2. При W спрайт корабля переключается на ThrustSprite

actual:
  1. UFO и астероиды пролетают сквозь друг друга
  2. Спрайт корабля не меняется при thrust

errors: Нет ошибок в консоли

reproduction:
  1. Play Mode -> ждать UFO -> наблюдать отсутствие коллизий с астероидами
  2. Play Mode -> W -> спрайт не меняется

started: После Phase 6 legacy cleanup / Phase 7 ShipPositionData wiring

## Eliminated

## Evidence

- timestamp: 2026-04-04T00:01:00Z
  checked: AsteroidVisual.cs — OnCollisionEnter2D
  found: Метод ОТСУТСТВУЕТ. AsteroidViewModel не имеет поля OnCollision.
  implication: Астероиды не могут сообщать о столкновениях в CollisionBridge.

- timestamp: 2026-04-04T00:02:00Z
  checked: UfoVisual.cs — OnCollisionEnter2D
  found: Вызывает ViewModel.OnCollision.Value?.Invoke() БЕЗ передачи col.gameObject. UfoViewModel.OnCollision тип ReactiveValue<Action> (без параметра Collision2D).
  implication: Даже если wiring добавить, UFO не может передать OTHER gameObject в CollisionBridge.

- timestamp: 2026-04-04T00:03:00Z
  checked: EntitiesCatalog.cs — CreateAsteroid, CreateBigUfo, CreateUfo
  found: НЕТ viewModel.OnCollision.Value = ... wiring к CollisionBridge.ReportCollision. Ship и Bullet имеют wiring.
  implication: Астероиды и UFO зарегистрированы в CollisionBridge.RegisterMapping, но MonoBehaviour не пробрасывает столкновения.

- timestamp: 2026-04-04T00:04:00Z
  checked: ObservableBridgeSystem.cs строки 84-87 — логика thrust sprite
  found: Код корректен: `thrust.ValueRO.IsActive ? _thrustSprite : _mainSprite`. Guard: `if (_mainSprite != null && _thrustSprite != null)`.
  implication: Если ThrustSprite не назначен в Inspector -> null -> guard пропускает обновление.

- timestamp: 2026-04-04T00:05:00Z
  checked: Application.cs — SetShipViewModel вызовы
  found: Второй вызов (строка 102) после _game.Start() передаёт корректный ShipViewModel + спрайты из _configs.
  implication: Pipeline корректен при условии что ThrustSprite != null в конфиге.

- timestamp: 2026-04-04T00:06:00Z
  checked: EcsThrustSystem.cs + Game.OnTrust + PlayerInput.OnAccelerate
  found: Полная цепочка Input -> ECS -> Bridge -> ViewModel корректна в коде.
  implication: Баг 2 — скорее всего проблема конфигурации GameData (ThrustSprite не назначен).

## Resolution

root_cause: |
  **Баг 1 (UFO-Asteroid коллизии):** Три пробела в bridge layer:
  1. AsteroidVisual не имел OnCollisionEnter2D + AsteroidViewModel не имел OnCollision поля
  2. UfoViewModel.OnCollision был типа Action (без параметров) — не передавал col.gameObject
  3. EntitiesCatalog не подключал OnCollision callback для астероидов и UFO

  **Баг 2 (thrust sprite):** Код ObservableBridgeSystem корректен. Вероятная причина — ThrustSprite не назначен в GameData Inspector (null), из-за чего guard `_mainSprite != null && _thrustSprite != null` пропускает обновление.

fix: |
  **Баг 1 (применено):**
  - AsteroidViewModel: добавлено ReactiveValue<Action<Collision2D>> OnCollision
  - AsteroidVisual: добавлен OnCollisionEnter2D
  - UfoViewModel: OnCollision изменён с ReactiveValue<Action> на ReactiveValue<Action<Collision2D>>
  - UfoVisual: передаёт col в OnCollision.Value?.Invoke(col)
  - EntitiesCatalog: добавлен OnCollision wiring для CreateAsteroid, CreateBigUfo, CreateUfo

  **Баг 2:** Нужно проверить GameData Inspector — назначен ли ThrustSprite.

  **Тесты добавлены:**
  - ObservableBridgeSystemTests: ThrustActive_SetsThrustSprite, ThrustInactive_SetsMainSprite, ThrustActive_NullSprites_DoesNotUpdateSprite
  - EcsBridgeRegressionTests: AsteroidViewModel_HasOnCollision_WithCollision2DParameter, UfoViewModel_HasOnCollision_WithCollision2DParameter

verification: Подтверждено пользователем в Play Mode — UFO и астероиды уничтожаются при столкновении, thrust sprite работает.
files_changed:
  - Assets/Scripts/View/AsteroidVisual.cs
  - Assets/Scripts/View/UfoVisual.cs
  - Assets/Scripts/Application/EntitiesCatalog.cs
  - Assets/Tests/EditMode/ECS/ObservableBridgeSystemTests.cs
  - Assets/Tests/EditMode/ECS/EcsBridgeRegressionTests.cs
