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
- [ ] **Phase 13: Input & Game Integration** - Запуск ракеты по кнопке R, интеграция в игровой цикл
- [ ] **Phase 14: Config & Visual Polish** - ScriptableObject конфигурация, инверсионный след, взрыв VFX
- [ ] **Phase 15: HUD** - Отображение боезапаса и таймера перезарядки ракет

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
**Plans**: 3 plans
Plans:
- [x] 10-01-PLAN.md -- ECS-компоненты ракеты + EntityFactory
- [ ] 10-02-PLAN.md -- TDD: EcsRocketGuidanceSystem (наведение)
- [ ] 10-03-PLAN.md -- TDD: EcsRocketAmmoSystem (перезарядка)

### Phase 14: Config & Visual Polish
**Goal**: Все параметры ракеты настраиваемы через ScriptableObject, визуал завершен
**Depends on**: Phase 13
**Requirements**: CONF-01, VIS-02, VIS-04
**Success Criteria** (what must be TRUE):
  1. Скорость, turn rate, боезапас, время перезарядки, время жизни и очки задаются в ScriptableObject без магических чисел в коде
  2. За ракетой тянется инверсионный след (ParticleSystem), корректно очищающийся при переиспользовании из пула
  3. При попадании ракеты воспроизводится взрыв VFX (переиспользование существующего эффекта)
**Plans**: 3 plans
Plans:
- [ ] 10-01-PLAN.md -- ECS-компоненты ракеты + EntityFactory
- [ ] 10-02-PLAN.md -- TDD: EcsRocketGuidanceSystem (наведение)
- [ ] 10-03-PLAN.md -- TDD: EcsRocketAmmoSystem (перезарядка)

### Phase 15: HUD
**Goal**: Игрок видит информацию о ракетах в HUD
**Depends on**: Phase 14
**Requirements**: HUD-01, HUD-02, TEST-03
**Success Criteria** (what must be TRUE):
  1. HUD отображает текущее количество доступных ракет (обновляется при запуске и перезарядке)
  2. HUD отображает таймер перезарядки ракет (прогресс до следующей ракеты)
  3. MCP-верификация подтверждает корректный визуал и геймплей в Unity Editor
**Plans**: 3 plans
Plans:
- [ ] 10-01-PLAN.md -- ECS-компоненты ракеты + EntityFactory
- [ ] 10-02-PLAN.md -- TDD: EcsRocketGuidanceSystem (наведение)
- [ ] 10-03-PLAN.md -- TDD: EcsRocketAmmoSystem (перезарядка)
**UI hint**: yes

## Progress

**Execution Order:**
Phases execute in numeric order: 10 -> 11 -> 12 -> 13 -> 14 -> 15

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 10. ECS Core | 3/3 | Complete    | 2026-04-05 |
| 11. Collision & Scoring | 1/1 | Complete    | 2026-04-05 |
| 12. Bridge & Lifecycle | 3/3 | Complete    | 2026-04-05 |
| 13. Input & Game Integration | 0/TBD | Not started | - |
| 14. Config & Visual Polish | 0/TBD | Not started | - |
| 15. HUD | 0/TBD | Not started | - |
