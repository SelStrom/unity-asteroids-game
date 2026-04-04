# Phase 4: ECS Foundation - Context

**Gathered:** 2026-04-02
**Status:** Ready for planning

<domain>
## Phase Boundary

Создание полного набора Unity DOTS ECS-компонентов (IComponentData) и систем (ISystem) с EditMode TDD-тестами. Все 8 игровых систем (Thrust, Rotate, Move, Gun, Laser, ShootTo, MoveTo, CollisionHandler) перенесены на ISystem. EntityFactory создаёт entities с правильными компонентами. Burst-компиляция применена к чистым системам (Move, Rotate, Thrust). Существующий ECS-подобный код на MonoBehaviour остаётся нетронутым — новый ECS-слой создаётся параллельно, интеграция через Bridge Layer в Phase 5.

</domain>

<decisions>
## Implementation Decisions

### System API Pattern
- **D-01:** Использовать **ISystem** (unmanaged) для всех систем — это современный Unity DOTS API, совместимый с Burst-компиляцией
- **D-02:** Burst-компиляция обязательна для чистых систем: MoveSystem, RotateSystem, ThrustSystem (требования ECS-04/05/06)
- **D-03:** Системы с managed-зависимостями (ShootToSystem, MoveToSystem, GunSystem, LaserSystem, CollisionHandler) реализуются как ISystem без BurstCompile — managed access через SystemAPI

### Component Data Granularity
- **D-04:** 1:1 маппинг существующих компонентов на IComponentData: MoveComponent → MoveData, ThrustComponent → ThrustData, RotateComponent → RotateData, GunComponent → GunData, LaserComponent → LaserData, ShootToComponent → ShootToData, MoveToComponent → MoveToData, LifeTimeComponent → LifeTimeData
- **D-05:** Суффикс `Data` для IComponentData (вместо `Component`) — избежать конфликта имён с существующими MonoBehaviour-компонентами
- **D-06:** Tag-компоненты для типов сущностей: ShipTag, AsteroidTag, BulletTag, UfoTag, UfoBigTag — для фильтрации в запросах
- **D-07:** Компонент AgeData для астероидов (int Age) — используется для логики дробления

### Managed Data Access (AI Systems)
- **D-08:** Позиция корабля доступна через **singleton component** `ShipPosition` — `SystemAPI.GetSingleton<ShipPosition>()` в ShootToSystem и MoveToSystem
- **D-09:** ShipPosition обновляется MoveSystem после перемещения корабля (порядок систем сохраняется как в оригинале)

### Файловая организация
- **D-10:** ECS-код размещается в **`Assets/Scripts/ECS/`** с подкаталогами `Components/`, `Systems/`, `Authoring/`
- **D-11:** Отдельный asmdef `AsteroidsECS` с зависимостью на `Unity.Entities`, `Unity.Burst`, `Unity.Mathematics`, `Unity.Collections`
- **D-12:** Тесты ECS в `Assets/Tests/EditMode/ECS/` с ссылкой на `AsteroidsECS` assembly

### Стратегия коллизий
- **D-13:** CollisionHandler реализуется как ISystem, принимающий коллизионные события через managed buffer/component — конкретный механизм передачи данных из Physics2D определяется в Phase 5 (Bridge Layer)
- **D-14:** В Phase 4 CollisionHandler тестируется с ручным добавлением коллизионных данных в ECS World (mock-подход)

### EntityFactory
- **D-15:** EntityFactory — статический класс или utility, создающий entities через EntityManager с правильным набором компонентов для каждого типа сущности
- **D-16:** Маппинг компонентов по типам сущностей сохраняется из оригинала: Ship → Move+Rotate+Gun+Laser+Thrust, Asteroid → Move+LifeTime(age), Bullet → Move+LifeTime, UfoBig → Move+Gun+ShootTo, Ufo → Move+Gun+ShootTo+MoveTo

### Claude's Discretion
- Конкретная структура IComponentData полей (float2 vs float для позиций, использование Unity.Mathematics)
- Способ реализации системного порядка (UpdateBefore/UpdateAfter атрибуты vs SystemGroup)
- Детали EntityFactory API (методы, параметры конфигурации)
- Подход к тестированию: создание World в SetUp, уничтожение в TearDown, helper-методы для entity creation

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Существующие компоненты (оригинал для 1:1 маппинга)
- `Assets/Scripts/Model/Components/MoveComponent.cs` — Position(Vector2), Speed(float), Direction(Vector2)
- `Assets/Scripts/Model/Components/ThrustComponent.cs` — MinSpeed, UnitsPerSecond, MaxSpeed, IsActive(bool)
- `Assets/Scripts/Model/Components/RotateComponent.cs` — DegreePerSecond, TargetDirection, Rotation(Vector2)
- `Assets/Scripts/Model/Components/GunComponent.cs` — MaxShoots, ReloadDurationSec, CurrentShoots, ReloadRemaining, Shooting, OnShooting
- `Assets/Scripts/Model/Components/LaserComponent.cs` — аналогичен GunComponent с ObservableValue
- `Assets/Scripts/Model/Components/ShootToComponent.cs` — ShipModel reference
- `Assets/Scripts/Model/Components/MoveToComponent.cs` — Every, ReadyRemaining, ShipModel reference
- `Assets/Scripts/Model/Components/LifeTimeComponent.cs` — TimeRemaining

### Существующие системы (логика для портирования)
- `Assets/Scripts/Model/Systems/MoveSystem.cs` — перемещение + тороидальный wrap
- `Assets/Scripts/Model/Systems/ThrustSystem.cs` — физика тяги корабля
- `Assets/Scripts/Model/Systems/RotateSystem.cs` — вращение через Quaternion
- `Assets/Scripts/Model/Systems/GunSystem.cs` — перезарядка и стрельба
- `Assets/Scripts/Model/Systems/LaserSystem.cs` — лазер с инкрементальной перезарядкой
- `Assets/Scripts/Model/Systems/ShootToSystem.cs` — предиктивное прицеливание UFO
- `Assets/Scripts/Model/Systems/MoveToSystem.cs` — перехват корабля малым UFO
- `Assets/Scripts/Model/Systems/LifeTimeSystem.cs` — время жизни пуль
- `Assets/Scripts/Model/Systems/BaseModelSystem.cs` — базовый класс систем

### Модели сущностей (маппинг компонентов)
- `Assets/Scripts/Model/Entities/ShipModel.cs` — Move+Rotate+Thrust+Gun+Laser
- `Assets/Scripts/Model/Entities/AsteroidModel.cs` — Move, Age
- `Assets/Scripts/Model/Entities/BulletModel.cs` — Move+LifeTime
- `Assets/Scripts/Model/Entities/UfoBigModel.cs` — Move+Gun+ShootTo
- `Assets/Scripts/Model/Model.cs` — порядок систем, Visitor-паттерн, GameArea, Update-цикл

### Конфиги (данные для инициализации компонентов)
- `Assets/Scripts/Configs/GameData.cs` — ShipData, BulletData, LaserData
- `Assets/Scripts/Configs/AsteroidData.cs` — Speed, SpriteVariants, Score
- `Assets/Scripts/Configs/UfoData.cs` — Speed, Score
- `Assets/Scripts/Configs/GunData.cs` — MaxShoots, ReloadDurationSec

### Архитектура и анализ
- `.planning/codebase/ARCHITECTURE.md` — полная архитектура, алгоритмы всех систем
- `.planning/codebase/STACK.md` — текущий стек и зависимости
- `.planning/codebase/CONVENTIONS.md` — код-стайл и паттерны

### Требования
- `.planning/REQUIREMENTS.md` §Hybrid DOTS -- ECS Foundation — ECS-01..ECS-11
- `.planning/REQUIREMENTS.md` §Testing (TDD) — TST-01..TST-09

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Assets/Tests/EditMode/` — тестовый assembly уже настроен (Phase 1), NUnit framework готов
- Паттерн тестов `Method_Scenario_Expected` установлен в Phase 1
- `com.unity.burst` 1.8.19 уже установлен как транзитивная зависимость (через 2D feature pack)
- `com.unity.collections` 1.2.4 и `com.unity.mathematics` 1.2.6 уже доступны

### Established Patterns
- 8 систем с чётким порядком обновления: Rotate → Thrust → Move → LifeTime → Gun → Laser → ShootTo → MoveTo
- Components как чистые C# классы с ObservableValue для reactive binding (в ECS заменяется на plain struct fields)
- Visitor-паттерн для регистрации entity в системах (в ECS заменяется на archetype queries)
- GameArea как Vector2 для тороидального wrapping (в ECS — singleton component)

### Integration Points
- `Packages/manifest.json` — добавление `com.unity.entities` пакета (ECS-01)
- `Assets/Scripts/ECS/` — новая директория для всего ECS-кода
- `Assets/Tests/EditMode/ECS/` — тесты ECS-компонентов и систем
- Phase 5 Bridge Layer будет связывать ECS entities с существующими GameObjects

</code_context>

<specifics>
## Specific Ideas

- Существующий код содержит известные баги (wrapping, UFO kill, division by zero) — они воспроизводятся 1:1 в ECS-версии для функциональной эквивалентности
- ShootToSystem хардкодит скорость пули 20 — в ECS версии аналогично (out of scope для рефакторинга)
- ObservableValue в компонентах заменяется plain struct fields в IComponentData — reactive binding через Bridge Layer (Phase 5)
- ActionScheduler не переносится в ECS — это utility уровня приложения, остаётся в managed коде

</specifics>

<deferred>
## Deferred Ideas

- **Исправление багов** (wrapping formula, UFO kill, division by zero в ShootTo/MoveTo) — отложено на отдельный milestone (QUAL-01)
- **Рефакторинг хардкодов** (скорость пули 20, MoveToComponent.Every = 3f, максимальная скорость осколков 10f) — не часть миграции 1:1

</deferred>

---

*Phase: 04-ecs-foundation*
*Context gathered: 2026-04-02*
