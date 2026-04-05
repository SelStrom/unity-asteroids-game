# Phase 15: HUD - Research

**Researched:** 2026-04-06
**Domain:** Unity ECS -> MVVM HUD bridge, UI biндинг через Shtl.Mvvm
**Confidence:** HIGH

## Summary

Фаза 15 -- минимальное расширение существующей HUD-инфраструктуры для отображения данных ракет. Паттерн полностью повторяет уже реализованный Laser HUD: `RocketAmmoData` (ECS) -> `ObservableBridgeSystem` -> `HudData` (ReactiveValue) -> `HudVisual` (TMP_Text). Все точки интеграции уже существуют, код-шаблоны лазера служат 1:1 образцом.

Ключевая техническая деталь: `ObservableBridgeSystem.OnUpdate()` использует `SystemAPI.Query<RefRO<MoveData>, RefRO<RotateData>, RefRO<ThrustData>, RefRO<LaserData>>().WithAll<ShipTag>()`. Добавление `RefRO<RocketAmmoData>` к этому запросу безопасно -- `RocketAmmoData` уже является компонентом ship entity (подтверждено в `AsteroidsEcsTestFixture.CreateShipEntity` и `EntityFactory`).

**Primary recommendation:** Расширить существующий Query в `ObservableBridgeSystem` добавлением `RefRO<RocketAmmoData>`, добавить 3 ReactiveValue в `HudData`, 2 SerializeField + 3 Bind в `HudVisual`, создать 2 TMP_Text объекта в сцене через MCP.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Добавить в `HudData` поля `ReactiveValue<string> RocketAmmoCount` и `ReactiveValue<string> RocketReloadTime` и `ReactiveValue<bool> IsRocketReloadTimeVisible` -- по аналогии с Laser полями
- **D-02:** Формат строки боезапаса: `"Rockets: {CurrentAmmo}"` -- краткий текстовый формат, единообразно с `"Laser shoots: {shoots}"`
- **D-03:** Формат строки таймера: `"Reload rocket: {seconds} sec"` -- единообразно с `"Reload laser: N sec"`
- **D-04:** Таймер перезарядки скрывается при полном боезапасе (`IsRocketReloadTimeVisible = CurrentAmmo < MaxAmmo`) -- по паттерну `IsLaserReloadTimeVisible`
- **D-05:** Добавить в `HudVisual` поля `[SerializeField] TMP_Text _rocketAmmoCount` и `[SerializeField] TMP_Text _rocketReloadTime`
- **D-06:** В `OnConnected()` привязать: `Bind.From(ViewModel.RocketAmmoCount).To(_rocketAmmoCount)`, `Bind.From(ViewModel.RocketReloadTime).To(_rocketReloadTime)`, `Bind.From(ViewModel.IsRocketReloadTimeVisible).To(_rocketReloadTime.gameObject)`
- **D-07:** Расширить Query в `ObservableBridgeSystem.OnUpdate()` -- добавить `RefRO<RocketAmmoData>` к существующему запросу `ShipTag` entity
- **D-08:** Добавить поле `_rocketMaxAmmo` (int) с setter `SetRocketMaxAmmo()` -- по паттерну `_laserMaxShoots`
- **D-09:** В блоке `if (_hudData != null)` добавить запись `_hudData.RocketAmmoCount.Value`, `_hudData.RocketReloadTime.Value`, `_hudData.IsRocketReloadTimeVisible.Value` -- чтение из `RocketAmmoData`
- **D-10:** В `GameScreen.ActivateHud()` вызвать `bridge.SetRocketMaxAmmo(_configs.Rocket.MaxAmmo)` -- по паттерну `bridge.SetLaserMaxShoots()`
- **D-11:** Добавить два TMP_Text объекта в HUD Canvas через MCP -- под существующими Laser полями
- **D-12:** Привязать новые TMP_Text к полям `_rocketAmmoCount` и `_rocketReloadTime` в HudVisual компоненте
- **D-13:** Запустить PlayMode, скриншот Game View через MCP, визуально подтвердить наличие rocket HUD элементов
- **D-14:** Проверить: при запуске ракеты счётчик уменьшается, при перезарядке -- увеличивается, таймер появляется/скрывается

### Claude's Discretion
- Точное расположение rocket HUD элементов относительно laser полей (ниже laser reload)
- Размер шрифта и цвет TMP_Text (единообразно с существующими HUD элементами)
- Форматирование таймера перезарядки (целые секунды или с десятичными)

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| HUD-01 | HUD отображает количество доступных ракет | D-01, D-02, D-05, D-06, D-07, D-09, D-11 -- полная цепочка ECS->HUD для боезапаса |
| HUD-02 | HUD отображает таймер перезарядки ракет | D-01, D-03, D-04, D-06, D-07, D-09, D-11 -- полная цепочка для таймера + visibility |
| TEST-03 | MCP-верификация визуала и геймплея в Unity Editor | D-13, D-14 -- PlayMode скриншот + проверка динамического обновления |
</phase_requirements>

## Architecture Patterns

### Цепочка данных ECS -> HUD (существующий паттерн)

```
RocketAmmoData (ECS IComponentData)
    |
    v
ObservableBridgeSystem.OnUpdate() -- читает через SystemAPI.Query<RefRO<RocketAmmoData>>
    |
    v
HudData (ViewModel) -- ReactiveValue<string> RocketAmmoCount/RocketReloadTime, ReactiveValue<bool> IsRocketReloadTimeVisible
    |
    v
HudVisual (View) -- Bind.From().To() -> TMP_Text
```

[VERIFIED: Assets/Scripts/Bridge/ObservableBridgeSystem.cs, Assets/Scripts/View/HudVisual.cs]

### Текущий Query в ObservableBridgeSystem

```csharp
// Текущий (строка 51-53):
foreach (var (move, rotate, thrust, laser) in
         SystemAPI.Query<RefRO<MoveData>, RefRO<RotateData>, RefRO<ThrustData>, RefRO<LaserData>>()
             .WithAll<ShipTag>())

// После расширения:
foreach (var (move, rotate, thrust, laser, rocketAmmo) in
         SystemAPI.Query<RefRO<MoveData>, RefRO<RotateData>, RefRO<ThrustData>, RefRO<LaserData>, RefRO<RocketAmmoData>>()
             .WithAll<ShipTag>())
```

[VERIFIED: ObservableBridgeSystem.cs:51-53]

### Формат строк (паттерн из лазера)

```csharp
// Laser (существующий, строки 69-73):
var shoots = laser.ValueRO.CurrentShoots;
_hudData.LaserShootCount.Value = $"Laser shoots: {shoots.ToString()}";
_hudData.LaserReloadTime.Value =
    $"Reload laser: {TimeSpan.FromSeconds((int)laser.ValueRO.ReloadRemaining):%s} sec";
_hudData.IsLaserReloadTimeVisible.Value = shoots < _laserMaxShoots;

// Rocket (по аналогии):
var rocketCurrentAmmo = rocketAmmo.ValueRO.CurrentAmmo;
_hudData.RocketAmmoCount.Value = $"Rockets: {rocketCurrentAmmo.ToString()}";
_hudData.RocketReloadTime.Value =
    $"Reload rocket: {TimeSpan.FromSeconds((int)rocketAmmo.ValueRO.ReloadRemaining):%s} sec";
_hudData.IsRocketReloadTimeVisible.Value = rocketCurrentAmmo < _rocketMaxAmmo;
```

[VERIFIED: ObservableBridgeSystem.cs:69-73]

### Ship Entity уже содержит RocketAmmoData

`AsteroidsEcsTestFixture.CreateShipEntity()` добавляет `RocketAmmoData` к ship entity (строки 46-52). Это означает, что расширение Query безопасно -- компонент уже присутствует.

[VERIFIED: Assets/Tests/EditMode/ECS/AsteroidsEcsTestFixture.cs:46-52]

### Visibility паттерн

```csharp
// Bind boolean -> GameObject.SetActive
Bind.From(ViewModel.IsRocketReloadTimeVisible).To(_rocketReloadTime.gameObject);
```

`To(GameObject)` привязывает `ReactiveValue<bool>` к `SetActive()`. Когда боезапас полный, таймер скрывается.

[VERIFIED: HudVisual.cs:32]

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Reactive binding | Ручная подписка на ECS events | `ReactiveValue<T>` + `Bind.From().To()` | Shtl.Mvvm автоматически управляет подписками и CleanUp |
| String formatting | Кастомный форматтер | `$"Rockets: {value}"` + `TimeSpan.FromSeconds` | Единообразие с существующим HUD |
| Visibility toggle | Ручной SetActive | `Bind.From(bool).To(gameObject)` | Автоматическая привязка visibility через MVVM |

## Common Pitfalls

### Pitfall 1: Query mismatch после расширения
**What goes wrong:** Если `RocketAmmoData` не добавлен к ship entity, расширенный Query не найдёт ни одного entity и HUD перестанет обновляться полностью (включая координаты, скорость, лазер).
**Why it happens:** `SystemAPI.Query` требует ВСЕ перечисленные компоненты.
**How to avoid:** Ship entity уже содержит `RocketAmmoData` (подтверждено в `AsteroidsEcsTestFixture` и `EntityFactory`). Тест `PushesRocketData_ToHudData` должен создавать entity через `CreateFullShipEntity` с дополнительным `RocketAmmoData`.
**Warning signs:** Все HUD-поля перестали обновляться после изменения Query.

### Pitfall 2: Забытый SetRocketMaxAmmo в GameScreen
**What goes wrong:** `_rocketMaxAmmo` остаётся 0, `IsRocketReloadTimeVisible` всегда `true` (даже при полном боезапасе), т.к. `currentAmmo < 0` всегда false, а `currentAmmo < _rocketMaxAmmo(=0)` тоже false.
**Why it happens:** `SetRocketMaxAmmo()` не вызван в `ActivateHud()`.
**How to avoid:** D-10 явно требует вызов `bridge.SetRocketMaxAmmo(_configs.Rocket.MaxAmmo)` в `GameScreen.ActivateHud()`.
**Warning signs:** Таймер перезарядки ракет не появляется при неполном боезапасе.

### Pitfall 3: Существующие тесты ObservableBridgeSystem сломаются
**What goes wrong:** `CreateFullShipEntity` в тестах не добавляет `RocketAmmoData`, расширенный Query не находит entity.
**Why it happens:** Helper `CreateFullShipEntity` создаёт entity напрямую, не через `CreateShipEntity` (который уже содержит `RocketAmmoData`).
**How to avoid:** Обновить `CreateFullShipEntity` в `ObservableBridgeSystemTests` -- добавить `m_Manager.AddComponentData(entity, new RocketAmmoData { ... })` или переписать helper через `CreateShipEntity` + последующую настройку компонентов.
**Warning signs:** Все существующие тесты `ObservableBridgeSystemTests` падают.

### Pitfall 4: SerializeField ссылки в сцене
**What goes wrong:** Новые `[SerializeField]` поля `_rocketAmmoCount` и `_rocketReloadTime` остаются null в рантайме.
**Why it happens:** Поля добавлены в код, но TMP_Text объекты не привязаны в сцене (Main.unity).
**How to avoid:** D-11 и D-12 требуют создание TMP_Text через MCP и привязку к полям HudVisual.
**Warning signs:** NullReferenceException при первом обновлении HUD.

## Code Examples

### HudData -- расширение ViewModel

```csharp
// Source: Assets/Scripts/View/HudVisual.cs (существующий паттерн)
public sealed class HudData : AbstractViewModel
{
    // ... существующие поля ...
    public readonly ReactiveValue<string> LaserShootCount = new();
    public readonly ReactiveValue<string> LaserReloadTime = new();
    public readonly ReactiveValue<bool> IsLaserReloadTimeVisible = new();

    // Новые поля (D-01):
    public readonly ReactiveValue<string> RocketAmmoCount = new();
    public readonly ReactiveValue<string> RocketReloadTime = new();
    public readonly ReactiveValue<bool> IsRocketReloadTimeVisible = new();
}
```

### HudVisual -- расширение View

```csharp
// Source: Assets/Scripts/View/HudVisual.cs (существующий паттерн)
public class HudVisual : AbstractWidgetView<HudData>
{
    // ... существующие поля ...
    [SerializeField] private TMP_Text _laserReloadTime = default;

    // Новые поля (D-05):
    [SerializeField] private TMP_Text _rocketAmmoCount = default;
    [SerializeField] private TMP_Text _rocketReloadTime = default;

    protected override void OnConnected()
    {
        // ... существующие Bind ...

        // Новые Bind (D-06):
        Bind.From(ViewModel.RocketAmmoCount).To(_rocketAmmoCount);
        Bind.From(ViewModel.RocketReloadTime).To(_rocketReloadTime);
        Bind.From(ViewModel.IsRocketReloadTimeVisible).To(_rocketReloadTime.gameObject);
    }
}
```

### ObservableBridgeSystem -- расширение bridge

```csharp
// Source: Assets/Scripts/Bridge/ObservableBridgeSystem.cs (существующий паттерн)

// Новое поле (D-08):
private int _rocketMaxAmmo;

public void SetRocketMaxAmmo(int maxAmmo)
{
    _rocketMaxAmmo = maxAmmo;
}

// В OnUpdate() -- расширенный Query (D-07):
foreach (var (move, rotate, thrust, laser, rocketAmmo) in
         SystemAPI.Query<RefRO<MoveData>, RefRO<RotateData>, RefRO<ThrustData>, RefRO<LaserData>, RefRO<RocketAmmoData>>()
             .WithAll<ShipTag>())
{
    if (_hudData != null)
    {
        // ... существующий код ...

        // Новый код (D-09):
        var rocketCurrentAmmo = rocketAmmo.ValueRO.CurrentAmmo;
        _hudData.RocketAmmoCount.Value = $"Rockets: {rocketCurrentAmmo.ToString()}";
        _hudData.RocketReloadTime.Value =
            $"Reload rocket: {TimeSpan.FromSeconds((int)rocketAmmo.ValueRO.ReloadRemaining):%s} sec";
        _hudData.IsRocketReloadTimeVisible.Value = rocketCurrentAmmo < _rocketMaxAmmo;
    }
    // ...
}
```

### GameScreen -- интеграция

```csharp
// Source: Assets/Scripts/Application/Screens/GameScreen.cs:56-67
private void ActivateHud()
{
    _hudData = new HudData();
    _hudVisual.Connect(_hudData);

    var world = World.DefaultGameObjectInjectionWorld;
    var bridge = world.GetExistingSystemManaged<ObservableBridgeSystem>();
    if (bridge != null)
    {
        bridge.SetHudData(_hudData);
        bridge.SetLaserMaxShoots(_configs.Laser.LaserMaxShoots);
        bridge.SetRocketMaxAmmo(_configs.Rocket.MaxAmmo); // D-10
    }
}
```

### Тест -- RocketAmmoData в ObservableBridgeSystem

```csharp
// По паттерну PushesLaserData_ToHudData (строки 129-151)
[Test]
public void PushesRocketData_ToHudData()
{
    var hudData = new HudData();
    _system.SetHudData(hudData);
    _system.SetRocketMaxAmmo(3);

    var entity = CreateFullShipEntity(
        position: float2.zero,
        speed: 0f,
        direction: float2.zero,
        rotation: new float2(1f, 0f),
        thrustActive: false,
        laserCurrentShoots: 3,
        laserMaxShoots: 3,
        reloadRemaining: 0f);
    // CreateFullShipEntity нужно расширить для добавления RocketAmmoData
    m_Manager.AddComponentData(entity, new RocketAmmoData
    {
        CurrentAmmo = 2,
        MaxAmmo = 3,
        ReloadRemaining = 4.0f,
        ReloadDurationSec = 5.0f
    });

    _system.Update();

    Assert.AreEqual("Rockets: 2", hudData.RocketAmmoCount.Value);
    Assert.IsTrue(hudData.IsRocketReloadTimeVisible.Value,
        "IsRocketReloadTimeVisible should be true when ammo < maxAmmo");
}
```

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Unity Test Framework 1.1.33 (NUnit) |
| Config file | `Assets/Tests/EditMode/EditMode.asmdef` |
| Quick run command | Запуск через Unity Test Runner (EditMode) |
| Full suite command | Unity Test Runner -- All EditMode tests |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| HUD-01 | RocketAmmoCount отображается в HUD | unit | ObservableBridgeSystemTests.PushesRocketData_ToHudData | Wave 0 |
| HUD-02 | RocketReloadTime + visibility отображается | unit | ObservableBridgeSystemTests.PushesRocketReloadVisibility | Wave 0 |
| TEST-03 | MCP-верификация визуала в Unity Editor | manual-only | MCP скриншот Game View | N/A |

### Sampling Rate
- **Per task commit:** Запуск ObservableBridgeSystemTests через Unity Test Runner
- **Per wave merge:** Все EditMode тесты
- **Phase gate:** Full suite green + MCP скриншот

### Wave 0 Gaps
- [ ] `ObservableBridgeSystemTests.PushesRocketData_ToHudData` -- новый тест для HUD-01
- [ ] `ObservableBridgeSystemTests.PushesRocketReloadVisibility` -- тест visibility для HUD-02
- [ ] Обновление `CreateFullShipEntity` -- добавить `RocketAmmoData` к helper

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Формат таймера `(int)ReloadRemaining` -- целые секунды, как у лазера | Code Examples | Низкий -- может потребоваться дробное отображение, но решение через Claude's Discretion |

**Все остальные утверждения верифицированы через код проекта.**

## Open Questions

1. **Форматирование таймера: целые или дробные секунды?**
   - What we know: Лазер использует `(int)laser.ValueRO.ReloadRemaining` -- целые секунды. Это Claude's Discretion.
   - Recommendation: Использовать тот же формат `(int)` для единообразия. Если перезарядка ракеты короткая (< 3 сек), можно рассмотреть `F1` для десятичных.

## Sources

### Primary (HIGH confidence)
- `Assets/Scripts/Bridge/ObservableBridgeSystem.cs` -- текущий Query, паттерн Laser HUD
- `Assets/Scripts/View/HudVisual.cs` -- HudData ViewModel, HudVisual View, Bind паттерн
- `Assets/Scripts/Application/Screens/GameScreen.cs` -- ActivateHud(), SetLaserMaxShoots паттерн
- `Assets/Scripts/ECS/Components/RocketAmmoData.cs` -- структура компонента (CurrentAmmo, MaxAmmo, ReloadRemaining)
- `Assets/Tests/EditMode/ECS/ObservableBridgeSystemTests.cs` -- существующие тесты bridge
- `Assets/Tests/EditMode/ECS/AsteroidsEcsTestFixture.cs` -- CreateShipEntity с RocketAmmoData
- `Assets/Scripts/Configs/GameData.cs` -- RocketData struct с MaxAmmo, ReloadDurationSec
- `Assets/Scenes/Main.unity` -- HUD scene hierarchy (Hud GameObject, fileID:1687355233)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- все библиотеки и паттерны уже используются в проекте
- Architecture: HIGH -- 1:1 повторение паттерна Laser HUD
- Pitfalls: HIGH -- выявлены через анализ кода (CreateFullShipEntity, Query mismatch)

**Research date:** 2026-04-06
**Valid until:** 2026-05-06 (стабильная кодовая база, паттерн не изменится)
