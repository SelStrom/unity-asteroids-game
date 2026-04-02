# Phase 1: Dev Tooling + shtl-mvvm Fix - Context

**Gathered:** 2026-04-02
**Status:** Ready for planning

<domain>
## Phase Boundary

Настройка инструментов разработки (Unity-MCP, тестовый фреймворк NUnit) и фикс библиотеки shtl-mvvm для совместимости с Unity 6.3 — удаление зависимости com.unity.textmeshpro через условную компиляцию с обратной совместимостью Unity 2022.3+. Фикс публикуется в отдельном репозитории и подключается к проекту Asteroids.

</domain>

<decisions>
## Implementation Decisions

### TMP-стратегия для shtl-mvvm
- **D-01:** Использовать `#if` директивы для условной компиляции — `#if UNITY_6000_0_OR_NEWER` для Unity 6+ пути (TMP через ugui), иначе через com.unity.textmeshpro
- **D-02:** Минимальная поддерживаемая версия Unity — 2022.3+ (текущий LTS и выше)
- **D-03:** Тестирование совместимости через EditMode тесты, проверяющие доступность TMP-типов и работу bindings

### Unity-MCP установка
- **D-04:** Установка через UPM git URL (аналогично shtl-mvvm — через manifest.json)
- **D-05:** Полный набор MCP-возможностей: инспекция сцены, чтение компонентов, консоль, запуск кода, модификация объектов, запуск тестов

### Тестовый фреймворк
- **D-06:** Тесты размещаются в `Assets/Tests/` с подкаталогами `EditMode/` и `PlayMode/`
- **D-07:** Конвенция именования тестов: `Method_Scenario_Expected` (например `ThrustSystem_ApplyThrust_IncreasesVelocity`)
- **D-08:** Отдельные test assemblies (.asmdef) для EditMode и PlayMode

### Git стратегия для shtl-mvvm
- **D-09:** Версионирование фикса через git tag (например v1.1.0), manifest.json ссылается на тег
- **D-10:** После публикации фикса — обновить git URL в manifest.json проекта Asteroids на новый тег

### Claude's Discretion
- Способ работы с asmdef ссылкой на Unity.TextMeshPro (удалить/оставить с versionDefines/override defines)
- Выбор workflow для работы с репозиторием shtl-mvvm (отдельный clone, local packages, или другой подход)
- Структура условной компиляции внутри кода shtl-mvvm (обёртки, абстракции, прямые #if)
- Конфигурация MCP сервера в Claude Code settings

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### shtl-mvvm библиотека
- `Library/PackageCache/com.shtl.mvvm@c7bda1c328/` — текущий исходный код библиотеки, asmdef файлы, package.json с зависимостями
- `Library/PackageCache/com.shtl.mvvm@c7bda1c328/README.md` — документация библиотеки (~800 строк)

### Unity-MCP
- Внешний репозиторий: `github.com/IvanMurzak/Unity-MCP` — документация по установке и настройке MCP сервера

### Анализ кодовой базы
- `.planning/codebase/STACK.md` — текущий стек, все пакеты и версии
- `.planning/codebase/INTEGRATIONS.md` — интеграции, включая shtl-mvvm и TMP
- `.planning/codebase/CONVENTIONS.md` — код-стайл и паттерны проекта

### Research
- `.planning/research/STACK.md` — исследование стека миграции, TMP-миграция
- `.planning/research/PITFALLS.md` — подводные камни, включая shtl-mvvm TMP-зависимость

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Packages/manifest.json` — текущая конфигурация пакетов, включая git URL для shtl-mvvm (`"com.shtl.mvvm": "https://github.com/SelStrom/shtl-mvvm.git"`)
- `com.unity.test-framework 1.1.33` — уже подключен, но тесты не написаны (0 тестов)

### Established Patterns
- Git-пакеты через UPM: shtl-mvvm уже подключен через git URL в manifest.json — Unity-MCP подключается аналогично
- Allman brackets, 4-space indent, всегда фигурные скобки — тесты должны следовать тому же стилю
- Naming: PascalCase классы, `_camelCase` приватные поля

### Integration Points
- `Packages/manifest.json` — добавление Unity-MCP пакета и обновление shtl-mvvm URL
- `Assets/Tests/` — новая директория для тестовых assemblies
- shtl-mvvm repo (github.com/SelStrom/shtl-mvvm) — внешний репозиторий для фикса

</code_context>

<specifics>
## Specific Ideas

- shtl-mvvm — собственная библиотека пользователя, полный контроль над кодом
- Фикс должен быть чистым и не ломать API для существующих пользователей библиотеки
- Unity-MCP нужен для помощи Claude с инспекцией сцены, запуском тестов и модификацией объектов в редакторе

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 01-dev-tooling-shtl-mvvm-fix*
*Context gathered: 2026-04-02*
