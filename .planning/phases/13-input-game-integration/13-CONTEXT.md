# Phase 13: Input & Game Integration - Context

**Gathered:** 2026-04-05 (auto mode)
**Status:** Ready for planning

<domain>
## Phase Boundary

Игрок управляет запуском ракет -- нажатие R запускает ракету из позиции корабля в направлении его rotation. Ракета не запускается при пустом боезапасе. При рестарте все активные ракеты уничтожаются и боезапас сбрасывается. Требование ROCK-01.

</domain>

<decisions>
## Implementation Decisions

### Input System: кнопка R
- **D-01:** Добавить новый action `Rocket` (type: Button) в `player_actions.inputactions` с биндингом `<Keyboard>/r`
- **D-02:** Перегенерировать `PlayerActions.cs` после изменения inputactions
- **D-03:** Добавить `OnRocketAction` event (Action) в `PlayerInput` по аналогии с `OnAttackAction` / `OnLaserAction`
- **D-04:** Подписка `_playerControls.Rocket.performed += OnRocket` в конструкторе PlayerInput

### Механизм запуска ракеты (ECS pipeline)
- **D-05:** Расширить `RocketAmmoData` полями `Shooting` (bool), `ShootPosition` (float2), `Direction` (float2) -- по паттерну `GunData`/`LaserData`
- **D-06:** В `Game.OnRocket()`: получить ship entity, прочитать `RocketAmmoData`, если `CurrentAmmo > 0` -- установить `Shooting = true`, записать `ShootPosition` из `MoveData.Position` и `Direction` из `RotateData.Rotation`
- **D-07:** В `EcsRocketAmmoSystem.OnUpdate()`: если `Shooting == true` -- декрементировать `CurrentAmmo`, сгенерировать `RocketShootEvent` в `DynamicBuffer`, сбросить `Shooting = false`
- **D-08:** `ShootEventProcessorSystem.ProcessRocketEvents()` уже реализован в Phase 12 -- вызовет `EntitiesCatalog.CreateRocket()` автоматически

### Подписка/отписка в Game
- **D-09:** В `Game.Start()`: `_playerInput.OnRocketAction += OnRocket` (после существующих подписок)
- **D-10:** В `Game.Stop()`: `_playerInput.OnRocketAction -= OnRocket` (после существующих отписок)

### Сброс при рестарте
- **D-11:** `ReleaseAllGameEntities()` в `Game.Restart()` уже уничтожает все GameObjects (включая ракеты) через DeadEntityCleanupSystem -- дополнительная очистка ракетных entity не нужна
- **D-12:** Добавить очистку `RocketShootEvent` буфера в `Game.ClearEcsEventBuffers()` -- по паттерну GunShootEvent/LaserShootEvent
- **D-13:** Сброс `RocketAmmoData` на ship entity НЕ нужен отдельно -- `ReleaseAllGameEntities` уничтожает ship entity, `Start()` пересоздаёт его с полным боезапасом через `EntityFactory.CreateShip()`

### Claude's Discretion
- Нужна ли проверка `CurrentAmmo > 0` в `Game.OnRocket()` (guard на стороне Game) или только в `EcsRocketAmmoSystem` (guard на стороне ECS) -- оба подхода валидны, guard в Game предотвращает лишний SetComponentData
- Порядок нового action в inputactions файле (после Laser или в конце)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` -- ROCK-01: игрок может запустить самонаводящуюся ракету нажатием R

### Input System
- `Assets/Input/player_actions.inputactions` -- определение input actions, добавить Rocket action
- `Assets/Scripts/Input/PlayerInput.cs` -- обёртка Input System, добавить OnRocketAction
- `Assets/Scripts/Input/Generated/PlayerActions.cs` -- автогенерируемый код (перегенерировать после изменения inputactions)

### Game integration (primary target)
- `Assets/Scripts/Application/Game.cs` -- игровой цикл, Start/Stop подписки, OnRocket handler
- `Assets/Scripts/Application/EntitiesCatalog.cs:110` -- CreateShip() с `rocketMaxAmmo: 3, rocketReloadSec: 5f`

### ECS shooting pipeline
- `Assets/Scripts/ECS/Components/RocketAmmoData.cs` -- расширить полями Shooting/ShootPosition/Direction
- `Assets/Scripts/ECS/Systems/EcsRocketAmmoSystem.cs` -- добавить логику стрельбы (генерация RocketShootEvent)
- `Assets/Scripts/ECS/Components/RocketShootEvent.cs` -- событие стрельбы (уже создано в Phase 12)
- `Assets/Scripts/Bridge/ShootEventProcessorSystem.cs:152-178` -- ProcessRocketEvents() уже реализован

### Existing patterns (analogs)
- `Assets/Scripts/ECS/Components/GunData.cs` -- паттерн для Shooting/Direction/ShootPosition полей
- `Assets/Scripts/ECS/Components/LaserData.cs` -- паттерн для Shooting/Direction/ShootPosition полей
- `Assets/Scripts/ECS/Systems/EcsGunSystem.cs` -- паттерн системы со стрельбой и генерацией ShootEvent

### Prior phase context
- `.planning/phases/10-ecs-core/10-CONTEXT.md` -- D-11..D-13: боезапас и перезарядка
- `.planning/phases/12-bridge-lifecycle/12-CONTEXT.md` -- D-07..D-09: RocketShootEvent pipeline

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Game.OnAttack()` / `Game.OnLaser()` -- точные шаблоны для `OnRocket()`: TryGetShipEntity, прочитать данные, установить Shooting flag
- `Game.ClearEcsEventBuffers()` -- шаблон для добавления очистки RocketShootEvent
- `PlayerInput` -- все механики ввода уже реализованы по единому паттерну
- `EcsGunSystem.OnUpdate()` -- паттерн для добавления логики стрельбы в EcsRocketAmmoSystem
- `ShootEventProcessorSystem.ProcessRocketEvents()` -- уже готов, ждёт RocketShootEvent в буфере

### Established Patterns
- Input flow: InputActions -> PlayerInput event -> Game handler -> ECS component flag -> ECS system -> ShootEvent -> Bridge -> Visual
- Shooting flag pattern: Game устанавливает `Shooting = true` + позицию/направление, ECS system сбрасывает flag и генерирует event
- Restart pattern: `Restart()` -> `ReleaseAllGameEntities()` -> `ClearEcsEventBuffers()` -> `ResetEcsScore()` -> `Start()`

### Integration Points
- `player_actions.inputactions` -- добавить action
- `PlayerInput.cs` -- добавить event + handler
- `Game.cs` -- OnRocket, Start/Stop подписки
- `RocketAmmoData.cs` -- расширить struct
- `EcsRocketAmmoSystem.cs` -- добавить shooting logic
- `Game.ClearEcsEventBuffers()` -- добавить RocketShootEvent очистку

</code_context>

<specifics>
## Specific Ideas

No specific requirements -- open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

None -- analysis stayed within phase scope

</deferred>

---

*Phase: 13-input-game-integration*
*Context gathered: 2026-04-05*
