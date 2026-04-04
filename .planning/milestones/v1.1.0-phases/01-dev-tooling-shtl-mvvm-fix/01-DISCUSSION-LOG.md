# Phase 1: Dev Tooling + shtl-mvvm Fix - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-02
**Phase:** 01-dev-tooling-shtl-mvvm-fix
**Areas discussed:** TMP-стратегия, Unity-MCP, Тесты, Git стратегия

---

## TMP-стратегия

### Как убрать зависимость com.unity.textmeshpro из shtl-mvvm?

| Option | Description | Selected |
|--------|-------------|----------|
| #if директивы | Условная компиляция #if UNITY_6000_0_OR_NEWER — на Unity 6+ использовать ugui, на старых TMP | ✓ |
| Замена на ugui | Полностью заменить зависимость на com.unity.ugui | |
| asmdef опционально | Сделать ссылку Unity.TextMeshPro опциональной в asmdef + версионные defines | |

**User's choice:** #if директивы
**Notes:** —

### Как поступить с asmdef ссылкой на Unity.TextMeshPro?

| Option | Description | Selected |
|--------|-------------|----------|
| Удалить | Убрать ссылку — если TMP классы доступны через ugui assembly | |
| Override defines | Оставить ссылку + добавить versionDefines для условной компиляции | |
| Ты решай | Claude выберет оптимальный подход при планировании | ✓ |

**User's choice:** Claude's discretion

### Минимальная версия Unity для совместимости?

| Option | Description | Selected |
|--------|-------------|----------|
| Unity 2022.3+ | С текущего LTS и выше (в т.ч. Unity 6) | ✓ |
| Unity 2021.3+ | Более широкая совместимость | |
| Unity 6+ только | Только новые версии | |

**User's choice:** Unity 2022.3+

### Тестирование совместимости?

| Option | Description | Selected |
|--------|-------------|----------|
| Вручную | Открыть проект в обеих версиях редактора | |
| EditMode тесты | Написать тесты проверяющие доступность TMP-типов и bindings | ✓ |
| Ты решай | Claude выберет подход | |

**User's choice:** EditMode тесты

---

## Unity-MCP

### Способ установки?

| Option | Description | Selected |
|--------|-------------|----------|
| UPM git URL | Добавить через manifest.json как git-пакет | ✓ |
| .unitypackage | Скачать и импортировать как .unitypackage | |
| Ты решай | Claude выберет оптимальный способ | |

**User's choice:** UPM git URL

### Набор MCP-возможностей?

| Option | Description | Selected |
|--------|-------------|----------|
| Базовые | Инспекция сцены, чтение компонентов, консоль | |
| Полные | Базовые + запуск кода, модификация объектов, запуск тестов | ✓ |
| Ты решай | Настроить по документации Unity-MCP | |

**User's choice:** Полные

---

## Тесты

### Расположение тестов?

| Option | Description | Selected |
|--------|-------------|----------|
| Assets/Tests/ | Отдельная папка с EditMode/ и PlayMode/ подкаталогами | ✓ |
| Рядом с кодом | Тесты рядом с тестируемым кодом | |
| Ты решай | Claude выберет подходящую структуру | |

**User's choice:** Assets/Tests/

### Конвенция именования?

| Option | Description | Selected |
|--------|-------------|----------|
| Method_Scenario_Expected | ThrustSystem_ApplyThrust_IncreasesVelocity | ✓ |
| Should_Expected_When | Should_IncreaseVelocity_When_ThrustApplied | |
| Ты решай | Claude выберет стиль по конвенциям проекта | |

**User's choice:** Method_Scenario_Expected

---

## Git стратегия

### Работа с репозиторием shtl-mvvm?

| Option | Description | Selected |
|--------|-------------|----------|
| Отдельный clone | Клонировать рядом, сделать фикс, запушить | |
| Local packages | file: ссылка для локальной разработки, потом переключить на git | |
| Ты решай | Claude выберет оптимальный workflow | ✓ |

**User's choice:** Claude's discretion

### Версионирование фикса?

| Option | Description | Selected |
|--------|-------------|----------|
| Git hash | Как сейчас — ссылка на конкретный коммит | |
| Git tag | Создать tag (v1.1.0) и ссылаться на него | ✓ |
| Ты решай | Claude выберет | |

**User's choice:** Git tag

---

## Claude's Discretion

- Способ работы с asmdef ссылкой на Unity.TextMeshPro
- Workflow для работы с репозиторием shtl-mvvm

## Deferred Ideas

None — discussion stayed within phase scope
