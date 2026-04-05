# Stack Research

**Domain:** Самонаводящиеся ракеты для Asteroids (Unity 6.3, гибридный DOTS)
**Researched:** 2026-04-05
**Confidence:** HIGH

## Контекст: что уже есть и НЕ нуждается в изменениях

Следующие технологии уже установлены и валидированы в v1.1.0. Добавление ракет НЕ требует обновления версий:

| Технология | Версия | Роль в ракетах |
|------------|--------|----------------|
| Unity 6.3 | 6000.3 | Движок |
| com.unity.entities | 1.4.5 | ECS-ядро: `IComponentData`, `ISystem`, `SystemAPI` |
| com.unity.inputsystem | 1.19.0 | Input Action для кнопки R |
| com.unity.render-pipelines.universal | 17.0.5 | URP 2D Renderer |
| com.shtl.mvvm | v1.1.0 (git) | MVVM для HUD (ReactiveValue, Bind) |
| com.unity.modules.particlesystem | 1.0.0 | ParticleSystem для инверсионного следа |
| com.unity.modules.physics2d | 1.0.0 | Collider2D + OnCollisionEnter2D |
| Unity.Mathematics | (транзитивный) | `float2`, `math.atan2`, `math.normalize` |
| Unity.Burst | (транзитивный) | `[BurstCompile]` для ECS-систем |

**Вывод: новых пакетов не требуется. Все необходимые модули уже в manifest.json.**

## Рекомендуемый стек для ракет

### Новые ECS-компоненты

| Компонент | Тип | Назначение | Почему именно так |
|-----------|-----|------------|-------------------|
| `RocketTag` | `IComponentData` (пустой struct) | Тег ракеты для фильтрации в запросах | По аналогии с `BulletTag`, `AsteroidTag` -- устоявшийся паттерн проекта |
| `RocketData` | `IComponentData` | Параметры наведения: `float TurnRateDegPerSec`, `float2 TargetPosition`, `bool HasTarget` | Отдельный компонент вместо расширения `MoveData` -- SRP, ракета повторно использует `MoveData` для позиции/скорости/направления |
| `PlayerRocketTag` | `IComponentData` (пустой struct) | Маркер принадлежности ракеты игроку | По аналогии с `PlayerBulletTag` -- необходим для `EcsCollisionHandlerSystem` |
| `RocketShootEvent` | `IBufferElementData` | Событие запуска ракеты (Entity стрелка, позиция, направление) | По аналогии с `GunShootEvent`, `LaserShootEvent` -- буфер событий для Bridge Layer |
| `RocketAmmoData` | `IComponentData` | Запас ракет и перезарядка: `int MaxRockets`, `int CurrentRockets`, `float ReloadDurationSec`, `float ReloadRemaining` | По аналогии с `GunData`/`LaserData` на ShipTag entity -- единообразная перезарядка |

### Новые ECS-системы

| Система | Тип | Назначение | Почему именно так |
|---------|-----|------------|-------------------|
| `EcsRocketHomingSystem` | `ISystem` (не BurstCompile) | Наведение: поиск ближайшей цели, поворот направления к цели с ограниченной скоростью поворота | Не Burst потому что нужен `EntityQuery` с несколькими WithAny/WithAll для поиска ближайшей цели среди разных типов. `MoveData.Direction` обновляется, `EcsMoveSystem` двигает ракету -- разделение ответственности |
| `EcsRocketAmmoSystem` | `ISystem` | Перезарядка ракет: декремент таймера, восстановление по одной ракете | По аналогии с `EcsLaserSystem` -- инкрементальная перезарядка. Обработка флага `Shooting` + запись `RocketShootEvent` |

**Ordering:**
- `EcsRocketHomingSystem` -- `[UpdateBefore(typeof(EcsMoveSystem))]` (обновляет Direction до того, как MoveSystem двигает)
- `EcsRocketAmmoSystem` -- `[UpdateAfter(typeof(EcsGunSystem))]` (по аналогии с EcsLaserSystem)

### Новый Input Action

| Элемент | Значение | Почему |
|---------|----------|--------|
| Action Name | `Rocket` | Единообразно с `Attack`, `Laser` |
| Type | `Button` | По аналогии с `Attack` и `Laser` -- одиночное нажатие |
| Binding | `<Keyboard>/r` | Указано в требованиях |
| Файл | `Assets/Input/player_actions.inputactions` | Существующий файл, добавляется action в `PlayerControls` map |

**PlayerInput.cs:** добавить `event Action OnRocketAction` + обработчик `OnRocket(InputAction.CallbackContext _)` по аналогии с `OnLaser`.

### Новый ScriptableObject-конфиг

| Поле | Тип | Назначение | Почему |
|------|-----|------------|--------|
| `RocketData` (nested struct в `GameData`) | `[Serializable] struct` | Конфигурация ракеты | По аналогии с `BulletData`, `LaserData` -- вложенные struct в `GameData` |
| `Prefab` | `GameObject` | Префаб ракеты (спрайт + коллайдер + ParticleSystem) | Стандарт для всех entity в проекте |
| `Speed` | `float` | Скорость полёта ракеты | Из конфига, не магическое число |
| `TurnRateDegPerSec` | `float` | Максимальная скорость поворота (градусы/сек) | Определяет "дугу" полёта -- ключевой параметр homing |
| `LifeTimeSeconds` | `float` | Время жизни ракеты (самоуничтожение) | Защита от вечных ракет, как у `BulletData` |
| `MaxRockets` | `int` | Максимальный запас | Аналог `LaserData.LaserMaxShoots` |
| `ReloadDurationSec` | `float` | Время перезарядки одной ракеты | Аналог `LaserData.LaserUpdateDurationSec` |
| `Sprite` | `Sprite` | Спрайт ракеты | Отдельное поле позволяет позже заменить на уникальный спрайт без изменений в коде |

### Новые View-компоненты (GameObject слой)

| Компонент | Базовый класс | Назначение | Почему |
|-----------|---------------|------------|--------|
| `RocketViewModel` | `AbstractViewModel` | `ReactiveValue<Action<Collision2D>> OnCollision` | Полная аналогия с `BulletViewModel` |
| `RocketVisual` | `AbstractWidgetView<RocketViewModel>` + `IEntityView` | Коллайдер + OnCollisionEnter2D, управление ParticleSystem (trail) | Аналогия с `BulletVisual`, дополнительно управляет дочерним ParticleSystem |

### Инверсионный след (Trail VFX)

| Решение | Почему | Альтернативы отвергнуты |
|---------|--------|-------------------------|
| **ParticleSystem** на дочернем GameObject префаба ракеты | Уже используется в проекте (`EffectVisual` с `_particleSystem`). Работает с URP. Не требует новых пакетов. WebGL-совместим | Trail Renderer -- проще, но менее контролируем для "дымного следа". VFX Graph -- не поддерживает WebGL (критическое ограничение платформы) |
| Simulation Space: **World** | Частицы остаются на месте при движении ракеты -- реалистичный след | Local Space -- след движется с ракетой, выглядит неправильно |
| Stop Action: **None** (управляется из кода) | Ракета пулится; ParticleSystem.Stop() + Clear() при Release, Play() при Get | Destroy -- несовместимо с пулингом |

**Настройки ParticleSystem для следа:**
- Emission: Rate over Distance (частицы по пути, а не по времени) -- след зависит от скорости
- Shape: Point (узкий след из одной точки)
- Start Lifetime: 0.3-0.5 сек (короткий след)
- Start Size: маленький, с Size over Lifetime уменьшение к нулю
- Color over Lifetime: от белого к прозрачному
- Renderer: Billboard, Material: URP Particles/Unlit (встроен в URP 17.0.5)

### HUD-расширения

| Элемент | Реализация | Почему |
|---------|------------|--------|
| Счётчик ракет | `ReactiveValue<string> RocketCount` в `HudData` + `TMP_Text` в `HudVisual` | По аналогии с `LaserShootCount` |
| Таймер перезарядки | `ReactiveValue<string> RocketReloadTime` + `ReactiveValue<bool> IsRocketReloadTimeVisible` | По аналогии с `LaserReloadTime` + `IsLaserReloadTimeVisible` |

## Точки интеграции с существующим кодом

### EntityFactory

Добавить `CreateRocket(EntityManager em, float2 position, float speed, float2 direction, float lifeTime, float turnRate)`:
- `RocketTag` + `PlayerRocketTag`
- `MoveData` (позиция, скорость, направление)
- `LifeTimeData` (время жизни -- повторное использование)
- `RocketData` (turnRate, targetPosition, hasTarget)

Ракета повторно использует `MoveData` + `LifeTimeData` -- обрабатывается существующими `EcsMoveSystem` и `EcsDeadByLifeTimeSystem`.

### EcsCollisionHandlerSystem

Добавить проверки:
- `PlayerRocket + Enemy` -> MarkDead обоих + AddScore (по аналогии с PlayerBullet + Enemy)
- `PlayerRocket + Ship` -> ничего (ракета не вредит своему кораблю)

**ВАЖНО:** Отдельный метод `IsPlayerRocket(ref EntityManager em, Entity entity)` вместо расширения `IsPlayerBullet` -- не нарушать существующую логику.

### GameObjectSyncSystem

Уже синхронизирует позицию ECS -> GameObject по `MoveData` + `GameObjectRef`. Ракета получает синхронизацию автоматически при наличии `GameObjectRef`.

### ObservableBridgeSystem

Нужен bridge `RocketAmmoData` -> HUD (через ObservableValue -> ReactiveValue -> TMP_Text). По аналогии с мостом для LaserData.

### DeadEntityCleanupSystem

Уничтожение ракеты по `DeadTag` -- автоматически, без изменений.

### Object Pool

Ракета пулится по тому же паттерну, что и пуля:
- `GameObjectPool.Get(rocketPrefab)` при создании
- `GameObjectPool.Release(rocketGO)` при уничтожении
- ParticleSystem.Stop() + Clear() при Release в `RocketVisual`

## Что НЕ добавлять

| Технология | Почему не нужна |
|------------|-----------------|
| **VFX Graph** (`com.unity.visualeffectgraph`) | Не поддерживает WebGL -- платформенное ограничение проекта. ParticleSystem достаточен для простого следа |
| **DOTween / LeanTween** | Анимация дуги полёта -- через ECS-систему с `TurnRateDegPerSec`, не через tweening. ECS-first архитектура |
| **Unity.Physics** / **DOTS Physics** | Нет production-ready 2D DOTS Physics. Коллизии через существующий Collider2D + OnCollisionEnter2D -> CollisionEventData буфер |
| **NavMesh / AI Navigation** | Overkill для homing-ракеты. Простой поворот к цели через `math.atan2` + ограничение скорости поворота |
| **Cinemachine** | Камера не следит за ракетой, статичная камера |
| **UniTask / Awaitable** | Корутины не нужны для ракеты -- вся логика в ECS Update loop |
| **Любые новые пакеты** | Все необходимое уже в manifest.json |

## Алгоритм наведения (рекомендация для EcsRocketHomingSystem)

Proportional Navigation избыточен для аркады. Рекомендуется "поворот к цели с ограничением скорости поворота":

```
1. Найти ближайшую цель (AsteroidTag || UfoTag || UfoBigTag) по расстоянию
2. Вычислить желаемый угол: desiredAngle = math.atan2(delta.y, delta.x)
3. Вычислить текущий угол: currentAngle = math.atan2(direction.y, direction.x)
4. Разница углов: deltaAngle = desiredAngle - currentAngle (нормализовать в [-PI, PI])
5. Ограничить поворот: actualTurn = math.clamp(deltaAngle, -turnRate * dt, turnRate * dt)
6. Новое направление: newAngle = currentAngle + actualTurn
7. MoveData.Direction = float2(math.cos(newAngle), math.sin(newAngle))
8. MoveSystem двигает ракету на основе MoveData
```

Burst-совместимость: `math.atan2`, `math.clamp`, `math.sin`, `math.cos` -- всё из `Unity.Mathematics`. Поиск ближайшей цели требует итерации, но для десятков врагов прямая итерация достаточна.

**Если цель не найдена:** ракета летит прямо (MoveData.Direction без изменений). `HasTarget = false`.

**Пересчёт цели:** каждый кадр заново -- цель может быть уничтожена. Стоимость мала при десятках entities.

## Совместимость версий

| Компонент | Совместим с | Примечание |
|-----------|-------------|------------|
| `IComponentData` struct | com.unity.entities 1.4.5 | Стабильный API с 1.0 |
| `ISystem` (unmanaged) | com.unity.entities 1.4.5 | Стабильный API |
| `SystemAPI.Query<>` | com.unity.entities 1.4.5 | Source-generated foreach |
| `IBufferElementData` | com.unity.entities 1.4.5 | Для `RocketShootEvent`, как существующие `GunShootEvent`/`LaserShootEvent` |
| ParticleSystem + URP | URP 17.0.5 | Particles/Unlit shader встроен в URP. Подтверждено наличием `EffectVisual` в проекте |
| Input System Button action | com.unity.inputsystem 1.19.0 | Тот же паттерн что Attack/Laser |
| `[BurstCompile]` на ISystem | Unity.Burst (транзитивный) | Опционально для RocketHomingSystem -- зависит от реализации поиска цели |

## Источники

- Кодовая база проекта `Assets/Scripts/ECS/` -- паттерны компонентов, систем, EntityFactory (HIGH confidence)
- `Packages/manifest.json` -- точные версии пакетов (HIGH confidence)
- Существующие visual patterns: `BulletVisual`, `EffectVisual`, `ShipVisual` (HIGH confidence)
- `Assets/Input/player_actions.inputactions` + `PlayerInput.cs` -- паттерн добавления input actions (HIGH confidence)
- `Assets/Scripts/Configs/GameData.cs` -- паттерн вложенных конфигов (HIGH confidence)
- `Assets/Scripts/View/HudVisual.cs` -- паттерн HUD через ReactiveValue (HIGH confidence)
- Unity Entities 1.4.5 API: `IComponentData`, `ISystem`, `SystemAPI.Query`, `IBufferElementData` -- стабильный API с 1.0 (HIGH confidence)
- ParticleSystem + URP совместимость -- подтверждена наличием `EffectVisual` с ParticleSystem в проекте (HIGH confidence)

---
*Stack research for: Самонаводящиеся ракеты (Asteroids v1.2.0)*
*Researched: 2026-04-05*
