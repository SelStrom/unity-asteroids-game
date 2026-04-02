# Concerns & Technical Debt

**Дата анализа:** 2026-03-26

---

## Critical Issues

### Нет тестов — вообще
- **Файлы:** весь каталог `Assets/Scripts/`
- **Суть:** В проекте отсутствуют юнит-тесты, интеграционные тесты и любые тестовые сборки. Единственный найденный файл с упоминанием `Test` — это сгенерированный `PlayerActions.cs` из Input System.
- **Риск:** Любой рефакторинг или добавление новой механики производится вслепую. Баги в логике движения, пересчёта очков, работы лазера невозможно поймать автоматически.
- **Что нужно:** Добавить Assembly Definition для тестов, покрыть хотя бы `MoveSystem`, `GunSystem`, `LaserSystem`, `ActionScheduler`, `Model.PlaceWithinGameArea`.

### Утечка памяти при рестарте игры
- **Файлы:** `Assets/Scripts/Application/ModelFactory.cs` (строки 14, 23), `Assets/Scripts/Application/Game.cs` (строка 72–74)
- **Суть:** `ModelFactory.Release()` — пустой метод с комментарием `// TODO @a.shatalov: model pool`. При вызове `Game.Restart()` → `Model.CleanUp()` все старые модели уничтожаются, но нет пула — каждый рестарт аллоцирует новые объекты через `new TModel()`. ViewFactory при этом использует `GameObjectPool` (правильно), а ModelFactory — нет.
- **Риск:** Нарастающая GC-нагрузка при многократных рестартах. В мобильных сборках — заметные паузы.

---

## Technical Debt

### TODO-комментарии, зафиксированные в коде

| Файл | Строка | Описание |
|------|--------|----------|
| `Assets/Scripts/Application/ModelFactory.cs` | 14, 23 | Пул моделей не реализован |
| `Assets/Scripts/Application/Game.cs` | 28 | `_model.OnEntityDestroyed` — нужен рефакторинг подписки |
| `Assets/Scripts/Application/Game.cs` | 133 | Получение очков: `// TODO: impl score receiver` |
| `Assets/Scripts/Application/Game.cs` | 154 | Эффекты после смерти не очищаются через пул: `// TODO: cleanup effect` |
| `Assets/Scripts/Model/ActionScheduler.cs` | 28 | Добавление действий во время итерации — потенциальный баг |
| `Assets/Scripts/View/Base/BaseVisual.cs` | 31 | Лишнее копирование value-типов при `Connect()` |

### ActionScheduler — потенциальный баг при добавлении во время обхода
- **Файл:** `Assets/Scripts/Model/ActionScheduler.cs`, строка 28
- **Суть:** Сам автор оставил комментарий: `//TODO theoretically it can be added during update. So it should be add new entries collection`. Метод `ScheduleAction()` вызывается изнутри `SpawnNewEnemy()` (который сам запускается из `ActionScheduler.Update()`). Это означает, что список `_scheduledEntries` модифицируется во время итерации по нему в `Update()`. В текущей реализации цикл идёт `for` от конца к началу, поэтому `Add` в конец списка технически не ломает индексацию, но это хрупкое и неочевидное поведение.
- **Риск:** При добавлении нового типа action-цепочки или рефакторинге цикла — скрытый баг.

### Magick numbers в игровой логике
- **Файлы:**
  - `Assets/Scripts/Application/Game.cs`, строка 79: `Random.Range(0, 3)` — количество типов врагов захардкожено.
  - `Assets/Scripts/Application/Game.cs`, строка 114: `Random.Range(1f, 3f)` — диапазон скорости астероида.
  - `Assets/Scripts/Application/Game.cs`, строка 185: `Math.Min(..., 10f)` — максимальная скорость осколка астероида.
  - `Assets/Scripts/Application/EntitiesCatalog.cs`, строка 149: `model.MoveTo.Every = 3f` — интервал смены направления UFO.
  - `Assets/Scripts/Model/Systems/ShootToSystem.cs`, строка 17: `(20 - ship.Move.Speed.Value)` — скорость пули врага захардкожена как `20`.
  - `Assets/Scripts/Application/Game.cs`, строка 220: `new RaycastHit2D[30]` — максимальное число хитов лазера.
- **Риск:** Баланс игры невозможно настроить через `ScriptableObject`-конфиги. При изменении одного значения — нужно искать по всему коду.

### Непоследовательное использование `ObservableValue` в компонентах
- **Файлы:**
  - `Assets/Scripts/Model/Components/LaserComponent.cs` — `CurrentShoots` и `ReloadRemaining` — `ObservableValue`.
  - `Assets/Scripts/Model/Components/GunComponent.cs` — `CurrentShoots` и `ReloadRemaining` — обычные `int`/`float`, не реактивные.
  - `Assets/Scripts/Model/Components/MoveComponent.cs` — `Position` и `Speed` — `ObservableValue`, `Direction` — обычный `Vector2`.
- **Суть:** Единого правила нет: часть данных компонента реактивна (нужна для биндинга View), часть — нет. Логика выбора нигде не документирована. В `EntitiesCatalog.CreateShip()` ручная привязка `bindings.From(model.Move.Position).To(viewModel.Position)` работает, только потому что `Position` — `ObservableValue`. Если разработчик добавит новый компонент и выберет неправильный тип — биндинг молча не заработает.

### `Dispose()` в `Application` приватный, но `Quit()` публичный
- **Файл:** `Assets/Scripts/Application/Application.cs`, строки 82–106
- **Суть:** Метод `Dispose()` приватный, вызывается только из `Quit()`. Это нарушает соглашение `IDisposable` и затрудняет тестирование и управление жизненным циклом. Класс не реализует `IDisposable`.

### `EntitiesCatalog.CleanUp()` не возвращает объекты в пул View
- **Файл:** `Assets/Scripts/Application/EntitiesCatalog.cs`, строки 193–203
- **Суть:** Метод `CleanUp()` очищает только словари биндингов, но не вызывает `_viewFactory.Release()` для каждого view. При этом `Dispose()` (строка 206) вызывает `CleanUp()` без релиза view. Реальный release view происходит через `Release(IGameEntityModel)` → `OnEntityDestroyed` → `Model.CleanUp()`. Цепочка непрямая и сложна для понимания.

---

## Performance Risks

### Аллокации в `Model.Update()` — LINQ и `Where` каждый кадр
- **Файл:** `Assets/Scripts/Model/Model.cs`, строки 128, 144, 153
- **Суть:**
  - Строка 128: `if (_newEntities.Any())` — создаёт `IEnumerator` каждый кадр.
  - Строка 144: `foreach (var entity in _entities.Where(x => x.IsDead()))` — создаёт `IEnumerable<>` обёртку каждый кадр при наличии мёртвых сущностей.
  - Строка 153: `_entities.RemoveWhere(x => x.IsDead())` — повторный обход коллекции для удаления.
- **Риск:** При каждом уничтожении сущности происходит двойной обход `HashSet` плюс LINQ-аллокация. В пике боя (много взрывов одновременно) — заметный GC spike.
- **Решение:** Завести отдельный `List<IGameEntityModel> _deadEntities`, заполнять в первом проходе, удалять во втором.

### `ActionScheduler.Update()` — `_scheduledEntries.Any()` каждый кадр
- **Файл:** `Assets/Scripts/Model/ActionScheduler.cs`, строка 32
- **Суть:** `_scheduledEntries.Any()` создаёт `IEnumerator` из `List<T>` каждый кадр. Правильнее: `if (_scheduledEntries.Count == 0)`.

### Строковые аллокации в HUD каждый кадр
- **Файл:** `Assets/Scripts/Application/Screens/GameScreen.cs`, строки 78–101
- **Суть:** Методы `OnShipPositionChanged`, `OnShipSpeedChanged`, `OnShipRotationChanged`, `OnCurrentShootsChanged`, `OnReloadRemainingChanged` вызываются каждый кадр (реактивно при изменении значений, а `Position` и `Speed` меняются каждый кадр в `MoveSystem`). Каждый вызов создаёт новую строку через интерполяцию/`ToString()`. На мобильных устройствах это заметная GC-нагрузка.
- **Пример:** `$"Coordinates: {position.ToString("F1")}"` — строка создаётся ~60 раз в секунду.
- **Решение:** Сравнивать с предыдущим значением (порог), использовать `StringBuilder` или `TMP`-форматирование напрямую.

### Лазер выделяет массив `RaycastHit2D[30]` каждый выстрел
- **Файл:** `Assets/Scripts/Application/Game.cs`, строка 220
- **Суть:** `var hits = new RaycastHit2D[30]` — аллокация при каждом выстреле лазером. Правильнее вынести как статическое поле или переиспользовать.

---

## Architecture Smells

### `Game.cs` — смешение обязанностей
- **Файл:** `Assets/Scripts/Application/Game.cs`
- **Суть:** Класс `Game` одновременно отвечает за:
  1. Спавн всех типов врагов (`SpawnAsteroid`, `SpawnUfo`, `SpawnBigUfo`).
  2. Обработку всех коллизий (корабль, пуля игрока, пуля врага, UFO).
  3. Логику смерти и дробления астероидов.
  4. Воспроизведение эффектов взрыва.
  5. Подписку/отписку на ввод игрока.
  6. Логику подсчёта очков (через `_model.ReceiveScore`).
- **Риск:** При добавлении нового типа врага или механики (например, щит корабля, бомба) класс разрастается. Уже 263 строки.

### `Model.ReceiveScore()` — switch по типам вместо полиморфизма
- **Файл:** `Assets/Scripts/Model/Model.cs`, строки 108–122
- **Суть:** Метод содержит `switch` по конкретным типам (`AsteroidModel`, `UfoModel`, `UfoBigModel`). Аналогичный `switch` есть в `Game.Kill()` (строки 173–195). При добавлении нового типа врага нужно обновлять оба switch-а.
- **Правильный подход:** Вынести `Score` в `IGameEntityModel` или в `BaseGameEntityData`, получать через интерфейс.

### `EntitiesCatalog` — фабрика + реестр + маппинг
- **Файл:** `Assets/Scripts/Application/EntitiesCatalog.cs`
- **Суть:** Класс выполняет три разные роли: создаёт сущности (фабрика), хранит маппинг model↔view↔gameObject↔bindings (реестр), предоставляет поиск по `GameObject` (запросы). 214 строк, метод `CreateShip` вручную конфигурирует 8 параметров модели.
- **Риск:** При добавлении нового типа врага нужно добавить `CreateXxx` метод с ручным копированием конфигурации из `GameData`.

### `UfoModel` наследует `UfoBigModel` — неверная иерархия
- **Файл:** `Assets/Scripts/Model/Entities/UfoBigModel.cs`
- **Суть:** `UfoModel` (`sealed`) наследует `UfoBigModel`, хотя семантически это два разных типа врагов одного уровня абстракции. `UfoBigModel` — базовый класс, а не "большой UFO". Название вводит в заблуждение.
- **Симптом:** В `EntitiesCatalog.CreateUfo()` (строка 146) используется `_configs.UfoBig` для маленького UFO — скопировали конфиг и забыли изменить:
  ```csharp
  model.SetData(_configs.UfoBig, position, direction, _configs.Ufo.Speed);
  //            ^^^^^^^^^^^^ конфиг большого UFO используется для маленького
  ```
  Это явный баг: маленький UFO получает данные (например, `Score`) от `UfoBig`.

### `GameScreen` напрямую зависит от `MonoBehaviour` через `_coroutineHost`
- **Файл:** `Assets/Scripts/Application/Screens/GameScreen.cs`, строка 33, 191, 222
- **Суть:** `GameScreen` — чистый C#-класс (не MonoBehaviour), но принимает `MonoBehaviour _coroutineHost` только для запуска корутин. Это вынуждает `GameScreen` зависеть от Unity-рантайма и не позволяет тестировать логику экрана без Unity.

### `PlayerInput` не отписывается от `InputSystem` при уничтожении
- **Файл:** `Assets/Scripts/Input/PlayerInput.cs`
- **Суть:** В конструкторе производятся подписки на события `InputAction` (`_playerControls.Attack.performed += OnAttack` и т.д.), но нет метода `Dispose()` или деструктора, который бы их снял. При вызове `Application.Quit()` → `Dispose()` объект `PlayerInput` обнуляется, но `_actions` и `_playerControls` продолжают жить и держат делегаты. В текущей реализации это не критично (приложение закрывается), но при сценарии рестарта/переинициализации — утечка.

---

## Missing Capabilities

### Нет паузы во время игры
- Текущая реализация `OnApplicationPause` в `ApplicationEntry` только реагирует на системную паузу ОС. Нет кнопки паузы, нет состояния `Paused` в `Game`.

### Нет обработки потери фокуса во время корутин лидерборда
- **Файл:** `Assets/Scripts/Application/Screens/GameScreen.cs`, строки 200–204, 238–242, 253–257
- **Суть:** Защита от устаревшего ViewModel: `if (_score.ViewModel != viewModel) { yield break; }` — правильная техника. Однако если игрок нажимает Restart пока идут несколько параллельных корутин (`SubmitAndShowLeaderboardRoutine` и `FetchAndShowLeaderboardRoutine` теоретически могут запуститься одновременно при гонке), проверка `viewModel` срабатывает правильно. Но `_coroutineHost` (`ApplicationEntry`) может быть уничтожен (при `OnApplicationQuit`), пока корутина ещё работает — `StopAllCoroutines()` при этом не вызывается.

### Нет ограничения на количество сущностей в сцене
- **Файл:** `Assets/Scripts/Application/Game.cs`, строка 93
- **Суть:** `SpawnNewEnemy()` вызывает сам себя рекурсивно через `ActionScheduler` каждые `SpawnNewEnemyDurationSec` секунд без ограничения. При длительной игре количество UFO и астероидов на экране неограниченно растёт. Нет `MaxEnemyCount` или подобного условия.

### Нет сохранения прогресса (хай-скора локально)
- Счёт сохраняется только в облако (лидерборд). Локального рекорда нет. При отсутствии интернета прошлые результаты не видны.

### `OnRestartAction` в `PlayerInput` не используется
- **Файл:** `Assets/Scripts/Input/PlayerInput.cs`, строка 17
- **Суть:** Событие `OnRestartAction` объявлено, подписка зарегистрирована, но в `Game` или `Application` нигде не подключается к нажатию кнопки Restart. Рестарт происходит только через UI-кнопку в `ScoreVisual`.

---

## Recommendations

### Высокий приоритет

1. **Исправить баг с конфигом UFO** (`EntitiesCatalog.cs:146`): заменить `_configs.UfoBig` на `_configs.Ufo` при создании маленького UFO.

2. **Устранить аллокации в `Model.Update()`**: заменить LINQ (`Any()`, `Where()`) на прямые проверки `Count > 0` и буфер мёртвых сущностей. Это горячий путь — выполняется каждый кадр.

3. **Вынести magic numbers в конфиги**: `10f` (макс скорость осколка), `30` (размер массива raycast), `3f` (интервал MoveTo) и `20` (скорость пули в `ShootToSystem`) — всё должно быть в `GameData` или соответствующих `ScriptableObject`.

4. **Добавить `Dispose()` / отписку в `PlayerInput`**: добавить метод `Dispose()` с `_playerControls.Disable()` и отпиской всех делегатов; вызвать из `Application.Quit()`.

### Средний приоритет

5. **Реализовать пул моделей** (`ModelFactory`): модели — простые POCO-объекты, пул через `Stack<T>` снизит GC на рестартах.

6. **Снизить строковые аллокации в HUD**: использовать порог изменения или StringBuilder для формирования строк скорости/позиции.

7. **Разделить `Game.cs`**: выделить `SpawnManager` (спавн врагов), `CollisionHandler` (обработка коллизий), `EffectPlayer` (VFX).

8. **Переименовать `UfoBigModel`** в `UfoBaseModel` или `BaseUfoModel`; `UfoModel` (маленький) и `UfoBigModel` (большой) должны наследовать общий базовый класс, а не друг друга.

### Низкий приоритет

9. **Добавить минимальные юнит-тесты** для детерминированной логики: `ActionScheduler`, `MoveSystem`, `PlaceWithinGameArea`, `ThrustSystem`.

10. **Ввести ограничение максимального числа врагов** (`MaxEnemyCount` в `GameData`) чтобы предотвратить деградацию производительности при длительной игре.

11. **Подключить `OnRestartAction`** из `PlayerInput` к логике рестарта (hotkey R/Back).

---

*Аудит выполнен: 2026-03-26*
