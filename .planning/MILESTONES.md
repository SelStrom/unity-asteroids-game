# Milestones

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
