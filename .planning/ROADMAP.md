# Roadmap: Asteroids

## Milestones

- ✅ **v1.1.0 Техническая миграция** - Phases 1-9 (shipped 2026-04-04)
- 🚧 **v1.2.0 Самонаводящиеся ракеты** - Phases 10-15 (in progress)

## Phases

<details>
<summary>✅ v1.1.0 Техническая миграция (Phases 1-9) - SHIPPED 2026-04-04</summary>

See [milestones/v1.1.0-ROADMAP.md](milestones/v1.1.0-ROADMAP.md) for details.

</details>

### 🚧 v1.2.0 Самонаводящиеся ракеты (In Progress)

**Milestone Goal:** Добавить систему самонаводящихся ракет с полным TDD-покрытием, вписанную в ECS + визуал архитектуру.

- [x] **Phase 10: ECS Core -- данные и логика ракет** - ECS-компоненты, системы наведения, боезапаса и перезарядки (completed 2026-04-05)
- [x] **Phase 11: Collision & Scoring** - Коллизия ракеты с врагами, начисление очков, уничтожение ракеты (completed 2026-04-05)
- [x] **Phase 12: Bridge & Lifecycle** - Связь ECS с GameObject, спавн визуала, синхронизация позиции и вращения (completed 2026-04-05)
- [x] **Phase 13: Input & Game Integration** - Запуск ракеты по кнопке R, интеграция в игровой цикл (completed 2026-04-05)
- [x] **Phase 14: Config & Visual Polish** - ScriptableObject конфигурация, инверсионный след, взрыв VFX (completed 2026-04-05)
- [x] **Phase 15: HUD** - Отображение боезапаса и таймера перезарядки ракет (completed 2026-04-05)
- [x] **Phase 16: Asset & Config Fix** - Исправление Score=0 в ассете, верификация trail на префабе (completed 2026-04-05)
- [ ] **Phase 17: Documentation & Verification Closure** - SUMMARY frontmatter, REQUIREMENTS чекбоксы, PlayMode верификация

## Phase Details

### Phase 10: ECS Core -- данные и логика ракет
**Goal**: Ракета существует как ECS-entity с полной логикой наведения, боезапаса и перезарядки
**Depends on**: Nothing (первая фаза milestone v1.2.0; v1.1.0 ECS-инфраструктура уже на месте)
**Requirements**: ROCK-02, ROCK-03, ROCK-04, ROCK-05, ROCK-06, TEST-01
**Success Criteria** (what must be TRUE):
  1. ECS-entity ракеты каждый кадр поворачивает Direction к ближайшему врагу с ограниченным turn rate
  2. При уничтожении текущей цели (DeadTag) ракета переключается на следующую ближайшую цель
  3. Ракета самоуничтожается по истечении времени жизни (LifeTimeData)
  4. Боезапас на Ship entity уменьшается при запуске и восстанавливается инкрементально по таймеру
  5. Все ECS-компоненты и системы покрыты EditMode юнит-тестами
**Plans**: 3 plans
Plans:
- [x] 10-01-PLAN.md -- ECS-компоненты ракеты + EntityFactory
- [x] 10-02-PLAN.md -- TDD: EcsRocketGuidanceSystem (наведение)
- [x] 10-03-PLAN.md -- TDD: EcsRocketAmmoSystem (перезарядка)

### Phase 11: Collision & Scoring
**Goal**: Ракета взаимодействует с игровым миром -- уничтожает врагов и уничтожается сама
**Depends on**: Phase 10
**Requirements**: COLL-01, COLL-02, COLL-03
**Success Criteria** (what must be TRUE):
  1. Ракета уничтожает астероид при столкновении и начисляет очки (дробление работает)
  2. Ракета уничтожает UFO при столкновении и начисляет очки
  3. Ракета уничтожается при любом столкновении с врагом (включая случайные по пути к цели)
**Plans**: 1 plans
Plans:
- [x] 11-01-PLAN.md -- TDD: Коллизия ракеты с врагами (DeadTag + Score)

### Phase 12: Bridge & Lifecycle
**Goal**: Ракета видима на экране -- ECS-данные синхронизируются с GameObject визуалом
**Depends on**: Phase 10
**Requirements**: VIS-01, VIS-03, TEST-02
**Success Criteria** (what must be TRUE):
  1. При запуске ракеты создается GameObject с уменьшенным спрайтом корабля
  2. Спрайт ракеты вращается по направлению полёта (синхронизация MoveData.Direction -> Transform.rotation)
  3. Позиция GameObject синхронизируется с ECS MoveData.Position каждый кадр
  4. Интеграционные тесты подтверждают полный lifecycle: спавн -> наведение -> коллизия -> уничтожение
**Plans**: 3 plans
Plans:
- [x] 12-01-PLAN.md -- GameObjectSyncSystem третья ветка для RocketTag + RocketShootEvent
- [x] 12-02-PLAN.md -- RocketVisual, CreateRocket, ShootEventProcessorSystem, RocketData
- [x] 12-03-PLAN.md -- Интеграционные тесты lifecycle ракеты

### Phase 13: Input & Game Integration
**Goal**: Игрок управляет запуском ракет -- нажатие R запускает ракету в игровом мире
**Depends on**: Phase 11, Phase 12
**Requirements**: ROCK-01
**Success Criteria** (what must be TRUE):
  1. Нажатие R во время игры запускает ракету из позиции корабля в направлении его rotation
  2. Ракета не запускается при пустом боезапасе
  3. При рестарте игры все активные ракеты уничтожаются и боезапас сбрасывается
**Plans**: 2 plans
Plans:
- [x] 13-01-PLAN.md -- Input action Rocket + RocketAmmoData shooting + EcsRocketAmmoSystem + тесты
- [x] 13-02-PLAN.md -- Game.OnRocket handler + Start/Stop подписки + ClearEcsEventBuffers

### Phase 14: Config & Visual Polish
**Goal**: Все параметры ракеты настраиваемы через ScriptableObject, визуал завершен
**Depends on**: Phase 13
**Requirements**: CONF-01, VIS-02, VIS-04
**Success Criteria** (what must be TRUE):
  1. Скорость, turn rate, боезапас, время перезарядки, время жизни и очки задаются в ScriptableObject без магических чисел в коде
  2. За ракетой тянется инверсионный след (ParticleSystem), корректно очищающийся при переиспользовании из пула
  3. При попадании ракеты воспроизводится взрыв VFX (переиспользование существующего эффекта)
**Plans**: 2 plans
Plans:
- [x] 14-01-PLAN.md -- ScriptableObject конфигурация, ScoreValue, trail код, VFX взрыв
- [x] 14-02-PLAN.md -- MCP: Rocket префаб с ParticleSystem trail, GameData.asset значения

### Phase 15: HUD
**Goal**: Игрок видит информацию о ракетах в HUD
**Depends on**: Phase 14
**Requirements**: HUD-01, HUD-02, TEST-03
**Success Criteria** (what must be TRUE):
  1. HUD отображает текущее количество доступных ракет (обновляется при запуске и перезарядке)
  2. HUD отображает таймер перезарядки ракет (прогресс до следующей ракеты)
  3. MCP-верификация подтверждает корректный визуал и геймплей в Unity Editor
**Plans**: 2 plans
Plans:
- [x] 15-01-PLAN.md -- HudData/HudVisual/ObservableBridgeSystem/GameScreen код + тесты
- [x] 15-02-PLAN.md -- MCP: TMP_Text объекты в сцене + PlayMode верификация
**UI hint**: yes

### Phase 16: Asset & Config Fix
**Goal**: Все параметры ракеты корректно заданы в ScriptableObject ассете, trail настроен на префабе
**Depends on**: Phase 14
**Requirements**: CONF-01
**Gap Closure:** Closes gaps from audit
**Success Criteria** (what must be TRUE):
  1. Score=50 в GameData.asset для RocketData (вместо 0)
  2. Rocket префаб имеет корректно настроенный trail ParticleSystem
**Plans**: 1 plans
Plans:
- [x] 16-01-PLAN.md -- Исправление Score=50, запуск Editor-скрипта для trail

### Phase 17: Documentation & Verification Closure
**Goal**: Все requirements отмечены как выполненные, визуал верифицирован в PlayMode
**Depends on**: Phase 16
**Requirements**: ROCK-01, ROCK-02, ROCK-03, ROCK-04, ROCK-05, ROCK-06, VIS-02, VIS-03, VIS-04, TEST-01, TEST-03
**Gap Closure:** Closes gaps from audit
**Success Criteria** (what must be TRUE):
  1. SUMMARY frontmatter всех фаз содержит корректный `requirements_completed`
  2. REQUIREMENTS.md — все satisfied requirements отмечены `[x]`
  3. PlayMode верификация подтверждает: trail за ракетой, взрыв VFX при попадании
**Plans**: 2 plans
Plans:
- [ ] 17-01-PLAN.md -- SUMMARY frontmatter + REQUIREMENTS.md чекбоксы (doc gaps)
- [ ] 17-02-PLAN.md -- PlayMode верификация VIS-02/VIS-04 + финальные чекбоксы

## Progress

**Execution Order:**
Phases execute in numeric order: 10 -> 11 -> 12 -> 13 -> 14 -> 15 -> 16 -> 17

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 10. ECS Core | 3/3 | Complete    | 2026-04-05 |
| 11. Collision & Scoring | 1/1 | Complete    | 2026-04-05 |
| 12. Bridge & Lifecycle | 3/3 | Complete    | 2026-04-05 |
| 13. Input & Game Integration | 2/2 | Complete    | 2026-04-05 |
| 14. Config & Visual Polish | 2/2 | Complete    | 2026-04-05 |
| 15. HUD | 2/2 | Complete    | 2026-04-05 |
| 16. Asset & Config Fix | 1/1 | Complete    | 2026-04-06 |
| 17. Docs & Verification Closure | 0/2 | Planned    | -- |
