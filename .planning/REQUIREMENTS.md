# Requirements: Asteroids

**Defined:** 2026-04-05
**Core Value:** Играбельная классическая механика Asteroids с онлайн-лидербордом -- на современном стеке Unity с ECS-ядром

## v1.2 Requirements

Requirements for milestone v1.2.0: Самонаводящиеся ракеты. Each maps to roadmap phases.

### Rocket Core (ECS)

- [ ] **ROCK-01**: Игрок может запустить самонаводящуюся ракету нажатием R
- [ ] **ROCK-02**: Ракета автоматически наводится на ближайшего врага (астероид/UFO) с ограниченным turn rate
- [ ] **ROCK-03**: Ракета переключает цель при уничтожении текущей
- [ ] **ROCK-04**: Ракета имеет ограниченное время жизни (LifeTimeData)
- [ ] **ROCK-05**: Боезапас ракет ограничен конфигурируемым количеством
- [ ] **ROCK-06**: Ракеты респавнятся по таймеру (инкрементальная перезарядка)

### Collision & Scoring

- [ ] **COLL-01**: Ракета уничтожает астероиды при столкновении и начисляет очки
- [ ] **COLL-02**: Ракета уничтожает UFO при столкновении и начисляет очки
- [ ] **COLL-03**: Ракета уничтожается при столкновении с любым врагом (включая случайные по пути)

### Visual

- [ ] **VIS-01**: Ракета отображается как уменьшенный спрайт корабля
- [ ] **VIS-02**: Ракета имеет инверсионный след (ParticleSystem)
- [ ] **VIS-03**: Спрайт ракеты вращается по направлению полёта
- [ ] **VIS-04**: Взрыв при попадании ракеты (переиспользование существующего VFX)

### Config

- [ ] **CONF-01**: Все параметры ракеты задаются через ScriptableObject (скорость, turn rate, боезапас, перезарядка, время жизни, очки)

### HUD

- [ ] **HUD-01**: HUD отображает количество доступных ракет
- [ ] **HUD-02**: HUD отображает таймер перезарядки ракет

### Testing

- [ ] **TEST-01**: Юнит-тесты на ECS-компоненты и системы ракет (EditMode)
- [ ] **TEST-02**: Интеграционные тесты на lifecycle ракеты (спавн -> наведение -> коллизия -> уничтожение)
- [ ] **TEST-03**: MCP-верификация визуала и геймплея в Unity Editor

## v2 Requirements

Deferred to future releases.

### Enhanced Rockets

- **ROCK-07**: Множественные типы ракет с разными характеристиками
- **ROCK-08**: Враждебные ракеты для UFO
- **ROCK-09**: Визуальная индикация цели ракеты (подсветка врага)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Управляемая игроком ракета (player-guided missile) | Ломает flow аркадного геймплея -- игрок перестаёт управлять кораблём |
| Proportional Navigation / предиктивное прицеливание | Избыточная сложность для аркады; простой seek достаточен |
| Тороидальное наведение (shortest path через wrap) | Непропорциональная сложность: 9 фантомных позиций для edge case |
| Множественные типы ракет | Один тип для v1.2, разнообразие в будущих milestone |
| Ракеты для UFO | Радикально меняет баланс, требует систему уклонения |
| Lock-on индикатор на цели | Перегружает минималистичный UI Asteroids |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| ROCK-01 | Phase 13 | Pending |
| ROCK-02 | Phase 10 | Pending |
| ROCK-03 | Phase 10 | Pending |
| ROCK-04 | Phase 10 | Pending |
| ROCK-05 | Phase 10 | Pending |
| ROCK-06 | Phase 10 | Pending |
| COLL-01 | Phase 11 | Pending |
| COLL-02 | Phase 11 | Pending |
| COLL-03 | Phase 11 | Pending |
| VIS-01 | Phase 12 | Pending |
| VIS-02 | Phase 14 | Pending |
| VIS-03 | Phase 12 | Pending |
| VIS-04 | Phase 14 | Pending |
| CONF-01 | Phase 14 | Pending |
| HUD-01 | Phase 15 | Pending |
| HUD-02 | Phase 15 | Pending |
| TEST-01 | Phase 10 | Pending |
| TEST-02 | Phase 12 | Pending |
| TEST-03 | Phase 15 | Pending |

**Coverage:**
- v1.2 requirements: 19 total
- Mapped to phases: 19
- Unmapped: 0

---
*Requirements defined: 2026-04-05*
*Last updated: 2026-04-05 after roadmap creation*
