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
- [ ] **Phase 5: Bridge Layer + Integration** - Интеграция ECS с GameObjects и финальная верификация

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
**Plans:** 1/2 plans executed

Plans:
- [x] 02-01-PLAN.md -- Удаление локальных TMP-ассетов, исправление asmdef-ссылок на GUID, проверка deprecated API и compiler warnings
- [x] 02-02-PLAN.md -- Тесты верификации апгрейда (EditMode + PlayMode) и ручная верификация геймплея 1:1

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
- [ ] 03-02-PLAN.md -- Запуск всех тестов и ручная верификация визуала и геймплея 1:1

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
**Plans**: TBD

Plans:
- [ ] 04-01: TBD
- [ ] 04-02: TBD
- [ ] 04-03: TBD
- [ ] 04-04: TBD

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
**Plans**: TBD

Plans:
- [ ] 05-01: TBD
- [ ] 05-02: TBD
- [ ] 05-03: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 -> 2 -> 3 -> 4 -> 5

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Dev Tooling + shtl-mvvm Fix | 0/3 | Not started | - |
| 2. Unity 6.3 Upgrade | 1/2 | In Progress|  |
| 3. URP Migration | 0/2 | Not started | - |
| 4. ECS Foundation | 0/4 | Not started | - |
| 5. Bridge Layer + Integration | 0/3 | Not started | - |
