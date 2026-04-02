# Concerns & Technical Debt

**Дата анализа:** 2026-04-02

---

## Critical Issues

### 1. Баг: UFO маленький получает данные (Score) от UfoBig

- **Файлы:** `Assets/Scripts/Application/EntitiesCatalog.cs:146`
- **Суть:** В `CreateUfo()` вызывается `model.SetData(_configs.UfoBig, position, direction, _configs.Ufo.Speed)`. Первый аргумент -- `_configs.UfoBig` вместо `_configs.Ufo`. Метод `SetData()` в `UfoBigModel` (строка 28) присваивает `Data = data`, таким образом маленький UFO получает `Data.Score == 4` (от UfoBig) вместо `Data.Score == 5` (от Ufo). Скорость передаётся правильно (`_configs.Ufo.Speed`), но Score, Prefab и Gun -- из конфига большого UFO.
- **Влияние:** Игрок получает 4 очка за маленький UFO вместо 5. Баланс нарушен.
- **Исправление:** Заменить `_configs.UfoBig` на `_configs.Ufo` в строке 146.

### 2. Баг: `PlaceWithinGameArea` некорректно обрабатывает пересечение границы

- **Файлы:** `Assets/Scripts/Model/Model.cs:156-167`
- **Суть:** Алгоритм врапинга (screen wrapping):
  ```csharp
  if (position > side / 2)
  {
      position = -side + position;
  }
  if (position < -side / 2)
  {
      position = side - position;  // ошибка: минус не инвертируется
  }
  ```
  Рассмотрим случай выхода через левую границу. Пусть `side = 80` (ширина), `position = -42`.
  - Условие: `-42 < -40` => true.
  - Результат: `position = 80 - (-42) = 122`. Объект улетает на `+122`, далеко за правую границу.
  - Правильная формула: `position = side + position` (т.е. `80 + (-42) = 38`).

  Аналогично для верхней границы (`position > side / 2`): `position = -side + position`.
  При `side = 80`, `position = 42`: `-80 + 42 = -38` -- это корректно.

  Итого: **правая/верхняя граница работает правильно, левая/нижняя -- содержит баг**. Объект, вышедший за левый/нижний край, телепортируется за правый/верхний край и на следующем кадре снова перебрасывается, вызывая осцилляцию. На практике эффект может быть незаметен при малых `deltaTime` (позиция не уходит далеко за границу), но при больших скачках или лагах -- объект может мерцать.
- **Влияние:** Корабль и все сущности некорректно оборачиваются при пересечении левой/нижней границы экрана.
- **Исправление:** Строка 163: заменить `position = side - position` на `position = side + position`.

### 3. Баг: `MoveToSystem` -- деление на ноль / отрицательное время перехвата

- **Файлы:** `Assets/Scripts/Model/Systems/MoveToSystem.cs:18-19`
- **Суть:** Алгоритм перехвата корабля маленьким UFO вычисляет время:
  ```csharp
  var time = (ship.Move.Position.Value - node.Move.Position.Value).magnitude
             / (node.Move.Speed.Value - ship.Move.Speed.Value);
  ```
  Проблемы:
  1. **Деление на ноль:** если скорость UFO равна скорости корабля (`node.Move.Speed.Value == ship.Move.Speed.Value`), результат -- `Infinity` / `NaN`. При `Ufo.Speed = 10` и `Ship.MaxSpeed = 15`, корабль может достичь 10 ед/с, и деление даст ноль.
  2. **Отрицательное время:** если корабль быстрее UFO (скорость корабля > 10 ед/с), знаменатель отрицательный, `time` отрицательный. UFO полетит в противоположную сторону от корабля.
  3. **Огромное время:** при почти равных скоростях, `time` стремится к бесконечности -- `pendingPosition` улетает за пределы экрана.
- **Влияние:** Маленький UFO (единственный, использующий `MoveToSystem`) непредсказуемо меняет направление при быстром движении корабля.
- **Исправление:** Ограничить `time` минимальным положительным значением: `time = Mathf.Max(time, 0.1f)` или использовать другой алгоритм перехвата (например, proportional navigation).

### 4. Баг: `ShootToSystem` -- аналогичная проблема с вычислением упреждения

- **Файлы:** `Assets/Scripts/Model/Systems/ShootToSystem.cs:16-17`
- **Суть:** Тот же паттерн:
  ```csharp
  var time = (ship.Move.Position.Value - node.Move.Position.Value).magnitude
             / (20 - ship.Move.Speed.Value);
  ```
  Магическое число `20` -- скорость пули (`Bullet.Speed = 20` из конфига). Если скорость корабля >= 20 (невозможно при текущем балансе, `MaxSpeed = 15`), будет деление на ноль или отрицательное время. Но при скорости корабля = 15, знаменатель = 5, что даёт большое время перехвата и неточное упреждение. Также `20` захардкожено вместо получения из конфига.
- **Влияние:** При высокой скорости корабля UFO стреляет крайне неточно -- упреждение слишком большое, пуля летит мимо.
- **Исправление:** Использовать `_configs.Bullet.Speed` вместо `20`. Ограничить минимальный знаменатель.

### 5. Нет тестов -- вообще

- **Файлы:** весь каталог `Assets/Scripts/`
- **Суть:** В проекте отсутствуют юнит-тесты, интеграционные тесты и любые тестовые сборки.
- **Риск:** Баги в `PlaceWithinGameArea`, `MoveToSystem`, `ShootToSystem`, `ActionScheduler` невозможно обнаружить автоматически. Все перечисленные выше Critical-баги могли быть предотвращены минимальными тестами.
- **Исправление:** Добавить Assembly Definition для тестов, покрыть `MoveSystem`, `PlaceWithinGameArea`, `ThrustSystem`, `ActionScheduler`, `MoveToSystem`, `ShootToSystem`.

---

## High Severity

### 6. `Model.ReceiveScore()` -- `UfoModel` ветка недостижима

- **Файлы:** `Assets/Scripts/Model/Model.cs:108-122`
- **Суть:** Switch содержит три ветки:
  ```csharp
  case AsteroidModel ctx: ...
  case UfoModel ctx: ...
  case UfoBigModel ctx: ...
  ```
  `UfoModel` наследует `UfoBigModel`. C# switch по типу проверяет сверху вниз. `UfoModel` стоит вторым -- для экземпляра `UfoModel` сработает ветка `UfoModel`, для `UfoBigModel` -- ветка `UfoBigModel`. Порядок корректный. **Однако:** из-за бага #1 (UFO маленький получает `Data` от `_configs.UfoBig`), `UfoModel` возвращает Score = 4 вместо 5.
- **Влияние:** Даже после исправления бага #1, при добавлении нового типа врага разработчик обязан добавить ветку в оба switch (здесь и в `Game.Kill()`). Это хрупкий паттерн.
- **Исправление:** Вынести `Score` в `IGameEntityModel` или использовать интерфейс `IScoreable`.

### 7. `Game.OnUserBulletCollided` -- UFO не уничтожается после получения очков

- **Файлы:** `Assets/Scripts/Application/Game.cs:128-144`
- **Суть:** При попадании пули игрока в объект:
  ```csharp
  if (_catalog.TryFindModel<AsteroidModel>(col.gameObject, out var asteroidModel))
  {
      _model.ReceiveScore(asteroidModel);
      Kill(asteroidModel);   // <-- уничтожается
  }
  if (_catalog.TryFindModel<UfoBigModel>(col.gameObject, out var ufoModel))
  {
      _model.ReceiveScore(ufoModel);
      // Kill(ufoModel) -- НЕТ вызова Kill!
  }
  ```
  UFO получает очки, но не уничтожается пулей. Уничтожение UFO происходит только через коллизию в `OnCollisionEnter2D` -> `OnUfoCollided`. Но `col.otherCollider.enabled = false` (строка 131) отключает коллайдер цели. Если коллайдер уже отключён -- повторного `OnCollisionEnter2D` не будет. UFO становится бессмертным к пулям: получает очки (дубли), но не умирает.
- **Влияние:** UFO невозможно убить пулями -- только лазером. Очки за UFO начисляются при каждом попадании повторно.
- **Исправление:** Добавить `Kill(ufoModel)` после `_model.ReceiveScore(ufoModel)` (строка 143).

### 8. Утечка памяти при рестарте игры -- `ModelFactory.Release()` пуст

- **Файлы:** `Assets/Scripts/Application/ModelFactory.cs:14,23`
- **Суть:** `ModelFactory.Release()` -- пустой метод с TODO. При `Game.Restart()` -> `Model.CleanUp()` все модели уничтожаются через `OnEntityDestroyed` -> `EntitiesCatalog.Release()` -> `ModelFactory.Release()` (пустой). Новые модели аллоцируются через `new TModel()`. ViewFactory использует `GameObjectPool` (пул работает), а ModelFactory -- нет.
- **Влияние:** GC-нагрузка при многократных рестартах. На мобильных -- паузы.

### 9. `ActionScheduler.Update()` -- модификация списка во время итерации

- **Файлы:** `Assets/Scripts/Model/ActionScheduler.cs:44-57`
- **Суть:** Цикл `for (var i = _scheduledEntries.Count - 1; i >= 0; i--)` использует swap-and-remove паттерн (строки 52-55). При `entry.Action?.Invoke()` (строка 56) вызывается `SpawnNewEnemy()`, который вызывает `ScheduleAction()`, добавляя элемент в конец `_scheduledEntries`. Текущий индекс `i` идёт от конца к началу. После swap-and-remove последний элемент перемещается на позицию `i`, а `Count` уменьшается. Новый элемент добавляется в конец -- после текущей позиции `i` (если `i > 0`). Этот элемент не будет обработан в текущем цикле. **Однако:** `_nextUpdateDuration` не пересчитывается в конце цикла после добавления новых записей -- новый `_nextUpdateDuration` устанавливается в `ScheduleAction()`, но при следующем вызове `Update()` значение `_secondsSinceLastUpdate` сброшено в 0, а `_nextUpdateDuration` содержит уже скорректированное значение. Логика работает, но хрупка.
- **Влияние:** При рефакторинге цикла или добавлении нового типа action -- скрытый баг.

### 10. `ActionScheduler` -- `_nextUpdateDuration` не сбрасывается при обработке

- **Файлы:** `Assets/Scripts/Model/ActionScheduler.cs:44-59`
- **Суть:** В начале цикла обработки (строка 44) `_nextUpdateDuration` не сбрасывается в `float.MaxValue`. Вместо этого внутри цикла на строке 48 значение обновляется через `Math.Min`. Но если все элементы были обработаны и удалены (ни один не имеет `Duration > 0`), `_nextUpdateDuration` останется равным старому значению с предыдущей итерации. Это не вызывает проблем благодаря `_secondsSinceLastUpdate = 0` в строке 59, но код неочевиден.
- **Исправление:** Добавить `_nextUpdateDuration = float.MaxValue` перед циклом `for`.

---

## Medium Severity

### 11. Аллокации в `Model.Update()` -- LINQ каждый кадр

- **Файлы:** `Assets/Scripts/Model/Model.cs:128,144,153`
- **Суть:**
  - Строка 128: `if (_newEntities.Any())` -- создаёт `IEnumerator` через `HashSet<T>.GetEnumerator()` каждый кадр. Замена: `_newEntities.Count > 0`.
  - Строка 144: `foreach (var entity in _entities.Where(x => x.IsDead()))` -- создаёт `IEnumerable<>` обёртку + замыкание каждый кадр.
  - Строка 153: `_entities.RemoveWhere(x => x.IsDead())` -- повторный обход `HashSet` + делегат-аллокация.
  Итого: 2 полных обхода `_entities` + 2 аллокации делегата + 1 LINQ-обёртка на каждый кадр.
- **Влияние:** GC-спайки в пике боя.
- **Исправление:** Завести `List<IGameEntityModel> _deadBuffer`, заполнять в первом проходе, удалять/уведомлять во втором.

### 12. Строковые аллокации в HUD каждый кадр

- **Файлы:** `Assets/Scripts/Application/Screens/GameScreen.cs:77-101`
- **Суть:** `OnShipPositionChanged` и `OnShipSpeedChanged` вызываются каждый кадр (Position и Speed меняются в `MoveSystem` каждый кадр). Каждый вызов создаёт новую строку: `$"Coordinates: {position.ToString("F1")}"` (~60 раз/с). `OnShipRotationChanged` -- при каждом повороте (~60 раз/с при удержании клавиши).
- **Влияние:** ~180 строковых аллокаций/с при активной игре.
- **Исправление:** Ввести порог изменения (epsilon), обновлять HUD только при значимом изменении.

### 13. `RaycastHit2D[30]` аллоцируется при каждом выстреле лазером

- **Файлы:** `Assets/Scripts/Application/Game.cs:220`
- **Суть:** `var hits = new RaycastHit2D[30]` -- аллокация массива 30 элементов при каждом выстреле.
- **Исправление:** Вынести как `static readonly` поле класса.

### 14. `ActionScheduler.Update()` -- `_scheduledEntries.Any()` каждый кадр

- **Файлы:** `Assets/Scripts/Model/ActionScheduler.cs:33`
- **Суть:** `.Any()` на `List<T>` создаёт `List.Enumerator` struct через интерфейс `IEnumerable`, boxing в `IEnumerator`. Правильнее: `_scheduledEntries.Count == 0`.
- **Влияние:** Мелкая аллокация каждый кадр.

### 15. `GameObjectPool.Get()` -- строковая аллокация `ToString()` каждый раз

- **Файлы:** `Assets/Scripts/Utils/GameObjectPool.cs:23`
- **Суть:** `var prefabId = prefab.GetInstanceID().ToString()` -- каждый вызов `Get()` или `Release()` аллоцирует строку. `InstanceID` -- стабильный `int`, лучше использовать `int` как ключ словаря.
- **Исправление:** Заменить `Dictionary<string, ...>` на `Dictionary<int, ...>`, использовать `GetInstanceID()` напрямую.

### 16. Magic numbers в игровой логике

- **Файлы:**
  - `Assets/Scripts/Application/Game.cs:79` -- `Random.Range(0, 3)` -- количество типов врагов.
  - `Assets/Scripts/Application/Game.cs:114` -- `Random.Range(1f, 3f)` -- диапазон скорости астероида.
  - `Assets/Scripts/Application/Game.cs:185` -- `Math.Min(..., 10f)` -- максимальная скорость осколка.
  - `Assets/Scripts/Application/Game.cs:220` -- `new RaycastHit2D[30]` -- размер массива.
  - `Assets/Scripts/Application/EntitiesCatalog.cs:149` -- `model.MoveTo.Every = 3f` -- интервал смены направления UFO.
  - `Assets/Scripts/Model/Systems/ShootToSystem.cs:17` -- `20` -- скорость пули.
  - `Assets/Scripts/Model/Components/RotateComponent.cs:8` -- `90` -- скорость вращения (deg/s), хотя это `const` в коде, не из конфига.
  - `Assets/Scripts/Model/Components/ThrustComponent.cs:7` -- `0.0f` -- `MinSpeed`, аналогично.
- **Влияние:** Невозможно настроить баланс через ScriptableObject.

### 17. `ThrustSystem` -- асимметричное торможение

- **Файлы:** `Assets/Scripts/Model/Systems/ThrustSystem.cs:10-22`
- **Суть:** При ускорении:
  ```csharp
  var acceleration = node.Thrust.UnitsPerSecond * deltaTime;  // = 6 * dt
  ```
  При торможении (отпускание газа):
  ```csharp
  node.Move.Speed.Value = Math.Max(
      node.Move.Speed.Value - node.Thrust.UnitsPerSecond / 2 * deltaTime,  // = 3 * dt
      ThrustComponent.MinSpeed);
  ```
  Замедление = половина ускорения (3 ед/с^2 vs 6 ед/с^2). Коэффициент `/2` захардкожен. Это дизайнерское решение, но не параметризовано.
- **Влияние:** Невозможно настроить инерцию корабля отдельно от ускорения.

### 18. `ThrustSystem` -- ускорение не учитывает текущую скорость корректно

- **Файлы:** `Assets/Scripts/Model/Systems/ThrustSystem.cs:12-16`
- **Суть:**
  ```csharp
  var velocity = node.Move.Direction * node.Move.Speed.Value + node.Rotate.Rotation.Value * acceleration;
  node.Move.Direction = velocity.normalized;
  node.Move.Speed.Value = Math.Min(velocity.magnitude, node.Thrust.MaxSpeed);
  ```
  `acceleration` здесь -- приращение скорости (`UnitsPerSecond * deltaTime`), а не ускорение. Это корректно по формуле `v_new = v_old + a*dt`. Однако `Rotation.Value` -- вектор направления взгляда корабля (`Vector2`), а `Direction` -- вектор направления движения. При повороте корабля без газа и последующем включении тяги, ускорение применяется в направлении взгляда, а не движения -- это правильная физика аркадного корабля. Но есть нюанс: при повороте на 180 градусов и газе, `velocity.magnitude` может стать почти нулевым (вектора почти гасят друг друга), и `velocity.normalized` станет `(0,0)`, что приведёт к потере направления на один кадр.
- **Влияние:** Кратковременная потеря направления при резком развороте и газе. Edge case.

### 19. `EntitiesCatalog.CleanUp()` -- не возвращает view в пул

- **Файлы:** `Assets/Scripts/Application/EntitiesCatalog.cs:193-204`
- **Суть:** `CleanUp()` очищает словари, но не вызывает `_viewFactory.Release()` для view. Вызывается из `Game.Restart()` -> `Model.CleanUp()` -> `OnEntityDestroyed` -> `EntitiesCatalog.Release()` (правильная цепочка). Но если `CleanUp()` вызвать напрямую (например, из `Dispose()`), view останутся в сцене без возврата в пул.
- **Влияние:** Потенциальная утечка при некорректном порядке очистки.

### 20. `Restart()` -- двойная подписка на `OnEntityDestroyed`

- **Файлы:** `Assets/Scripts/Application/Game.cs:19-30,71-74`
- **Суть:** В конструкторе `Game`:
  ```csharp
  _model.OnEntityDestroyed += OnEntityDestroyed;
  ```
  `Game.Restart()` вызывает `_model.CleanUp()` -> `Start()`. Но `Start()` не переподписывает, а `_model` -- тот же экземпляр. Подписка остаётся одна -- это корректно. Однако при каждом `Start()` снова вызывается:
  ```csharp
  _playerInput.OnAttackAction += OnAttack;
  ```
  А в `Stop()`:
  ```csharp
  _playerInput.OnAttackAction -= OnAttack;
  ```
  Цепочка: `Start()` -> `Stop()` (при смерти) -> `Restart()` -> `Start()`. Подписки симметричны. Но если игрок нажмёт Restart на экране Score без смерти (через UI кнопку), `Restart()` вызовет `Start()`, который подпишется повторно без предыдущего `Stop()`. Текущий flow: смерть -> `Stop()` -> экран Score -> Restart кнопка -> `Restart()`. Порядок корректный. Но при рефакторинге -- опасность двойной подписки.

### 21. `SpawnNewEnemy` рекурсивно планирует себя без лимита

- **Файлы:** `Assets/Scripts/Application/Game.cs:77-94`
- **Суть:** `SpawnNewEnemy()` вызывает `_model.ActionScheduler.ScheduleAction(SpawnNewEnemy, ...)` каждый раз. При длительной игре количество сущностей растёт неограниченно. Нет `MaxEnemyCount`.
- **Влияние:** Деградация FPS при длительной игре.

---

## Low Severity

### 22. `UfoModel` наследует `UfoBigModel` -- обманчивая иерархия

- **Файлы:** `Assets/Scripts/Model/Entities/UfoBigModel.cs:7,17`
- **Суть:** `UfoModel` (маленький) наследует `UfoBigModel`, хотя `UfoBigModel` по сути является базовым классом. Название вводит в заблуждение.
- **Исправление:** Переименовать `UfoBigModel` в `BaseUfoModel`. Создать отдельный `UfoBigModel : BaseUfoModel`.

### 23. Непоследовательное использование `ObservableValue`

- **Файлы:**
  - `Assets/Scripts/Model/Components/LaserComponent.cs` -- `CurrentShoots` и `ReloadRemaining` -- `ObservableValue`.
  - `Assets/Scripts/Model/Components/GunComponent.cs` -- `CurrentShoots` и `ReloadRemaining` -- обычные `int`/`float`.
  - `Assets/Scripts/Model/Components/MoveComponent.cs` -- `Position` и `Speed` -- `ObservableValue`, `Direction` -- обычный `Vector2`.
- **Суть:** Логика выбора: `ObservableValue` используется только для полей, которые нужны в биндинге View/HUD. `LaserComponent` биндится к HUD (через `GameScreen`), `GunComponent` -- нет. `Direction` не отображается в UI. Правило не документировано.
- **Влияние:** При добавлении нового компонента разработчик может выбрать неправильный тип.

### 24. `Application.Dispose()` приватный, не реализует `IDisposable`

- **Файлы:** `Assets/Scripts/Application/Application.cs:82-96`
- **Суть:** `Dispose()` приватный, вызывается только из `Quit()`. Класс не реализует `IDisposable`.

### 25. `PlayerInput` не имеет `Dispose()` -- нет отписки от InputSystem

- **Файлы:** `Assets/Scripts/Input/PlayerInput.cs:19-33`
- **Суть:** В конструкторе: `_playerControls.Attack.performed += OnAttack` и т.д. Нет метода для отписки. `_playerControls.Disable()` не вызывается. При текущем flow (приложение закрывается целиком) не критично, но при рестарте уровня без перезапуска приложения -- утечка делегатов.

### 26. `OnRestartAction` в `PlayerInput` не используется

- **Файлы:** `Assets/Scripts/Input/PlayerInput.cs:17`
- **Суть:** Событие `OnRestartAction` объявлено и подписано на `_playerControls.Restart.performed`, но нигде в `Game` или `Application` не подключено. Restart происходит только через UI кнопку. Клавиша Space привязана и к `Attack`, и к `Restart` -- при нажатии сработают оба.

### 27. `GameScreen` зависит от `MonoBehaviour` через `_coroutineHost`

- **Файлы:** `Assets/Scripts/Application/Screens/GameScreen.cs:33`
- **Суть:** `GameScreen` -- C#-класс (не MonoBehaviour), принимает `MonoBehaviour _coroutineHost` для корутин. Невозможно тестировать без Unity.

### 28. `BulletData` ScriptableObject не используется

- **Файлы:** `Assets/Scripts/Configs/BulletData.cs`
- **Суть:** Класс `BulletData : ScriptableObject` определён, но нигде не используется. Пули конфигурируются через `GameData.BulletData` (struct). Мёртвый код.

### 29. `Game.cs` -- god class (263 строки, 6+ обязанностей)

- **Файлы:** `Assets/Scripts/Application/Game.cs`
- **Суть:** Отвечает за спавн, коллизии, смерть, дробление астероидов, VFX, ввод, подсчёт очков.
- **Исправление:** Выделить `SpawnManager`, `CollisionHandler`, `EffectPlayer`.

### 30. `SpawnBigUfo` -- направление с вертикальным подавлением

- **Файлы:** `Assets/Scripts/Application/Game.cs:106`
- **Суть:**
  ```csharp
  (Random.insideUnitCircle * new Vector2(1, 0.1f)).normalized
  ```
  Покомпонентное умножение (`*`) с `(1, 0.1f)` подавляет вертикальную составляющую, делая движение большого UFO преимущественно горизонтальным. После `normalized` вектор единичный. Это дизайнерское решение, но: если `Random.insideUnitCircle` вернёт `(0, y)`, результат будет `(0, y*0.1).normalized = (0, +-1)` -- чисто вертикальное движение (edge case). Также при `insideUnitCircle == (0, 0)` (вероятность ~0) -- деление на ноль в `normalized`.
- **Влияние:** Минимальный, edge case.

### 31. `GetRandomUfoPosition` -- только на левом краю

- **Файлы:** `Assets/Scripts/Utils/GameUtils.cs:9-21`
- **Суть:**
  ```csharp
  var position = new Vector2(0, Random.Range(0, gameArea.y)) - gameArea * 0.5f;
  ```
  `x` компонента = `0 - gameArea.x * 0.5` = левый край экрана. Все UFO спавнятся только на левом краю.
- **Влияние:** Предсказуемость появления UFO для игрока. Возможно дизайнерское решение.

### 32. `GetRandomAsteroidPosition` -- при негативном `allowedDistance` астероид может спавниться ближе к кораблю

- **Файлы:** `Assets/Scripts/Utils/GameUtils.cs:24-37`
- **Суть:**
  ```csharp
  var distance = shipPosition - position;
  var allowedDistance = distance.magnitude - spawnAllowedRadius;
  if (allowedDistance < 0)
  {
      position += distance.normalized * allowedDistance;
  }
  ```
  `allowedDistance` отрицательный (астероид слишком близко к кораблю). `distance.normalized` -- направление от астероида к кораблю. `position += direction * negative` -- сдвиг от корабля. Но сдвиг может вывести позицию за пределы `gameArea`. Проверки границ нет. Результат: астероид может спавниться за пределами видимой области.
- **Влияние:** Астероид может появиться за экраном (но `MoveSystem` -> `PlaceWithinGameArea` вернёт его при следующем обновлении).

### 33. `GetRandomUfoPosition` -- некорректная коррекция по вертикали

- **Файлы:** `Assets/Scripts/Utils/GameUtils.cs:14-18`
- **Суть:**
  ```csharp
  var verticalDistance = shipPosition.y - position.y;
  var allowedDistance = verticalDistance - spawnAllowedRadius;
  if (allowedDistance < 0)
  {
      position.y += verticalDistance / Math.Abs(verticalDistance) * allowedDistance;
  }
  ```
  `verticalDistance` может быть отрицательным (корабль ниже точки спавна). `allowedDistance = negative - 20` = ещё более отрицательный -- условие всегда true. `verticalDistance / Math.Abs(verticalDistance)` = `sign(verticalDistance)` (но вычисляется через деление -- потенциальный NaN если `verticalDistance == 0`). `sign * allowedDistance` -- коррекция в направлении знака. Алгоритм проверяет только вертикальное расстояние, а не общее -- при `verticalDistance == 0` и горизонтальной разнице < `spawnAllowedRadius`, UFO спавнится рядом с кораблём. Деление на ноль при `verticalDistance == 0`.
- **Влияние:** Деление на ноль (NaN) если корабль и точка спавна на одной высоте. UFO может спавниться рядом с кораблём.

### 34. `BaseModelSystem.Update()` -- итерация по `Dictionary.Values` во время потенциальной модификации

- **Файлы:** `Assets/Scripts/Model/Systems/BaseModelSystem.cs:27-31`
- **Суть:** `foreach (var node in _entityToNode.Values)` -- если `UpdateNode` вызовет `Kill()` на какой-то сущности, она будет удалена из `_entityToNode` только в `Model.Update()` после завершения всех систем (строки 144-153). Это безопасно, т.к. `Kill()` только устанавливает `_killed = true`, не модифицируя словарь. Но `GunSystem.UpdateNode` вызывает `node.OnShooting?.Invoke(node)` (строка 22), что вызывает `OnUserGunShooting` -> `_catalog.CreateBullet` -> `_model.AddEntity()` -> добавление в `_newEntities` (не в `_entities`). Это безопасно. `OnUserLaserShooting` вызывает `Kill()` на обнаруженных сущностях и `_model.ActionScheduler.ScheduleAction()` -- тоже безопасно.
- **Вывод:** Текущий код безопасен, но только потому что `Kill()` -- lazy (не модифицирует коллекции). При рефакторинге с eager-удалением -- баг.

---

## TODO-комментарии в коде

| Файл | Строка | Описание |
|------|--------|----------|
| `Assets/Scripts/Application/ModelFactory.cs` | 14, 23 | Пул моделей не реализован |
| `Assets/Scripts/Application/Game.cs` | 28 | Рефакторинг подписки `_model.OnEntityDestroyed` |
| `Assets/Scripts/Application/Game.cs` | 133 | `impl score receiver` -- очки начисляются, но нет унифицированного приёмника |
| `Assets/Scripts/Application/Game.cs` | 154 | `cleanup effect` -- эффекты не очищаются через пул (используют `OnParticleSystemStopped`) |
| `Assets/Scripts/Model/ActionScheduler.cs` | 28 | Добавление во время итерации -- нужен отдельный буфер |
| `Assets/Scripts/View/Base/BaseVisual.cs` | 31 | Лишнее копирование value-типов при `Connect()` |

---

## Recommendations

### Критический приоритет

1. **Исправить Kill UFO пулями** (`Game.cs:143`): добавить `Kill(ufoModel)` после `_model.ReceiveScore(ufoModel)`.

2. **Исправить конфиг UFO** (`EntitiesCatalog.cs:146`): заменить `_configs.UfoBig` на `_configs.Ufo`.

3. **Исправить `PlaceWithinGameArea`** (`Model.cs:163`): заменить `position = side - position` на `position = side + position`.

4. **Исправить деление на ноль в `MoveToSystem`** (`MoveToSystem.cs:18-19`): добавить `Mathf.Max(denominator, 0.1f)`.

5. **Исправить деление на ноль в `ShootToSystem`** (`ShootToSystem.cs:16-17`): использовать `_configs.Bullet.Speed` и ограничить знаменатель.

6. **Исправить деление на ноль в `GetRandomUfoPosition`** (`GameUtils.cs:17`): добавить проверку `verticalDistance != 0`.

### Высокий приоритет

7. **Устранить LINQ-аллокации в `Model.Update()`**: заменить `.Any()`, `.Where()`, `.RemoveWhere()` на буфер `_deadBuffer`.

8. **Вынести magic numbers в конфиги**: `10f`, `30`, `3f`, `20`, `0.5f` (торможение), скорость вращения.

9. **Добавить `Dispose()` в `PlayerInput`**: отписка от InputAction + `_playerControls.Disable()`.

10. **Добавить минимальные юнит-тесты**: `PlaceWithinGameArea`, `MoveToSystem`, `ShootToSystem`, `ActionScheduler`.

### Средний приоритет

11. Реализовать пул моделей в `ModelFactory`.
12. Снизить строковые аллокации в HUD (epsilon-порог).
13. Заменить `string` на `int` ключи в `GameObjectPool`.
14. Разделить `Game.cs` на `SpawnManager`, `CollisionHandler`, `EffectPlayer`.
15. Переименовать `UfoBigModel` в `BaseUfoModel`.

### Низкий приоритет

16. Ввести ограничение `MaxEnemyCount`.
17. Подключить `OnRestartAction` к логике рестарта.
18. Удалить неиспользуемый `BulletData.cs` (ScriptableObject).
19. Параметризовать коэффициент торможения в `ThrustSystem`.

---

*Аудит выполнен: 2026-04-02*
