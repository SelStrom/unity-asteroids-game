# Integrations

**Дата анализа:** 2026-04-02

## 1. Unity Gaming Services (UGS) — Authentication + Leaderboards

### Архитектура

Трёхслойная абстракция с интерфейсами для подмены:

```
GameScreen → LeaderboardService → IAuthProxy / ILeaderboardProxy
                                  ↓                ↓
                            UnityAuthProxy    UnityLeaderboardProxy
                                  ↓                ↓
                        AuthenticationService  LeaderboardsService
```

### Используемые API

```csharp
using Unity.Services.Core;           // UnityServices.InitializeAsync()
using Unity.Services.Authentication; // AuthenticationService.Instance
using Unity.Services.Leaderboards;   // LeaderboardsService.Instance
```

Пакеты: `com.unity.services.authentication` 3.3.3, `com.unity.services.leaderboards` 2.1.0.

### Алгоритм инициализации (`LeaderboardService.EnsureInitialized`, `LeaderboardService.cs:46-91`)

1. Если уже инициализирован — return
2. Если инициализация в процессе — `WaitUntil(() => !_initializing)`, проверить результат
3. Установить `_initializing = true`
4. `_auth.Initialize()` → `UnityServices.InitializeAsync()` (если не Initialized)
5. Если не SignedIn → `_auth.SignInAnonymously()` → `AuthenticationService.Instance.SignInAnonymouslyAsync()`
6. Установить `_initialized = true`, `_initializing = false`

**Защита от параллельных вызовов:** семафор через `_initializing` + `WaitUntil`. Все методы сервиса сначала проходят через `EnsureInitialized`.

### Алгоритм submit + best-score (`GameScreen.SubmitAndShowLeaderboardRoutine`, `GameScreen.cs:225-289`)

1. Загрузить текущий лучший результат: `GetPlayerScore()`
2. `bestScore = Math.Max(serverScore ?? 0, currentGameScore)` — клиент сам выбирает максимум
3. `SubmitScore(playerName, bestScore)` — отправить лучший
4. Загрузить топ-10: `GetTopScores()`
5. Загрузить персональный ранг повторно (мог измениться)
6. Показать лидерборд

**Защита от stale coroutine:** `if (_score.ViewModel != viewModel) yield break` — проверка на каждом `yield return`. Если пользователь нажал Restart, ViewModel сменился, и старая корутина прекращается.

### Метаданные игрока

Имя игрока передаётся в `Metadata` как JSON: `{"playerName": "..."}` (`UnityLeaderboardProxy.cs:13-15`).

Парсинг: `JsonUtility.FromJson<PlayerMetadata>(metadataJson)` с fallback на `"???"` при ошибке (`UnityLeaderboardProxy.cs:74-89`).

### Coroutine Result Pattern (`CoroutineResult.cs`)

```csharp
public class CoroutineResult {
    public Exception Error { get; set; }
    public bool IsSuccess => Error == null;
}
public class CoroutineResult<T> : CoroutineResult {
    public T Value { get; set; }
}
```

Паттерн: каждая корутина получает `CoroutineResult` как out-параметр. Проверка `!result.IsSuccess` после `yield return` определяет, произошла ли ошибка.

## 2. Unity Input System

### Конфигурация (`Assets/Input/player_actions.inputactions`)

Одна Action Map: **PlayerControls**

| Action | Type | Binding | Особенности |
|--------|------|---------|-------------|
| Attack | Button | `<Keyboard>/space` | performed only |
| Rotate | Value (Axis) | 1DAxis: A (negative) / D (positive) | **Инвертированный:** `minValue=1, maxValue=-1` → A=правый поворот, D=левый |
| Accelerate | Value | `<Keyboard>/w` | performed + canceled → bool |
| Laser | Button | `<Keyboard>/q` | performed only |
| Back | Button | `<Keyboard>/escape` | Quit (не работает в WebGL) |
| Restart | Button | `<Keyboard>/space` | ⚠ Конфликт с Attack (та же клавиша) |

### Маршрутизация ввода (`PlayerInput.cs`)

```
PlayerActions → PlayerInput (C# события) → Game (обработчики)
```

- `OnRotate(ctx)` → `ctx.ReadValue<float>()` → `OnRotateAction(float)` — `-1` или `1`
- `OnAccelerate(ctx)` → `ctx.performed` (bool) → `OnTrustAction(bool)`
- `OnAttack/OnLaser` → без параметров
- Подписка на `performed` + `canceled` для осей, только `performed` для кнопок

## 3. Shtl.Mvvm — реактивные привязки

### Ключевые типы (из `Shtl.Mvvm.dll`)

- `ObservableValue<T>` — модельное значение с уведомлением об изменении (Model layer)
- `ReactiveValue<T>` — значение для привязки к View (View layer)
- `EventBindingContext` — управление временем жизни подписок
- `AbstractWidgetView<TViewModel>` — базовый MonoBehaviour для View с ViewModel
- `AbstractViewModel` — базовый класс ViewModel
- `ReactiveList<T>` — список с уведомлениями (для лидерборда)
- `BindFrom<T>` — промежуточный объект для fluent-API привязок

### Паттерн привязки

```csharp
// В EntitiesCatalog — связь Model → ViewModel
bindings.From(model.Move.Position).To(viewModel.Position);  // ObservableValue → ReactiveValue
bindings.InvokeAll();  // Начальная синхронизация

// В Visual.OnConnected — связь ViewModel → Unity UI
Bind.From(ViewModel.Position).To(transform);  // ReactiveValue → Transform
Bind.From(ViewModel.Score).To(_scoreText);    // ReactiveValue<string> → TMP_Text
Bind.From(ViewModel.IsVisible).To(gameObject); // ReactiveValue<bool> → SetActive
Bind.From(_button).To(viewModel.OnAction);    // Button.onClick → ReactiveValue<Action>
```

### Специальное расширение (`BindingToExtensions.cs`)

```csharp
// ReactiveValue<Vector2> → Transform.position (сохраняя z)
from.Source.Connect(value => {
    var position = target.position;
    position.x = value.x;
    position.y = value.y;
    target.position = position;
});
```

### Lifecycle

1. `EventBindingContext` создаётся при создании сущности
2. `bindings.InvokeAll()` — начальная синхронизация всех привязок
3. При уничтожении: `bindings.CleanUp()` — отписка всех обработчиков
4. Для экранов: `AbstractScreen._context.CleanUp()` при смене состояния

## 4. Physics2D — коллизии и лазерный луч

### Коллизии (Unity Rigidbody2D + Collider2D)

Обработка через MonoBehaviour callbacks:
- `ShipVisual.OnCollisionEnter2D` → `ViewModel.OnCollision.Value?.Invoke(col)`
- `BulletVisual.OnCollisionEnter2D` → `ViewModel.OnCollision.Value?.Invoke(col)`
- `UfoVisual.OnCollisionEnter2D` → `ViewModel.OnCollision.Value?.Invoke()`

**Маршрут:** Unity Physics → Visual (MonoBehaviour) → ViewModel (ReactiveValue<Action>) → Game (обработчик)

**Слои (Layer Mask):** "Asteroid", "Enemy" — используются для фильтрации лазерного raycast.

### Лазерный Raycast (`Game.OnUserLaserShooting`, `Game.cs:210-238`)

```csharp
var hits = new RaycastHit2D[30];  // Стековый буфер на 30 попаданий
var size = Physics2D.RaycastNonAlloc(
    origin: shipPosition,
    direction: shipRotation,       // Vector2 направление
    results: hits,
    distance: gameArea.magnitude,  // Диагональ игровой области
    layerMask: LayerMask.GetMask("Asteroid", "Enemy")
);
```

**Алгоритм:**
1. Выстрелить луч от позиции корабля в направлении поворота
2. Дальность = диагональ GameArea (покрывает весь экран)
3. Для каждого попадания: найти модель → начислить очки → уничтожить
4. Пробивает ВСЕ объекты на пути (не останавливается на первом)

## 5. ParticleSystem — эффекты взрывов

**`EffectVisual`** (`EffectVisual.cs`) — MonoBehaviour обёртка:
1. `OnConnected()` → `_particleSystem.Play()`
2. `OnParticleSystemStopped()` → вызов callback → `Game.OnEffectStopped` → `ViewFactory.Release(effect)` → возврат в пул

**Stop Action** настроен в ParticleSystem на "Callback" для вызова `OnParticleSystemStopped`.

## 6. PlayerPrefs — сохранение имени

- `PlayerPrefs.GetString("PlayerName", "")` — загрузка при EndGame (`GameScreen.cs:147`)
- `PlayerPrefs.SetString("PlayerName", name)` + `Save()` — сохранение при Submit (`GameScreen.cs:171-172`)
- `DefaultPlayerName` передаётся в ViewModel для предзаполнения input field при "Change Name"

## 7. GameObjectPool — пул объектов

**Алгоритм** (`GameObjectPool.cs`):

**Get:**
1. Получить `prefabId = prefab.GetInstanceID().ToString()` — ⚠ строковая аллокация при каждом вызове
2. Если есть в пуле (`Stack<GameObject>`) — Pop, SetParent, SetActive(true)
3. Иначе — `Object.Instantiate(prefab, parent)`
4. Зарегистрировать в `_gameObjectToPrefabId` для обратного поиска

**Release:**
1. Найти `prefabId` по `GameObject` в словаре
2. `SetActive(false)`, `SetParent(poolContainer)`
3. Push в Stack
4. Удалить из обратного словаря

**Ключи пула:** строковое представление InstanceID (`ToString()`) — создаёт GC-давление при каждой операции.

## 8. ScriptableObject конфигурация

### GameData (`GameData.cs`) — главный конфиг

| Поле | Тип | Описание |
|------|-----|----------|
| `AsteroidInitialCount` | int | Стартовое кол-во астероидов |
| `SpawnAllowedRadius` | int | Минимальная дистанция спауна от корабля |
| `SpawnNewEnemyDurationSec` | float | Интервал спауна врагов |
| `VfxBlowPrefab` | GameObject | Prefab эффекта взрыва |
| `UfoBig` / `Ufo` | UfoData | Конфиг больших/малых UFO |
| `AsteroidBig/Medium/Small` | AsteroidData | 3 размера астероидов |
| `Bullet` | BulletData (struct) | Speed, LifeTimeSeconds, Prefab, EnemyPrefab |
| `Laser` | LaserData (struct) | Prefab, BeamEffectLifetimeSec, LaserUpdateDurationSec, LaserMaxShoots |
| `Ship` | ShipData (struct) | Prefab, Sprites, ThrustUnitsPerSecond, MaxSpeed, GunData ref |
| `LeaderboardId` | string | ID лидерборда UGS (default: "asteroids_highscores") |

### Вложенные структуры в GameData

- `BulletData` (struct) — **не путать** с `Configs/BulletData.cs` (ScriptableObject, dead code)
- `ShipData` (struct) — содержит ссылку на `GunData` ScriptableObject
- `LaserData` (struct) — inline конфиг лазера

### UfoData (`UfoData.cs`)
- `Score: int` (наследуется от `BaseGameEntityData`)
- `Prefab: GameObject`, `Speed: float`, `Gun: GunData`

### AsteroidData (`AsteroidData.cs`)
- `Score: int` (наследуется от `BaseGameEntityData`)
- `Prefab: GameObject`, `SpriteVariants: Sprite[]` — случайный выбор спрайта при создании

## 9. TextMeshPro

Используется для всего текстового UI:
- HUD: координаты, скорость, угол, лазерные счётчики (5 полей `TMP_Text`)
- Score screen: итоговый счёт, лидерборд, ввод имени (`TMP_InputField`)
- `GuiText` — обёртка для `TMP_Text` (используется как компонент)

## 10. Тороидальный экран — интеграция MoveSystem и Model

**Алгоритм обёртки** (`Model.PlaceWithinGameArea`, `Model.cs:156-167`):
```csharp
if (position > side / 2)
    position = -side + position;  // Выход за правый/верхний край
if (position < -side / 2)
    position = side - position;   // Выход за левый/нижний край (⚠ баг)
```

Применяется к каждой оси (x, y) отдельно в `MoveSystem.UpdateNode`.

**GameArea** вычисляется один раз в `Application.Start()`:
```csharp
var orthographicSize = mainCamera.orthographicSize;
var sceneWidth = mainCamera.aspect * orthographicSize * 2;
var sceneHeight = orthographicSize * 2;
```
