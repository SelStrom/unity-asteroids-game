# Milestones

## v1.2.0 Самонаводящиеся ракеты (Shipped: 2026-04-06)

**Phases completed:** 8 phases, 16 plans, 23 tasks

**Key accomplishments:**

- 3 IComponentData structs (RocketTag, RocketTargetData, RocketAmmoData) + EntityFactory.CreateRocket + расширенный CreateShip с боезапасом ракет
- TDD-driven Rocket+Enemy collision branches with IsRocket helper, DeadTag assignment and score accumulation for all enemy types
- RocketVisual MonoBehaviour + CreateRocket factory + RocketShootEvent processing -- полный bridge между ECS ракетой и GameObject
- 5 integration tests covering full rocket lifecycle: spawn with components, GameObjectRef sync (position + rotation from Direction), DeadTag cleanup with callback, full spawn-sync-dead cycle, RotateData absence confirmation
- 1. [Rule 3 - Blocking] Существующие тесты перезарядки ломались без RocketShootEvent singleton
- Commit:
- Вынос hardcoded параметров ракеты в ScriptableObject, добавление ScoreValue на entity, trail ParticleSystem и взрыв VFX при уничтожении
- Editor-скрипт для создания trail ParticleSystem на Rocket префабе, значения конфигурации ракеты в GameData.asset (Speed=8, LifeTimeSec=5, TurnRate=180, MaxAmmo=3, Reload=5, Score=50)
- Два TMP_Text объекта (rocket_ammo_count, rocket_reload_time) добавлены в HUD Canvas как PrefabInstance gui_text.prefab и привязаны к HudVisual SerializeField
- Score=50 в GameData.asset для RocketData, Editor-скрипт для trail доступен

---

## v1.1.0 Техническая миграция (Shipped: 2026-04-04)

**Phases completed:** 9 phases, 28 plans, 50 tasks
**Timeline:** 3 дня (2026-04-02 → 2026-04-04)
**Code:** ~4700 LOC scripts + ~4350 LOC tests, 142 теста (135 EditMode + 7 PlayMode)

**Key accomplishments:**

1. Фикс shtl-mvvm для совместимости с Unity 6.3 (условная компиляция TMP, обратная совместимость с 2022.3+)
2. Апгрейд на Unity 6.3 — удалены локальные TMP-ассеты, asmdef на GUID-ссылках, deprecated API заменены
3. Миграция на URP 2D Renderer с Post-Processing (Bloom, Vignette), визуал 1:1
4. ECS Foundation — 13 IComponentData, 8 тегов, 8 Burst-систем, полное TDD-покрытие
5. Bridge Layer — гибридный DOTS (ECS логика + GameObject рендер + MVVM UI), 3 bridge-системы
6. Legacy Cleanup — удалены 27 legacy-файлов, единый ECS data path, ActionScheduler standalone
7. UFO AI wiring (ShipPositionData singleton) + UFO-Asteroid коллизии
8. ECS tech debt cleanup — ordering-атрибуты, vestigial fields, dead bindings

**Audit:** Passed (65/65 requirements, 9/9 phases, 28/28 integration, 5/5 E2E flows)

**Archives:** [ROADMAP](milestones/v1.1.0-ROADMAP.md) | [REQUIREMENTS](milestones/v1.1.0-REQUIREMENTS.md) | [AUDIT](milestones/v1.1.0-MILESTONE-AUDIT.md)

---
