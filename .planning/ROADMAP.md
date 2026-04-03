# Roadmap: Asteroids

## Overview

Техническая миграция аркады Asteroids с Unity 2022.3 + Built-in RP на Unity 6.3 + URP + гибридный DOTS. Путь: подготовка инструментов и фикс библиотеки shtl-mvvm (блокер) -> апгрейд движка -> миграция рендера на URP -> создание ECS-слоя с тестами -> интеграция через Bridge Layer. Каждая фаза завершается работающей игрой с геймплеем 1:1.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [ ] **Phase 1: Dev Tooling + shtl-mvvm Fix** - Настройка инструментов разработки и фикс библиотеки MVVM для совместимости с Unity 6.3
- [ ] **Phase 2: Unity 6.3 Upgrade** - Апгрейд проекта на Unity 6.3 с сохранением функциональности 1:1
- [ ] **Phase 3: URP Migration** - Миграция рендеринга с Built-in RP на Universal Render Pipeline
- [ ] **Phase 4: ECS Foundation** - Создание ECS-компонентов и систем с TDD-тестами
- [x] **Phase 5: Bridge Layer + Integration** - Интеграция ECS с GameObjects и финальная верификация (2026-04-03)
- [ ] **Phase 6: Legacy Cleanup** - Удаление legacy MonoBehaviour-слоя, перенос ActionScheduler на ECS, единый ECS data path
- [ ] **Phase 7: ShipPositionData Wiring + Traceability Fix** - Фикс production wiring ShipPositionData singleton, закрытие гэпов ECS-09/ECS-10/UFO AI, трассировка LC-*

## Phase Details

### Phase 1: Dev Tooling + shtl-mvvm Fix
**Goal**: Разработчик имеет настроенные инструменты (Unity-MCP, тестовый фреймворк) и исправленную библиотеку shtl-mvvm, готовую к Unity 6.3
**Depends on**: Nothing (first phase)
**Requirements**: TOOL-01, TOOL-02, MVVM-01, MVVM-02, MVVM-03, MVVM-04, MVVM-05, MVVM-06, TST-11
**Success Criteria** (what must be TRUE):
  1. Unity-MCP пакет установлен и Claude Code взаимодействует с Unity Editor через MCP сервер
  2. Тестовый фреймворк настроен: EditMode и PlayMode test assemblies создаются, NUnit-тесты запускаются из Editor
  3. Библиотека shtl-mvvm компилируется и работает на Unity 2022.3+ (обратная совместимость)
  4. Библиотека shtl-mvvm компилируется и работает на Unity 6.3 (зависимость com.unity.textmeshpro удалена)
  5. Фикс shtl-mvvm опубликован в github.com/SelStrom/shtl-mvvm и проект Asteroids обновлен на новую версию
**Plans:** 3 plans

Plans:
- [x] 01-01-PLAN.md -- Фикс shtl-mvvm: замена textmeshpro на ugui, условная компиляция, публикация тега v1.1.0
- [x] 01-02-PLAN.md -- Настройка инструментов: Unity-MCP + тестовый фреймворк (EditMode/PlayMode assemblies)
- [x] 01-03-PLAN.md -- Обновление Asteroids на shtl-mvvm v1.1.0 + EditMode тесты TMP-совместимости

### Phase 2: Unity 6.3 Upgrade
**Goal**: Проект Asteroids открывается, компилируется и запускается в Unity 6.3 с полным геймплеем 1:1
**Depends on**: Phase 1
**Requirements**: UPG-01, UPG-02, UPG-03, UPG-04, UPG-05
**Success Criteria** (what must be TRUE):
  1. Проект открывается в Unity 6.3 без ошибок компиляции
  2. Все deprecated API заменены (FindObjectsOfType -> FindObjectsByType и другие)
  3. TextMeshPro работает как внутренний модуль Unity 6.3 (старый пакет com.unity.textmeshpro удален из manifest)
  4. Игра запускается в Editor и воспроизводит весь геймплей 1:1 (корабль, стрельба, астероиды, НЛО, лидерборд)
**Plans:** 3 plans

Plans:
- [x] 02-01-PLAN.md -- Удаление локальных TMP-ассетов, исправление asmdef-ссылок на GUID, проверка deprecated API и compiler warnings
- [x] 02-02-PLAN.md -- Тесты верификации апгрейда (EditMode + PlayMode) и ручная верификация геймплея 1:1
- [x] 02-03-PLAN.md -- Gap closure: исправление ошибок компиляции (URP asmdef, TimeData квалификация, дубликаты ссылок)

### Phase 3: URP Migration
**Goal**: Проект рендерится через URP 2D Renderer с визуальным результатом, соответствующим оригиналу
**Depends on**: Phase 2
**Requirements**: URP-01, URP-02, URP-03, URP-04, URP-05, URP-06
**Success Criteria** (what must be TRUE):
  1. URP пакет установлен, 2D Renderer Asset создан и назначен в Project Settings
  2. Все материалы конвертированы: спрайты, частицы и UI отображаются корректно (без розовых материалов)
  3. Post-Processing настроен через URP Volume (Bloom, Vignette или аналогичные эффекты)
  4. Игра запускается в Editor и воспроизводит весь геймплей 1:1 с визуальным результатом, соответствующим оригиналу
**Plans:** 2 plans

Plans:
- [x] 03-01-PLAN.md -- Установка URP, создание ассетов, конвертация материалов, Post-Processing, EditMode тесты
- [x] 03-02-PLAN.md -- Запуск всех тестов и ручная верификация визуала и геймплея 1:1

### Phase 4: ECS Foundation
**Goal**: Полный набор ECS-компонентов и систем создан и покрыт EditMode-тестами, готов к интеграции
**Depends on**: Phase 3
**Requirements**: ECS-01, ECS-02, ECS-03, ECS-04, ECS-05, ECS-06, ECS-07, ECS-08, ECS-09, ECS-10, ECS-11, TST-01, TST-02, TST-03, TST-04, TST-05, TST-06, TST-07, TST-08, TST-09
**Success Criteria** (what must be TRUE):
  1. Пакеты com.unity.entities и com.unity.burst установлены, проект компилируется
  2. IComponentData определены для всех игровых сущностей и EntityFactory создает entities с правильными компонентами
  3. Все 8 игровых систем (Thrust, Rotate, Move, Gun, Laser, ShootTo, MoveTo, CollisionHandler) перенесены на ISystem
  4. EditMode-тесты покрывают каждый компонент и каждую систему (TST-01 через TST-09) и все проходят зеленым
  5. Burst-компиляция применена к чистым системам (Move, Rotate, Thrust) без ошибок
**Plans:** 4 plans

Plans:
- [x] 04-01-PLAN.md -- Установка com.unity.entities, IComponentData компоненты, тестовая инфраструктура, тесты компонентов
- [x] 04-02-PLAN.md -- EntityFactory + Burst-системы (Rotate, Thrust, Move, LifeTime) и тесты
- [x] 04-03-PLAN.md -- Системы вооружения (Gun, Laser) и тесты
- [x] 04-04-PLAN.md -- AI-системы (ShootTo, MoveTo) и CollisionHandler с mock-данными и тесты

### Phase 5: Bridge Layer + Integration
**Goal**: Полностью работающая игра на гибридном DOTS -- ECS управляет логикой, GameObjects отвечают за рендеринг и UI
**Depends on**: Phase 4
**Requirements**: BRG-01, BRG-02, BRG-03, BRG-04, BRG-05, BRG-06, TST-10, TST-12
**Success Criteria** (what must be TRUE):
  1. Bridge Layer связывает Entity с GameObject: позиция/ротация синхронизируется из ECS в Transform каждый кадр
  2. Physics2D коллизии корректно передаются в ECS World через CollisionBridge
  3. ECS-данные транслируются в ObservableValue для shtl-mvvm UI (очки, жизни, заряды отображаются корректно)
  4. Жизненный цикл Entity и GameObject синхронизирован (создание, уничтожение)
  5. Игра проходит полный цикл в PlayMode-тесте (старт -> игра -> конец) и воспроизводит весь геймплей 1:1
**Plans:** 5/5 plans executed

Plans:
- [x] 05-01-PLAN.md -- Bridge-компоненты (GameObjectRef, ShootEvents), GameObjectSyncSystem, расширение Gun/Laser/LifeTime систем, EditMode тесты
- [x] 05-02-PLAN.md -- CollisionBridge, ObservableBridgeSystem, DeadEntityCleanupSystem, EditMode тесты (TST-10)
- [x] 05-03-PLAN.md -- Интеграция bridge в игровой код (EntitiesCatalog, Application, Game.cs), PlayMode тесты (TST-12), ручная верификация геймплея 1:1
- [x] 05-04-PLAN.md -- Gap closure: фикс лазерного kill-пути (DeadTag вместо Kill) + очистка лазерного VFX при гибели
- [x] 05-05-PLAN.md -- Gap closure: синхронизация ScoreData -> Model.Score через ObservableBridgeSystem

### Phase 6: Legacy Cleanup
**Goal**: Полное удаление legacy MonoBehaviour-слоя (Model/Systems, Model/Entities, Model/Components), переключателя _useEcs, dual-creation паттерна. ActionScheduler как standalone managed-класс. Единый ECS data path без дублирования.
**Depends on**: Phase 5
**Requirements**: LC-01, LC-02, LC-03, LC-04, LC-05, LC-06, LC-07
**Success Criteria** (what must be TRUE):
  1. Все legacy-системы (MoveSystem, RotateSystem, ThrustSystem, GunSystem, LaserSystem, ShootToSystem, MoveToSystem, LifeTimeSystem, BaseModelSystem) удалены
  2. Legacy-модели (ShipModel, AsteroidModel, BulletModel, UfoModel, UfoBigModel) и компоненты (Model/Components/) удалены
  3. Переключатель _useEcs и dual-creation паттерн удалены -- единый ECS data path
  4. ActionScheduler выделен из Model как standalone managed-класс
  5. Model.cs удален -- score/state хранятся только в ECS singletons
  6. Все существующие тесты проходят зеленым, новые тесты покрывают измененный code path
  7. Игра воспроизводит весь геймплей 1:1 без legacy-слоя
**Plans:** 4 plans

Plans:
- [x] 06-01-PLAN.md -- Удаление _useEcs и else-веток, извлечение ActionScheduler из Model, _gameArea как поле
- [x] 06-02-PLAN.md -- Рефакторинг EntitiesCatalog (без ModelFactory, EntityType tracking), OnDeadEntity через ECS tags, Score из ECS
- [x] 06-03-PLAN.md -- Удаление 26 legacy-файлов, очистка ObservableBridgeSystem, рефакторинг тестов
- [x] 06-04-PLAN.md -- Запуск тестов и ручная верификация геймплея 1:1

### Phase 7: ShipPositionData Wiring + Traceability Fix
**Goal**: ShipPositionData singleton создаётся в production, UFO AI системы (ShootTo, MoveTo) работают, LC-* требования добавлены в трассировочную таблицу
**Depends on**: Phase 6
**Requirements**: ECS-09, ECS-10, LC-01, LC-02, LC-03, LC-04, LC-05, LC-06, LC-07
**Gap Closure:** Closes gaps from v1.1.0 audit
**Success Criteria** (what must be TRUE):
  1. ShipPositionData singleton создаётся в InitializeEcsSingletons() и доступен ECS-системам
  2. EcsShootToSystem запускается в production — UFO стреляют в игрока с упреждением
  3. EcsMoveToSystem запускается в production — малые UFO преследуют игрока
  4. Регрессионный тест подтверждает наличие ShipPositionData singleton после инициализации
  5. LC-01..LC-07 добавлены в REQUIREMENTS.md traceability table
**Plans:** 2 plans

Plans:
- [x] 07-01-PLAN.md -- ShipPositionData singleton wiring в InitializeEcsSingletons() + регрессионный тест + трассировочная таблица
- [x] 07-02-PLAN.md -- Ручная верификация геймплея 1:1 и UFO AI (LC-07)

### Phase 8: UFO-Asteroid Collision
**Goal**: Добавить обработку коллизии UFO+Asteroid в EcsCollisionHandlerSystem — в классическом Asteroids UFO и астероиды уничтожают друг друга при столкновении
**Depends on**: Phase 7
**Requirements**: COL-01, COL-02, COL-03, COL-04
**Success Criteria** (what must be TRUE):
  1. EcsCollisionHandlerSystem обрабатывает пары AsteroidTag+UfoTag и AsteroidTag+UfoBigTag
  2. При столкновении UFO и астероид уничтожаются (MarkDead)
  3. Регрессионный тест подтверждает обработку коллизии UFO+Asteroid
  4. Ручная верификация: UFO и астероиды коллайдятся в Play Mode
**Plans:** 2/2 plans executed

Plans:
- [x] 08-01-PLAN.md -- TDD: регрессионные тесты + реализация UFO+Asteroid коллизии в EcsCollisionHandlerSystem
- [x] 08-02-PLAN.md -- Ручная верификация UFO+Asteroid коллизии в Play Mode

## Progress

**Execution Order:**
Phases execute in numeric order: 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Dev Tooling + shtl-mvvm Fix | 3/3 | Human UAT pending | 2026-04-02 |
| 2. Unity 6.3 Upgrade | 3/3 | Human UAT pending | 2026-04-02 |
| 3. URP Migration | 2/2 | Human UAT pending | 2026-04-02 |
| 4. ECS Foundation | 4/4 | Human UAT pending | 2026-04-03 |
| 5. Bridge Layer + Integration | 5/5 | Complete | 2026-04-03 |
| 6. Legacy Cleanup | 4/4 | Complete | 2026-04-03 |
| 7. ShipPositionData Wiring + Traceability Fix | 2/2 | Complete | 2026-04-04 |
| 8. UFO-Asteroid Collision | 2/2 | Complete | 2026-04-04 |
