# Phase 13: Input & Game Integration - Research

**Researched:** 2026-04-05
**Domain:** Unity Input System + ECS shooting pipeline integration
**Confidence:** HIGH

## Summary

Фаза 13 подключает кнопку R к существующему ECS-пайплайну стрельбы ракетами. Все компоненты уже на месте: `RocketAmmoData` (Phase 10), `RocketShootEvent` + `ProcessRocketEvents()` (Phase 12). Требуется: (1) добавить action в Input System, (2) расширить `RocketAmmoData` полями `Shooting/Direction/ShootPosition`, (3) добавить логику стрельбы в `EcsRocketAmmoSystem`, (4) подключить `OnRocket` handler в `Game.cs`.

Паттерн полностью повторяет существующую реализацию `GunData`/`EcsGunSystem` и `LaserData`/`EcsLaserSystem`. Нет неизвестных -- все аналоги уже работают в кодовой базе.

**Primary recommendation:** Следовать паттерну `OnAttack` -> `GunData.Shooting` -> `EcsGunSystem` -> `GunShootEvent` один к одному, заменяя Gun на Rocket.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Добавить новый action `Rocket` (type: Button) в `player_actions.inputactions` с биндингом `<Keyboard>/r`
- **D-02:** Перегенерировать `PlayerActions.cs` после изменения inputactions
- **D-03:** Добавить `OnRocketAction` event (Action) в `PlayerInput` по аналогии с `OnAttackAction` / `OnLaserAction`
- **D-04:** Подписка `_playerControls.Rocket.performed += OnRocket` в конструкторе PlayerInput
- **D-05:** Расширить `RocketAmmoData` полями `Shooting` (bool), `ShootPosition` (float2), `Direction` (float2) -- по паттерну `GunData`/`LaserData`
- **D-06:** В `Game.OnRocket()`: получить ship entity, прочитать `RocketAmmoData`, если `CurrentAmmo > 0` -- установить `Shooting = true`, записать `ShootPosition` из `MoveData.Position` и `Direction` из `RotateData.Rotation`
- **D-07:** В `EcsRocketAmmoSystem.OnUpdate()`: если `Shooting == true` -- декрементировать `CurrentAmmo`, сгенерировать `RocketShootEvent` в `DynamicBuffer`, сбросить `Shooting = false`
- **D-08:** `ShootEventProcessorSystem.ProcessRocketEvents()` уже реализован в Phase 12 -- вызовет `EntitiesCatalog.CreateRocket()` автоматически
- **D-09:** В `Game.Start()`: `_playerInput.OnRocketAction += OnRocket` (после существующих подписок)
- **D-10:** В `Game.Stop()`: `_playerInput.OnRocketAction -= OnRocket` (после существующих отписок)
- **D-11:** `ReleaseAllGameEntities()` в `Game.Restart()` уже уничтожает все GameObjects (включая ракеты) через DeadEntityCleanupSystem -- дополнительная очистка ракетных entity не нужна
- **D-12:** Добавить очистку `RocketShootEvent` буфера в `Game.ClearEcsEventBuffers()` -- по паттерну GunShootEvent/LaserShootEvent
- **D-13:** Сброс `RocketAmmoData` на ship entity НЕ нужен отдельно -- `ReleaseAllGameEntities` уничтожает ship entity, `Start()` пересоздаёт его с полным боезапасом через `EntityFactory.CreateShip()`

### Claude's Discretion
- Нужна ли проверка `CurrentAmmo > 0` в `Game.OnRocket()` (guard на стороне Game) или только в `EcsRocketAmmoSystem` (guard на стороне ECS) -- оба подхода валидны, guard в Game предотвращает лишний SetComponentData
- Порядок нового action в inputactions файле (после Laser или в конце)

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| ROCK-01 | Игрок может запустить самонаводящуюся ракету нажатием R | Полный пайплайн: Input action (R) -> PlayerInput.OnRocketAction -> Game.OnRocket() -> RocketAmmoData.Shooting -> EcsRocketAmmoSystem -> RocketShootEvent -> ShootEventProcessorSystem.ProcessRocketEvents() -> EntitiesCatalog.CreateRocket(). Все аналоги верифицированы в кодовой базе. |
</phase_requirements>

## Standard Stack

Новых библиотек не требуется. Все зависимости уже в проекте.

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Unity Input System | 1.19.0 | Определение input action для кнопки R | Уже используется для всех остальных действий [VERIFIED: manifest.json] |
| Unity Entities | via Unity 2022.3 | ECS компоненты и системы | Уже используется для GunData, LaserData, EcsGunSystem [VERIFIED: codebase] |

## Architecture Patterns

### Полный Input-to-Entity pipeline (существующий паттерн)

```
Клавиша R
  -> InputActions.Rocket.performed
    -> PlayerInput.OnRocketAction event
      -> Game.OnRocket()
        -> EntityManager.SetComponentData<RocketAmmoData>(Shooting=true)
          -> EcsRocketAmmoSystem: if Shooting && CurrentAmmo > 0 -> RocketShootEvent
            -> ShootEventProcessorSystem.ProcessRocketEvents()
              -> EntitiesCatalog.CreateRocket()
```

Это точная копия пайплайна Gun и Laser, верифицированная в кодовой базе. [VERIFIED: Game.cs OnAttack/OnLaser, EcsGunSystem, ShootEventProcessorSystem]

### Pattern 1: Shooting Flag Pattern
**What:** Game устанавливает `Shooting = true` + позицию/направление на IComponentData, ECS system обрабатывает флаг и генерирует ShootEvent, затем сбрасывает `Shooting = false`.
**When to use:** Любой случай, когда input из MonoBehaviour-мира должен запустить действие в ECS.
**Example (аналог из GunData/EcsGunSystem):**
```csharp
// Game.cs -- MonoBehaviour сторона
// Source: Assets/Scripts/Application/Game.cs:247-259
private void OnAttack()
{
    if (TryGetShipEntity(out var entity))
    {
        var gunData = _entityManager.GetComponentData<ECS.GunData>(entity);
        var rotateData = _entityManager.GetComponentData<RotateData>(entity);
        var moveData = _entityManager.GetComponentData<MoveData>(entity);

        gunData.Shooting = true;
        gunData.Direction = rotateData.Rotation;
        gunData.ShootPosition = moveData.Position;
        _entityManager.SetComponentData(entity, gunData);
    }
}
```

```csharp
// EcsGunSystem.cs -- ECS сторона
// Source: Assets/Scripts/ECS/Systems/EcsGunSystem.cs:35-47
if (gun.ValueRO.Shooting && gun.ValueRO.CurrentShoots > 0)
{
    gun.ValueRW.CurrentShoots--;
    gunEvents.Add(new GunShootEvent
    {
        ShooterEntity = entity,
        Position = gun.ValueRO.ShootPosition,
        Direction = gun.ValueRO.Direction,
        IsPlayer = state.EntityManager.HasComponent<ShipTag>(entity)
    });
}

gun.ValueRW.Shooting = false;
```

### Pattern 2: Input Action Registration
**What:** Добавление нового action в PlayerInput по установленному паттерну.
**Example:**
```csharp
// Source: Assets/Scripts/Input/PlayerInput.cs
public event Action OnRocketAction;

// В конструкторе:
_playerControls.Rocket.performed += OnRocket;

[PublicAPI]
private void OnRocket(InputAction.CallbackContext _)
{
    OnRocketAction?.Invoke();
}
```

### Pattern 3: Event Buffer Cleanup при Restart
**What:** В `ClearEcsEventBuffers()` нужно очистить DynamicBuffer соответствующего ShootEvent.
**Example (аналог из Game.cs):**
```csharp
// Source: Assets/Scripts/Application/Game.cs:101-125
var rocketQuery = _entityManager.CreateEntityQuery(typeof(RocketShootEvent));
if (rocketQuery.CalculateEntityCount() > 0)
{
    var entities = rocketQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
    for (int i = 0; i < entities.Length; i++)
    {
        _entityManager.GetBuffer<RocketShootEvent>(entities[i]).Clear();
    }

    entities.Dispose();
}
```

### Anti-Patterns to Avoid
- **Дублирование guard-логики:** Не создавать сложную логику валидации в двух местах (Game И EcsSystem). Простая проверка `CurrentAmmo > 0` в Game допустима как оптимизация, но основная логика -- только в EcsRocketAmmoSystem. [VERIFIED: EcsGunSystem не имеет guard в Game.OnAttack]

### Рекомендация по Claude's Discretion

**Guard в Game.OnRocket():** Рекомендую добавить guard `CurrentAmmo > 0` в `Game.OnRocket()`. Обоснование: паттерн `OnAttack` не имеет guard (Gun всегда перезаряжается полностью, `CurrentShoots` восстанавливается до `MaxShoots` за одну перезарядку). Ракеты перезаряжаются **по одной** и могут быть на нуле долго. Guard в Game предотвращает бессмысленный `SetComponentData` каждый кадр при зажатой R с пустым боезапасом.

**Порядок action в inputactions:** Рекомендую добавить после Laser (перед Back). Обоснование: группировка боевых action вместе (Attack, Laser, Rocket).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Input binding | Ручная обработка KeyCode в Update() | Unity Input System action | Кроссплатформенность, единый паттерн с остальными действиями [VERIFIED: все input через InputSystem] |
| Event dispatching | Собственная шина событий | Shooting flag + DynamicBuffer<ShootEvent> | Уже работающий паттерн для Gun и Laser, синхронизирован с ECS update loop [VERIFIED: codebase] |
| Ammo validation | Отдельный validator/service | Guard в EcsRocketAmmoSystem.OnUpdate | Логика уже рядом с данными, один source of truth [VERIFIED: EcsGunSystem pattern] |

## Common Pitfalls

### Pitfall 1: Забыть перегенерировать PlayerActions.cs
**What goes wrong:** Компиляция упадёт -- `_playerControls.Rocket` не существует.
**Why it happens:** `PlayerActions.cs` в `Assets/Scripts/Input/Generated/` -- автогенерируемый файл. Unity генерирует его при сохранении .inputactions через Inspector.
**How to avoid:** После редактирования `player_actions.inputactions` вручную (JSON), открыть файл в Unity Inspector и нажать "Apply" или использовать Generate C# Class checkbox.
**Warning signs:** `PlayerActions` не содержит свойство `Rocket`.

### Pitfall 2: Не добавить WithEntityAccess() в EcsRocketAmmoSystem
**What goes wrong:** Невозможно получить entity для `RocketShootEvent.ShooterEntity`.
**Why it happens:** Текущий `EcsRocketAmmoSystem` использует `SystemAPI.Query<RefRW<RocketAmmoData>>()` без `.WithEntityAccess()` -- entity не нужен для перезарядки. Для стрельбы entity нужен.
**How to avoid:** Изменить query на `SystemAPI.Query<RefRW<RocketAmmoData>>().WithEntityAccess()` по паттерну `EcsGunSystem`. [VERIFIED: EcsGunSystem.cs:23]
**Warning signs:** Компиляционная ошибка при попытке использовать `entity` в foreach.

### Pitfall 3: Не добавить RequireForUpdate<RocketShootEvent>() в OnCreate
**What goes wrong:** Система может упасть при попытке `GetSingletonBuffer<RocketShootEvent>()` если singleton ещё не создан.
**Why it happens:** `EcsRocketAmmoSystem.OnCreate` сейчас пустой. `EcsGunSystem` имеет `state.RequireForUpdate<GunShootEvent>()`.
**How to avoid:** Добавить `state.RequireForUpdate<RocketShootEvent>()` в `OnCreate`. [VERIFIED: EcsGunSystem.cs:12]
**Warning signs:** NullReferenceException или "singleton not found" при первом кадре до инициализации буфера.

### Pitfall 4: Не очистить RocketShootEvent в ClearEcsEventBuffers
**What goes wrong:** При рестарте оставшиеся RocketShootEvent в буфере могут вызвать спавн ракет в следующем кадре нового раунда.
**Why it happens:** `ClearEcsEventBuffers` сейчас очищает только Gun, Laser и Collision. Ракетный буфер не очищается.
**How to avoid:** Добавить блок очистки по паттерну существующих (D-12). [VERIFIED: Game.cs:101-137]
**Warning signs:** Фантомные ракеты при рестарте.

### Pitfall 5: Shooting flag не сбрасывается при пустом боезапасе
**What goes wrong:** `Shooting` остаётся `true` навсегда, и при следующей перезарядке ракета стреляет автоматически без нажатия R.
**Why it happens:** Если ECS система проверяет `if (Shooting && CurrentAmmo > 0)` но сбрасывает `Shooting = false` только внутри этого блока -- при `CurrentAmmo == 0` флаг не сбросится.
**How to avoid:** Сбрасывать `Shooting = false` **безусловно** в конце обработки каждой entity, вне блока if. По паттерну `EcsGunSystem.cs:47` -- `gun.ValueRW.Shooting = false` выполняется всегда. [VERIFIED: EcsGunSystem.cs:47]
**Warning signs:** Автоматическая стрельба ракетой сразу после перезарядки.

## Code Examples

### 1. Расширение RocketAmmoData (по паттерну GunData)
```csharp
// Source: аналог Assets/Scripts/ECS/Components/GunData.cs
using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    public struct RocketAmmoData : IComponentData
    {
        public int MaxAmmo;
        public float ReloadDurationSec;
        public int CurrentAmmo;
        public float ReloadRemaining;
        public bool Shooting;
        public float2 Direction;
        public float2 ShootPosition;
    }
}
```

### 2. EcsRocketAmmoSystem с логикой стрельбы (по паттерну EcsGunSystem)
```csharp
// Source: аналог Assets/Scripts/ECS/Systems/EcsGunSystem.cs
public void OnCreate(ref SystemState state)
{
    state.RequireForUpdate<RocketShootEvent>();
}

public void OnUpdate(ref SystemState state)
{
    var deltaTime = SystemAPI.Time.DeltaTime;
    var rocketEvents = SystemAPI.GetSingletonBuffer<RocketShootEvent>();

    foreach (var (ammo, entity) in SystemAPI.Query<RefRW<RocketAmmoData>>().WithEntityAccess())
    {
        // Перезарядка (существующий код)
        if (ammo.ValueRO.CurrentAmmo < ammo.ValueRO.MaxAmmo)
        {
            ammo.ValueRW.ReloadRemaining -= deltaTime;
            if (ammo.ValueRO.ReloadRemaining <= 0)
            {
                ammo.ValueRW.ReloadRemaining = ammo.ValueRO.ReloadDurationSec;
                ammo.ValueRW.CurrentAmmo += 1;
            }
        }

        // Стрельба
        if (ammo.ValueRO.Shooting && ammo.ValueRO.CurrentAmmo > 0)
        {
            ammo.ValueRW.CurrentAmmo--;
            rocketEvents.Add(new RocketShootEvent
            {
                ShooterEntity = entity,
                Position = ammo.ValueRO.ShootPosition,
                Direction = ammo.ValueRO.Direction
            });
        }

        ammo.ValueRW.Shooting = false;
    }
}
```

### 3. Game.OnRocket (по паттерну OnAttack/OnLaser)
```csharp
// Source: аналог Assets/Scripts/Application/Game.cs:247-275
private void OnRocket()
{
    if (TryGetShipEntity(out var entity))
    {
        var rocketAmmo = _entityManager.GetComponentData<RocketAmmoData>(entity);
        if (rocketAmmo.CurrentAmmo <= 0)
        {
            return;
        }

        var rotateData = _entityManager.GetComponentData<RotateData>(entity);
        var moveData = _entityManager.GetComponentData<MoveData>(entity);

        rocketAmmo.Shooting = true;
        rocketAmmo.Direction = rotateData.Rotation;
        rocketAmmo.ShootPosition = moveData.Position;
        _entityManager.SetComponentData(entity, rocketAmmo);
    }
}
```

### 4. Input Action JSON для Rocket
```json
{
    "name": "Rocket",
    "type": "Button",
    "id": "<generated-guid>",
    "expectedControlType": "",
    "processors": "",
    "interactions": "",
    "initialStateCheck": false
}
```
С биндингом:
```json
{
    "name": "",
    "id": "<generated-guid>",
    "path": "<Keyboard>/r",
    "interactions": "",
    "processors": "",
    "groups": "",
    "action": "Rocket",
    "isComposite": false,
    "isPartOfComposite": false
}
```

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | NUnit 3.x (Unity Test Framework 1.1.33) |
| Config file | `Assets/Tests/EditMode/EditMode.asmdef` |
| Quick run command | Unity Editor -> Window -> General -> Test Runner -> EditMode -> Run All |
| Full suite command | Unity Editor -> Test Runner -> Run All (EditMode + PlayMode) |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| ROCK-01a | Shooting=true + ammo>0 -> RocketShootEvent + ammo decremented | unit | Unity Test Runner: RocketAmmoSystemTests | Файл существует, тесты нужно добавить |
| ROCK-01b | Shooting=true + ammo==0 -> no event, no decrement | unit | Unity Test Runner: RocketAmmoSystemTests | Файл существует, тесты нужно добавить |
| ROCK-01c | Shooting flag сбрасывается безусловно | unit | Unity Test Runner: RocketAmmoSystemTests | Файл существует, тест нужно добавить |
| ROCK-01d | RocketShootEvent содержит корректные Position/Direction | unit | Unity Test Runner: RocketAmmoSystemTests | Файл существует, тест нужно добавить |

### Sampling Rate
- **Per task commit:** Запуск RocketAmmoSystemTests в Unity Test Runner
- **Per wave merge:** Полный набор EditMode тестов
- **Phase gate:** Все EditMode тесты зелёные

### Wave 0 Gaps
- [ ] Новые тесты стрельбы в `Assets/Tests/EditMode/ECS/RocketAmmoSystemTests.cs` -- тесты перезарядки уже есть, нужны тесты на Shooting логику

## Security Domain

Не применимо. Фаза затрагивает только локальный input и ECS компоненты без сетевого взаимодействия.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Unity автогенерирует PlayerActions.cs при изменении .inputactions через Inspector | Pitfall 1 | Нужно будет генерировать вручную или через API. Низкий риск -- стандартное поведение Unity Input System |

## Open Questions

Нет открытых вопросов. Все паттерны верифицированы в кодовой базе, решения зафиксированы в CONTEXT.md.

## Sources

### Primary (HIGH confidence)
- `Assets/Scripts/Application/Game.cs` -- OnAttack, OnLaser, Start, Stop, ClearEcsEventBuffers паттерны
- `Assets/Scripts/Input/PlayerInput.cs` -- паттерн добавления action events
- `Assets/Input/player_actions.inputactions` -- структура JSON для input actions
- `Assets/Scripts/ECS/Components/GunData.cs` -- паттерн struct с Shooting/Direction/ShootPosition
- `Assets/Scripts/ECS/Systems/EcsGunSystem.cs` -- паттерн системы со стрельбой и генерацией ShootEvent
- `Assets/Scripts/ECS/Systems/EcsRocketAmmoSystem.cs` -- текущий код системы (только перезарядка)
- `Assets/Scripts/ECS/Components/RocketAmmoData.cs` -- текущий struct (без полей стрельбы)
- `Assets/Scripts/ECS/Components/RocketShootEvent.cs` -- событие стрельбы (уже создано)
- `Assets/Tests/EditMode/ECS/RocketAmmoSystemTests.cs` -- существующие тесты перезарядки

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- все библиотеки уже в проекте, ничего нового
- Architecture: HIGH -- паттерн 1:1 с Gun/Laser, верифицирован в коде
- Pitfalls: HIGH -- все 5 pitfalls основаны на анализе реального кода

**Research date:** 2026-04-05
**Valid until:** 2026-05-05 (стабильная кодовая база, паттерны не меняются)
