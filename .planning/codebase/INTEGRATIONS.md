# Integrations

**Дата анализа:** 2026-03-26

---

## External APIs

### Unity Gaming Services (UGS)

Проект использует облачную платформу Unity Gaming Services для аутентификации игроков и таблицы лидеров.

**Точки подключения в коде:**
- `Assets/Scripts/Application/Leaderboard/UnityAuthProxy.cs` — инициализация UGS и анонимный вход
- `Assets/Scripts/Application/Leaderboard/UnityLeaderboardProxy.cs` — запись очков и чтение таблицы
- `Assets/Scripts/Application/Leaderboard/LeaderboardService.cs` — сервисный слой (оркестрирует Auth + Leaderboard)
- `Assets/Scripts/Application/Leaderboard/IAuthProxy.cs` — интерфейс для подмены реализации
- `Assets/Scripts/Application/Leaderboard/ILeaderboardProxy.cs` — интерфейс для подмены реализации

**Используемые namespace:**
```csharp
using Unity.Services.Core;           // UnityServices.InitializeAsync()
using Unity.Services.Authentication; // AuthenticationService.Instance
using Unity.Services.Leaderboards;   // LeaderboardsService.Instance
```

**Операции с Leaderboard API:**
- `LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score, options)` — отправка счёта
- `LeaderboardsService.Instance.GetScoresAsync(leaderboardId, options)` — топ игроков
- `LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId, options)` — счёт текущего игрока

**Метаданные записи лидерборда:** имя игрока передаётся как JSON-поле `playerName` в поле `Metadata`.

---

## Third-party Services

### Unity Version Control (Plastic SCM)

- Пакет: `com.unity.collab-proxy` 2.7.1
- Файл настроек: `ProjectSettings/VersionControlSettings.asset`
- Используется для версионирования ассетов и кода внутри Unity Editor

### Unity Connect (Analytics, Ads, Crash Reporting)

- Файл: `ProjectSettings/UnityConnectSettings.asset`
- Состояние: **все сервисы отключены** (`m_Enabled: 0`)
  - UnityAnalytics: выключена
  - UnityAds: выключена
  - CrashReporting: выключена
  - UnityPurchasing: выключена

---

## Platform SDKs

### Unity Authentication SDK

- Пакет: `com.unity.services.authentication` 3.6.0
- Режим: анонимная аутентификация (`SignInAnonymouslyAsync`)
- Реализация: `Assets/Scripts/Application/Leaderboard/UnityAuthProxy.cs`
- Зависит от: `com.unity.services.core` 1.16.0

### Unity Leaderboards SDK

- Пакет: `com.unity.services.leaderboards` 2.3.3
- Реализация: `Assets/Scripts/Application/Leaderboard/UnityLeaderboardProxy.cs`
- Зависит от: `com.unity.services.authentication` 3.3.3+, `com.unity.services.core`

### Unity Input System

- Пакет: `com.unity.inputsystem` 1.19.0
- Файл схемы действий: `Assets/Input/player_actions.inputactions`
- Сгенерированный класс: `Assets/Scripts/Input/Generated/PlayerActions.cs`
- Обёртка: `Assets/Scripts/Input/PlayerInput.cs` (ручная обёртка с событиями `OnBackAction` и др.)
- Namespace: `UnityEngine.InputSystem`

### Newtonsoft Json.NET

- Пакет: `com.unity.nuget.newtonsoft-json` 3.2.2
- Подтягивается как транзитивная зависимость `com.shtl.mvvm`
- В игровом коде используется `JsonUtility` (встроенный Unity), а не Json.NET напрямую

---

## Asset Store / Plugins

### com.shtl.mvvm (MVVM-фреймворк)

- Источник: Git — `https://github.com/SelStrom/shtl-mvvm.git` (hash `c7bda1c`)
- Namespace: `Shtl.Mvvm`
- Сборка: `Shtl.Mvvm` (подключена в `Assets/Asteroids.asmdef` и `Assets/Editor/AsteroidsEditor.asmdef`)
- Широко используется по всему проекту: 16+ файлов `using Shtl.Mvvm`
- Предоставляет: реактивные поля (`ObservableField` в `Assets/Scripts/Utils/ObservableField.cs`), привязки данных (`Assets/Scripts/View/Bindings/BindingToExtensions.cs`)
- Собственный репозиторий автора проекта (SelStrom)

### TextMesh Pro

- Пакет: `com.unity.textmeshpro` 3.0.9
- Ассеты расположены в `Assets/TextMesh Pro/` (шрифты, материалы, шейдеры, спрайты)
- Используется для UI-текста: `Assets/Scripts/View/Components/GuiText.cs`, подключён в `AsteroidsEditor.asmdef`

### Unity Memory Profiler

- Присутствуют `.csproj` файлы: `Unity.MemoryProfiler.Editor.csproj`, `Unity.MemoryProfiler.csproj`
- Используется только в Editor для профилирования памяти (директория `MemoryCaptures/`)

---

## Конфигурация окружения

**Идентификаторы проекта UGS:**
- Файл: `ProjectSettings/UnityConnectSettings.asset`
- `m_Enabled: 0` — облачные сервисы отключены на уровне настроек (но Authentication/Leaderboard инициализируются программно в runtime)

**Leaderboard ID:**
- Строковый идентификатор передаётся в `LeaderboardService` при вызове; хранится в конфигурации игры (предположительно в `Assets/Media/configs/` или `GameData`)

---

*Аудит интеграций: 2026-03-26*
