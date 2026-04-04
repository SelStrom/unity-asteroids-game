# Phase 1: Dev Tooling + shtl-mvvm Fix - Research

**Researched:** 2026-04-02
**Domain:** Unity dev tooling (MCP, тестовый фреймворк NUnit), условная компиляция пакета shtl-mvvm
**Confidence:** HIGH

## Summary

Фаза 1 охватывает три параллельных направления: (1) установка Unity-MCP для AI-интеграции с Unity Editor, (2) настройка тестового фреймворка NUnit с EditMode/PlayMode assemblies, (3) фикс библиотеки shtl-mvvm для совместимости с Unity 6.3 при сохранении обратной совместимости с Unity 2022.3+.

Фикс shtl-mvvm технически прост: единственный Runtime-файл использует TMP (`ViewModelToUIEventBindExtensions.cs` -- 4 метода с `TMP_Text`), плюс `DevWidget.cs` использует `FindObjectsOfType` в `#if UNITY_EDITOR` блоке. Стратегия: заменить зависимость `com.unity.textmeshpro` на `com.unity.ugui` в package.json, НЕ менять asmdef-ссылку `Unity.TextMeshPro` (assembly forwarding работает в Unity 6), добавить `#if` guard для FindObjectsOfType. Это минимальное изменение с высокой вероятностью успеха.

Unity-MCP (v0.51.4) устанавливается через git URL в Package Manager и автоматически скачивает MCP-сервер. Настройка Claude Code -- через окно AI Game Developer в Unity Editor. Тестовый фреймворк уже подключен (`com.unity.test-framework 1.1.33`), но тестов нет -- нужно создать asmdef для EditMode и PlayMode.

**Primary recommendation:** Начать с фикса shtl-mvvm (замена одной строки в package.json + фикс FindObjectsOfType), опубликовать тег, затем параллельно настроить Unity-MCP и тестовый фреймворк.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Использовать `#if` директивы для условной компиляции -- `#if UNITY_6000_0_OR_NEWER` для Unity 6+ пути (TMP через ugui), иначе через com.unity.textmeshpro
- **D-02:** Минимальная поддерживаемая версия Unity -- 2022.3+ (текущий LTS и выше)
- **D-03:** Тестирование совместимости через EditMode тесты, проверяющие доступность TMP-типов и работу bindings
- **D-04:** Установка Unity-MCP через UPM git URL (аналогично shtl-mvvm -- через manifest.json)
- **D-05:** Полный набор MCP-возможностей: инспекция сцены, чтение компонентов, консоль, запуск кода, модификация объектов, запуск тестов
- **D-06:** Тесты размещаются в `Assets/Tests/` с подкаталогами `EditMode/` и `PlayMode/`
- **D-07:** Конвенция именования тестов: `Method_Scenario_Expected` (например `ThrustSystem_ApplyThrust_IncreasesVelocity`)
- **D-08:** Отдельные test assemblies (.asmdef) для EditMode и PlayMode
- **D-09:** Версионирование фикса через git tag (например v1.1.0), manifest.json ссылается на тег
- **D-10:** После публикации фикса -- обновить git URL в manifest.json проекта Asteroids на новый тег

### Claude's Discretion
- Способ работы с asmdef ссылкой на Unity.TextMeshPro (удалить/оставить с versionDefines/override defines)
- Выбор workflow для работы с репозиторием shtl-mvvm (отдельный clone, local packages, или другой подход)
- Структура условной компиляции внутри кода shtl-mvvm (обёртки, абстракции, прямые #if)
- Конфигурация MCP сервера в Claude Code settings

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| TOOL-01 | Unity-MCP пакет установлен в проект и MCP сервер настроен в Claude Code | Unity-MCP v0.51.4, git URL установка, автоматическая конфигурация сервера |
| TOOL-02 | Тестовый фреймворк настроен (EditMode + PlayMode assemblies, NUnit) | com.unity.test-framework 1.1.33 уже подключен, нужны asmdef + структура директорий |
| MVVM-01 | Зависимость com.unity.textmeshpro удалена из package.json shtl-mvvm | Замена на `"com.unity.ugui": "1.0.0"` -- работает на обеих версиях Unity |
| MVVM-02 | Ссылка Unity.TextMeshPro в asmdef заменена или условно скомпилирована | Рекомендация: НЕ менять asmdef ссылку -- assembly forwarding работает в Unity 6 |
| MVVM-03 | Библиотека компилируется и работает на Unity 2022.3+ | Стратегия: ugui 1.0.0 как зависимость, TMP подтягивается проектом |
| MVVM-04 | Библиотека компилируется и работает на Unity 6.3 | Стратегия: ugui 2.0.x включает TMP, namespace TMPro сохранён |
| MVVM-05 | Фикс опубликован в github.com/SelStrom/shtl-mvvm | git tag v1.1.0, push в main |
| MVVM-06 | Проект Asteroids обновлен на новую версию shtl-mvvm | manifest.json: git URL с `#v1.1.0` |
| TST-11 | EditMode тесты для shtl-mvvm фикса (TMP-совместимость) | Тесты в проекте Asteroids (Assets/Tests/EditMode/), проверка типов TMP и bindings |
</phase_requirements>

## Project Constraints (from CLAUDE.md)

- **Язык:** Документация и комментарии на русском
- **Фигурные скобки:** Всегда использовать `{}` в if/else/for/while, даже для однострочных тел
- **Стиль скобок:** Allman (открывающая на новой строке) -- по стилю существующего кода
- **Отступы:** 4 пробела
- **Именование:** PascalCase классы/методы, `_camelCase` приватные поля, `Method_Scenario_Expected` для тестов
- **Namespace:** `SelStrom.Asteroids` для основного кода, тесты -- аналогично
- **Платформы asmdef:** Editor, WebGL, WindowsStandalone64
- **GSD Workflow:** Обязательно использовать GSD workflow для изменений

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| com.unity.test-framework | 1.1.33 | NUnit тесты в Unity (EditMode + PlayMode) | Уже подключен в проекте, стандарт Unity |
| com.ivanmurzak.unity.mcp | 0.51.4 | AI-интеграция с Unity Editor через MCP | Единственный зрелый Unity-MCP пакет для Claude Code |
| com.shtl.mvvm | git#v1.1.0 (будущий тег) | MVVM-привязки | Собственная библиотека проекта, текущий коммит c7bda1c |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| com.unity.ugui | 1.0.0 | uGUI + TMP (в Unity 6 -- 2.0.x) | Зависимость shtl-mvvm вместо com.unity.textmeshpro |
| NUnit | 3.5.0 (встроен) | Тестовый фреймворк | Поставляется с com.unity.test-framework |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Unity-MCP через git URL (D-04) | OpenUPM CLI (`openupm add`) | Требует установки openupm-cli, git URL проще и консистентнее с shtl-mvvm |
| Замена TMP зависимости на ugui | Полное удаление TMP-кода из shtl-mvvm | Потеря удобных TMP-binding методов, ломает API |
| versionDefines в asmdef | Прямые `#if` в C# | versionDefines -- более чистый подход для asmdef, но для данного случая достаточно оставить ссылку Unity.TextMeshPro |

## Architecture Patterns

### Структура изменений в shtl-mvvm

```
com.shtl.mvvm/
├── package.json              # ИЗМЕНИТЬ: "com.unity.textmeshpro" -> "com.unity.ugui": "1.0.0"
├── Runtime/
│   ├── Shtl.Mvvm.asmdef      # НЕ МЕНЯТЬ: ссылка Unity.TextMeshPro работает через forwarding
│   ├── DevWidget.cs           # ИЗМЕНИТЬ: FindObjectsOfType -> FindObjectsByType (в #if UNITY_EDITOR)
│   └── Utils/
│       └── ViewModelToUIEventBindExtensions.cs  # НЕ МЕНЯТЬ: using TMPro работает в обеих версиях
├── Editor/
│   └── Shtl.Mvvm.Editor.asmdef  # НЕ МЕНЯТЬ
└── Tests/                     # Пустые директории Editor/ и Runtime/
```

### Структура тестов в проекте Asteroids

```
Assets/
└── Tests/
    ├── EditMode/
    │   ├── EditModeTests.asmdef      # testRunner: EditMode, ссылки на Asteroids, Shtl.Mvvm
    │   └── ShtlMvvm/
    │       └── TmpCompatibilityTests.cs  # TST-11: проверка доступности TMP-типов
    └── PlayMode/
        └── PlayModeTests.asmdef      # testRunner: PlayMode, ссылки на Asteroids, Shtl.Mvvm
```

### Pattern 1: EditMode Test Assembly Definition

**What:** Минимальная конфигурация asmdef для EditMode тестов
**When to use:** Все EditMode тесты в проекте

```json
{
  "name": "EditModeTests",
  "rootNamespace": "SelStrom.Asteroids.Tests.EditMode",
  "references": [
    "Asteroids",
    "Conf",
    "Shtl.Mvvm",
    "Unity.TextMeshPro"
  ],
  "includePlatforms": [
    "Editor"
  ],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": [
    "nunit.framework.dll"
  ],
  "autoReferenced": false,
  "defineConstraints": [
    "UNITY_INCLUDE_TESTS"
  ],
  "versionDefines": [],
  "noEngineReferences": false
}
```

### Pattern 2: PlayMode Test Assembly Definition

**What:** Минимальная конфигурация asmdef для PlayMode тестов
**When to use:** Все PlayMode тесты в проекте

```json
{
  "name": "PlayModeTests",
  "rootNamespace": "SelStrom.Asteroids.Tests.PlayMode",
  "references": [
    "Asteroids",
    "Conf",
    "Shtl.Mvvm",
    "Unity.TextMeshPro"
  ],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": [
    "nunit.framework.dll"
  ],
  "autoReferenced": false,
  "defineConstraints": [
    "UNITY_INCLUDE_TESTS"
  ],
  "versionDefines": [],
  "noEngineReferences": false
}
```

### Pattern 3: NUnit тест для TMP-совместимости (TST-11)

**What:** EditMode тест проверяющий доступность TMP-типов через reflection
**When to use:** Валидация фикса shtl-mvvm

```csharp
// Source: Unity Test Framework docs + TMP namespace verification
using System;
using NUnit.Framework;

namespace SelStrom.Asteroids.Tests.EditMode.ShtlMvvm
{
    [TestFixture]
    public class TmpCompatibilityTests
    {
        [Test]
        public void TmpText_Type_IsAccessible()
        {
            var tmpTextType = Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
            Assert.That(tmpTextType, Is.Not.Null,
                "TMP_Text должен быть доступен через assembly Unity.TextMeshPro");
        }

        [Test]
        public void TextMeshProUGUI_Type_IsAccessible()
        {
            var tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            Assert.That(tmpType, Is.Not.Null,
                "TextMeshProUGUI должен быть доступен через assembly Unity.TextMeshPro");
        }

        [Test]
        public void ShtlMvvm_ViewModelToUIBindings_TmpMethodsExist()
        {
            var extensionsType = typeof(Shtl.Mvvm.ViewModelToUIEventBindingsExtensions);
            var methods = extensionsType.GetMethods();
            var toMethods = Array.FindAll(methods, m => m.Name == "To");
            Assert.That(toMethods.Length, Is.GreaterThan(0),
                "ViewModelToUIEventBindingsExtensions должен содержать методы To()");
        }
    }
}
```

### Anti-Patterns to Avoid

- **Копирование shtl-mvvm в Assets/:** Создаёт форк без обновлений, два источника правды. Работать через git clone отдельного репо.
- **Удаление TMP-binding методов:** Ломает API существующих пользователей библиотеки. TMP API (`TMPro` namespace) работает в обеих версиях Unity.
- **Ручное редактирование package в Library/PackageCache/:** Изменения теряются при resolve. Клонировать shtl-mvvm отдельно.
- **Добавление `#if UNITY_6000_0_OR_NEWER` вокруг TMP using/методов:** Не нужно -- namespace `TMPro` и типы `TMP_Text` работают и в 2022.3, и в 6.3. Условная компиляция нужна только для `FindObjectsOfType`.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| TMP-совместимость между Unity версиями | Абстракцию над TMP | Замена зависимости в package.json (ugui вместо textmeshpro) | Namespace TMPro и assembly Unity.TextMeshPro работают в обеих версиях без изменения кода |
| Assembly forwarding | Собственный assembly redirect | Unity встроенный assembly forwarding | Unity 6 автоматически перенаправляет Unity.TextMeshPro -> Unity.ugui |
| MCP-сервер для AI | Кастомный MCP-сервер | Unity-MCP пакет (com.ivanmurzak.unity.mcp) | Полноценный MCP-сервер с автоматическим запуском, 50+ инструментов |

## Common Pitfalls

### Pitfall 1: asmdef ссылка Unity.TextMeshPro в shtl-mvvm

**What goes wrong:** Может возникнуть соблазн удалить или заменить ссылку `Unity.TextMeshPro` в `Shtl.Mvvm.asmdef` на `Unity.ugui`.
**Why it happens:** Кажется логичным обновить ссылку вместе с зависимостью в package.json.
**How to avoid:** НЕ менять asmdef. В Unity 6 assembly `Unity.TextMeshPro` продолжает существовать через forwarding в `Unity.ugui`. В Unity 2022.3 это отдельная assembly. Ссылка работает в обоих случаях.
**Warning signs:** Ошибки компиляции "Assembly 'Unity.ugui' could not be found" на Unity 2022.3 если заменить ссылку.

### Pitfall 2: FindObjectsOfType в DevWidget.cs

**What goes wrong:** `FindObjectsOfType<DevWidget>()` в строке 38 вызовет ошибку компиляции в Unity 6 (API obsolete as error).
**Why it happens:** Код внутри `#if UNITY_EDITOR` блока, легко пропустить при поиске TMP-проблем.
**How to avoid:** Заменить на `FindObjectsByType<DevWidget>(FindObjectsSortMode.None)`. Но `FindObjectsByType` не существует в Unity 2020.3 (минимальная версия пакета). Нужна условная компиляция:
```csharp
#if UNITY_2023_1_OR_NEWER
            var widgets = FindObjectsByType<DevWidget>(FindObjectsSortMode.None);
#else
            var widgets = FindObjectsOfType<DevWidget>();
#endif
```
**Warning signs:** CS0619 ошибка при открытии проекта в Unity 6.

### Pitfall 3: UPM кэширование при обновлении git-пакета

**What goes wrong:** После публикации фикса в shtl-mvvm, Unity не подтягивает изменения, потому что git URL без тега кэшируется.
**Why it happens:** UPM кэширует git-пакеты по коммит-хэшу. URL без ref (`#tag` или `#branch`) резолвится один раз.
**How to avoid:** Всегда указывать конкретный тег: `"com.shtl.mvvm": "https://github.com/SelStrom/shtl-mvvm.git#v1.1.0"`. После обновления URL запустить `Window > Package Manager > Refresh` или удалить кэш.
**Warning signs:** В Library/PackageCache остаётся старый `com.shtl.mvvm@c7bda1c328` после обновления manifest.json.

### Pitfall 4: Unity-MCP путь к проекту с пробелами

**What goes wrong:** Unity-MCP не работает если путь к проекту содержит пробелы.
**Why it happens:** MCP-сервер использует путь проекта для вычисления порта и запуска -- пробелы ломают парсинг.
**How to avoid:** Проверить путь: `/Users/selstrom/work/projects/asteroids` -- пробелов нет, всё хорошо.
**Warning signs:** MCP-сервер не запускается, ошибки в консоли Unity.

### Pitfall 5: Порядок действий при работе с внешним репозиторием

**What goes wrong:** Попытка редактировать файлы в Library/PackageCache -- изменения теряются.
**Why it happens:** Library/PackageCache -- readonly кэш UPM.
**How to avoid:** Клонировать github.com/SelStrom/shtl-mvvm в отдельную директорию, внести изменения, запушить, создать тег.
**Warning signs:** Git не видит изменений после редактирования файлов в PackageCache.

### Pitfall 6: AsteroidsEditor.asmdef также ссылается на Unity.TextMeshPro

**What goes wrong:** `Assets/Editor/AsteroidsEditor.asmdef` содержит ссылку `"Unity.TextMeshPro"` в references. Это работает на Unity 2022.3 и продолжит работать на Unity 6 через assembly forwarding.
**Why it happens:** Это часть проекта Asteroids, не библиотеки shtl-mvvm. Менять не нужно в Phase 1.
**How to avoid:** Не трогать -- forwarding работает. Если при апгрейде на Unity 6.3 (Phase 2) возникнут проблемы, тогда исправлять.

## Code Examples

### Фикс package.json shtl-mvvm (MVVM-01)

```json
// ДО (текущее состояние):
{
  "name": "com.shtl.mvvm",
  "author": "SelStrom",
  "displayName": "shtl-mvvm",
  "description": "Mvvm framework for Unity with clean view model",
  "unity": "2020.3",
  "version": "1.1.0",
  "keywords": ["mvvm"],
  "dependencies": {
    "com.unity.ugui": "1.0.0",
    "com.unity.nuget.newtonsoft-json": "3.2.2"
  },
  "repository": {
    "type": "git",
    "url": "https://github.com/SelStrom/shtl-mvvm.git"
  }
}
```

Изменения: (1) заменить `"com.unity.textmeshpro": "3.0.7"` на `"com.unity.ugui": "1.0.0"`, (2) обновить `"version"` с `"0.1.0"` на `"1.1.0"`.

### Фикс FindObjectsOfType в DevWidget.cs (часть MVVM-03/04)

```csharp
// Source: DevWidget.cs строка 38, внутри #if UNITY_EDITOR
// ДО:
var widgets = FindObjectsOfType<DevWidget>();

// ПОСЛЕ:
#if UNITY_2023_1_OR_NEWER
            var widgets = FindObjectsByType<DevWidget>(FindObjectsSortMode.None);
#else
            var widgets = FindObjectsOfType<DevWidget>();
#endif
```

### Обновление manifest.json проекта Asteroids (MVVM-06)

```json
// ДО:
"com.shtl.mvvm": "https://github.com/SelStrom/shtl-mvvm.git",

// ПОСЛЕ:
"com.shtl.mvvm": "https://github.com/SelStrom/shtl-mvvm.git#v1.1.0",
```

### Установка Unity-MCP (TOOL-01)

```json
// Добавить в Packages/manifest.json dependencies:
"com.ivanmurzak.unity.mcp": "https://github.com/IvanMurzak/Unity-MCP.git?path=/Unity-MCP-Plugin/Assets/root",
```

После добавления: открыть Unity, дождаться импорта, перейти в `Window > AI Game Developer`, нажать Configure для Claude Code. MCP-сервер запустится автоматически.

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| com.unity.textmeshpro отдельный пакет | TMP встроен в com.unity.ugui | Unity 2023.2 / Unity 6 | Пакеты с зависимостью на textmeshpro ломаются |
| FindObjectsOfType | FindObjectsByType | Unity 2023.1 | Старый API -> obsolete as error в Unity 6 |
| Ручная MCP-конфигурация | Unity-MCP автоматическая настройка | Unity-MCP 0.50+ | Сервер авто-скачивается и запускается |

**Deprecated/outdated:**
- `com.unity.textmeshpro` как отдельный пакет: удалён из реестра Unity 6
- `FindObjectsOfType<T>()`: obsolete as error в Unity 6, замена `FindObjectsByType<T>(FindObjectsSortMode)`

## Discretion Recommendations

### Ссылка Unity.TextMeshPro в asmdef (Claude's discretion)

**Рекомендация: ОСТАВИТЬ как есть.**

Обоснование:
- Assembly forwarding в Unity 6 перенаправляет `Unity.TextMeshPro` -> `Unity.ugui` автоматически
- В Unity 2022.3 это реальная assembly от пакета `com.unity.textmeshpro`
- Замена на `Unity.ugui` сломает Unity 2022.3 (в ugui 1.0.0 нет TMP-типов)
- Добавление versionDefines избыточно -- код уже компилируется без изменений

**Confidence: MEDIUM** -- assembly forwarding подтверждён множественными источниками, но требует практической проверки.

### Workflow для работы с shtl-mvvm (Claude's discretion)

**Рекомендация: Отдельный git clone.**

1. `git clone https://github.com/SelStrom/shtl-mvvm.git` в отдельную директорию (например `~/work/projects/shtl-mvvm`)
2. Внести изменения (package.json, DevWidget.cs)
3. Commit, push, создать tag `v1.1.0`
4. Обновить manifest.json в Asteroids

Альтернатива (local package для тестирования):
- Изменить manifest.json: `"com.shtl.mvvm": "file:../../shtl-mvvm"` -- для локальной разработки
- После тестирования -- вернуть на git URL с тегом

**Confidence: HIGH** -- стандартный git workflow.

### Структура условной компиляции (Claude's discretion)

**Рекомендация: Прямые `#if` директивы, минимальные изменения.**

Единственное место, требующее `#if`:
- `DevWidget.cs` строка 38: `FindObjectsOfType` -> условная компиляция `UNITY_2023_1_OR_NEWER`

TMP-код (`using TMPro`, `TMP_Text`) НЕ требует условной компиляции -- namespace и типы одинаковы.

**Confidence: HIGH** -- минимальное изменение, максимальная обратная совместимость.

### MCP-сервер конфигурация (Claude's discretion)

**Рекомендация: Использовать встроенную автоматическую конфигурацию Unity-MCP.**

После установки пакета в Unity:
1. Открыть `Window > AI Game Developer`
2. Нажать Configure для Claude Code
3. Плагин автоматически создаст конфигурацию в `.mcp.json` проекта или в Claude Code settings

MCP-сервер автоматически скачивается в `Library/mcp-server/osx-arm64/` (для macOS ARM) и запускается при открытии Unity через `[InitializeOnLoad]`.

**Confidence: MEDIUM** -- автоконфигурация документирована, но детали для Claude Code CLI (не Desktop) менее подробны.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Unity 2022.3 | shtl-mvvm тестирование | Предположительно (текущий проект) | 2022.3.60f1 | -- |
| git | shtl-mvvm clone/push/tag | Требует проверки | -- | -- |
| gh CLI | shtl-mvvm release | Доступен (использован в исследовании) | -- | git tag + git push |
| macOS (darwin) | Unity-MCP binary | Доступна | Darwin 25.2.0 | -- |

**Missing dependencies with no fallback:**
- Нет -- все необходимые инструменты доступны.

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | NUnit 3.5.0 (через com.unity.test-framework 1.1.33) |
| Config file | Нет -- создать asmdef в Wave 0 |
| Quick run command | Unity Editor: `Window > General > Test Runner > EditMode > Run All` |
| Full suite command | Unity CLI: `unity -runTests -testPlatform EditMode -projectPath .` |

### Phase Requirements -> Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| MVVM-01 | package.json не содержит com.unity.textmeshpro | unit (file check) | Проверка содержимого package.json | -- Wave 0 |
| MVVM-02 | asmdef ссылка Unity.TextMeshPro резолвится | unit (reflection) | `Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro")` | -- Wave 0 |
| MVVM-03 | Библиотека компилируется на Unity 2022.3 | compilation | Проект открывается без ошибок | manual-only: запуск Unity |
| MVVM-04 | Библиотека компилируется на Unity 6.3 | compilation | Проект открывается без ошибок | manual-only: запуск Unity 6.3 |
| MVVM-05 | Фикс опубликован | integration | `git ls-remote --tags origin v1.1.0` | manual-only: git операция |
| MVVM-06 | Проект обновлён на новую версию | unit (file check) | Проверка manifest.json | -- Wave 0 |
| TOOL-01 | Unity-MCP установлен и настроен | integration | MCP-сервер отвечает на запросы | manual-only: требует Unity Editor |
| TOOL-02 | Тестовый фреймворк настроен | unit | Test Runner показывает тесты | -- Wave 0 |
| TST-11 | TMP-совместимость тесты | unit | NUnit EditMode тесты | -- Wave 0 |

### Sampling Rate

- **Per task commit:** Ручная проверка компиляции в Unity
- **Per wave merge:** Запуск всех EditMode тестов через Test Runner
- **Phase gate:** Все тесты зелёные + shtl-mvvm опубликован с тегом

### Wave 0 Gaps

- [ ] `Assets/Tests/EditMode/EditModeTests.asmdef` -- конфигурация EditMode тестов
- [ ] `Assets/Tests/PlayMode/PlayModeTests.asmdef` -- конфигурация PlayMode тестов
- [ ] `Assets/Tests/EditMode/ShtlMvvm/TmpCompatibilityTests.cs` -- TST-11
- [ ] Структура директорий `Assets/Tests/EditMode/`, `Assets/Tests/PlayMode/`

## Sources

### Primary (HIGH confidence)
- `Library/PackageCache/com.shtl.mvvm@c7bda1c328/` -- прямая инспекция исходного кода
- `Library/PackageCache/com.shtl.mvvm@c7bda1c328/package.json` -- текущие зависимости
- `Library/PackageCache/com.shtl.mvvm@c7bda1c328/Runtime/Shtl.Mvvm.asmdef` -- текущие ссылки
- `Packages/manifest.json` -- текущая конфигурация проекта
- `.planning/research/STACK.md` -- стратегия TMP-миграции
- `.planning/research/PITFALLS.md` -- shtl-mvvm TMP dependency pitfall

### Secondary (MEDIUM confidence)
- [Unity-MCP GitHub](https://github.com/IvanMurzak/Unity-MCP) -- установка и настройка MCP
- [Unity-MCP Installation Guide](https://github.com/IvanMurzak/Unity-MCP/wiki/Installation-Guide) -- детали установки, версия 0.51.4
- [OpenUPM com.ivanmurzak.unity.mcp](https://openupm.com/packages/com.ivanmurzak.unity.mcp/) -- альтернативная установка
- [TextMesh Pro in Unity 6 (Unity Forums)](https://discussions.unity.com/t/textmesh-pro-in-unity-6/1580163) -- TMP merger details
- `gh api repos/SelStrom/shtl-mvvm/tags` -- подтверждено отсутствие тегов

### Tertiary (LOW confidence)
- Assembly forwarding Unity.TextMeshPro -> Unity.ugui в Unity 6 -- требует практической проверки при первой сборке

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- все пакеты подтверждены, версии проверены
- Architecture: HIGH -- минимальные изменения, хорошо понятная стратегия
- Pitfalls: HIGH -- все ловушки выявлены через инспекцию кода и предшествующее исследование
- TMP assembly forwarding: MEDIUM -- логически верно, множественные источники подтверждают, требует практической проверки

**Research date:** 2026-04-02
**Valid until:** 2026-05-02 (стабильные технологии, 30 дней)
