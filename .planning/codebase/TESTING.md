# Testing

**Дата анализа:** 2026-03-26

## Test Framework

В проекте установлен `com.unity.test-framework` версии **1.1.33** (зафиксировано в `Packages/packages-lock.json`).
Зависимость попала в проект транзитивно через `com.unity.collections`, что указывает на то, что пакет тестового фреймворка присутствует, но **намеренно не используется** — тестовые файлы отсутствуют.

**Дополнительно установлены:**
- `com.unity.ext.nunit` 1.0.6 — NUnit для Unity (транзитивно)

**Инструменты разработки:**
- `com.unity.ide.rider` 3.0.39 — интеграция с JetBrains Rider
- `com.unity.ide.vscode` 1.2.5 — интеграция с VS Code

---

## Test Coverage

**Тестовое покрытие: 0%**

В проекте **полностью отсутствуют тесты**:
- Нет папок `Tests/`, `EditMode/`, `PlayMode/` в `Assets/`
- Нет файлов `*.Test.cs`, `*.Spec.cs`, `*Tests.cs`
- Нет `.asmdef` файлов для тестовых сборок
- Нет тестовых fixtures, helpers или mock-объектов

Команда поиска `find Assets/ -name "*Test*" -o -name "*Spec*"` не возвращает результатов.

---

## Test Structure

Структура тестов не определена — тесты отсутствуют.

**Архитектурный потенциал для тестирования:**
Проект использует паттерны, которые хорошо поддаются unit-тестированию:

1. **Pure C# модели** — `ShipModel`, `AsteroidModel`, `BulletModel` не зависят от Unity. Можно тестировать без `[RequiresPlayMode]`.

2. **ECS-системы** — `GunSystem`, `MoveSystem`, `LaserSystem`, `LifeTimeSystem` наследуют `BaseModelSystem<TNode>` и принимают компоненты напрямую. Пример unit-теста без Unity-зависимостей:
   ```csharp
   // Пример EditMode теста (не существует, показано как должно быть)
   [Test]
   public void GunSystem_Shoots_WhenShootingFlagIsTrue()
   {
       var gun = new GunComponent { MaxShoots = 1, Shooting = true };
       // ...
   }
   ```

3. **`ActionScheduler`** — не зависит от Unity, легко тестируется.

4. **`CoroutineResult`** — простая data-структура, тестируется тривиально.

5. **`LeaderboardService`** — принимает `IAuthProxy`, `ILeaderboardProxy` через конструктор, что позволяет подставлять моки в тестах EditMode.

6. **`Model.PlaceWithinGameArea`** — статический метод с чистой логикой, идеален для параметризованных тестов.

---

## How to Run Tests

Тесты отсутствуют — запуск невозможен.

Если тесты появятся, их можно запустить через:
- **Unity Editor**: `Window → General → Test Runner`
- **Командная строка (CI)**:
  ```bash
  # EditMode тесты
  Unity -batchmode -runTests -testPlatform EditMode -projectPath .

  # PlayMode тесты
  Unity -batchmode -runTests -testPlatform PlayMode -projectPath .
  ```

---

## Gaps

### Критические пробелы

**Игровая логика (`Game.cs`)**
- Файл: `Assets/Scripts/Application/Game.cs`
- Не покрыты: спавн врагов, обработка столкновений, логика смерти корабля, дробление астероидов.
- Риск: любые изменения в `Kill()`, `SpawnAsteroid()`, `OnUserBulletCollided()` могут сломать игровой баланс незаметно.

**ECS-системы**
- Файлы: `Assets/Scripts/Model/Systems/`
- Не покрыты: `GunSystem` (перезарядка, стрельба), `LaserSystem`, `MoveSystem` (телепортация через границу поля), `ThrustSystem`, `LifeTimeSystem`.
- Риск: баги в физике движения или тайминге выстрелов.

**`ActionScheduler`**
- Файл: `Assets/Scripts/Model/ActionScheduler.cs`
- Не покрыт: порядок выполнения при одновременных действиях, edge case с добавлением действия во время итерации (есть TODO в коде: `//TODO theoretically it can be added during update`).
- Риск: редкий race condition без тестов необнаружим до прода.

**`LeaderboardService`**
- Файл: `Assets/Scripts/Application/Leaderboard/LeaderboardService.cs`
- Не покрыты: повторный вызов `Initialize()`, параллельные инициализации, ошибки сети.
- Риск: трудновоспроизводимые баги с состоянием `_initialized`/`_initializing`.

**`Model.PlaceWithinGameArea`**
- Файл: `Assets/Scripts/Model/Model.cs`
- Не покрыты: граничные случаи (position == side/2, отрицательные значения, нулевой side).
- Приоритет: **High** — статический метод без зависимостей, тест написать тривиально.

**`EntitiesCatalog`**
- Файл: `Assets/Scripts/Application/EntitiesCatalog.cs`
- Не покрыто: `TryFindModel` с несуществующим объектом, `Release` несуществующей сущности.
- Риск: исключения в runtime без внятных сообщений об ошибке.

**`GameObjectPool`**
- Файл: `Assets/Scripts/Utils/GameObjectPool.cs`
- Не покрыто: `Release` несуществующего объекта выбрасывает `Exception` — нет теста на корректность сообщения об ошибке.

### Отсутствие интеграционных тестов
Нет PlayMode-тестов для проверки взаимодействия Model + Systems + View. Полный игровой цикл (старт → спавн → столкновение → рестарт) не тестируется автоматически.

### Рекомендации по приоритетам
| Что тестировать | Тип теста | Приоритет |
|---|---|---|
| `Model.PlaceWithinGameArea` | EditMode, NUnit | High |
| `ActionScheduler` | EditMode, NUnit | High |
| `GunSystem.UpdateNode` | EditMode, NUnit | High |
| `LifeTimeSystem.UpdateNode` | EditMode, NUnit | Medium |
| `LeaderboardService` с мок-прокси | EditMode, NUnit | Medium |
| `MoveSystem` телепортация | EditMode, NUnit | Medium |
| Игровой цикл (Game.Start → Kill) | PlayMode | Low |
