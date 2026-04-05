# Phase 14: Config & Visual Polish - Context

**Gathered:** 2026-04-05 (auto mode)
**Status:** Ready for planning

<domain>
## Phase Boundary

Все параметры ракеты задаются через ScriptableObject без магических чисел в коде. За ракетой тянется инверсионный след (ParticleSystem). При попадании ракеты воспроизводится взрыв VFX. Требования: CONF-01, VIS-02, VIS-04.

</domain>

<decisions>
## Implementation Decisions

### ScriptableObject конфигурация (CONF-01)
- **D-01:** Расширить `GameData.RocketData` struct полями: `float Speed`, `float LifeTimeSec`, `float TurnRateDegPerSec`, `int MaxAmmo`, `float ReloadDurationSec`, `int Score`
- **D-02:** Убрать hardcoded значения из `EntitiesCatalog.CreateRocket()` (speed: 8f, lifeTime: 5f, turnRateDegPerSec: 180f) -- читать из `_configs.Rocket`
- **D-03:** Убрать hardcoded значения из `EntitiesCatalog.CreateShip()` (rocketMaxAmmo: 3, rocketReloadSec: 5f) -- читать из `_configs.Rocket.MaxAmmo` и `_configs.Rocket.ReloadDurationSec`
- **D-04:** Передать `_configs.Rocket.Score` в EntityFactory.CreateRocket() -- добавить ScoreValue компонент на rocket entity (сейчас отсутствует, нужен для начисления очков)
- **D-05:** Обновить сигнатуру `EntitiesCatalog.CreateRocket()` -- принимать `GameData.RocketData` или отдельные параметры из конфига

### Инверсионный след (VIS-02)
- **D-06:** Использовать ParticleSystem (не TrailRenderer) -- лучше интегрируется с ObjectPool, поддерживает Stop/Clear для корректного переиспользования
- **D-07:** Добавить `ParticleSystem` компонент на Rocket префаб как дочерний объект
- **D-08:** В `RocketVisual` добавить `[SerializeField] ParticleSystem _trailEffect` -- Play в `OnConnected()`, Stop+Clear при возврате в пул
- **D-09:** При возврате ракеты в пул: `_trailEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear)` -- предотвращает "хвост" от предыдущего полёта при переиспользовании

### Взрыв VFX при попадании (VIS-04)
- **D-10:** Переиспользовать существующий `VfxBlowPrefab` через `Game.PlayEffect()` -- единообразно с астероидами, UFO и кораблём
- **D-11:** Добавить ветку `EntityType.Rocket` в `Application.OnEntityDestroyed()` -- вызвать `_game.PlayEffect(_configs.VfxBlowPrefab, position)` перед `ReleaseByGameObject`

### Префаб ракеты (обновление)
- **D-12:** Обновить существующий Rocket префаб через MCP -- добавить дочерний ParticleSystem для trail
- **D-13:** Настройка trail ParticleSystem: короткий яркий след, Simulation Space = World (чтобы след оставался на месте при движении ракеты), Stop Action = None
- **D-14:** Если Rocket префаб ещё не создан или не назначен в GameData -- создать через MCP и назначить `GameData.Rocket.Prefab`

### Claude's Discretion
- Конкретные значения по умолчанию в ScriptableObject (speed, turnRate и т.д.) -- использовать текущие hardcoded как отправную точку
- Параметры ParticleSystem trail (размер частиц, lifetime, emission rate, цвет)
- Масштаб взрыва VFX для ракеты (может отличаться от астероидного)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` -- CONF-01 (ScriptableObject конфигурация), VIS-02 (инверсионный след), VIS-04 (взрыв VFX)

### Конфигурация (primary target)
- `Assets/Scripts/Configs/GameData.cs` -- RocketData struct (строка 60-64), расширить полями
- `Assets/Scripts/Application/EntitiesCatalog.cs:110-111` -- hardcoded rocketMaxAmmo: 3, rocketReloadSec: 5f в CreateShip()
- `Assets/Scripts/Application/EntitiesCatalog.cs:282-288` -- hardcoded speed: 8f, lifeTime: 5f, turnRateDegPerSec: 180f в CreateRocket()
- `Assets/Scripts/ECS/EntityFactory.cs:154-180` -- CreateRocket() сигнатура и создание entity

### VFX (trail + explosion)
- `Assets/Scripts/View/RocketVisual.cs` -- минимальный Visual, расширить для trail ParticleSystem
- `Assets/Scripts/View/EffectVisual.cs` -- паттерн VFX через ParticleSystem + пул (переиспользовать для взрыва)
- `Assets/Scripts/Application/Game.cs:216-226` -- PlayEffect() и OnEffectStopped() -- паттерн воспроизведения VFX
- `Assets/Scripts/Application/Application.cs:213-237` -- OnEntityDestroyed() -- добавить ветку для Rocket

### Existing patterns (analogs)
- `Assets/Scripts/Configs/GameData.cs:10-16` -- BulletData struct как паттерн для расширения RocketData
- `Assets/Scripts/Configs/GameData.cs:46` -- VfxBlowPrefab -- существующий эффект взрыва для переиспользования

### Prior phase context
- `.planning/phases/10-ecs-core/10-CONTEXT.md` -- D-07..D-10: ECS-компоненты ракеты
- `.planning/phases/12-bridge-lifecycle/12-CONTEXT.md` -- D-10..D-12: RocketVisual, префаб, RocketData в GameData

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Game.PlayEffect()` + `EffectVisual` + `VfxBlowPrefab` -- полная цепочка VFX через пул, переиспользуется для ракеты
- `GameData.RocketData` struct -- уже существует с полем Prefab, расширяется новыми полями
- `EntitiesCatalog.CreateRocket()` -- уже создаёт визуал, нужно убрать hardcoded значения
- `ObjectPool` (`ViewFactory`) -- автоматически подхватит Rocket prefab при переиспользовании

### Established Patterns
- Config struct pattern: `BulletData` (Prefab, Speed, LifeTimeSeconds) -- точный шаблон для RocketData
- VFX pattern: `Application.OnEntityDestroyed()` -> `Game.PlayEffect(prefab, position)` -> пул
- ParticleSystem lifecycle: `EffectVisual` использует `OnParticleSystemStopped` для возврата в пул
- Все entity конфиги живут в `GameData` ScriptableObject

### Integration Points
- `GameData.RocketData` -- расширить struct
- `EntitiesCatalog.CreateRocket()` -- параметры из конфига
- `EntitiesCatalog.CreateShip()` -- rocketMaxAmmo/rocketReloadSec из конфига
- `Application.OnEntityDestroyed()` -- добавить ветку EntityType.Rocket
- `RocketVisual` -- добавить ParticleSystem support
- Rocket prefab -- добавить дочерний ParticleSystem через MCP

</code_context>

<specifics>
## Specific Ideas

- Пользователь: "не забудь сделать префаб для эффекта и прокинуть его в конфиги если вдруг этого нет в плане" -- план ОБЯЗАН включать создание/обновление Unity assets (префабы) через MCP и привязку к GameData конфигам
- Взрыв переиспользует существующий VfxBlowPrefab (тот же эффект что для астероидов)

</specifics>

<deferred>
## Deferred Ideas

None -- analysis stayed within phase scope

</deferred>

---

*Phase: 14-config-visual-polish*
*Context gathered: 2026-04-05*
