# Phase 10: ECS Core -- данные и логика ракет - Context

**Gathered:** 2026-04-05 (auto mode)
**Status:** Ready for planning

<domain>
## Phase Boundary

ECS-entity ракеты с полной логикой наведения, боезапаса и перезарядки. Чистая ECS-логика без визуала (визуал -- Phase 12). Покрытие EditMode юнит-тестами.

</domain>

<decisions>
## Implementation Decisions

### Алгоритм наведения
- **D-01:** Простой seek с ограниченным turn rate -- каждый кадр Direction ракеты поворачивается к позиции цели на фиксированную дельту (turnRate * deltaTime), создавая дугообразную траекторию
- **D-02:** Turn rate задаётся через компонент (градусы/сек), аналогично RotateData.TargetDirection -- но вращение автоматическое к цели, а не по вводу игрока
- **D-03:** Ракета летит прямо в текущем Direction с постоянной скоростью (MoveData), наведение влияет только на Direction

### Выбор цели
- **D-04:** Ближайший враг по евклидову расстоянию (без учёта тороидального wrap) -- итерация по всем entity с AsteroidTag, UfoBigTag, UfoTag
- **D-05:** При уничтожении цели (DeadTag на target) -- немедленный пересчёт на следующего ближайшего врага
- **D-06:** Если врагов нет -- ракета летит прямо в текущем Direction

### ECS-компоненты ракеты
- **D-07:** Переиспользовать существующие: MoveData (позиция, скорость, направление), LifeTimeData (время жизни)
- **D-08:** Новый тег: RocketTag (IComponentData маркер, аналогично BulletTag)
- **D-09:** Новый компонент: RocketTargetData -- хранит Entity цели (Entity.Null если цели нет)
- **D-10:** Новый компонент: RocketAmmoData -- на Ship entity: текущий боезапас, макс. боезапас, таймер перезарядки, длительность перезарядки

### Боезапас и перезарядка
- **D-11:** Инкрементальная перезарядка (как LaserSystem) -- одна ракета за период перезарядки
- **D-12:** RocketAmmoData живёт на Ship entity (аналогично GunData/LaserData)
- **D-13:** Перезарядка работает когда CurrentAmmo < MaxAmmo: таймер уменьшается, при достижении 0 -- CurrentAmmo += 1, таймер сбрасывается

### Claude's Discretion
- Конкретная формула поворота Direction к цели (math.atan2 или cross-product подход -- оба Burst-совместимы)
- Порядок новой системы (EcsRocketGuidanceSystem) в update chain
- Нужна ли отдельная система для перезарядки боезапаса или встроить в существующую EcsGunSystem

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` -- ROCK-02..ROCK-06, TEST-01 -- требования на наведение, переключение целей, время жизни, боезапас, перезарядку, тесты

### ECS patterns (existing)
- `Assets/Scripts/ECS/Components/MoveData.cs` -- структура данных движения (Position, Speed, Direction)
- `Assets/Scripts/ECS/Components/LifeTimeData.cs` -- структура времени жизни
- `Assets/Scripts/ECS/Components/GunData.cs` -- пример компонента с боезапасом и перезарядкой
- `Assets/Scripts/ECS/Components/Tags/DeadTag.cs` -- маркер уничтоженной entity
- `Assets/Scripts/ECS/Components/ShipPositionData.cs` -- singleton с позицией корабля
- `Assets/Scripts/ECS/Systems/EcsGunSystem.cs` -- паттерн ISystem с перезарядкой и стрельбой
- `Assets/Scripts/ECS/Systems/EcsShootToSystem.cs` -- паттерн системы с доступом к ShipPositionData
- `Assets/Scripts/ECS/EntityFactory.cs` -- паттерн создания entity (статические методы)
- `Assets/Scripts/ECS/Systems/EcsCollisionHandlerSystem.cs` -- паттерн обработки коллизий и AddScore
- `Assets/Scripts/Bridge/DeadEntityCleanupSystem.cs` -- паттерн cleanup мёртвых entity

### Out of scope decisions
- `.planning/REQUIREMENTS.md` §Out of Scope -- Proportional Navigation исключён, тороидальное наведение исключено

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `MoveData` -- полностью переиспользуется для позиции, скорости и направления ракеты
- `LifeTimeData` + `EcsDeadByLifeTimeSystem` -- переиспользуется для самоуничтожения по таймеру
- `DeadTag` -- единый путь уничтожения entity через DeadEntityCleanupSystem
- `EntityFactory` -- добавить CreateRocket() по аналогии с CreateBullet()
- `EcsShipPositionUpdateSystem` + `ShipPositionData` -- singleton для доступа к позиции корабля в системе наведения

### Established Patterns
- IComponentData struct для данных (без логики, Burst-совместимые)
- partial struct : ISystem для Burst-совместимых систем
- SystemAPI.Query<RefRW<T>>() для итерации по entity
- Tag-компоненты для типизации (ShipTag, BulletTag, AsteroidTag и т.д.)
- UpdateAfter/UpdateBefore атрибуты для порядка систем
- float2 вместо Vector2 (Unity.Mathematics для Burst)
- ECB (EntityCommandBuffer) для structural changes

### Integration Points
- EntityFactory.CreateRocket() -- новый метод
- EcsCollisionHandlerSystem -- Phase 11 добавит обработку Rocket + Enemy (не в этой фазе)
- Ship entity -- получит RocketAmmoData компонент
- DeadEntityCleanupSystem -- уже обрабатывает DeadTag, ракета подхватится автоматически

</code_context>

<specifics>
## Specific Ideas

No specific requirements -- open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

None -- discussion stayed within phase scope

</deferred>

---

*Phase: 10-ecs-core*
*Context gathered: 2026-04-05*
