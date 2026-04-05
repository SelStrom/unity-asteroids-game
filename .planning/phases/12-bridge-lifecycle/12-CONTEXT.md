# Phase 12: Bridge & Lifecycle - Context

**Gathered:** 2026-04-05 (assumptions mode)
**Status:** Ready for planning

<domain>
## Phase Boundary

Ракета видима на экране -- ECS-данные синхронизируются с GameObject визуалом. Создание визуала при запуске, синхронизация позиции и вращения каждый кадр, корректное уничтожение при DeadTag. Интеграционные тесты lifecycle.

</domain>

<decisions>
## Implementation Decisions

### Синхронизация позиции и вращения
- **D-01:** Добавить третью ветку в `GameObjectSyncSystem` для entity с `RocketTag` + `MoveData` + `GameObjectRef` без `RotateData` -- синхронизировать rotation из `MoveData.Direction` через `math.atan2(dir.y, dir.x)`
- **D-02:** Не добавлять `RotateData` на rocket entity -- это вызвало бы конфликт с `EcsRotateSystem`, которая обрабатывает RotateData по TargetDirection от ввода игрока
- **D-03:** Позиция синхронизируется как обычно: `MoveData.Position` -> `Transform.position`

### Создание визуала ракеты
- **D-04:** Новый метод `EntitiesCatalog.CreateRocket()` по аналогии с `CreateBullet()` -- создаёт ViewModel, Visual, привязки коллизий, GameObjectRef, регистрация в CollisionBridge и AddToCatalog
- **D-05:** Расширить enum `EntityType` значением `Rocket`
- **D-06:** `GameObjectRef` обязателен -- без него `DeadEntityCleanupSystem` не подхватит уничтожение ракеты и GameObject останется в сцене

### Триггер спавна ракеты
- **D-07:** Новый `RocketShootEvent` как `DynamicBuffer<IBufferElementData>` -- единообразно с `GunShootEvent` и `LaserShootEvent`
- **D-08:** Обработка в `ShootEventProcessorSystem` -- добавить ветку для `RocketShootEvent`, вызывающую `EntitiesCatalog.CreateRocket()`
- **D-09:** `EcsRocketAmmoSystem` генерирует `RocketShootEvent` при запуске ракеты (аналогично тому как GunSystem генерирует GunShootEvent)

### Спрайт и префаб ракеты
- **D-10:** Создать минимальный `RocketVisual` (аналог `BulletVisual`) -- хранит `Collider2D`, пробрасывает `OnCollisionEnter2D` через ViewModel.OnCollision
- **D-11:** Отдельный префаб ракеты с уменьшенным спрайтом корабля (VIS-01: `ShipData.MainSprite` в уменьшенном масштабе)
- **D-12:** Конфигурация `RocketData` в `GameData` -- ссылка на префаб, как для всех остальных entity

### Интеграционные тесты lifecycle
- **D-13:** Интеграционные тесты покрывают полный lifecycle: спавн entity + GameObjectRef -> наведение (GuidanceSystem обновляет Direction) -> коллизия (DeadTag) -> cleanup (DeadEntityCleanupSystem уничтожает GameObject)
- **D-14:** Тесты в EditMode используя существующий `AsteroidsEcsTestFixture` -- проверка что GameObjectRef корректно создаётся и уничтожается

### Claude's Discretion
- Конкретный масштаб уменьшения спрайта ракеты (0.3x-0.5x от оригинала -- решить при создании префаба)
- Размер и форма Collider2D ракеты
- Порядок обработки RocketShootEvent в ShootEventProcessorSystem (до или после GunShootEvent)
- Формулировка Assert-сообщений в интеграционных тестах

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` -- VIS-01 (спрайт как уменьшенный корабль), VIS-03 (вращение по направлению полёта), TEST-02 (интеграционные тесты lifecycle)

### Bridge layer (primary target)
- `Assets/Scripts/ECS/Systems/GameObjectSyncSystem.cs` -- синхронизация ECS -> GameObject, добавить третью ветку для ракет
- `Assets/Scripts/Bridge/ShootEventProcessorSystem.cs` -- обработка событий стрельбы, добавить RocketShootEvent
- `Assets/Scripts/Bridge/DeadEntityCleanupSystem.cs` -- cleanup мёртвых entity с GameObjectRef
- `Assets/Scripts/Bridge/CollisionBridge.cs` -- регистрация коллизий entity

### EntitiesCatalog (factory + registry)
- `Assets/Scripts/Application/EntitiesCatalog.cs` -- фабрика entity с визуалом, паттерн CreateBullet()
- `Assets/Scripts/ECS/EntityFactory.cs` -- создание ECS entity, метод CreateRocket()

### Visual patterns (analogs)
- `Assets/Scripts/View/BulletVisual.cs` -- ближайший аналог для RocketVisual (минимальный Visual с Collider2D)
- `Assets/Scripts/View/ShipVisual.cs` -- источник спрайта (ShipData.MainSprite)
- `Assets/Scripts/Configs/GameData.cs` -- конфигурация entity, добавить RocketData

### ECS components (from Phase 10)
- `Assets/Scripts/ECS/Components/Tags/RocketTag.cs` -- тег ракеты
- `Assets/Scripts/ECS/Components/RocketTargetData.cs` -- данные цели
- `Assets/Scripts/ECS/Components/RocketAmmoData.cs` -- боезапас
- `Assets/Scripts/ECS/Components/MoveData.cs` -- Position, Speed, Direction

### Existing shoot events (pattern reference)
- `Assets/Scripts/ECS/Components/GunShootEvent.cs` -- паттерн события стрельбы
- `Assets/Scripts/ECS/Components/LaserShootEvent.cs` -- паттерн события стрельбы

### Prior phase context
- `.planning/phases/10-ecs-core/10-CONTEXT.md` -- решения Phase 10 по ECS-компонентам ракеты
- `.planning/phases/11-collision-scoring/11-CONTEXT.md` -- решения Phase 11 по коллизиям ракеты

### Test patterns
- `Assets/Tests/EditMode/ECS/AsteroidsEcsTestFixture.cs` -- базовый fixture с helper-методами
- `Assets/Tests/EditMode/ECS/CollisionHandlerTests.cs` -- паттерн тестирования ECS-систем

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `GameObjectSyncSystem` -- уже синхронизирует MoveData -> Transform.position для entity с GameObjectRef, нужна третья ветка для вращения по Direction
- `ShootEventProcessorSystem` -- обрабатывает GunShootEvent и LaserShootEvent, расширяемый для RocketShootEvent
- `DeadEntityCleanupSystem` -- автоматически подхватит ракету через GameObjectRef при DeadTag
- `CollisionBridge` -- регистрация коллизий для entity с визуалом
- `EntitiesCatalog.CreateBullet()` -- точный шаблон для CreateRocket()
- `BulletVisual` -- минимальный Visual с Collider2D, шаблон для RocketVisual
- `GameObjectPool` -- переиспользование объектов, ракета автоматически подхватится через стандартный паттерн
- `EntityFactory.CreateRocket()` -- уже создан в Phase 10

### Established Patterns
- Entity с визуалом: EntityFactory создаёт ECS entity -> EntitiesCatalog добавляет GameObjectRef + регистрирует в CollisionBridge
- GameObjectRef: обязательный компонент для всех entity, имеющих GameObject в сцене
- DynamicBuffer<ShootEvent>: событийная модель передачи данных ECS -> Bridge
- DeadTag -> DeadEntityCleanupSystem: единый путь уничтожения entity с визуалом

### Integration Points
- `EntitiesCatalog` -- добавить CreateRocket(), enum EntityType.Rocket
- `GameObjectSyncSystem` -- добавить третью ветку для RocketTag
- `ShootEventProcessorSystem` -- добавить обработку RocketShootEvent
- `GameData` -- добавить RocketData с ссылкой на префаб
- `EcsRocketAmmoSystem` -- подключить генерацию RocketShootEvent при запуске

</code_context>

<specifics>
## Specific Ideas

No specific requirements -- open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

- Инверсионный след (ParticleSystem trail) -- Phase 14
- Взрыв VFX при попадании -- Phase 14
- Input (кнопка R) для запуска ракеты -- Phase 13
- ScriptableObject конфигурация параметров -- Phase 14
- HUD отображение боезапаса -- Phase 15

</deferred>

---

*Phase: 12-bridge-lifecycle*
*Context gathered: 2026-04-05*
