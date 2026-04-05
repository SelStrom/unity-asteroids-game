# Phase 14: Config & Visual Polish - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md -- this log preserves the alternatives considered.

**Date:** 2026-04-05
**Phase:** 14-config-visual-polish
**Areas discussed:** ScriptableObject конфигурация, Trail VFX, Взрыв VFX, Префаб и пулинг trail
**Mode:** auto (all options auto-selected)

---

## ScriptableObject конфигурация

| Option | Description | Selected |
|--------|-------------|----------|
| Расширить GameData.RocketData | Добавить все параметры (Speed, LifeTimeSec, TurnRateDegPerSec, MaxAmmo, ReloadDurationSec, Score) в существующий struct | ✓ |
| Отдельный ScriptableObject | Создать RocketConfig : ScriptableObject отдельно от GameData | |

**User's choice:** [auto] Расширить GameData.RocketData (recommended -- единообразно с BulletData, LaserData)
**Notes:** Hardcoded значения в EntitiesCatalog.cs:110-111 и 282-288 заменяются чтением из конфига

---

## Trail VFX

| Option | Description | Selected |
|--------|-------------|----------|
| ParticleSystem | Stop/Clear при пулинге, Simulation Space = World, гибкая настройка | ✓ |
| TrailRenderer | Проще настройка, но сложнее очистка при переиспользовании из пула | |

**User's choice:** [auto] ParticleSystem (recommended -- STATE.md отмечал это как concern, ParticleSystem лучше с пулом)
**Notes:** Stop + Clear предотвращает "хвост" от предыдущего полёта

---

## Взрыв VFX

| Option | Description | Selected |
|--------|-------------|----------|
| Переиспользовать VfxBlowPrefab | Тот же эффект что для астероидов/UFO/корабля через Game.PlayEffect() | ✓ |
| Отдельный эффект для ракеты | Новый VFX с другими параметрами | |

**User's choice:** [auto] Переиспользовать VfxBlowPrefab (recommended -- VIS-04 явно указывает "переиспользование существующего эффекта")
**Notes:** Добавить ветку EntityType.Rocket в Application.OnEntityDestroyed()

---

## Префаб и пулинг trail

| Option | Description | Selected |
|--------|-------------|----------|
| Stop+Clear ParticleSystem в Visual | RocketVisual управляет lifecycle trail | ✓ |
| Отдельный trail GameObject | Отсоединять trail при уничтожении ракеты | |

**User's choice:** [auto] Stop+Clear в RocketVisual (recommended -- проще, единый объект в пуле)
**Notes:** Пользователь явно попросил убедиться что префаб создан через MCP и привязан к конфигам

---

## Claude's Discretion

- Конкретные значения по умолчанию в ScriptableObject
- Параметры ParticleSystem trail
- Масштаб VFX

## Deferred Ideas

None
