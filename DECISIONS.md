# DECISIONS.md — Homing Rockets Feature

## Original Prompt

> Самонаводящиеся ракеты. Нужно сделать новую фичу:
> - У игрока есть одна ракета, запускаемая клавишей R
> - Ракета самонаводится на ближайшую цель (летит дугой к ближайшему астероиду/НЛО)
> - Если ракета по пути задела любого врага — это засчитывается
> - После запуска начинается перезарядка
> - Количество ракет и время перезарядки — конфиг
> - Ракета коллизит с астероидами и НЛО
> - Визуал: уменьшенный спрайт корабля с инверсионным следом (particle trail)
> - HUD показывает количество ракет и время перезарядки
> - TDD подход, полное покрытие тестами
> - Верификация через юнит-тесты, интеграционные тесты и MCP

## Decisions

### 1. ECS Architecture (not MonoBehaviour ECS-like)
**Decision:** Реализовать на Unity DOTS ECS (Entities package), следуя паттернам уже мигрированной кодовой базы.
**Rationale:** Проект уже полностью мигрирован на гибридный DOTS. Все системы (Move, Gun, Laser, Collision) работают через ISystem/SystemAPI.

### 2. Homing Algorithm — Cross Product Turn
**Decision:** Использовать знак Z-компоненты cross product (current × desired) для определения направления поворота, clamp угла по TurnSpeed * dt, 2D rotation matrix.
**Rationale:** Простой, эффективный, предсказуемый алгоритм без тригонометрических вычислений кроме финального sin/cos. Даёт плавную дугу к цели.

### 3. Target Selection — Nearest enemy excluding dead
**Decision:** Ракета выбирает ближайшую из всех AsteroidTag/UfoTag/UfoBigTag сущностей без DeadTag.
**Rationale:** Соответствует ТЗ "ближайший астероид/НЛО". Исключение DeadTag предотвращает наведение на уже уничтоженные цели.

### 4. Rocket as PlayerBullet
**Decision:** Ракета имеет и RocketTag и PlayerBulletTag. Коллизионная система проверяет `RocketTag || PlayerBulletTag` для определения игровых снарядов.
**Rationale:** Минимальные изменения в EcsCollisionHandlerSystem. Ракета участвует в тех же коллизионных парах что и пуля.

### 5. Reload Mechanic — Identical to Laser
**Decision:** Перезарядка инкрементальная (+1 ракета за ReloadDurationSec), паттерн 1:1 с EcsLaserSystem.
**Rationale:** Consistency с существующими механиками. Конфигурируемо: MaxShoots=1, ReloadDurationSec=5.

### 6. Visual — Scaled ship sprite + ParticleSystem trail
**Decision:** Префаб rocket использует SpriteRenderer (спрайт корабля, scale 0.5) + дочерний ParticleSystem для инверсионного следа.
**Rationale:** Соответствует ТЗ. ParticleSystem.Stop(withChildren, StopEmitting) при смерти для плавного затухания следа.

### 7. HUD — Duplicate laser pattern
**Decision:** Добавлены ReactiveValue<string> RocketShootCount, RocketReloadTime и ReactiveValue<bool> IsRocketReloadTimeVisible в HudData. TMP тексты размещены под лазерными.
**Rationale:** Consistency с отображением лазера. Минимальные изменения в UI.

### 8. Config — GameData.RocketData struct
**Decision:** Новый [Serializable] struct RocketData в GameData с полями: Prefab, Speed, TurnSpeed, LifeTimeSeconds, MaxShoots, ReloadDurationSec.
**Rationale:** Следует паттерну BulletData/LaserData. Все параметры конфигурируемы из инспектора.

### 9. Input — R key via Input System
**Decision:** Добавлен action "Rocket" (Button) с binding на R в player_actions.inputactions.
**Rationale:** Следует существующему паттерну (Attack=Space, Laser=Q).

## Token Estimate

- Planning & analysis: ~15K tokens
- Test writing (TDD): ~20K tokens
- Implementation: ~40K tokens
- MCP setup (prefab, config, scene): ~25K tokens
- Debugging & fixes: ~30K tokens
- **Total estimated: ~130K tokens**

## Test Coverage

| Test Class | Tests | Coverage |
|---|---|---|
| EcsRocketSystemTests | 7 | Reload, firing, direction, boundary cases |
| EcsHomingSystemTests | 7 | Turn toward target, nearest selection, UFO/UfoBig targeting, no targets, speed limit, dead entity exclusion |
| CollisionHandlerRocketTests | 4 | Kills asteroid/ufo/ufoBig with score, doesn't kill ship |
| ObservableBridgeSystemTests | 9 (2 modified) | HUD data push including rocket fields |
| **Total new/modified** | **18** | |
| **All ECS tests passing** | **183** | No regressions |

## Files Created (11)
- `Assets/Scripts/ECS/Components/Tags/RocketTag.cs`
- `Assets/Scripts/ECS/Components/RocketData.cs`
- `Assets/Scripts/ECS/Components/RocketShootEvent.cs`
- `Assets/Scripts/ECS/Components/HomingData.cs`
- `Assets/Scripts/ECS/Systems/EcsRocketSystem.cs`
- `Assets/Scripts/ECS/Systems/EcsHomingSystem.cs`
- `Assets/Scripts/View/RocketVisual.cs`
- `Assets/Media/prefabs/rocket.prefab`
- `Assets/Tests/EditMode/ECS/EcsRocketSystemTests.cs`
- `Assets/Tests/EditMode/ECS/EcsHomingSystemTests.cs`
- `Assets/Tests/EditMode/ECS/CollisionHandlerRocketTests.cs`

## Files Modified (11)
- `Assets/Input/player_actions.inputactions`
- `Assets/Scripts/Configs/GameData.cs`
- `Assets/Scripts/ECS/EntityFactory.cs`
- `Assets/Scripts/Application/EntitiesCatalog.cs`
- `Assets/Scripts/Application/Game.cs`
- `Assets/Scripts/Application/Application.cs`
- `Assets/Scripts/Application/Screens/GameScreen.cs`
- `Assets/Scripts/Input/PlayerInput.cs`
- `Assets/Scripts/Input/PlayerActions.cs`
- `Assets/Scripts/Bridge/ObservableBridgeSystem.cs`
- `Assets/Scripts/Bridge/ShootEventProcessorSystem.cs`
- `Assets/Scripts/ECS/Systems/EcsCollisionHandlerSystem.cs`
- `Assets/Scripts/View/HudVisual.cs`
- `Assets/Tests/EditMode/ECS/AsteroidsEcsTestFixture.cs`
- `Assets/Tests/EditMode/ECS/ObservableBridgeSystemTests.cs`
- `Assets/Scenes/Main.unity`
- `Assets/Media/configs/GameData.asset`
