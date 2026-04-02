# Tech Stack

**Дата анализа:** 2026-03-26

## Primary Language & Runtime

**Язык:**
- C# 9.0 (указан в `<LangVersion>9.0</LangVersion>` во всех `.csproj` файлах)

**Целевой фреймворк:**
- .NET Standard 2.1 / .NET Framework 4.7.1
  - `<TargetFrameworkVersion>v4.7.1</TargetFrameworkVersion>` — формально в `.csproj` (генерируется Rider)
  - Фактически активны define-символы `NET_STANDARD_2_1`, `NETSTANDARD2_1` — Unity использует .NET Standard 2.1 профиль
  - Scripting backend: **Mono** (`ENABLE_MONO` в define-символах)

**Среда выполнения:**
- Mono (встроенный в Unity)
- Целевые платформы согласно `.asmdef`: `Editor`, `WebGL`, `WindowsStandalone64`

---

## Framework / Engine

**Игровой движок:**
- **Unity 2022.3.60f1** (LTS)
  - Файл версии: `ProjectSettings/ProjectVersion.txt`
  - Редактор установлен по пути: `/Applications/Unity/Hub/Editor/2022.3.60f1/`
  - Разрядность: 64-bit (`UNITY_64`, `PLATFORM_ARCH_64`)
  - Render pipeline: встроенный (Built-in Render Pipeline, не URP/HDRP)
  - Рендеринг 2D: Physics2D, Sprites, Tilemap, Particle System

---

## Key Libraries & Packages

### Прямые зависимости (из `Packages/manifest.json`)

| Пакет | Версия | Назначение |
|---|---|---|
| `com.shtl.mvvm` | git (hash `c7bda1c`) | MVVM-фреймворк собственной разработки ([SelStrom/shtl-mvvm](https://github.com/SelStrom/shtl-mvvm.git)), реактивные привязки `ObservableField` |
| `com.unity.inputsystem` | 1.19.0 | Новая система ввода Unity; генерируются классы в `Assets/Scripts/Input/Generated/` |
| `com.unity.textmeshpro` | 3.0.9 | Текстовые компоненты UI |
| `com.unity.ugui` | 1.0.0 | Стандартный UI (uGUI) |
| `com.unity.timeline` | 1.7.7 | Timeline-анимация |
| `com.unity.feature.2d` | 2.0.1 | Метапакет 2D-инструментов |
| `com.unity.services.core` | 1.16.0 | Базовый SDK Unity Gaming Services |
| `com.unity.services.authentication` | 3.6.0 | Анонимная аутентификация через UGS |
| `com.unity.services.leaderboards` | 2.3.3 | Таблица лидеров UGS |
| `com.unity.test-framework` | 1.1.33 | Тестирование (NUnit внутри Unity) |
| `com.unity.collab-proxy` | 2.7.1 | Unity Version Control (Plastic SCM) |
| `com.unity.ide.rider` | 3.0.39 | Интеграция с JetBrains Rider |
| `com.unity.ide.vscode` | 1.2.5 | Интеграция с VS Code |

### Транзитивные зависимости (из `Packages/packages-lock.json`)

| Пакет | Версия | Назначение |
|---|---|---|
| `com.unity.nuget.newtonsoft-json` | 3.2.2 | Json.NET (требуется `com.shtl.mvvm`) |
| `com.unity.burst` | 1.8.19 | Компилятор Burst (транзитивно через 2D) |
| `com.unity.collections` | 1.2.4 | Native-коллекции (транзитивно) |
| `com.unity.mathematics` | 1.2.6 | Математическая библиотека (транзитивно) |
| `com.unity.2d.animation` | 9.1.3 | 2D-анимация костей |
| `com.unity.2d.aseprite` | 1.1.8 | Импорт Aseprite-файлов |
| `com.unity.2d.pixel-perfect` | 5.0.3 | Pixel-perfect камера |
| `com.unity.2d.psdimporter` | 8.0.5 | Импорт PSD |
| `com.unity.2d.spriteshape` | 9.0.5 | SpriteShape |
| `com.unity.2d.tilemap.extras` | 3.1.3 | Расширения Tilemap |

---

## Build & Package Management

**Менеджер пакетов:**
- **Unity Package Manager (UPM)**
  - Файл зависимостей: `Packages/manifest.json`
  - Lockfile: `Packages/packages-lock.json` (присутствует, зафиксированы версии)
  - Реестр по умолчанию: `https://packages.unity.com`
  - Один git-пакет: `com.shtl.mvvm` подключён напрямую через git URL

**Сборочная система:**
- MSBuild (генерируется Unity/Rider), формат `.csproj` — ToolsVersion 4.0
- Файл решения: `asteroids.sln`
- Source generators: `Unity.SourceGenerators.dll`, `Unity.Properties.SourceGenerator.dll` (подключены как `<Analyzer>`)
- Unsafe-код: запрещён (`<AllowUnsafeBlocks>False</AllowUnsafeBlocks>`)
- Предупреждения 0169, 0649 подавлены

**Определённые сборки (Assembly Definition Files):**
- `Assets/Asteroids.asmdef` → сборка `Asteroids` (основная логика + сервисы)
- `Assets/Scripts/Configs/Configs.asmdef` → сборка `Conf` (данные конфигурации)
- `Assets/Editor/AsteroidsEditor.asmdef` → сборка `AsteroidsEditor` (только Editor)

---

## Development Tools

**IDE:**
- JetBrains Rider (основной — `com.unity.ide.rider` 3.0.39; файл настроек `Asteroids.csproj.DotSettings`)
- VS Code (дополнительный — `com.unity.ide.vscode` 1.2.5)

**Анализаторы кода:**
- Unity Source Generators (`Unity.SourceGenerators.dll`, `Unity.Properties.SourceGenerator.dll`)

**Контроль версий:**
- Unity Version Control / Plastic SCM (`com.unity.collab-proxy` 2.7.1)
- Git (файлы `.meta` указывают на использование Git для внешних пакетов)

**Профилировщик памяти:**
- Unity Memory Profiler (пакет присутствует как `.csproj` файлы: `Unity.MemoryProfiler.Editor.csproj`, `Unity.MemoryProfiler.csproj`)

**Input Actions:**
- Файл `Assets/Input/player_actions.inputactions` — описание входных действий
- Сгенерированный C#-класс: `Assets/Scripts/Input/Generated/PlayerActions.cs`

---

*Анализ стека: 2026-03-26*
