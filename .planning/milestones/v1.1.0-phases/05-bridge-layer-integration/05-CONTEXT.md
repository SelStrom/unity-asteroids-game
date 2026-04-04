# Phase 5: Bridge Layer + Integration - Context

**Gathered:** 2026-04-03
**Status:** Ready for planning

<domain>
## Phase Boundary

Интеграция ECS-слоя (Phase 4) с существующими GameObjects и MonoBehaviour-визуалами. Bridge Layer связывает Entity с GameObject: синхронизация позиции/ротации из ECS в Transform, передача Physics2D коллизий в ECS World, трансляция ECS-данных в ObservableValue для shtl-mvvm UI, синхронизация жизненного цикла Entity↔GameObject. Итог — полностью работающая игра на гибридном DOTS с геймплеем 1:1.

</domain>

<decisions>
## Implementation Decisions

### GameObject↔Entity Binding
- **D-01:** Managed component `GameObjectRef` (ICleanupComponentData) хранит ссылку на Transform привязанного GameObject — стандартный Unity DOTS hybrid-паттерн
- **D-02:** Обратный маппинг `Dictionary<GameObject, Entity>` поддерживается в managed-коде для быстрого разрешения Entity по GameObject (O(1) lookup при коллизиях)
- **D-03:** GameObjectRef добавляется в EntityFactory при создании entity — каждый entity, имеющий визуальное представление, получает этот компонент

### ECS→Transform Sync
- **D-04:** `GameObjectSyncSystem` (managed ISystem) синхронизирует позицию и ротацию из MoveData/RotateData в Transform **каждый кадр** для всех entities с GameObjectRef
- **D-05:** Синхронизация без change filter — количество entities мало (~20-50), overhead минимален, простота реализации важнее оптимизации
- **D-06:** Порядок систем: ECS-системы (Rotate→Thrust→Move...) → GameObjectSyncSystem → рендер Unity

### Collision Bridge
- **D-07:** Существующие MonoBehaviour-визуалы (ShipVisual, BulletVisual, UfoVisual) сохраняют `OnCollisionEnter2D` — Physics2D коллизии остаются на стороне GameObjects
- **D-08:** `CollisionBridge` — managed компонент или utility, вызываемый из OnCollisionEnter2D, разрешает Entity через обратный маппинг GameObject→Entity и записывает `CollisionEventData` в singleton DynamicBuffer
- **D-09:** EcsCollisionHandlerSystem (уже реализован в Phase 4) обрабатывает CollisionEventData буфер без изменений — интерфейс совместим

### Observable Bridge (ECS→MVVM)
- **D-10:** `ObservableBridgeSystem` (managed ISystem) читает ECS-компоненты и пушит значения в ReactiveValue/ObservableValue каждый кадр
- **D-11:** Бридж покрывает: ScoreData→ScoreViewModel, GunData/LaserData→HudData (заряды, перезарядка), MoveData→HudData (координаты, скорость), RotateData→HudData (угол поворота), ThrustData→ShipViewModel (спрайт тяги)
- **D-12:** Бридж заменяет текущие EventBindingContext привязки из EntitiesCatalog — данные теперь идут из ECS, а не из Model-компонентов

### Lifecycle Orchestration
- **D-13:** EntitiesCatalog остаётся оркестратором создания — создаёт и Entity (через EntityFactory), и GameObject, поддерживает двунаправленный маппинг
- **D-14:** `DeadTag` в ECS (устанавливается CollisionHandler/LifeTimeSystem) триггерит cleanup: GameObjectSyncSystem или отдельная `DeadEntityCleanupSystem` обнаруживает DeadTag, вызывает EntitiesCatalog.Release, уничтожает Entity
- **D-15:** Существующая логика Game.cs (спавн врагов, дробление астероидов, старт/стоп) сохраняется с минимальными изменениями — Game.cs переключается с Model.Update() на ECS World Update
- **D-16:** ActionScheduler остаётся в managed-коде (решение из Phase 4) — спавн врагов по таймеру через EntitiesCatalog

### Migration Strategy
- **D-17:** Поэтапная замена: сначала Bridge Layer работает параллельно со старым Model-слоем для верификации, затем старый Model-слой отключается
- **D-18:** Существующие MonoBehaviour-системы (Model, BaseModelSystem и наследники) не удаляются — отключаются через флаг, чтобы при необходимости можно было вернуться

### Pending Phase 4 Items (ECS-07, ECS-08)
- **D-19:** GunSystem (ECS-07) и LaserSystem (ECS-08) из Phase 4 остались нереализованными — они ДОЛЖНЫ быть завершены в Phase 5 перед интеграцией, так как Bridge Layer требует полного ECS-слоя
- **D-20:** TST-05 (тесты GunSystem) и TST-06 (тесты LaserSystem) также реализуются в Phase 5

### Claude's Discretion
- Конкретная реализация обратного маппинга (static dictionary, singleton managed component, или часть EntitiesCatalog)
- Порядок инициализации ECS World относительно ApplicationEntry.Awake
- Детали миграционного флага (bool в Game.cs, ScriptableObject, или define)
- Подход к PlayMode-тестам (TST-12): сценарии, длительность, assertions

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### ECS-слой (Phase 4 output)
- `Assets/Scripts/ECS/EntityFactory.cs` — фабрика создания entities с компонентами
- `Assets/Scripts/ECS/Components/` — все IComponentData (MoveData, RotateData, ThrustData, GunData, LaserData, ShootToData, MoveToData, etc.)
- `Assets/Scripts/ECS/Components/Tags/` — tag-компоненты (ShipTag, AsteroidTag, DeadTag, PlayerBulletTag, etc.)
- `Assets/Scripts/ECS/Components/CollisionEventData.cs` — IBufferElementData для коллизий
- `Assets/Scripts/ECS/Components/ScoreData.cs` — singleton для очков
- `Assets/Scripts/ECS/Systems/` — все ECS-системы (EcsMoveSystem, EcsRotateSystem, EcsThrustSystem, EcsCollisionHandlerSystem, etc.)

### Существующий GameObject-слой (точки интеграции)
- `Assets/Scripts/Application/EntitiesCatalog.cs` — оркестратор создания entity↔view, двунаправленные маппинги, Release/CleanUp
- `Assets/Scripts/Application/Game.cs` — игровой процесс, спавн, коллизии, ActionScheduler
- `Assets/Scripts/Application/Application.cs` — корневой объект, создание подсистем, Update-цикл
- `Assets/Scripts/View/ShipVisual.cs` — пример MVVM-привязки (Position→Transform, Rotation→Quaternion, OnCollisionEnter2D)
- `Assets/Scripts/View/HudVisual.cs` — HUD с ReactiveValue привязками (координаты, скорость, лазер)
- `Assets/Scripts/View/BulletVisual.cs` — пуля с OnCollisionEnter2D
- `Assets/Scripts/View/UfoVisual.cs` — UFO с OnCollisionEnter2D

### MVVM-фреймворк
- `Packages/com.shtl.mvvm/` — ObservableValue, ReactiveValue, EventBindingContext, AbstractWidgetView

### Существующие компоненты Model (заменяются ECS)
- `Assets/Scripts/Model/Model.cs` — порядок систем, Update-цикл (будет заменён ECS World)
- `Assets/Scripts/Model/Systems/` — все 8 систем (MoveSystem, RotateSystem, etc.)
- `Assets/Scripts/Model/Components/` — ObservableValue-компоненты (заменяются IComponentData)

### Конфиги
- `Assets/Scripts/Configs/GameData.cs` — ShipData, BulletData, LaserData, SpawnNewEnemyDurationSec
- `Assets/Scripts/Configs/AsteroidData.cs`, `UfoData.cs`, `GunData.cs` — данные сущностей

### Архитектура и требования
- `.planning/REQUIREMENTS.md` §Hybrid DOTS -- Bridge Layer — BRG-01..BRG-06
- `.planning/REQUIREMENTS.md` §Testing (TDD) — TST-10, TST-12
- `.planning/REQUIREMENTS.md` §Hybrid DOTS -- ECS Foundation — ECS-07, ECS-08 (pending)
- `.planning/phases/04-ecs-foundation/04-CONTEXT.md` — решения Phase 4, D-13/D-14 (коллизии через bridge)
- `.planning/codebase/ARCHITECTURE.md` — полная архитектура и алгоритмы

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `EntityFactory` (Phase 4) — создаёт entities с полным набором компонентов, расширяется добавлением GameObjectRef
- `EntitiesCatalog` — двунаправленные маппинги model↔view, Release/CleanUp — паттерн переиспользуется для entity↔gameObject
- `AsteroidsEcsTestFixture` — базовый тестовый класс с World setup/teardown
- `EcsCollisionHandlerSystem` — полностью реализован, принимает CollisionEventData buffer
- Все ECS-системы Phase 4 (Move, Rotate, Thrust, LifeTime, ShootTo, MoveTo) — готовы к интеграции
- `GameObjectPool` — пул GameObject'ов по prefab ID, продолжает использоваться для визуалов

### Established Patterns
- OnCollisionEnter2D в визуалах (ShipVisual, BulletVisual, UfoVisual) → callback в Game.cs — CollisionBridge подключается сюда
- EventBindingContext + From().To() для MVVM-привязок — ObservableBridge заменяет источник данных с Model на ECS
- Visitor-паттерн + GroupCreator для регистрации entity в системах — в ECS заменяется архетипными запросами
- Model.Update(deltaTime) вызывается из ApplicationEntry — заменяется на World.Update()
- Порядок систем: Rotate → Thrust → Move → LifeTime → Gun → Laser → ShootTo → MoveTo

### Integration Points
- `ApplicationEntry.cs` — точка подключения ECS World, инициализация Bridge-систем
- `EntitiesCatalog.Create*()` — расширяются для создания Entity параллельно с GameObject
- `Game.OnShipCollided/OnUserBulletCollided/OnUfoCollided` — переключаются на CollisionBridge
- `GameScreen.Connect()` — HudData привязки переключаются на ObservableBridge

</code_context>

<specifics>
## Specific Ideas

- Phase 4 оставила нереализованными ECS-07 (GunSystem) и ECS-08 (LaserSystem) — они завершаются в начале Phase 5 как prerequisite для интеграции
- Существующие баги (wrapping, UFO kill) воспроизводятся 1:1 — Bridge Layer не исправляет логику, только переключает источник данных
- ActionScheduler остаётся в managed-коде — ECS не имеет аналога для delayed actions с произвольными callback'ами
- Physics2D остаётся на GameObjects (DOTS Physics 2D не production-ready) — коллизии проксируются через bridge
- DeadTag паттерн уже реализован в EcsCollisionHandlerSystem — cleanup-система подхватывает entities с DeadTag

</specifics>

<deferred>
## Deferred Ideas

- **Полный переход на DOTS Physics** — пакет не production-ready, Physics2D на GameObjects достаточен (Out of Scope)
- **Entities Graphics для рендеринга** — не поддерживает SpriteRenderer и WebGL (Out of Scope)
- **Удаление старого Model-слоя** — отключается флагом в Phase 5, полное удаление в будущем cleanup milestone
- **Оптимизация sync с change filter** — при текущем количестве entities (~20-50) не нужна

</deferred>

---

*Phase: 05-bridge-layer-integration*
*Context gathered: 2026-04-03*
