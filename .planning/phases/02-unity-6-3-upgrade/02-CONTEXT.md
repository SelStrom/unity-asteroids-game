# Phase 2: Unity 6.3 Upgrade - Context

**Gathered:** 2026-04-02
**Status:** Ready for planning

<domain>
## Phase Boundary

Проект Asteroids открывается, компилируется и запускается в Unity 6.3 с полным геймплеем 1:1. Удаление зависимости com.unity.textmeshpro (встроенный TMP в Unity 6.3), замена deprecated API, обновление совместимых пакетов, удаление локальных TMP-ассетов, верификация всех игровых механик.

**Текущее состояние:** Проект уже открыт в Unity 6000.3.12f1 (6.3), пакеты автоматически обновлены Unity (ugui 2.0.0, test-framework 1.6.0, timeline 1.8.11). com.unity.textmeshpro удалён из manifest.json в Phase 1. shtl-mvvm v1.1.0 с условной компиляцией TMP подключён. Deprecated `FindObjectsOfType` не обнаружен в коде проекта.

</domain>

<decisions>
## Implementation Decisions

### Верификация геймплея
- **D-01:** Полный тест сюит — EditMode + PlayMode тесты для верификации работоспособности после апгрейда
- **D-02:** Состав тестов — на усмотрение Claude, балансируя покрытие и отсутствие дублирования с Phase 4 (ECS-тесты TST-01..TST-09) и Phase 5 (TST-12 полный игровой цикл)

### Локальные TMP-ассеты
- **D-03:** Удалить директорию Assets/TextMesh Pro/ целиком — Unity 6.3 использует встроенный TMP, локальные шейдеры/настройки могут конфликтовать

### Предупреждения компилятора
- **D-04:** Zero warnings — исправить все предупреждения компилятора в коде проекта (Assets/). Чистая консоль перед Phase 3

### Совместимость пакетов
- **D-05:** Исследовать и обновить все пакеты до версий, совместимых с Unity 6.3 (InputSystem, UGS Auth/Leaderboards, 2D feature pack и др.)

### Deprecated API
- **D-06:** Найти и заменить все deprecated API на актуальные аналоги Unity 6.3

### Claude's Discretion
- Конкретный состав EditMode и PlayMode тестов (D-02)
- Стратегия обновления пакетов (порядок, версии)
- Подход к поиску deprecated API (статический анализ, grep, Unity API Updater)
- Порядок выполнения шагов апгрейда

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Текущее состояние проекта
- `ProjectSettings/ProjectVersion.txt` — текущая версия Unity (6000.3.12f1)
- `Packages/manifest.json` — текущий список пакетов и версий
- `Packages/packages-lock.json` — полный граф зависимостей с версиями

### Анализ кодовой базы
- `.planning/codebase/STACK.md` — полный стек, все пакеты и версии до апгрейда
- `.planning/codebase/INTEGRATIONS.md` — интеграции, UGS и shtl-mvvm
- `.planning/codebase/CONCERNS.md` — известные баги и технический долг (out of scope, но важно не усугубить)

### Phase 1 контекст
- `.planning/phases/01-dev-tooling-shtl-mvvm-fix/01-CONTEXT.md` — решения по TMP-стратегии, shtl-mvvm, тестам

### Требования
- `.planning/REQUIREMENTS.md` §Unity 6.3 Upgrade — UPG-01..UPG-05

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Assets/Tests/EditMode/` — тестовый фреймворк уже настроен (Phase 1), EditMode assembly готов
- `Assets/Tests/PlayMode/` — PlayMode assembly готов (Phase 1)
- `Assets/Tests/EditMode/ShtlMvvm/TmpCompatibilityTests.cs` — существующие TMP-тесты, паттерн для новых

### Established Patterns
- MVVM через shtl-mvvm: `ObservableValue`, `ReactiveValue`, `EventBindingContext` — не трогать
- ECS-подобные системы на MonoBehaviour — не трогать (миграция в Phase 4)
- `using TMPro` / `TMP_Text` используется в 5 файлах (HudVisual, GuiText, ScoreVisual, LeaderboardEntryVisual, LeaderboardPrefabCreator) — должно работать через встроенный TMP

### Integration Points
- `Packages/manifest.json` — обновление версий пакетов
- `Assets/TextMesh Pro/` — удаление локальных TMP-ассетов (36+ файлов в git status)
- Потенциально deprecated API в коде (не обнаружено FindObjectsOfType, но исследователь должен проверить глубже)

</code_context>

<specifics>
## Specific Ideas

- Проект уже открыт в Unity 6.3 — основная часть апгрейда сделана автоматически Unity Hub
- Пользователь хочет полный тест сюит, но не дублировать Phase 4/5 тесты
- Чистая консоль (zero warnings) — критерий готовности к Phase 3

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 02-unity-6-3-upgrade*
*Context gathered: 2026-04-02*
