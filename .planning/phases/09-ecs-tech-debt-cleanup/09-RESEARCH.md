# Phase 9: ECS Tech Debt Cleanup - Research

**Researched:** 2026-04-04
**Domain:** Unity ECS (Entities 1.x) system ordering, MVVM bindings cleanup, git hygiene
**Confidence:** HIGH

## Summary

Phase 9 -- финальная фаза milestone v1.1.0, устраняющая 6 элементов tech debt, выявленных при milestone audit. Все изменения -- точечные правки существующих файлов без добавления нового функционала. Риск минимален: каждое изменение либо добавляет атрибут, либо удаляет неиспользуемый код, либо коммитит уже существующие файлы.

Ключевые области: (1) ECS system ordering через `[UpdateAfter]`/`[UpdateBefore]` атрибуты, (2) удаление vestigial полей из `ShootToData`, (3) удаление dead MVVM bindings из non-ship ViewModel, (4) устранение двойной записи Ship Transform, (5) коммит .meta файлов.

**Primary recommendation:** Выполнять как 1 план с 6 задачами -- каждое изменение независимо и малого объёма. Все тесты должны пройти зелёным после каждого изменения.

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| TD-01 | EcsGunSystem и EcsLaserSystem имеют раскомментированные [UpdateAfter]/[UpdateBefore] ordering-атрибуты | System ordering chain полностью задокументирован, конкретные атрибуты определены |
| TD-02 | EcsShootToSystem и EcsMoveToSystem имеют [UpdateAfter(EcsShipPositionUpdateSystem)] | EcsShipPositionUpdateSystem уже имеет ordering-атрибуты, нужно только добавить [UpdateAfter] к двум AI-системам |
| TD-03 | ShootToData не содержит неиспользуемых полей ReadyRemaining/Every | Поля подтверждены как vestigial -- EcsShootToSystem НЕ читает их, в отличие от MoveToData/EcsMoveToSystem |
| TD-04 | Non-ship ViewModel классы не содержат dead Position binding | AsteroidViewModel, BulletViewModel, UfoViewModel содержат ReactiveValue Position, но Transform пишется GameObjectSyncSystem |
| TD-05 | Ship Transform пишется одним путём | Двойная запись: GameObjectSyncSystem (Position+Rotation) + ObservableBridgeSystem -> ShipVisual (Position+Rotation через ViewModel). Нужно убрать один путь |
| TD-06 | Все .meta файлы из Assets/Tests/ закоммичены | 3 untracked .meta файла обнаружены в git status |
</phase_requirements>

## Project Constraints (from CLAUDE.md)

- **Язык:** Документация и комментарии на русском
- **Фигурные скобки:** Всегда использовать `{}` в операторах ветвления и циклах, даже для одной строки
- **Тестирование:** При исправлении бага -- обязателен регрессионный тест
- **Namespace:** `SelStrom.Asteroids` для основного кода, `SelStrom.Asteroids.ECS` для ECS-компонентов/систем
- **Naming:** PascalCase для классов, `_camelCase` для приватных полей
- **GSD Workflow:** Не делать прямых правок в репо вне GSD workflow

## Architecture Patterns

### Текущая цепочка System Ordering (до фазы 9)

```
EcsRotateSystem
  [UpdateBefore(EcsThrustSystem)]
      |
EcsThrustSystem
  [UpdateAfter(EcsRotateSystem)]
  [UpdateBefore(EcsMoveSystem)]
      |
EcsMoveSystem
  [UpdateAfter(EcsThrustSystem)]
  [UpdateBefore(EcsShipPositionUpdateSystem)]
      |
EcsShipPositionUpdateSystem
  [UpdateAfter(EcsMoveSystem)]
  [UpdateBefore(EcsLifeTimeSystem)]
      |
EcsLifeTimeSystem
  [UpdateAfter(EcsShipPositionUpdateSystem)]
      |
EcsDeadByLifeTimeSystem
  [UpdateAfter(EcsLifeTimeSystem)]

--- Без ordering (нужно добавить) ---
EcsGunSystem          -- нет ordering-атрибутов
EcsLaserSystem        -- нет ordering-атрибутов
EcsShootToSystem      -- нет ordering-атрибутов
EcsMoveToSystem       -- нет ordering-атрибутов
EcsCollisionHandlerSystem -- нет ordering-атрибутов

--- PresentationSystemGroup ---
GameObjectSyncSystem      [UpdateInGroup(PresentationSystemGroup)]
ObservableBridgeSystem    [UpdateInGroup(PresentationSystemGroup)]
```

### Целевая цепочка System Ordering (после фазы 9)

```
EcsRotateSystem
      |
EcsThrustSystem
      |
EcsMoveSystem
      |
EcsShipPositionUpdateSystem
      |
EcsGunSystem              [UpdateAfter(EcsShipPositionUpdateSystem)]
  |                        [UpdateBefore(EcsLaserSystem)]
EcsLaserSystem            [UpdateAfter(EcsGunSystem)]
      |
EcsShootToSystem          [UpdateAfter(EcsShipPositionUpdateSystem)]
EcsMoveToSystem           [UpdateAfter(EcsShipPositionUpdateSystem)]
      |
EcsLifeTimeSystem
      |
EcsDeadByLifeTimeSystem
```

**Обоснование ordering для Gun/Laser:**
- EcsGunSystem должен обработать `Shooting` флаг ДО EcsLaserSystem, чтобы оба не стреляли в одном кадре (Gun приоритетнее)
- Оба должны быть после EcsShipPositionUpdateSystem, чтобы ShootPosition был актуальным

**Обоснование ordering для ShootTo/MoveTo:**
- Обе системы читают `ShipPositionData` singleton
- `EcsShipPositionUpdateSystem` обновляет этот singleton из `MoveData` корабля
- Без `[UpdateAfter]` ShipPositionData может быть устаревшим на 1 кадр

### Pattern: Ship Transform -- единый путь записи

**Текущее состояние (двойная запись):**
1. `GameObjectSyncSystem` (PresentationSystemGroup) -- пишет `position` и `rotation` в Transform для ВСЕХ entities с GameObjectRef
2. `ObservableBridgeSystem` (PresentationSystemGroup) -- пишет `Position` и `Rotation` в ShipViewModel -> ShipVisual привязывает к Transform

**Решение:** Убрать запись Position в Transform из ShipVisual. ObservableBridgeSystem должен продолжать обновлять ShipViewModel.Rotation (для конвертации Vector2->Quaternion) и ShipViewModel.Sprite (для переключения спрайтов thrust). Но ShipViewModel.Position может быть удалён -- GameObjectSyncSystem уже пишет Position напрямую в Transform.

**Важно:** ShipVisual.OnRotationChanged конвертирует Vector2 -> angle -> Quaternion.Euler. Но GameObjectSyncSystem тоже делает эту конвертацию. Таким образом, можно убрать из ObservableBridgeSystem запись Position и Rotation в ShipViewModel, оставив только Sprite (thrust sprite switch) и HUD-данные.

### Pattern: Dead MVVM Bindings

**Текущее состояние:** AsteroidViewModel, BulletViewModel, UfoViewModel содержат `ReactiveValue<Vector2> Position`, и их Visual классы привязывают `Bind.From(ViewModel.Position).To(transform)`. Однако Position НИКОГДА не устанавливается -- Transform обновляется напрямую через GameObjectSyncSystem.

**Решение:** Удалить `Position` из AsteroidViewModel, BulletViewModel, UfoViewModel. Удалить `Bind.From(ViewModel.Position).To(transform)` из соответствующих Visual классов.

### Pattern: Vestigial ShootToData Fields

**Текущее состояние:**
- `ShootToData` содержит поля `Every` и `ReadyRemaining`
- `EntityFactory` устанавливает их при создании UfoBig и Ufo entities
- `EcsShootToSystem` запрашивает `RefRO<ShootToData>` но НЕ читает ни одного поля -- стреляет каждый кадр при наличии патронов

**Сравнение с MoveToData:** `MoveToData` тоже имеет `Every`/`ReadyRemaining`, и `EcsMoveToSystem` АКТИВНО их использует для таймера перенацеливания.

**Решение:** Удалить поля `Every` и `ReadyRemaining` из `ShootToData`. Обновить `EntityFactory` (убрать инициализацию этих полей). `ShootToData` станет пустым маркерным компонентом -- можно либо оставить пустую структуру, либо заменить на `IComponentData` tag. Лучше оставить пустую структуру `ShootToData` -- она используется как фильтр в запросе EcsShootToSystem.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| System ordering | Ручной вызов Update в определённом порядке | `[UpdateAfter]`/`[UpdateBefore]` атрибуты | Unity Entities scheduler автоматически сортирует |

## Common Pitfalls

### Pitfall 1: Удаление ShipViewModel.Position ломает ObservableBridgeSystem
**What goes wrong:** ObservableBridgeSystem пишет в `_shipViewModel.Position.Value` (строка 79). Если удалить поле -- ошибка компиляции.
**How to avoid:** Убрать запись Position из ObservableBridgeSystem одновременно с удалением поля из ShipViewModel.

### Pitfall 2: Удаление Bind.From(Position).To(transform) из ShipVisual ломает Rotation
**What goes wrong:** Можно случайно удалить привязку Rotation вместе с Position.
**How to avoid:** ShipVisual должен СОХРАНИТЬ `ViewModel.Rotation.Connect(OnRotationChanged)` -- или удалить и его, если GameObjectSyncSystem уже пишет rotation. Проверка: GameObjectSyncSystem пишет rotation для всех entities с RotateData -- Ship имеет RotateData, значит rotation уже пишется. Можно удалить оба.

### Pitfall 3: ShipVisual.Sprite привязка зависит от ObservableBridgeSystem
**What goes wrong:** Если удалить ObservableBridgeSystem полностью, перестанет работать переключение спрайтов ship (thrust on/off) и HUD.
**How to avoid:** ObservableBridgeSystem нужен для HUD-данных и Sprite. Удаляем только запись Position и Rotation в ShipViewModel.

### Pitfall 4: ShootToData как пустой struct
**What goes wrong:** Unity ECS может предупредить о пустых IComponentData.
**How to avoid:** Пустая struct IComponentData допустима и широко используется как tag component (аналогично ShipTag, AsteroidTag). Предупреждений не будет.

### Pitfall 5: Circular ordering dependency
**What goes wrong:** Добавление [UpdateAfter] может создать циклическую зависимость, и Unity выдаст ошибку при bootstrap.
**How to avoid:** EcsGunSystem и EcsLaserSystem не участвуют в существующей цепочке. Добавление [UpdateAfter(EcsShipPositionUpdateSystem)] безопасно -- нет обратных зависимостей.

## Code Examples

### TD-01: Ordering для EcsGunSystem

```csharp
// Assets/Scripts/ECS/Systems/EcsGunSystem.cs
using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    [UpdateAfter(typeof(EcsShipPositionUpdateSystem))]
    [UpdateBefore(typeof(EcsLaserSystem))]
    public partial struct EcsGunSystem : ISystem
    {
        // ... без изменений в теле
    }
}
```

### TD-01: Ordering для EcsLaserSystem

```csharp
// Assets/Scripts/ECS/Systems/EcsLaserSystem.cs
using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    [UpdateAfter(typeof(EcsGunSystem))]
    public partial struct EcsLaserSystem : ISystem
    {
        // ... без изменений в теле
    }
}
```

### TD-02: Ordering для AI-систем

```csharp
// Assets/Scripts/ECS/Systems/EcsShootToSystem.cs
[UpdateAfter(typeof(EcsShipPositionUpdateSystem))]
public partial struct EcsShootToSystem : ISystem { ... }

// Assets/Scripts/ECS/Systems/EcsMoveToSystem.cs
[UpdateAfter(typeof(EcsShipPositionUpdateSystem))]
public partial struct EcsMoveToSystem : ISystem { ... }
```

### TD-03: Очистка ShootToData

```csharp
// Assets/Scripts/ECS/Components/ShootToData.cs
using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    public struct ShootToData : IComponentData
    {
        // Маркерный компонент -- поля Every/ReadyRemaining удалены (vestigial)
    }
}
```

### TD-04: Очистка non-ship ViewModel (пример AsteroidViewModel)

```csharp
public class AsteroidViewModel : AbstractViewModel
{
    // Position удалён -- Transform пишется GameObjectSyncSystem
    public readonly ReactiveValue<Sprite> Sprite = new();
    public readonly ReactiveValue<Action<Collision2D>> OnCollision = new();
}

public class AsteroidVisual : AbstractWidgetView<AsteroidViewModel>, IEntityView
{
    [SerializeField] private SpriteRenderer _spriteRenderer = default;

    protected override void OnConnected()
    {
        // Bind.From(ViewModel.Position).To(transform) удалён
        ViewModel.Sprite.Connect(sprite => _spriteRenderer.sprite = sprite);
    }
    // ...
}
```

### TD-05: Устранение двойной записи Ship Transform

В ObservableBridgeSystem удалить строки записи Position и Rotation в ShipViewModel:
```csharp
// УДАЛИТЬ из OnUpdate():
// _shipViewModel.Position.Value = new Vector2(pos.x, pos.y);
// _shipViewModel.Rotation.Value = new Vector2(rot.x, rot.y);

// ОСТАВИТЬ:
if (_mainSprite != null && _thrustSprite != null)
{
    _shipViewModel.Sprite.Value =
        thrust.ValueRO.IsActive ? _thrustSprite : _mainSprite;
}
```

В ShipVisual удалить привязку Position (и Rotation, т.к. GameObjectSyncSystem уже пишет rotation):
```csharp
protected override void OnConnected()
{
    // Bind.From(ViewModel.Position).To(transform) -- УДАЛИТЬ
    // ViewModel.Rotation.Connect(OnRotationChanged) -- УДАЛИТЬ
    ViewModel.Sprite.Connect(sprite => _spriteRenderer.sprite = sprite);
}
// OnRotationChanged метод -- УДАЛИТЬ
```

Из ShipViewModel удалить поля Position и Rotation:
```csharp
public class ShipViewModel : AbstractViewModel
{
    // Position и Rotation удалены -- Transform пишется GameObjectSyncSystem
    public readonly ReactiveValue<Sprite> Sprite = new();
    public readonly ReactiveValue<Action<Collision2D>> OnCollision = new();
}
```

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Unity Test Framework 1.1.33 (NUnit) |
| Config file | `Assets/Tests/EditMode/ECS/EcsEditModeTests.asmdef`, `Assets/Tests/EditMode/EditModeTests.asmdef` |
| Quick run command | Unity Editor: Run EditMode Tests |
| Full suite command | Unity Editor: Run All Tests (EditMode + PlayMode) |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| TD-01 | Gun/Laser ordering attributes | unit (compile-time) | Компиляция проекта в Unity Editor | N/A -- атрибуты верифицируются компиляцией |
| TD-02 | ShootTo/MoveTo ordering | unit (compile-time) | Компиляция проекта в Unity Editor | N/A -- атрибуты верифицируются компиляцией |
| TD-03 | ShootToData vestigial fields removed | unit | Существующий `ShootToSystemTests.cs` должен пройти | ShootToSystemTests.cs -- обновить |
| TD-04 | Dead Position binding removed | unit | Существующие тесты + компиляция | N/A -- компиляция верифицирует |
| TD-05 | Ship single write path | unit | `GameObjectSyncSystemTests.cs` + `ObservableBridgeSystemTests.cs` | Существуют |
| TD-06 | .meta files committed | manual | `git status Assets/Tests/` | N/A |

### Sampling Rate
- **Per task commit:** Запуск всех EditMode тестов
- **Per wave merge:** Полный набор (EditMode + PlayMode)
- **Phase gate:** Full suite green перед `/gsd:verify-work`

### Wave 0 Gaps
- [ ] Обновить `ShootToSystemTests.cs` -- убрать ссылки на `ReadyRemaining`/`Every` из ShootToData
- [ ] Обновить `EntityFactoryTests.cs` -- убрать проверки ShootToData.Every/ReadyRemaining если есть
- [ ] Проверить `ObservableBridgeSystemTests.cs` -- убрать проверки записи Position/Rotation в ShipViewModel

## Файлы, затрагиваемые фазой 9

### Изменяемые файлы (production)
| Файл | Изменение | Req |
|------|-----------|-----|
| `Assets/Scripts/ECS/Systems/EcsGunSystem.cs` | Добавить `[UpdateAfter]`/`[UpdateBefore]` | TD-01 |
| `Assets/Scripts/ECS/Systems/EcsLaserSystem.cs` | Добавить `[UpdateAfter]` | TD-01 |
| `Assets/Scripts/ECS/Systems/EcsShootToSystem.cs` | Добавить `[UpdateAfter]` | TD-02 |
| `Assets/Scripts/ECS/Systems/EcsMoveToSystem.cs` | Добавить `[UpdateAfter]` | TD-02 |
| `Assets/Scripts/ECS/Components/ShootToData.cs` | Удалить поля Every/ReadyRemaining | TD-03 |
| `Assets/Scripts/ECS/EntityFactory.cs` | Убрать инициализацию ShootToData полей | TD-03 |
| `Assets/Scripts/View/AsteroidVisual.cs` | Удалить Position из ViewModel, Bind из Visual | TD-04 |
| `Assets/Scripts/View/BulletVisual.cs` | Удалить Position из ViewModel, Bind из Visual | TD-04 |
| `Assets/Scripts/View/UfoVisual.cs` | Удалить Position из ViewModel, Bind из Visual | TD-04 |
| `Assets/Scripts/View/ShipVisual.cs` | Удалить Position/Rotation из ViewModel, Bind/Connect из Visual | TD-05 |
| `Assets/Scripts/Bridge/ObservableBridgeSystem.cs` | Удалить запись Position/Rotation в ShipViewModel | TD-05 |

### Изменяемые файлы (тесты)
| Файл | Изменение | Req |
|------|-----------|-----|
| `Assets/Tests/EditMode/ECS/ShootToSystemTests.cs` | Обновить создание ShootToData (пустая struct) | TD-03 |
| `Assets/Tests/EditMode/ECS/EntityFactoryTests.cs` | Обновить проверки ShootToData | TD-03 |
| `Assets/Tests/EditMode/ECS/ObservableBridgeSystemTests.cs` | Обновить проверки (без Position/Rotation) | TD-05 |

### Файлы для git add (без изменений)
| Файл | Req |
|------|-----|
| `Assets/Tests/EditMode/ECS/LegacyCleanupValidationTests.cs.meta` | TD-06 |
| `Assets/Tests/EditMode/ECS/SingletonInitTests.cs.meta` | TD-06 |
| `Assets/Tests/EditMode/ShtlMvvm/Phase01InfraValidationTests.cs.meta` | TD-06 |

## Open Questions

1. **EcsGunSystem ordering: [UpdateBefore(EcsLaserSystem)] нужен ли?**
   - Что мы знаем: В оригинальном коде Gun и Laser -- независимые системы. EcsShootToSystem устанавливает `gun.Shooting = true`, а лазер управляется только через player input.
   - Что неясно: Есть ли случай, когда порядок Gun/Laser имеет значение?
   - Рекомендация: Добавить [UpdateBefore(EcsLaserSystem)] к EcsGunSystem для детерминизма, т.к. audit документ упоминал "ordering-атрибуты" как артефакт параллельной разработки.

2. **ShootToData: оставить пустую struct или заменить на tag?**
   - Что мы знаем: ShootToData используется как фильтр в query EcsShootToSystem (`RefRO<ShootToData>`)
   - Рекомендация: Оставить пустую struct -- менее инвазивное изменение, query продолжает работать.

## Sources

### Primary (HIGH confidence)
- Прямой анализ исходного кода проекта (все файлы прочитаны через Read tool)
- `.planning/v1.1.0-MILESTONE-AUDIT.md` -- источник всех 6 tech debt items
- Git status -- подтверждение 3 untracked .meta файлов

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- все изменения в существующем коде, стек не меняется
- Architecture: HIGH -- ordering chain полностью задокументирован, все файлы прочитаны
- Pitfalls: HIGH -- все edge cases выявлены при чтении кода (двойная запись, зависимости)

**Research date:** 2026-04-04
**Valid until:** 2026-05-04 (стабильный проект, нет внешних зависимостей)
