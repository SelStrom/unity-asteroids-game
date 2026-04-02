# Requirements: Asteroids — Техническая миграция

**Defined:** 2026-04-02
**Core Value:** Играбельная классическая механика Asteroids — фундамент для технической миграции на современный стек Unity

## v1 Requirements

Требования для milestone v1.0. Каждое маппится на фазу roadmap.

### Dev Tooling

- [ ] **TOOL-01**: Unity-MCP пакет установлен в проект и MCP сервер настроен в Claude Code для взаимодействия с Unity Editor
- [ ] **TOOL-02**: Тестовый фреймворк настроен (EditMode + PlayMode assemblies, NUnit)

### Unity 6.3 Upgrade

- [ ] **UPG-01**: Проект открывается и компилируется в Unity 6.3 без ошибок
- [ ] **UPG-02**: Все deprecated API заменены (FindObjectsOfType → FindObjectsByType и др.)
- [ ] **UPG-03**: TextMeshPro работает как внутренний модуль (зависимость com.unity.textmeshpro удалена)
- [ ] **UPG-04**: Все существующие пакеты (InputSystem, UGS Auth, UGS Leaderboards, uGUI) совместимы с Unity 6.3
- [ ] **UPG-05**: Игра запускается в Editor и воспроизводит весь геймплей 1:1

### shtl-mvvm Fix

- [ ] **MVVM-01**: Зависимость com.unity.textmeshpro удалена из package.json shtl-mvvm
- [ ] **MVVM-02**: Ссылка Unity.TextMeshPro в asmdef заменена или условно скомпилирована
- [ ] **MVVM-03**: Библиотека компилируется и работает на Unity 2022.3+
- [ ] **MVVM-04**: Библиотека компилируется и работает на Unity 6.3
- [ ] **MVVM-05**: Фикс опубликован в репозиторий github.com/SelStrom/shtl-mvvm
- [ ] **MVVM-06**: Проект Asteroids обновлён на новую версию shtl-mvvm

### URP Migration

- [ ] **URP-01**: URP пакет установлен, 2D Renderer Asset создан и назначен
- [ ] **URP-02**: Render Pipeline Converter выполнен, все материалы конвертированы
- [ ] **URP-03**: ParticleSystem материалы адаптированы под URP
- [ ] **URP-04**: URP Volume с базовым Post-Processing настроен (Bloom, Vignette или аналогичные эффекты)
- [ ] **URP-05**: Визуальный результат соответствует оригиналу (спрайты, частицы, UI)
- [ ] **URP-06**: Игра запускается в Editor и воспроизводит весь геймплей 1:1

### Hybrid DOTS — ECS Foundation

- [ ] **ECS-01**: Пакеты com.unity.entities и com.unity.burst установлены и совместимы с Unity 6.3
- [ ] **ECS-02**: IComponentData определены для всех игровых сущностей (Ship, Asteroid, Bullet, UfoBig, Ufo)
- [ ] **ECS-03**: EntityFactory создаёт entities с правильными компонентами
- [ ] **ECS-04**: ThrustSystem перенесена на ISystem с Burst-компиляцией
- [ ] **ECS-05**: RotateSystem перенесена на ISystem с Burst-компиляцией
- [ ] **ECS-06**: MoveSystem перенесена на ISystem с Burst-компиляцией (включая тороидальное обёртывание)
- [ ] **ECS-07**: GunSystem перенесена на ISystem (перезарядка, стрельба)
- [ ] **ECS-08**: LaserSystem перенесена на ISystem (заряды, cooldown)
- [ ] **ECS-09**: ShootToSystem (AI наведение НЛО) перенесена на ISystem
- [ ] **ECS-10**: MoveToSystem (движение НЛО к цели) перенесена на ISystem
- [ ] **ECS-11**: CollisionHandler перенесён на ISystem (обработка столкновений через Physics2D результаты)

### Hybrid DOTS — Bridge Layer

- [ ] **BRG-01**: Managed component GameObjectRef связывает Entity с GameObject/Transform
- [ ] **BRG-02**: GameObjectSyncSystem синхронизирует позицию/ротацию из ECS в Transform каждый кадр
- [ ] **BRG-03**: CollisionBridge передаёт результаты Physics2D коллизий в ECS World
- [ ] **BRG-04**: ObservableBridgeSystem транслирует ECS-данные в ObservableValue для shtl-mvvm UI
- [ ] **BRG-05**: Жизненный цикл Entity↔GameObject синхронизирован (создание, уничтожение)
- [ ] **BRG-06**: Игра запускается в Editor и воспроизводит весь геймплей 1:1

### Testing (TDD)

- [ ] **TST-01**: EditMode тесты для всех ECS компонентов (создание, значения по умолчанию)
- [ ] **TST-02**: EditMode тесты для ThrustSystem (физика тяги, направление, максимальная скорость)
- [ ] **TST-03**: EditMode тесты для MoveSystem (перемещение, тороидальное обёртывание)
- [ ] **TST-04**: EditMode тесты для RotateSystem (поворот, нормализация угла)
- [ ] **TST-05**: EditMode тесты для GunSystem (стрельба, перезарядка, лимит пуль)
- [ ] **TST-06**: EditMode тесты для LaserSystem (заряды, cooldown, активация)
- [ ] **TST-07**: EditMode тесты для ShootToSystem (предсказание позиции цели, расчёт упреждения)
- [ ] **TST-08**: EditMode тесты для MoveToSystem (движение к цели)
- [ ] **TST-09**: EditMode тесты для CollisionHandler (правильные пары столкновений, очки)
- [ ] **TST-10**: EditMode тесты для Bridge Layer (синхронизация позиций, жизненный цикл)
- [ ] **TST-11**: EditMode тесты для shtl-mvvm фикса (TMP-совместимость на обеих версиях Unity)
- [ ] **TST-12**: PlayMode тесты для полного игрового цикла (старт → игра → конец)

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
| DOTS Physics 2D | Пакет не существует в production-ready виде, Physics2D остаётся на GameObjects |
| 2D Lighting | Новый функционал, не часть миграции 1:1 |
| Исправление существующих багов | Миграция 1:1, баги в отдельном milestone |
| Мобильные платформы | Будущие планы, текущий scope — Editor + Windows |
| Новые игровые механики | Только миграция существующего функционала |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| TOOL-01 | — | Pending |
| TOOL-02 | — | Pending |
| UPG-01 | — | Pending |
| UPG-02 | — | Pending |
| UPG-03 | — | Pending |
| UPG-04 | — | Pending |
| UPG-05 | — | Pending |
| MVVM-01 | — | Pending |
| MVVM-02 | — | Pending |
| MVVM-03 | — | Pending |
| MVVM-04 | — | Pending |
| MVVM-05 | — | Pending |
| MVVM-06 | — | Pending |
| URP-01 | — | Pending |
| URP-02 | — | Pending |
| URP-03 | — | Pending |
| URP-04 | — | Pending |
| URP-05 | — | Pending |
| URP-06 | — | Pending |
| ECS-01 | — | Pending |
| ECS-02 | — | Pending |
| ECS-03 | — | Pending |
| ECS-04 | — | Pending |
| ECS-05 | — | Pending |
| ECS-06 | — | Pending |
| ECS-07 | — | Pending |
| ECS-08 | — | Pending |
| ECS-09 | — | Pending |
| ECS-10 | — | Pending |
| ECS-11 | — | Pending |
| BRG-01 | — | Pending |
| BRG-02 | — | Pending |
| BRG-03 | — | Pending |
| BRG-04 | — | Pending |
| BRG-05 | — | Pending |
| BRG-06 | — | Pending |
| TST-01 | — | Pending |
| TST-02 | — | Pending |
| TST-03 | — | Pending |
| TST-04 | — | Pending |
| TST-05 | — | Pending |
| TST-06 | — | Pending |
| TST-07 | — | Pending |
| TST-08 | — | Pending |
| TST-09 | — | Pending |
| TST-10 | — | Pending |
| TST-11 | — | Pending |
| TST-12 | — | Pending |

**Coverage:**
- v1 requirements: 43 total
- Mapped to phases: 0
- Unmapped: 43

---
*Requirements defined: 2026-04-02*
*Last updated: 2026-04-02 after initial definition*
