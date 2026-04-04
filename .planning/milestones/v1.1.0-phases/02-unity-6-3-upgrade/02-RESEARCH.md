# Phase 2: Unity 6.3 Upgrade - Research

**Researched:** 2026-04-02
**Domain:** Unity Engine upgrade (2022.3 LTS -> 6000.3.12f1), TextMeshPro migration, deprecated API, package compatibility
**Confidence:** HIGH

## Summary

Проект Asteroids уже открыт в Unity 6000.3.12f1 (6.3). Unity Hub автоматически обновил большинство пакетов (ugui 2.0.0, test-framework 1.6.0, timeline 1.8.11). Пакет `com.unity.textmeshpro` удален из manifest.json в Phase 1. Ключевая работа Phase 2 -- удаление локальных TMP-ассетов (82 файла в `Assets/TextMesh Pro/`), обновление ссылок в asmdef-файлах, обновление пакетов до совместимых версий, устранение compiler warnings и полная верификация геймплея.

Анализ кодовой базы показал: deprecated API `FindObjectsOfType` **не используется** в коде проекта. Все `[SerializeField]` применены к полям (не свойствам), что совместимо с Unity 6.3. Основные риски: (1) font GUID `8f586378b4e144a9851e7b34d9b748ee` (LiberationSans SDF) из локальных TMP-ассетов привязан к 7+ UI-компонентам в сцене и 2 префабам -- при удалении папки ссылки сломаются, (2) три asmdef-файла (EditModeTests, PlayModeTests, AsteroidsEditor) содержат строковую ссылку `"Unity.TextMeshPro"`, (3) `TMP_InputField` используется в `ScoreVisual.cs`.

**Primary recommendation:** Удалить `Assets/TextMesh Pro/`, затем импортировать TMP Essential Resources из встроенного ugui пакета (`Window > TextMeshPro > Import TMP Essential Resources`) для восстановления GUID-ов. Проверить ссылки `"Unity.TextMeshPro"` в asmdef -- они должны резолвиться через GUID `6055be8ebefd69e48b49212b09b47b2f` (уже внутри `com.unity.ugui` 2.0.0).

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Полный тест сюит -- EditMode + PlayMode тесты для верификации работоспособности после апгрейда
- **D-02:** Состав тестов -- на усмотрение Claude, балансируя покрытие и отсутствие дублирования с Phase 4 (ECS-тесты TST-01..TST-09) и Phase 5 (TST-12 полный игровой цикл)
- **D-03:** Удалить директорию Assets/TextMesh Pro/ целиком -- Unity 6.3 использует встроенный TMP, локальные шейдеры/настройки могут конфликтовать
- **D-04:** Zero warnings -- исправить все предупреждения компилятора в коде проекта (Assets/). Чистая консоль перед Phase 3
- **D-05:** Исследовать и обновить все пакеты до версий, совместимых с Unity 6.3 (InputSystem, UGS Auth/Leaderboards, 2D feature pack и др.)
- **D-06:** Найти и заменить все deprecated API на актуальные аналоги Unity 6.3

### Claude's Discretion
- Конкретный состав EditMode и PlayMode тестов (D-02)
- Стратегия обновления пакетов (порядок, версии)
- Подход к поиску deprecated API (статический анализ, grep, Unity API Updater)
- Порядок выполнения шагов апгрейда

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| UPG-01 | Проект открывается и компилируется в Unity 6.3 без ошибок | Проект уже открыт в 6000.3.12f1. Нужно: удалить конфликтующие локальные TMP-ассеты, обновить asmdef ссылки, проверить компиляцию |
| UPG-02 | Все deprecated API заменены | Grep по коду: `FindObjectsOfType` не найден. Нет `ExecuteDefaultAction`, `DepthAuto`, `ShadowAuto`. Основной фокус -- compiler warnings и потенциальные runtime obsolete |
| UPG-03 | TextMeshPro работает как внутренний модуль | TMP теперь внутри `com.unity.ugui` 2.0.0. Assembly `Unity.TextMeshPro` доступна через GUID `6055be8ebefd69e48b49212b09b47b2f` в ugui. 5 файлов используют `using TMPro`. 3 asmdef ссылаются на `Unity.TextMeshPro` строкой |
| UPG-04 | Все пакеты совместимы с Unity 6.3 | Текущие версии: InputSystem 1.19.0, Auth 3.6.0, Leaderboards 2.3.3, ugui 2.0.0. Все подтверждены совместимыми через official docs |
| UPG-05 | Игра запускается и воспроизводит геймплей 1:1 | Требует: PlayMode тесты + ручная верификация. Font GUID миграция критична для UI |
</phase_requirements>

## Project Constraints (from CLAUDE.md)

- **Язык:** Документация и комментарии на русском
- **Фигурные скобки:** Всегда `{}` в if/else/for/while, даже для одной строки
- **Без однострочников:** Никогда не использовать однострочные конструкции
- **Порядок миграции:** Unity 6.3 -> URP -> DOTS
- **Функциональная эквивалентность:** Геймплей 1:1 после каждого этапа миграции
- **GSD Workflow:** Не делать прямых правок без GSD workflow

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Unity Engine | 6000.3.12f1 | Game engine | Уже установлен, целевая версия проекта |
| com.unity.ugui | 2.0.0 | UI + встроенный TMP | Unity 6.3 -- TMP интегрирован в ugui |
| com.unity.inputsystem | 1.19.0 | Input | Совместим с Unity 6.3 (подтверждено docs, April 2026) |
| com.unity.test-framework | 1.6.0 | Testing NUnit | Обновлен автоматически Unity Hub |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| com.unity.services.authentication | 3.6.0 | UGS Auth | Текущая версия совместима с 6.3. Обновление до 3.6.1 опционально |
| com.unity.services.leaderboards | 2.3.3 | UGS Leaderboards | Подтверждена совместимость с 6000.3 |
| com.unity.services.core | 1.16.0 | UGS Core | Базовый SDK |
| com.unity.feature.2d | 2.0.2 | 2D metapackage | Обновлен автоматически |
| com.unity.timeline | 1.8.11 | Timeline | Обновлен автоматически |
| com.shtl.mvvm | v1.1.0 (git) | MVVM bindings | Фикс TMP совместимости выполнен в Phase 1 |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Ручной grep deprecated API | Unity API Updater (автоматический) | API Updater встроен в Unity и срабатывает при открытии проекта, но не покрывает все случаи. Grep дает полный контроль |

## Architecture Patterns

### Текущая структура (не меняется)
```
Assets/
  Scripts/
    Application/   # Точка входа, Game, Application
    Model/          # ECS-подобные системы, компоненты
    View/           # MonoBehaviour визуалы, MVVM
    Configs/        # ScriptableObject конфиги
  Editor/           # Editor-only утилиты
  Tests/
    EditMode/       # EditMode тесты (NUnit)
    PlayMode/       # PlayMode тесты
  Media/            # Префабы, спрайты, звуки
  TextMesh Pro/     # УДАЛИТЬ -- замена на встроенный TMP
```

### Pattern 1: TMP Migration в Unity 6.3
**What:** TextMeshPro перенесен внутрь `com.unity.ugui` 2.0.0. Assembly `Unity.TextMeshPro` теперь виртуальная (forwarding assembly) внутри ugui.
**When to use:** При любой работе с TMP в Unity 6+.
**Key details:**
- Namespace `TMPro` остается тем же -- код `using TMPro` не требует изменений
- Assembly reference `Unity.TextMeshPro` резолвится через GUID `6055be8ebefd69e48b49212b09b47b2f` в ugui
- Строковые ссылки `"Unity.TextMeshPro"` в asmdef должны работать (assembly forwarding), но рекомендуется проверить
- Локальные TMP-ассеты (шейдеры, настройки, шрифты) в `Assets/TextMesh Pro/` конфликтуют с встроенными

### Pattern 2: Re-import TMP Essential Resources
**What:** После удаления локальных TMP-ассетов нужно импортировать встроенные ресурсы через `Window > TextMeshPro > Import TMP Essential Resources`.
**When to use:** Когда UI-компоненты теряют ссылки на шрифты.
**Key details:**
- Unity 6.3 содержит `TMP Essential Resources.unitypackage` (942 KB) внутри `com.unity.ugui`
- Import создает новые ассеты в `Assets/TextMesh Pro/` с правильными GUID для Unity 6.3
- Сцена `Main.unity` и 2 префаба (`gui_text.prefab`, `leaderboard_entry.prefab`) используют font GUID `8f586378b4e144a9851e7b34d9b748ee`
- Если после re-import GUID шрифта изменится -- потребуется переназначение через Inspector или скрипт

### Anti-Patterns to Avoid
- **Удаление TMP-папки без re-import:** UI сломается -- потеря ссылок на шрифты в 7+ компонентах сцены и 2 префабах
- **Обновление пакетов до bleeding edge:** Использовать только проверенные совместимые версии. Не обновлять без причины
- **Игнорирование compiler warnings:** D-04 требует zero warnings. Warnings могут стать errors в следующих версиях Unity

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Поиск deprecated API | Ручной review каждого файла | `grep -rn` по паттернам + Unity Console warnings | Automated search быстрее и надежнее |
| Font GUID fix | Ручное редактирование .unity/.prefab YAML | Unity Editor Inspector re-assign или TMP Essential Resources re-import | YAML-формат Unity хрупкий, ручное редактирование может сломать сериализацию |
| Compiler warnings | Ручной поиск | Unity Console / CI build log | Компилятор Unity покажет все warnings при первой сборке |

## Common Pitfalls

### Pitfall 1: Font GUID mismatch после удаления TMP-папки
**What goes wrong:** Сцена и префабы теряют ссылки на шрифт LiberationSans SDF (GUID `8f586378b4e144a9851e7b34d9b748ee`). Все TMP-тексты показывают `<Missing Font>`.
**Why it happens:** Локальные TMP Essential Resources содержат font asset с одним GUID. Встроенные в Unity 6.3 ресурсы могут иметь другой GUID.
**How to avoid:** (1) Удалить `Assets/TextMesh Pro/`, (2) Сразу импортировать TMP Essential Resources через `Window > TextMeshPro > Import TMP Essential Resources`, (3) Проверить в Inspector, что шрифты назначены. Если GUID совпадает -- проблемы не будет. Если отличается -- переназначить через Inspector.
**Warning signs:** `Missing (Font Asset)` в Inspector на любом TMP-компоненте.

### Pitfall 2: asmdef строковые ссылки на Unity.TextMeshPro
**What goes wrong:** `EditModeTests.asmdef`, `PlayModeTests.asmdef` и `AsteroidsEditor.asmdef` ссылаются на `"Unity.TextMeshPro"` строкой, а не GUID. В Unity 6.3 assembly `Unity.TextMeshPro` определена внутри `com.unity.ugui`. Строковые ссылки должны работать через assembly resolution, но если нет -- ошибки компиляции.
**Why it happens:** Phase 1 создала asmdef с строковыми ссылками, а основной `Asteroids.asmdef` использует GUID.
**How to avoid:** Проверить компиляцию. Если ошибки -- заменить строковую ссылку `"Unity.TextMeshPro"` на GUID `"GUID:6055be8ebefd69e48b49212b09b47b2f"`.
**Warning signs:** `Assembly 'Unity.TextMeshPro' not found` в Console.

### Pitfall 3: SerializeField на свойствах (Unity 6.3 breaking change)
**What goes wrong:** Unity 6.3 ужесточил `[SerializeField]` -- теперь он работает ТОЛЬКО с полями. Применение к свойствам/методам вызывает ошибку компиляции.
**Why it happens:** Breaking change в Unity 6.3: `[SerializeField] attribute has been updated so that you can only apply it to fields`.
**How to avoid:** Проверено: все `[SerializeField]` в проекте применены к полям (`private TMP_Text _coordinates = default;`). **Этот pitfall не актуален для данного проекта**, но нужно проверить при добавлении нового кода.
**Warning signs:** `CS0592: Attribute 'SerializeField' is not valid on this declaration type`.

### Pitfall 4: Локальные TMP шейдеры конфликтуют со встроенными
**What goes wrong:** Если оставить локальные шейдеры (`Assets/TextMesh Pro/Shaders/`) рядом со встроенными, Unity может подхватить неправильную версию. Результат -- артефакты рендеринга текста или розовые шейдеры.
**Why it happens:** Unity ищет шейдеры по имени. Два шейдера с одинаковым именем -- неопределенное поведение.
**How to avoid:** Удалить ВСЮ папку `Assets/TextMesh Pro/` целиком (D-03).
**Warning signs:** Розовый текст, артефакты в шрифтах, shader warnings в Console.

### Pitfall 5: TMP_InputField в ScoreVisual.cs
**What goes wrong:** `TMP_InputField` может иметь измененный API или поведение в новой версии TMP (внутри ugui).
**Why it happens:** TMP_InputField переехал из отдельного пакета внутрь ugui. API стабилен, но поведение может отличаться.
**How to avoid:** Проверить поле ввода имени в EndGame экране. Убедиться, что ввод текста работает и submit корректно отправляет данные в leaderboard.
**Warning signs:** Не работает ввод текста, не отображается каретка, не срабатывает submit.

## Code Examples

### Текущие файлы с `using TMPro` (не требуют изменений)
```csharp
// Файлы, использующие TMPro -- namespace остается тем же в Unity 6.3:
// - Assets/Scripts/View/HudVisual.cs (TMP_Text x5)
// - Assets/Scripts/View/Components/GuiText.cs (TMP_Text x1)
// - Assets/Scripts/View/ScoreVisual.cs (TMP_Text x2, TMP_InputField x1)
// - Assets/Scripts/View/LeaderboardEntryVisual.cs (TMP_Text x3)
// - Assets/Editor/LeaderboardPrefabCreator.cs (TMP_Text, TextMeshProUGUI)
```

### asmdef reference fix (если строковая ссылка не резолвится)
```json
// Заменить:
"Unity.TextMeshPro"
// На GUID-ссылку:
"GUID:6055be8ebefd69e48b49212b09b47b2f"
```

### Паттерн deprecated API replacement (если найдется)
```csharp
// Старый:
// var obj = FindObjectOfType<T>();
// Новый (Unity 6+):
// var obj = FindFirstObjectByType<T>();

// Старый:
// var objs = FindObjectsOfType<T>();
// Новый:
// var objs = FindObjectsByType<T>(FindObjectsSortMode.None);
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `com.unity.textmeshpro` отдельный пакет | TMP встроен в `com.unity.ugui` | Unity 2023.2+ | `using TMPro` работает, assembly `Unity.TextMeshPro` через forwarding |
| `FindObjectsOfType<T>()` | `FindObjectsByType<T>(FindObjectsSortMode.None)` | Unity 2022.3+ | Не используется в проекте -- не актуально |
| `[SerializeField]` на свойствах | `[field: SerializeField]` или только на полях | Unity 6.3 | Не используется в проекте -- не актуально |

**Deprecated/outdated:**
- `com.unity.textmeshpro` пакет: заменен на встроенный TMP в ugui 2.0.0
- `FindObjectsOfType`: заменен на `FindObjectsByType` с `FindObjectsSortMode`

## Open Questions

1. **Font GUID preservation при re-import TMP Essential Resources**
   - What we know: Локальные TMP-ассеты имеют GUID `8f586378b4e144a9851e7b34d9b748ee` для LiberationSans SDF. Unity 6.3 ugui содержит `TMP Essential Resources.unitypackage`.
   - What's unclear: Совпадает ли GUID шрифта в новом unitypackage с локальным. Если совпадает -- миграция seamless. Если нет -- нужно переназначение.
   - Recommendation: Удалить папку, импортировать Essential Resources, проверить. Если GUID не совпал -- переназначить шрифты в Inspector на сцене и 2 префабах. Вероятнее всего GUID совпадает, т.к. Unity сохраняет обратную совместимость ресурсов.

2. **Compiler warnings**
   - What we know: Анализ кода не выявил deprecated API. Нет `#pragma warning` подавлений.
   - What's unclear: Какие именно warnings выдает компилятор Unity 6.3 при сборке проекта. Возможны warnings из пакетов (не из Assets/).
   - Recommendation: Собрать проект, собрать список warnings из Console, исправить только warnings из `Assets/`.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Unity Editor | Весь phase | Требует Unity Hub | 6000.3.12f1 | -- |
| Git | Version control | Проверен (git repo) | -- | -- |

**Missing dependencies with no fallback:**
- Unity Editor 6000.3.12f1 должен быть открыт для выполнения TMP re-import и PlayMode тестов. Это интерактивная операция через Unity Editor.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Unity Test Framework 1.6.0 (NUnit) |
| Config file | `Assets/Tests/EditMode/EditModeTests.asmdef`, `Assets/Tests/PlayMode/PlayModeTests.asmdef` |
| Quick run command | Unity Editor > Window > General > Test Runner > EditMode > Run All |
| Full suite command | Unity Editor > Window > General > Test Runner > All > Run All |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| UPG-01 | Проект компилируется без ошибок | smoke | Открытие проекта в Editor + 0 errors в Console | manual |
| UPG-02 | Deprecated API заменены | unit (EditMode) | Test Runner > EditMode > Run | Wave 0 |
| UPG-03 | TMP работает как встроенный модуль | unit (EditMode) | Test Runner > EditMode > TmpCompatibilityTests | Существует (Phase 1) |
| UPG-04 | Пакеты совместимы | smoke | Открытие проекта + 0 errors | manual |
| UPG-05 | Геймплей 1:1 | PlayMode + manual | Test Runner > PlayMode > Run + ручной UAT | Wave 0 (PlayMode) |

### Рекомендуемый состав тестов (D-02)

**EditMode тесты (не дублируют Phase 4/5):**
- `UpgradeValidationTests.cs` -- проверка отсутствия deprecated API через reflection/compilation markers
- `TmpIntegrationTests.cs` -- расширение существующих TMP-тестов: проверка TMP_InputField, TextMeshProUGUI availability
- `PackageCompatibilityTests.cs` -- проверка наличия ключевых типов из пакетов (InputSystem, UGS)

**PlayMode тесты (не дублируют TST-12):**
- `GameplaySmoke_AfterUpgrade.cs` -- базовый smoke: сцена загружается, корабль существует, input работает. НЕ полный цикл (это TST-12).

### Sampling Rate
- **Per task commit:** EditMode tests через Test Runner
- **Per wave merge:** Full suite (EditMode + PlayMode)
- **Phase gate:** Full suite green + ручной UAT геймплея

### Wave 0 Gaps
- [ ] `Assets/Tests/EditMode/Upgrade/UpgradeValidationTests.cs` -- covers UPG-02
- [ ] `Assets/Tests/EditMode/Upgrade/TmpIntegrationTests.cs` -- extends UPG-03
- [ ] `Assets/Tests/PlayMode/Upgrade/GameplaySmokeTests.cs` -- covers UPG-05

## Detailed Findings

### Deprecated API Analysis (UPG-02)

**Проверено grep по всем `.cs` в `Assets/`:**

| Pattern | Found | Action |
|---------|-------|--------|
| `FindObjectsOfType` | 0 | Не требуется |
| `FindObjectOfType` | 0 | Не требуется |
| `ExecuteDefaultAction` | 0 | Не требуется |
| `PreventDefault` | 0 | Не требуется |
| `DepthAuto` / `ShadowAuto` | 0 | Не требуется |
| `OnBecameInvisible` | 0 | Не требуется |
| `SendMessage` / `BroadcastMessage` | 0 | Не требуется |
| `#pragma warning` / `[Obsolete]` | 0 | Не требуется |

**Вывод:** Код проекта не использует известные deprecated API. Основной фокус D-06 -- compiler warnings, которые выявятся при сборке.

### TextMeshPro Migration Details (UPG-03)

**Текущее состояние:**
- `com.unity.textmeshpro` удален из `manifest.json` (Phase 1)
- `com.unity.ugui` обновлен до 2.0.0 (содержит TMP)
- Assembly `Unity.TextMeshPro` доступна через GUID `6055be8ebefd69e48b49212b09b47b2f` внутри ugui
- 82 файла в `Assets/TextMesh Pro/` -- локальные TMP-ассеты (шейдеры, шрифты, настройки)

**Файлы, требующие внимания:**

| File | Issue | Action |
|------|-------|--------|
| `Assets/TextMesh Pro/` (82 files) | Конфликт с встроенным TMP | Удалить целиком (D-03) |
| `Assets/Tests/EditMode/EditModeTests.asmdef` | Строковая ссылка `"Unity.TextMeshPro"` | Проверить компиляцию, при ошибке -- заменить на GUID |
| `Assets/Tests/PlayMode/PlayModeTests.asmdef` | Строковая ссылка `"Unity.TextMeshPro"` | Аналогично |
| `Assets/Editor/AsteroidsEditor.asmdef` | Строковая ссылка `"Unity.TextMeshPro"` | Аналогично |
| `Assets/Scenes/Main.unity` | 7 font references к GUID `8f586378b4e144a9851e7b34d9b748ee` | Проверить после re-import |
| `Assets/Media/prefabs/gui/gui_text.prefab` | Font reference | Проверить после re-import |
| `Assets/Media/prefabs/gui/leaderboard_entry.prefab` | Font reference | Проверить после re-import |

### Package Compatibility (UPG-04)

| Package | Current | Compatible with 6.3 | Source | Action |
|---------|---------|---------------------|--------|--------|
| com.unity.inputsystem | 1.19.0 | Да | [Unity Docs 6000.3](https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.inputsystem.html) | Нет |
| com.unity.services.authentication | 3.6.0 | Да (3.6.1 available) | [Unity Docs 6000.3](https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.services.authentication.html) | Опционально обновить до 3.6.1 |
| com.unity.services.leaderboards | 2.3.3 | Да | [Unity Docs 6000.3](https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.services.leaderboards.html) | Нет |
| com.unity.services.core | 1.16.0 | Да | Implicit (Auth/LB работают) | Нет |
| com.unity.ugui | 2.0.0 | Да | Обновлен автоматически | Нет |
| com.unity.test-framework | 1.6.0 | Да | Обновлен автоматически | Нет |
| com.unity.timeline | 1.8.11 | Да | Обновлен автоматически | Нет |
| com.unity.feature.2d | 2.0.2 | Да | Обновлен автоматически | Нет |
| com.shtl.mvvm | v1.1.0 | Да | Фикс Phase 1 (TST-11 pass) | Нет |
| com.unity.collab-proxy | 2.11.4 | Да | Обновлен автоматически | Нет |
| com.unity.ide.rider | 3.0.39 | Да | Без изменений | Нет |

## Sources

### Primary (HIGH confidence)
- Grep по кодовой базе проекта -- все `*.cs`, `*.asmdef` файлы в `Assets/`
- `Packages/manifest.json` -- текущие версии пакетов
- `Packages/packages-lock.json` -- полный граф зависимостей
- `Library/PackageCache/com.unity.ugui@8ccc29d23a79/` -- структура встроенного TMP

### Secondary (MEDIUM confidence)
- [Unity 6.3 Upgrade Guide](https://docs.unity3d.com/6000.3/Documentation/Manual/UpgradeGuideUnity63.html) -- breaking changes
- [Unity 6.0 Upgrade Guide](https://docs.unity3d.com/6000.1/Documentation/Manual/UpgradeGuideUnity6.html) -- deprecated APIs (FindObjectsByType и др.)
- [Unity 6000.3 InputSystem docs](https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.inputsystem.html) -- package compatibility
- [Unity 6000.3 Authentication docs](https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.services.authentication.html) -- package compatibility
- [Unity 6000.3 Leaderboards docs](https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.services.leaderboards.html) -- package compatibility
- [TextMesh Pro in Unity 6 Discussion](https://discussions.unity.com/t/textmesh-pro-in-unity-6/1580163) -- TMP migration experience

### Tertiary (LOW confidence)
- Font GUID preservation при re-import -- не подтверждено official docs, основано на опыте Unity backward compatibility

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- все версии проверены через official docs и packages-lock.json
- Architecture: HIGH -- кодовая база проанализирована grep, все deprecated API проверены
- Pitfalls: HIGH -- font GUID риск идентифицирован через анализ YAML сцены и prefabs
- TMP migration: MEDIUM -- точный GUID preservation при re-import не подтвержден

**Research date:** 2026-04-02
**Valid until:** 2026-05-02 (30 days -- стабильный Unity LTS release)
