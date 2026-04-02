# Requirements: Asteroids -- Техническая миграция

**Defined:** 2026-04-02
**Core Value:** Играбельная классическая механика Asteroids -- фундамент для технической миграции на современный стек Unity

## v1 Requirements

Требования для milestone v1.0. Каждое маппится на фазу roadmap.

### Dev Tooling

- [x] **TOOL-01**: Unity-MCP пакет установлен в проект и MCP сервер настроен в Claude Code для взаимодействия с Unity Editor
- [x] **TOOL-02**: Тестовый фреймворк настроен (EditMode + PlayMode assemblies, NUnit)

### Unity 6.3 Upgrade

- [x] **UPG-01**: Проект открывается и компилируется в Unity 6.3 без ошибок
- [x] **UPG-02**: Все deprecated API заменены (FindObjectsOfType -> FindObjectsByType и др.)
- [x] **UPG-03**: TextMeshPro работает как внутренний модуль (зависимость com.unity.textmeshpro удалена)
- [x] **UPG-04**: Все существующие пакеты (InputSystem, UGS Auth, UGS Leaderboards, uGUI) совместимы с Unity 6.3
- [x] **UPG-05**: Игра запускается в Editor и воспроизводит весь геймплей 1:1

### shtl-mvvm Fix

- [x] **MVVM-01**: Зависимость com.unity.textmeshpro удалена из package.json shtl-mvvm
- [x] **MVVM-02**: Ссылка Unity.TextMeshPro в asmdef заменена или условно скомпилирована
- [x] **MVVM-03**: Библиотека компилируется и работает на Unity 2022.3+
- [x] **MVVM-04**: Библиотека компилируется и работает на Unity 6.3
- [x] **MVVM-05**: Фикс опубликован в репозиторий github.com/SelStrom/shtl-mvvm
- [x] **MVVM-06**: Проект Asteroids обновлен на новую версию shtl-mvvm

### URP Migration

- [x] **URP-01**: URP пакет установлен, 2D Renderer Asset создан и назначен
- [x] **URP-02**: Render Pipeline Converter выполнен, все материалы конвертированы
- [x] **URP-03**: ParticleSystem материалы адаптированы под URP
- [x] **URP-04**: URP Volume с базовым Post-Processing настроен (Bloom, Vignette или аналогичные эффекты)
- [x] **URP-05**: Визуальный результат соответствует оригиналу (спрайты, частицы, UI)
- [x] **URP-06**: Игра запускается в Editor и воспроизводит весь геймплей 1:1

### Hybrid DOTS -- ECS Foundation

- [x] **ECS-01**: Пакеты com.unity.entities и com.unity.burst установлены и совместимы с Unity 6.3
- [x] **ECS-02**: IComponentData определены для всех игровых сущностей (Ship, Asteroid, Bullet, UfoBig, Ufo)
- [x] **ECS-03**: EntityFactory создает entities с правильными компонентами
- [x] **ECS-04**: ThrustSystem перенесена на ISystem с Burst-компиляцией
- [x] **ECS-05**: RotateSystem перенесена на ISystem с Burst-компиляцией
- [x] **ECS-06**: MoveSystem перенесена на ISystem с Burst-компиляцией (включая тороидальное обертывание)
- [x] **ECS-07**: GunSystem перенесена на ISystem (перезарядка, стрельба)
- [x] **ECS-08**: LaserSystem перенесена на ISystem (заряды, cooldown)
- [x] **ECS-09**: ShootToSystem (AI наведение НЛО) перенесена на ISystem
- [x] **ECS-10**: MoveToSystem (движение НЛО к цели) перенесена на ISystem
- [x] **ECS-11**: CollisionHandler перенесен на ISystem (обработка столкновений через Physics2D результаты)

### Hybrid DOTS -- Bridge Layer

- [x] **BRG-01**: Managed component GameObjectRef связывает Entity с GameObject/Transform
- [x] **BRG-02**: GameObjectSyncSystem синхронизирует позицию/ротацию из ECS в Transform каждый кадр
- [x] **BRG-03**: CollisionBridge передает результаты Physics2D коллизий в ECS World
- [x] **BRG-04**: ObservableBridgeSystem транслирует ECS-данные в ObservableValue для shtl-mvvm UI
- [x] **BRG-05**: Жизненный цикл Entity<->GameObject синхронизирован (создание, уничтожение)
- [x] **BRG-06**: Игра запускается в Editor и воспроизводит весь геймплей 1:1

### Testing (TDD)

- [x] **TST-01**: EditMode тесты для всех ECS компонентов (создание, значения по умолчанию)
- [x] **TST-02**: EditMode тесты для ThrustSystem (физика тяги, направление, максимальная скорость)
- [x] **TST-03**: EditMode тесты для MoveSystem (перемещение, тороидальное обертывание)
- [x] **TST-04**: EditMode тесты для RotateSystem (поворот, нормализация угла)
- [x] **TST-05**: EditMode тесты для GunSystem (стрельба, перезарядка, лимит пуль)
- [x] **TST-06**: EditMode тесты для LaserSystem (заряды, cooldown, активация)
- [x] **TST-07**: EditMode тесты для ShootToSystem (предсказание позиции цели, расчет упреждения)
- [x] **TST-08**: EditMode тесты для MoveToSystem (движение к цели)
- [x] **TST-09**: EditMode тесты для CollisionHandler (правильные пары столкновений, очки)
- [x] **TST-10**: EditMode тесты для Bridge Layer (синхронизация позиций, жизненный цикл)
- [x] **TST-11**: EditMode тесты для shtl-mvvm фикса (TMP-совместимость на обеих версиях Unity)
- [x] **TST-12**: PlayMode тесты для полного игрового цикла (старт -> игра -> конец)

## v2 Requirements

Отложены на будущие milestone.

### Платформы

- **PLAT-01**: Мобильные сборки (iOS/Android)
- **PLAT-02**: WebGL проверка/адаптация после миграции

### Визуал

- **VIS-01**: URP 2D Lighting (динамическое освещение)

### Качество кода

- **QUAL-01**: Исправление 7 критических багов из анализа кодовой базы
- **QUAL-02**: Обновление UGS пакетов для Unity 6.3

## Out of Scope

| Feature | Reason |
|---------|--------|
| Полный DOTS (без GameObjects) | Entities Graphics не поддерживает SpriteRenderer и WebGL |
| DOTS Physics 2D | Пакет не существует в production-ready виде, Physics2D остается на GameObjects |
| 2D Lighting | Новый функционал, не часть миграции 1:1 |
| Исправление существующих багов | Миграция 1:1, баги в отдельном milestone |
| Мобильные платформы | Будущие планы, текущий scope -- Editor + Windows |
| Новые игровые механики | Только миграция существующего функционала |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| TOOL-01 | Phase 1 | Complete |
| TOOL-02 | Phase 1 | Complete |
| UPG-01 | Phase 2 | Complete |
| UPG-02 | Phase 2 | Complete |
| UPG-03 | Phase 2 | Complete |
| UPG-04 | Phase 2 | Complete |
| UPG-05 | Phase 2 | Complete |
| MVVM-01 | Phase 1 | Complete |
| MVVM-02 | Phase 1 | Complete |
| MVVM-03 | Phase 1 | Complete |
| MVVM-04 | Phase 1 | Complete |
| MVVM-05 | Phase 1 | Complete |
| MVVM-06 | Phase 1 | Complete |
| URP-01 | Phase 3 | Complete |
| URP-02 | Phase 3 | Complete |
| URP-03 | Phase 3 | Complete |
| URP-04 | Phase 3 | Complete |
| URP-05 | Phase 3 | Complete |
| URP-06 | Phase 3 | Complete |
| ECS-01 | Phase 4 | Complete |
| ECS-02 | Phase 4 | Complete |
| ECS-03 | Phase 4 | Complete |
| ECS-04 | Phase 4 | Complete |
| ECS-05 | Phase 4 | Complete |
| ECS-06 | Phase 4 | Complete |
| ECS-07 | Phase 4 | Complete |
| ECS-08 | Phase 4 | Complete |
| ECS-09 | Phase 4 | Complete |
| ECS-10 | Phase 4 | Complete |
| ECS-11 | Phase 4 | Complete |
<<<<<<< HEAD
| BRG-01 | Phase 5 | Complete |
| BRG-02 | Phase 5 | Complete |
| BRG-03 | Phase 5 | Complete |
| BRG-04 | Phase 5 | Complete |
| BRG-05 | Phase 5 | Complete |
| BRG-06 | Phase 5 | Complete |
| TST-01 | Phase 4 | Complete |
| TST-02 | Phase 4 | Complete |
| TST-03 | Phase 4 | Complete |
| TST-04 | Phase 4 | Complete |
| TST-05 | Phase 4 | Complete |
| TST-06 | Phase 4 | Complete |
| TST-07 | Phase 4 | Complete |
| TST-08 | Phase 4 | Complete |
| TST-09 | Phase 4 | Complete |
| TST-10 | Phase 5 | Complete |
| TST-11 | Phase 1 | Complete |
| TST-12 | Phase 5 | Complete |

**Coverage:**
- v1 requirements: 48 total
- Mapped to phases: 48
- Unmapped: 0

---
*Requirements defined: 2026-04-02*
*Last updated: 2026-04-02 after roadmap creation*
