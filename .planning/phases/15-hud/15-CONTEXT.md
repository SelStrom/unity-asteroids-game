# Phase 15: HUD - Context

**Gathered:** 2026-04-06 (auto mode)
**Status:** Ready for planning

<domain>
## Phase Boundary

Игрок видит информацию о ракетах в HUD: количество доступных ракет (обновляется при запуске и перезарядке) и таймер перезарядки (прогресс до следующей ракеты). MCP-верификация подтверждает корректный визуал. Требования: HUD-01, HUD-02, TEST-03.

</domain>

<decisions>
## Implementation Decisions

### HudData ViewModel (HUD-01, HUD-02)
- **D-01:** Добавить в `HudData` поля `ReactiveValue<string> RocketAmmoCount` и `ReactiveValue<string> RocketReloadTime` и `ReactiveValue<bool> IsRocketReloadTimeVisible` -- по аналогии с Laser полями
- **D-02:** Формат строки боезапаса: `"Rockets: {CurrentAmmo}"` -- краткий текстовый формат, единообразно с `"Laser shoots: {shoots}"`
- **D-03:** Формат строки таймера: `"Reload rocket: {seconds} sec"` -- единообразно с `"Reload laser: N sec"`
- **D-04:** Таймер перезарядки скрывается при полном боезапасе (`IsRocketReloadTimeVisible = CurrentAmmo < MaxAmmo`) -- по паттерну `IsLaserReloadTimeVisible`

### HudVisual (UI binding)
- **D-05:** Добавить в `HudVisual` поля `[SerializeField] TMP_Text _rocketAmmoCount` и `[SerializeField] TMP_Text _rocketReloadTime`
- **D-06:** В `OnConnected()` привязать: `Bind.From(ViewModel.RocketAmmoCount).To(_rocketAmmoCount)`, `Bind.From(ViewModel.RocketReloadTime).To(_rocketReloadTime)`, `Bind.From(ViewModel.IsRocketReloadTimeVisible).To(_rocketReloadTime.gameObject)`

### ObservableBridgeSystem (ECS → HUD)
- **D-07:** Расширить Query в `ObservableBridgeSystem.OnUpdate()` -- добавить `RefRO<RocketAmmoData>` к существующему запросу `ShipTag` entity
- **D-08:** Добавить поле `_rocketMaxAmmo` (int) с setter `SetRocketMaxAmmo()` -- по паттерну `_laserMaxShoots`
- **D-09:** В блоке `if (_hudData != null)` добавить запись `_hudData.RocketAmmoCount.Value`, `_hudData.RocketReloadTime.Value`, `_hudData.IsRocketReloadTimeVisible.Value` -- чтение из `RocketAmmoData`

### GameScreen integration
- **D-10:** В `GameScreen.ActivateHud()` вызвать `bridge.SetRocketMaxAmmo(_configs.Rocket.MaxAmmo)` -- по паттерну `bridge.SetLaserMaxShoots()`

### UI Layout (сцена)
- **D-11:** Добавить два TMP_Text объекта в HUD Canvas через MCP -- под существующими Laser полями
- **D-12:** Привязать новые TMP_Text к полям `_rocketAmmoCount` и `_rocketReloadTime` в HudVisual компоненте

### MCP-верификация (TEST-03)
- **D-13:** Запустить PlayMode, скриншот Game View через MCP, визуально подтвердить наличие rocket HUD элементов
- **D-14:** Проверить: при запуске ракеты счётчик уменьшается, при перезарядке — увеличивается, таймер появляется/скрывается

### Claude's Discretion
- Точное расположение rocket HUD элементов относительно laser полей (ниже laser reload)
- Размер шрифта и цвет TMP_Text (единообразно с существующими HUD элементами)
- Форматирование таймера перезарядки (целые секунды или с десятичными)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` -- HUD-01 (количество ракет), HUD-02 (таймер перезарядки), TEST-03 (MCP-верификация)

### HUD (primary target)
- `Assets/Scripts/View/HudVisual.cs` -- HudData ViewModel + HudVisual View, расширить полями для ракет
- `Assets/Scripts/Bridge/ObservableBridgeSystem.cs` -- мост ECS→HUD, добавить чтение RocketAmmoData

### GameScreen integration
- `Assets/Scripts/Application/Screens/GameScreen.cs:56-67` -- ActivateHud(), SetHudData, SetLaserMaxShoots -- паттерн для SetRocketMaxAmmo

### ECS data source
- `Assets/Scripts/ECS/Components/RocketAmmoData.cs` -- CurrentAmmo, MaxAmmo, ReloadRemaining, ReloadDurationSec
- `Assets/Scripts/ECS/Systems/EcsRocketAmmoSystem.cs` -- логика перезарядки и стрельбы

### Existing patterns (analogs)
- `Assets/Scripts/Bridge/ObservableBridgeSystem.cs:69-73` -- паттерн чтения LaserData → HudData (точный шаблон)
- `Assets/Scripts/View/HudVisual.cs:12-14` -- паттерн LaserShootCount/LaserReloadTime/IsLaserReloadTimeVisible

### Prior phase context
- `.planning/phases/14-config-visual-polish/14-CONTEXT.md` -- D-01..D-05: ScriptableObject конфигурация, RocketData struct
- `.planning/phases/10-ecs-core/10-CONTEXT.md` -- D-11..D-13: RocketAmmoData компонент, боезапас и перезарядка

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `HudData` (ViewModel) -- уже содержит Laser поля, расширяется по тому же паттерну
- `HudVisual` (View) -- уже содержит TMP_Text биндинги, расширяется аналогично
- `ObservableBridgeSystem` -- уже читает LaserData для HUD, добавление RocketAmmoData — минимальное расширение
- `GameScreen.ActivateHud()` -- уже настраивает bridge, добавление SetRocketMaxAmmo — одна строка

### Established Patterns
- MVVM binding chain: `ECS Component → ObservableBridgeSystem → HudData (ReactiveValue) → HudVisual (TMP_Text)`
- Visibility pattern: `IsVisible = currentValue < maxValue` (laser reload time скрывается при полном боезапасе)
- String format pattern: `$"Label: {value}"` -- единообразное текстовое форматирование

### Integration Points
- `HudData` -- добавить 3 ReactiveValue поля
- `HudVisual` -- добавить 2 SerializeField + 3 Bind
- `ObservableBridgeSystem.OnUpdate()` -- расширить Query + добавить запись данных
- `ObservableBridgeSystem` -- добавить `_rocketMaxAmmo` field + setter
- `GameScreen.ActivateHud()` -- вызвать `SetRocketMaxAmmo()`
- HUD Canvas в сцене -- добавить 2 TMP_Text объекта через MCP

</code_context>

<specifics>
## Specific Ideas

No specific requirements -- open to standard approaches. Паттерн полностью повторяет Laser HUD.

</specifics>

<deferred>
## Deferred Ideas

None -- analysis stayed within phase scope

</deferred>

---

*Phase: 15-hud*
*Context gathered: 2026-04-06*
