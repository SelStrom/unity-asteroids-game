# Phase 14: Config & Visual Polish - Research

**Researched:** 2026-04-05
**Domain:** Unity ScriptableObject конфигурация, ParticleSystem trail, VFX переиспользование
**Confidence:** HIGH

## Summary

Фаза 14 охватывает три задачи: (1) вынос hardcoded параметров ракеты в ScriptableObject, (2) добавление инверсионного следа через ParticleSystem, (3) подключение взрыва VFX при уничтожении ракеты. Все три задачи имеют прямые аналоги в существующем коде проекта -- `BulletData` для конфига, `EffectVisual` для взрыва, и паттерн `ParticleSystem` уже используется в VFX.

Кодовая база содержит все необходимые точки интеграции. Hardcoded значения чётко идентифицированы в `EntitiesCatalog.cs` (строки 110-111 и 285-288). `EntityFactory.CreateRocket` не добавляет `ScoreValue` компонент -- это надо исправить для начисления очков. `Application.OnDeadEntity` не обрабатывает `EntityType.Rocket` -- нужна новая ветка.

**Primary recommendation:** Следовать существующим паттернам проекта (BulletData для конфига, EffectVisual для VFX). Расширить `RocketData` struct, обновить `CreateRocket` для чтения из конфига, добавить trail ParticleSystem на префаб через MCP, добавить ветку Rocket в OnDeadEntity.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Расширить `GameData.RocketData` struct полями: `float Speed`, `float LifeTimeSec`, `float TurnRateDegPerSec`, `int MaxAmmo`, `float ReloadDurationSec`, `int Score`
- **D-02:** Убрать hardcoded значения из `EntitiesCatalog.CreateRocket()` (speed: 8f, lifeTime: 5f, turnRateDegPerSec: 180f) -- читать из `_configs.Rocket`
- **D-03:** Убрать hardcoded значения из `EntitiesCatalog.CreateShip()` (rocketMaxAmmo: 3, rocketReloadSec: 5f) -- читать из `_configs.Rocket.MaxAmmo` и `_configs.Rocket.ReloadDurationSec`
- **D-04:** Передать `_configs.Rocket.Score` в EntityFactory.CreateRocket() -- добавить ScoreValue компонент на rocket entity (сейчас отсутствует, нужен для начисления очков)
- **D-05:** Обновить сигнатуру `EntitiesCatalog.CreateRocket()` -- принимать `GameData.RocketData` или отдельные параметры из конфига
- **D-06:** Использовать ParticleSystem (не TrailRenderer) -- лучше интегрируется с ObjectPool, поддерживает Stop/Clear для корректного переиспользования
- **D-07:** Добавить `ParticleSystem` компонент на Rocket префаб как дочерний объект
- **D-08:** В `RocketVisual` добавить `[SerializeField] ParticleSystem _trailEffect` -- Play в `OnConnected()`, Stop+Clear при возврате в пул
- **D-09:** При возврате ракеты в пул: `_trailEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear)` -- предотвращает "хвост" от предыдущего полёта при переиспользовании
- **D-10:** Переиспользовать существующий `VfxBlowPrefab` через `Game.PlayEffect()` -- единообразно с астероидами, UFO и кораблём
- **D-11:** Добавить ветку `EntityType.Rocket` в `Application.OnEntityDestroyed()` -- вызвать `_game.PlayEffect(_configs.VfxBlowPrefab, position)` перед `ReleaseByGameObject`
- **D-12:** Обновить существующий Rocket префаб через MCP -- добавить дочерний ParticleSystem для trail
- **D-13:** Настройка trail ParticleSystem: короткий яркий след, Simulation Space = World (чтобы след оставался на месте при движении ракеты), Stop Action = None
- **D-14:** Если Rocket префаб ещё не создан или не назначен в GameData -- создать через MCP и назначить `GameData.Rocket.Prefab`

### Claude's Discretion
- Конкретные значения по умолчанию в ScriptableObject (speed, turnRate и т.д.) -- использовать текущие hardcoded как отправную точку
- Параметры ParticleSystem trail (размер частиц, lifetime, emission rate, цвет)
- Масштаб взрыва VFX для ракеты (может отличаться от астероидного)

### Deferred Ideas (OUT OF SCOPE)
None -- analysis stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| CONF-01 | Все параметры ракеты задаются через ScriptableObject (скорость, turn rate, боезапас, перезарядка, время жизни, очки) | D-01..D-05: расширение RocketData struct, замена hardcoded в EntitiesCatalog, добавление ScoreValue в EntityFactory |
| VIS-02 | Ракета имеет инверсионный след (ParticleSystem) | D-06..D-09, D-12..D-13: ParticleSystem на префабе, управление lifecycle в RocketVisual |
| VIS-04 | Взрыв при попадании ракеты (переиспользование существующего VFX) | D-10..D-11: ветка Rocket в OnDeadEntity, вызов PlayEffect с VfxBlowPrefab |
</phase_requirements>

## Architecture Patterns

### Точки изменения в коде

```
Assets/Scripts/Configs/GameData.cs          # Расширить RocketData struct (D-01)
Assets/Scripts/Application/EntitiesCatalog.cs  # CreateRocket() и CreateShip() -- убрать hardcoded (D-02, D-03, D-05)
Assets/Scripts/ECS/EntityFactory.cs         # CreateRocket() -- добавить ScoreValue (D-04)
Assets/Scripts/View/RocketVisual.cs         # Добавить trail ParticleSystem support (D-08, D-09)
Assets/Scripts/Application/Application.cs   # OnDeadEntity -- ветка Rocket (D-11)
Assets/Media/prefabs/rocket.prefab          # Дочерний ParticleSystem через MCP (D-07, D-12, D-13)
Assets/Configs/GameData.asset               # Значения в инспекторе через MCP (D-14)
```

### Pattern 1: ScriptableObject Config Struct (аналог BulletData)
**What:** Конфигурация entity через вложенный `[Serializable] struct` в `GameData`
**When to use:** Для всех параметров игровых сущностей
**Существующий аналог:** [VERIFIED: codebase]
```csharp
// Источник: Assets/Scripts/Configs/GameData.cs:10-16
[Serializable]
public struct BulletData
{
    public GameObject Prefab;
    public GameObject EnemyPrefab;
    public int LifeTimeSeconds;
    public float Speed;
}
```
**Целевая структура для RocketData:**
```csharp
[Serializable]
public struct RocketData
{
    public GameObject Prefab;
    public float Speed;
    public float LifeTimeSec;
    public float TurnRateDegPerSec;
    public int MaxAmmo;
    public float ReloadDurationSec;
    public int Score;
}
```

### Pattern 2: VFX через PlayEffect + Pool (аналог астероидов/UFO)
**What:** Воспроизведение эффекта взрыва через пулинг EffectVisual
**Существующий аналог:** [VERIFIED: codebase]
```csharp
// Источник: Assets/Scripts/Application/Application.cs:215-234
// В OnDeadEntity:
if (entityType == EntityType.Asteroid)
{
    _game.PlayEffect(_configs.VfxBlowPrefab, position);
    // ... дробление
}
else if (entityType == EntityType.Ship)
{
    _game.PlayEffect(_configs.VfxBlowPrefab, position);
    _game.StopGame();
}
```
**Нужно добавить:**
```csharp
else if (entityType == EntityType.Rocket)
{
    _game.PlayEffect(_configs.VfxBlowPrefab, position);
}
```

### Pattern 3: ParticleSystem lifecycle в Visual
**What:** Управление ParticleSystem в MonoBehaviour Visual компоненте
**Существующий аналог:** [VERIFIED: codebase]
```csharp
// Источник: Assets/Scripts/View/EffectVisual.cs
// Play в OnConnected, callback на OnParticleSystemStopped
protected override void OnConnected()
{
    _particleSystem.Play();
}
```
**Для RocketVisual -- Play при подключении, Stop+Clear при возврате в пул:**
```csharp
protected override void OnConnected()
{
    _collider.enabled = true;
    _trailEffect.Clear();
    _trailEffect.Play();
}

// Вызывается перед возвратом в пул (через ReleaseByGameObject -> ViewFactory.Release)
private void OnDisable()
{
    _trailEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
}
```

### Anti-Patterns to Avoid
- **Забыть Clear() при переиспользовании из пула:** ParticleSystem сохраняет частицы между активациями. Без `Clear()` при `OnConnected` или `Stop(StopEmittingAndClear)` при деактивации будет виден "хвост" от предыдущей жизни ракеты. [VERIFIED: Unity docs -- ParticleSystem.Clear]
- **Забыть ScoreValue на rocket entity:** Текущий `EntityFactory.CreateRocket` не добавляет `ScoreValue`. Без этого компонента коллизионная система не начислит очки за уничтожение ракетой. [VERIFIED: codebase -- EntityFactory.cs:154-180]
- **Trail в Local space вместо World:** Если `Simulation Space = Local`, trail будет двигаться вместе с ракетой, а не оставаться на месте. Нужен `Simulation Space = World`. [ASSUMED]

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Эффект взрыва | Новый VFX эффект | `VfxBlowPrefab` через `Game.PlayEffect()` | Уже есть пулинг, callback, переиспользуется для всех entity [VERIFIED: codebase] |
| Инверсионный след | Кастомный LineRenderer/код | `ParticleSystem` с `Simulation Space = World` | Встроенная поддержка пулинга (Stop/Clear), GPU-ускорение, настройка через инспектор [ASSUMED] |
| Конфиг параметров | Отдельный JSON/конфиг файл | `GameData` ScriptableObject | Единый паттерн проекта, горячая перезагрузка в инспекторе [VERIFIED: codebase] |

## Common Pitfalls

### Pitfall 1: Отсутствие ScoreValue на rocket entity
**What goes wrong:** Ракета уничтожает врага, но очки не начисляются за саму ракету (если есть система начисления очков за тип оружия)
**Why it happens:** `EntityFactory.CreateRocket` не добавляет `ScoreValue` компонент, в отличие от астероидов и UFO
**How to avoid:** Добавить `em.AddComponentData(entity, new ScoreValue { Score = score })` в `EntityFactory.CreateRocket` и передать `score` параметр
**Warning signs:** Очки за уничтожение ракетой не начисляются при тестировании

### Pitfall 2: Trail от предыдущей жизни при pool reuse
**What goes wrong:** При переиспользовании ракеты из пула видны частицы от предыдущего полёта
**Why it happens:** ParticleSystem сохраняет внутреннее состояние между деактивациями/активациями
**How to avoid:** Вызвать `_trailEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear)` при деактивации И `_trailEffect.Clear(); _trailEffect.Play()` при активации
**Warning signs:** При повторном запуске ракеты виден "всплеск" частиц в начальной позиции

### Pitfall 3: Trail продолжает рендериться после уничтожения ракеты
**What goes wrong:** После попадания ракеты в цель частицы trail продолжают рисоваться в последней позиции
**Why it happens:** `ParticleSystem` с `Stop Action = None` может оставить "зависшие" частицы если объект деактивирован до завершения emission
**How to avoid:** Использовать `StopEmittingAndClear` (не просто `Stop`) при деактивации в `OnDisable`
**Warning signs:** "Призрачные" частицы в случайных точках экрана

### Pitfall 4: Забыть обновить GameData asset через MCP
**What goes wrong:** Код читает новые поля из конфига, но в ассете они имеют дефолтные значения (0 для float/int)
**Why it happens:** Расширение struct добавляет поля с default(T), в ассете нужно руками задать значения
**How to avoid:** Обязательно обновить `GameData.asset` через MCP, выставив значения из текущих hardcoded: Speed=8, LifeTimeSec=5, TurnRateDegPerSec=180, MaxAmmo=3, ReloadDurationSec=5, Score=значение по усмотрению (рекомендуется 50)
**Warning signs:** Ракета не движется (Speed=0) или мгновенно исчезает (LifeTimeSec=0)

### Pitfall 5: Метод OnDeadEntity использует OnDeadEntity, а не OnEntityDestroyed
**What goes wrong:** CONTEXT.md ссылается на `Application.OnEntityDestroyed()`, но в коде метод называется `OnDeadEntity`
**Why it happens:** Несоответствие между именем в CONTEXT.md и фактическим именем в коде
**How to avoid:** Использовать фактическое имя `OnDeadEntity` в `Application.cs:207`
**Warning signs:** Компиляционная ошибка при добавлении ветки

## Code Examples

### Расширение RocketData struct
```csharp
// Целевое состояние GameData.cs
[Serializable]
public struct RocketData
{
    public GameObject Prefab;
    [Space]
    public float Speed;
    public float LifeTimeSec;
    public float TurnRateDegPerSec;
    [Space]
    public int MaxAmmo;
    public float ReloadDurationSec;
    [Space]
    public int Score;
}
```

### Обновление EntityFactory.CreateRocket -- добавить ScoreValue и score параметр
```csharp
// Источник: Assets/Scripts/ECS/EntityFactory.cs:154-180
// Добавить параметр int score и компонент ScoreValue
public static Entity CreateRocket(
    EntityManager em,
    float2 position,
    float speed,
    float2 direction,
    float lifeTime,
    float turnRateDegPerSec,
    int score)
{
    var entity = em.CreateEntity();
    em.AddComponentData(entity, new RocketTag());
    em.AddComponentData(entity, new MoveData
    {
        Position = position,
        Speed = speed,
        Direction = direction
    });
    em.AddComponentData(entity, new LifeTimeData
    {
        TimeRemaining = lifeTime
    });
    em.AddComponentData(entity, new RocketTargetData
    {
        Target = Entity.Null,
        TurnRateDegPerSec = turnRateDegPerSec
    });
    em.AddComponentData(entity, new ScoreValue
    {
        Score = score
    });
    return entity;
}
```

### Обновление EntitiesCatalog.CreateRocket -- читать из конфига
```csharp
// Источник: Assets/Scripts/Application/EntitiesCatalog.cs:272-303
public void CreateRocket(Vector2 position, Vector2 direction)
{
    var viewModel = new RocketViewModel();
    var bindings = new EventBindingContext();
    bindings.InvokeAll();

    var view = _viewFactory.Get<RocketVisual>(_configs.Rocket.Prefab);
    view.Connect(viewModel);

    var entity = EntityFactory.CreateRocket(
        _entityManager,
        new float2(position.x, position.y),
        _configs.Rocket.Speed,
        new float2(direction.x, direction.y),
        _configs.Rocket.LifeTimeSec,
        _configs.Rocket.TurnRateDegPerSec,
        _configs.Rocket.Score
    );
    // ... остальной код без изменений
}
```

### Обновление EntitiesCatalog.CreateShip -- читать rocket ammo из конфига
```csharp
// Источник: Assets/Scripts/Application/EntitiesCatalog.cs:100-112
var entity = EntityFactory.CreateShip(
    _entityManager,
    float2.zero,
    0f,
    _configs.Ship.ThrustUnitsPerSecond,
    _configs.Ship.MaxSpeed,
    _configs.Ship.Gun.MaxShoots,
    _configs.Ship.Gun.ReloadDurationSec,
    _configs.Laser.LaserMaxShoots,
    _configs.Laser.LaserUpdateDurationSec,
    rocketMaxAmmo: _configs.Rocket.MaxAmmo,
    rocketReloadSec: _configs.Rocket.ReloadDurationSec
);
```

### RocketVisual с trail support
```csharp
// Источник: Assets/Scripts/View/RocketVisual.cs -- расширить
public class RocketVisual : AbstractWidgetView<RocketViewModel>, IEntityView
{
    [SerializeField] private Collider2D _collider = default;
    [SerializeField] private ParticleSystem _trailEffect = default;

    protected override void OnConnected()
    {
        _collider.enabled = true;
        if (_trailEffect != null)
        {
            _trailEffect.Clear();
            _trailEffect.Play();
        }
    }

    private void OnDisable()
    {
        if (_trailEffect != null)
        {
            _trailEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        ViewModel.OnCollision.Value?.Invoke(col);
    }
}
```

### Ветка Rocket в OnDeadEntity
```csharp
// Источник: Assets/Scripts/Application/Application.cs:207-238
// Добавить после блока UfoBig/Ufo:
else if (entityType == EntityType.Rocket)
{
    _game.PlayEffect(_configs.VfxBlowPrefab, position);
}
```

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Unity Test Framework 1.1.33 (NUnit) |
| Config file | `Assets/Tests/EditMode/EditMode.asmdef` |
| Quick run command | `unity-mcp tests-run --testMode EditMode --testFilter Rocket` |
| Full suite command | `unity-mcp tests-run --testMode EditMode` |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| CONF-01 | RocketData struct содержит все поля, EntityFactory использует score | unit | `tests-run --testFilter RocketConfig` | Wave 0 |
| CONF-01 | CreateShip читает rocket ammo из конфига | unit | `tests-run --testFilter RocketAmmo` | RocketAmmoSystemTests.cs |
| VIS-02 | Trail ParticleSystem play/stop/clear lifecycle | manual-only | MCP screenshot | N/A -- визуальный тест |
| VIS-04 | VFX взрыв при уничтожении ракеты | manual-only | MCP screenshot | N/A -- визуальный тест |

### Sampling Rate
- **Per task commit:** `tests-run --testMode EditMode --testFilter Rocket`
- **Per wave merge:** `tests-run --testMode EditMode`
- **Phase gate:** Full suite green + MCP visual verification

### Wave 0 Gaps
- [ ] `Assets/Tests/EditMode/ECS/RocketConfigTests.cs` -- тест что EntityFactory.CreateRocket добавляет ScoreValue
- Существующие `RocketAmmoSystemTests.cs` уже покрывают ammo/reload логику (9 тестов)

## MCP Operations Required

Фаза требует обязательных MCP-операций для Unity assets:

### 1. Обновление Rocket префаба -- добавить дочерний ParticleSystem
```
MCP: assets-prefab-open -> gameobject-create (child ParticleSystem) ->
     gameobject-component-modify (настройка ParticleSystem) ->
     assets-prefab-save -> assets-prefab-close
```
Путь: `Assets/Media/prefabs/rocket.prefab` [VERIFIED: codebase]

Параметры trail ParticleSystem (рекомендуемые):
- `Simulation Space`: World
- `Start Lifetime`: 0.3-0.5 сек
- `Start Size`: 0.05-0.1
- `Start Color`: белый или светло-голубой
- `Emission Rate over Time`: 30-50
- `Shape`: Cone, angle 0-5 (узкий поток)
- `Size over Lifetime`: убывающая кривая (от 1 к 0)
- `Color over Lifetime`: fade to transparent
- `Stop Action`: None
- `Play on Awake`: false (управляем вручную)

### 2. Привязка SerializeField на RocketVisual
```
MCP: assets-prefab-open -> gameobject-component-modify (RocketVisual._trailEffect = дочерний PS) ->
     assets-prefab-save -> assets-prefab-close
```

### 3. Обновление GameData.asset -- значения по умолчанию
```
MCP: assets-find (GameData) -> assets-modify / object-modify
```
Значения из текущих hardcoded:
- Speed: 8
- LifeTimeSec: 5
- TurnRateDegPerSec: 180
- MaxAmmo: 3
- ReloadDurationSec: 5
- Score: 50 (рекомендация -- между asteroid small=100 и bullet=0)

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Trail в Local space будет двигаться с ракетой, нужен World | Anti-Patterns | Визуальный баг -- trail "прилипнет" к ракете вместо рисования следа |
| A2 | ParticleSystem GPU-ускорен для trail | Don't Hand-Roll | Низкий -- ParticleSystem всё равно лучше кастомного решения |
| A3 | Score=50 для ракеты -- разумное значение | MCP Operations | Низкий -- легко поменять в инспекторе, не влияет на код |

## Open Questions

1. **Score за попадание ракетой**
   - What we know: ScoreValue добавляется на entity, коллизионная система использует его для начисления очков
   - What's unclear: Какой Score назначить ракете -- это score за само entity "ракета" (если враг как-то его уничтожит?) или это не используется?
   - Recommendation: Поставить Score=0 для ракеты (ракету никто не уничтожает за очки), но добавить ScoreValue для единообразия архитектуры. Очки за уничтоженного врага ракетой начисляются через ScoreValue ВРАГА, не ракеты.

## Sources

### Primary (HIGH confidence)
- Codebase: `GameData.cs` -- текущая структура RocketData (только Prefab)
- Codebase: `EntitiesCatalog.cs:110-111, 282-288` -- hardcoded значения
- Codebase: `EntityFactory.cs:154-180` -- CreateRocket без ScoreValue
- Codebase: `Application.cs:207-238` -- OnDeadEntity без ветки Rocket
- Codebase: `EffectVisual.cs` -- паттерн VFX через ParticleSystem + пул
- Codebase: `RocketVisual.cs` -- текущий минимальный Visual
- Codebase: `Game.cs:216-226` -- PlayEffect + OnEffectStopped
- Codebase: `rocket.prefab` -- существующий префаб
- Codebase: `vfx_blow.prefab` -- существующий VFX эффект

### Secondary (MEDIUM confidence)
- CONTEXT.md D-01..D-14 -- пользовательские решения
- Existing tests: `RocketAmmoSystemTests.cs` -- 9 тестов на ammo/reload

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- все библиотеки уже в проекте, расширяем существующие паттерны
- Architecture: HIGH -- прямые аналоги в кодовой базе (BulletData, EffectVisual)
- Pitfalls: HIGH -- конкретные проблемы идентифицированы через анализ кода

**Research date:** 2026-04-05
**Valid until:** 2026-05-05 (стабильный проект, паттерны не изменятся)
