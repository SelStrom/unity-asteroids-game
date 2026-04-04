# Phase 7: ShipPositionData Wiring + Traceability Fix - Research

**Researched:** 2026-04-03
**Domain:** Unity ECS singleton wiring, production integration gap closure
**Confidence:** HIGH

## Summary

Фаза 7 -- gap closure фаза, выявленная при аудите milestone v1.1.0. Основная проблема: `InitializeEcsSingletons()` в `Application.cs` создает 4 singleton (GameAreaData, ScoreData, GunShootEvent, LaserShootEvent), но **не создает ShipPositionData**. Из-за этого три ECS-системы (`EcsShipPositionUpdateSystem`, `EcsShootToSystem`, `EcsMoveToSystem`) никогда не запускаются в production -- они объявляют `RequireForUpdate<ShipPositionData>()` в `OnCreate`, и без singleton ECS World пропускает их update.

Код систем и тесты уже написаны и работают (Phase 4, TST-07/TST-08 пройдены). Проблема исключительно в wiring -- тестовая фикстура `AsteroidsEcsTestFixture.CreateShipPositionSingleton()` маскировала отсутствие production-инициализации. Дополнительно, требования LC-01..LC-07 (Phase 6 Legacy Cleanup) выполнены по факту (код удален, тесты проходят), но отсутствуют в трассировочной таблице REQUIREMENTS.md.

**Primary recommendation:** Добавить создание ShipPositionData singleton в `InitializeEcsSingletons()`, написать регрессионный тест на наличие singleton после инициализации, обновить трассировочную таблицу REQUIREMENTS.md.

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| ECS-09 | ShootToSystem (AI наведение НЛО) перенесена на ISystem | Система написана (EcsShootToSystem.cs), тесты пройдены. Нужен production wiring -- создание ShipPositionData singleton |
| ECS-10 | MoveToSystem (движение НЛО к цели) перенесена на ISystem | Система написана (EcsMoveToSystem.cs), тесты пройдены. Нужен production wiring -- тот же ShipPositionData singleton |
| LC-01 | Все legacy-системы удалены | Выполнено в Phase 6. Нужно добавить в REQUIREMENTS.md traceability table |
| LC-02 | Legacy-модели и компоненты удалены | Выполнено в Phase 6. Нужно добавить в REQUIREMENTS.md traceability table |
| LC-03 | Переключатель _useEcs удален | Выполнено в Phase 6. Нужно добавить в REQUIREMENTS.md traceability table |
| LC-04 | ActionScheduler выделен из Model | Выполнено в Phase 6. Нужно добавить в REQUIREMENTS.md traceability table |
| LC-05 | Model.cs удален | Выполнено в Phase 6. Нужно добавить в REQUIREMENTS.md traceability table |
| LC-06 | Все тесты проходят зеленым | Выполнено в Phase 6. Нужно добавить в REQUIREMENTS.md traceability table |
| LC-07 | Игра воспроизводит геймплей 1:1 | Блокируется ECS-09/ECS-10 (UFO AI broken). После ShipPositionData fix требует ручной верификации |
</phase_requirements>

## Architecture Patterns

### Текущая singleton-инициализация (паттерн idempotent)

Все singletons в `InitializeEcsSingletons()` используют idempotent паттерн: проверка наличия query, создание если нет, обновление если есть. Это обеспечивает совместимость с PlayMode тестами (повторная инициализация не ломает World).

```csharp
// Паттерн из Application.cs:110-169
var query = _entityManager.CreateEntityQuery(typeof(SomeData));
if (query.CalculateEntityCount() == 0)
{
    var entity = _entityManager.CreateEntity();
    _entityManager.AddComponentData(entity, new SomeData { /* defaults */ });
}
else
{
    var existingEntity = query.GetSingletonEntity();
    _entityManager.SetComponentData(existingEntity, new SomeData { /* defaults */ });
}
```

### ShipPositionData singleton -- требуемый fix

ShipPositionData отличается от других singletons тем, что его значения обновляются каждый кадр через `EcsShipPositionUpdateSystem`. Начальные значения -- нулевые (`Position = float2.zero, Speed = 0, Direction = float2.zero`), что корректно: корабль создается при старте игры, и `EcsShipPositionUpdateSystem` запишет актуальную позицию на первом же кадре.

### Цепочка зависимостей систем

```
EcsMoveSystem
  [UpdateBefore] -> EcsShipPositionUpdateSystem (обновляет ShipPositionData из ShipTag entity)
    [UpdateAfter] -> EcsLifeTimeSystem
EcsShootToSystem (читает ShipPositionData, пишет GunData.Shooting/Direction)
EcsMoveToSystem (читает ShipPositionData, пишет MoveData.Direction)
```

Все три системы имеют `RequireForUpdate<ShipPositionData>()` -- без singleton они silent-skip.

### Game.GetShipPosition() fallback

`Game.cs:150-160` уже обрабатывает отсутствие ShipPositionData gracefully: если query пуст, возвращает `Vector2.zero`. После fix это fallback останется, но будет срабатывать только до первого кадра (пока `EcsShipPositionUpdateSystem` не обновит singleton).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Singleton создание | Custom singleton manager | `EntityManager.AddComponentData` idempotent pattern | Уже используется для 4 других singletons, паттерн проверен |
| Проверка наличия singleton | Runtime assertions | `RequireForUpdate<T>()` в OnCreate | ECS-стандарт, системы auto-skip если singleton отсутствует |

## Common Pitfalls

### Pitfall 1: Забыть idempotent check
**What goes wrong:** Если создать singleton без проверки `CalculateEntityCount() == 0`, повторный вызов `InitializeEcsSingletons()` (при рестарте игры или в PlayMode тестах) создаст дубликат singleton entity, что вызовет exception при `GetSingleton<T>()`.
**How to avoid:** Использовать тот же idempotent паттерн, что и для остальных singletons.

### Pitfall 2: Тест проверяет наличие singleton в тестовом World вместо production
**What goes wrong:** Именно эта проблема привела к текущему gap -- тестовая фикстура создавала singleton, маскируя production wiring gap.
**How to avoid:** Регрессионный тест должен проверять, что `InitializeEcsSingletons()` действительно создает ShipPositionData. Варианты: (a) PlayMode тест на default World после Application.Start(), (b) EditMode тест с мок-EntityManager, вызывающий InitializeEcsSingletons.

### Pitfall 3: Нулевые начальные значения ShipPositionData
**What goes wrong:** До первого кадра EcsShipPositionUpdateSystem, ShipPositionData содержит нули. Если EcsShootToSystem/EcsMoveToSystem выполнятся раньше (ordering не гарантирован для них vs EcsShipPositionUpdateSystem), направление будет рассчитано к (0,0).
**How to avoid:** EcsShipPositionUpdateSystem имеет `[UpdateAfter(typeof(EcsMoveSystem))]` -- он обновляет ShipPositionData после movement. EcsShootToSystem/EcsMoveToSystem не имеют explicit ordering, но они тоже имеют `RequireForUpdate<ShipPositionData>` -- singleton уже должен быть заполнен к моменту UFO спавна (корабль спавнится первым).

## Code Examples

### Fix: ShipPositionData singleton в InitializeEcsSingletons()

```csharp
// Добавить в Application.InitializeEcsSingletons() ПОСЛЕ LaserShootEvent блока
// Файл: Assets/Scripts/Application/Application.cs

// ShipPositionData singleton
var shipPosQuery = _entityManager.CreateEntityQuery(typeof(ShipPositionData));
if (shipPosQuery.CalculateEntityCount() == 0)
{
    var shipPosEntity = _entityManager.CreateEntity();
    _entityManager.AddComponentData(shipPosEntity, new ShipPositionData
    {
        Position = float2.zero,
        Speed = 0f,
        Direction = float2.zero
    });
}
else
{
    var existingEntity = shipPosQuery.GetSingletonEntity();
    _entityManager.SetComponentData(existingEntity, new ShipPositionData
    {
        Position = float2.zero,
        Speed = 0f,
        Direction = float2.zero
    });
}
```

### Регрессионный тест: ShipPositionData singleton создается

```csharp
// Файл: Assets/Tests/EditMode/ECS/SingletonInitTests.cs (или аналогичный)

[Test]
public void InitializeEcsSingletons_CreatesShipPositionData()
{
    // Вызов InitializeEcsSingletons() или прямая проверка
    // что после Application.Start() singleton присутствует
    var query = m_Manager.CreateEntityQuery(typeof(ShipPositionData));
    // После fix: CalculateEntityCount() должен быть > 0
}
```

### Обновление REQUIREMENTS.md: traceability table

```markdown
| LC-01 | Phase 6 | Complete |
| LC-02 | Phase 6 | Complete |
| LC-03 | Phase 6 | Complete |
| LC-04 | Phase 6 | Complete |
| LC-05 | Phase 6 | Complete |
| LC-06 | Phase 6 | Complete |
| LC-07 | Phase 7 | Pending |
```

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Unity Test Framework 1.1.33 (NUnit) |
| Config file | Assets/Tests/EditMode/EditMode.asmdef, Assets/Tests/PlayMode/PlayMode.asmdef |
| Quick run command | Unity Editor -> Window -> General -> Test Runner -> EditMode -> Run All |
| Full suite command | Unity Editor -> Test Runner -> Run All (EditMode + PlayMode) |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| ECS-09 | ShootToSystem runs in production | unit (EditMode) | Test Runner -> ShootToSystemTests | Exists (3 tests) |
| ECS-10 | MoveToSystem runs in production | unit (EditMode) | Test Runner -> MoveToSystemTests | Exists (3 tests) |
| ECS-09+10 | ShipPositionData singleton exists after init | unit (EditMode) | Test Runner -> new regression test | Wave 0 |
| LC-01..LC-06 | Traceability table update | documentation | Manual review | N/A (doc only) |
| LC-07 | Full gameplay 1:1 | manual-only | Human UAT in Unity Editor | N/A |

### Sampling Rate
- **Per task commit:** Запуск EditMode тестов через Test Runner
- **Per wave merge:** Полный suite (EditMode + PlayMode)
- **Phase gate:** Full suite green + Human UAT для LC-07

### Wave 0 Gaps
- [ ] Регрессионный тест на создание ShipPositionData singleton в production init -- covers ECS-09+10 root cause

## Open Questions

1. **Уровень изоляции регрессионного теста**
   - What we know: `InitializeEcsSingletons()` -- private метод. Прямого доступа из теста нет.
   - What's unclear: Тестировать через PlayMode (полная интеграция, дороже) или через EditMode с рефлексией/извлечением метода?
   - Recommendation: EditMode тест, который создает EntityManager, вызывает тот же паттерн creation и проверяет наличие singleton. Это подтверждает паттерн, хотя не тестирует production Application.cs напрямую. Альтернатива -- PlayMode тест, проверяющий singleton в default World после загрузки сцены.

## Project Constraints (from CLAUDE.md)

- Язык ответов и документации -- русский
- Фигурные скобки `{}` обязательны для if/else/for/while, даже с одной строкой тела
- Однострочники запрещены
- При исправлении бага -- обязателен регрессионный тест
- Naming: PascalCase для классов/методов, _camelCase для приватных полей
- Namespace: `SelStrom.Asteroids.ECS` для ECS-кода
- Idempotent ECS singleton initialization для PlayMode test compatibility (решение Phase 6)

## Sources

### Primary (HIGH confidence)
- `Assets/Scripts/Application/Application.cs` -- production InitializeEcsSingletons(), 4 singletons создаются, ShipPositionData отсутствует
- `Assets/Scripts/ECS/Components/ShipPositionData.cs` -- struct определен, 3 поля (Position, Speed, Direction)
- `Assets/Scripts/ECS/Systems/EcsShootToSystem.cs` -- RequireForUpdate<ShipPositionData>, GetSingleton
- `Assets/Scripts/ECS/Systems/EcsMoveToSystem.cs` -- RequireForUpdate<ShipPositionData>, GetSingleton
- `Assets/Scripts/ECS/Systems/EcsShipPositionUpdateSystem.cs` -- RequireForUpdate<ShipPositionData>, SetSingleton
- `Assets/Tests/EditMode/ECS/AsteroidsEcsTestFixture.cs:146-157` -- CreateShipPositionSingleton() helper в тестах
- `.planning/v1.1.0-MILESTONE-AUDIT.md` -- аудит, выявивший gap

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- весь код уже написан, нужна 1 строка wiring
- Architecture: HIGH -- паттерн idempotent singleton creation повторяется 4 раза в том же методе
- Pitfalls: HIGH -- root cause понятен, аудит задокументирован

**Research date:** 2026-04-03
**Valid until:** 2026-05-03 (stable -- Unity ECS API не меняется в рамках Unity 6.3 LTS)
