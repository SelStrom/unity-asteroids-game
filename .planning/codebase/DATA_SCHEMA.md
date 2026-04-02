# Data Schema

**Дата анализа:** 2026-03-27

Все ScriptableObject-классы находятся в `Assets/Scripts/Configs/`.
Все .asset-файлы находятся в `Assets/Media/configs/`.
Namespace: `SelStrom.Asteroids.Configs`.

---

## BaseGameEntityData (`Assets/Scripts/Configs/BaseGameEntityData.cs`)

Абстрактный базовый класс. Наследуется `AsteroidData` и `UfoData`.

| Поле | Тип | Описание |
|------|-----|----------|
| `Score` | `int` | Очки за уничтожение сущности |

---

## AsteroidData (`Assets/Scripts/Configs/AsteroidData.cs`)

Наследует `BaseGameEntityData`. Меню создания: `Asteroid data`.

| Поле | Тип | Описание |
|------|-----|----------|
| `Score` | `int` | Очки (наследовано) |
| `Prefab` | `GameObject` | Ссылка на префаб астероида |
| `SpriteVariants` | `Sprite[]` | Варианты спрайтов (случайный выбор при спавне) |

### Значения .asset-файлов

| Asset | Score | Prefab | SpriteVariants (кол-во) |
|-------|-------|--------|------------------------|
| `AsteroidBigData.asset` | **1** | `asteroid_big.prefab` | 3 варианта |
| `AsteroidMediumData.asset` | **2** | `asteroid_medium.prefab` | 3 варианта |
| `AsteroidSmallData.asset` | **3** | `asteroid_small.prefab` | 3 варианта |

Все спрайты берутся из одного спрайтового атласа (guid: `39238117801b40c43856f62b7fdf50fe`).

---

## GunData (`Assets/Scripts/Configs/GunData.cs`)

Конфиг орудия (используется внутри `GameData.ShipData` и в `UfoData`). Меню создания: `Gun data`.

| Поле | Тип | Описание |
|------|-----|----------|
| `MaxShoots` | `int` | Максимальное количество пуль одновременно |
| `ReloadDurationSec` | `float` | Время перезарядки (секунды) |

### Значения .asset-файлов

| Asset | MaxShoots | ReloadDurationSec |
|-------|-----------|-------------------|
| `UserGunData.asset` | **5** | **2.0** с |
| `UfoGunData.asset` | **1** | **2.0** с |

---

## UfoData (`Assets/Scripts/Configs/UfoData.cs`)

Наследует `BaseGameEntityData`. Меню создания: `Ufo data`.

| Поле | Тип | Описание |
|------|-----|----------|
| `Score` | `int` | Очки (наследовано) |
| `Prefab` | `GameObject` | Ссылка на префаб НЛО |
| `Speed` | `float` | Скорость перемещения НЛО |
| `Gun` | `GunData` | Ссылка на ScriptableObject с параметрами орудия |

> Примечание: в `UfoData.asset` присутствует поле `ShootDurationSec: 2` (интервал стрельбы), хотя оно не объявлено в текущей версии класса `UfoData.cs`. Возможно, это устаревшее поле или поле из подкласса.

### Значения .asset-файлов

| Asset | Score | Speed | Gun |
|-------|-------|-------|-----|
| `UfoBigData.asset` | **4** | **5** ед/с | `UfoGunData.asset` |
| `UfoData.asset` | **5** | **10** ед/с | `UfoGunData.asset` |

---

## GameData (`Assets/Scripts/Configs/GameData.cs`)

Главный конфиг игры. Меню создания: `Game data`. Содержит вложенные сериализуемые структуры.

### Поля верхнего уровня

| Поле | Тип | Описание |
|------|-----|----------|
| `AsteroidInitialCount` | `int` | Начальное количество астероидов |
| `SpawnAllowedRadius` | `int` | Радиус зоны запрета спавна вокруг игрока |
| `SpawnNewEnemyDurationSec` | `float` | Интервал спавна нового врага (секунды) |
| `VfxBlowPrefab` | `GameObject` | Префаб эффекта взрыва |
| `UfoBig` | `UfoData` | Конфиг большого НЛО |
| `Ufo` | `UfoData` | Конфиг малого НЛО |
| `AsteroidBig` | `AsteroidData` | Конфиг большого астероида |
| `AsteroidMedium` | `AsteroidData` | Конфиг среднего астероида |
| `AsteroidSmall` | `AsteroidData` | Конфиг малого астероида |
| `Bullet` | `BulletData` *(struct)* | Параметры пуль |
| `Laser` | `LaserData` *(struct)* | Параметры лазера |
| `Ship` | `ShipData` *(struct)* | Параметры корабля игрока |
| `LeaderboardId` | `string` | ID таблицы лидеров |

### Вложенная структура: `GameData.BulletData`

| Поле | Тип | Описание |
|------|-----|----------|
| `Prefab` | `GameObject` | Префаб пули игрока |
| `EnemyPrefab` | `GameObject` | Префаб вражеской пули |
| `LifeTimeSeconds` | `int` | Время жизни пули (секунды) |
| `Speed` | `float` | Скорость пули |

### Вложенная структура: `GameData.LaserData`

| Поле | Тип | Описание |
|------|-----|----------|
| `Prefab` | `GameObject` | Префаб лазерного луча |
| `BeamEffectLifetimeSec` | `float` | Время показа визуального эффекта луча (секунды) |
| `LaserUpdateDurationSec` | `int` | Период восстановления заряда лазера (секунды) |
| `LaserMaxShoots` | `int` | Максимальное количество зарядов лазера |

### Вложенная структура: `GameData.ShipData`

| Поле | Тип | Описание |
|------|-----|----------|
| `Prefab` | `GameObject` | Префаб корабля |
| `MainSprite` | `Sprite` | Основной спрайт корабля |
| `ThrustSprite` | `Sprite` | Спрайт корабля с соплами (при ускорении) |
| `ThrustUnitsPerSecond` | `float` | Ускорение тяги (ед/с²) |
| `MaxSpeed` | `float` | Максимальная скорость |
| `Gun` | `GunData` | Ссылка на ScriptableObject орудия игрока |

### Значения `GameData.asset`

| Поле | Значение |
|------|----------|
| `AsteroidInitialCount` | **10** |
| `SpawnAllowedRadius` | **20** единиц |
| `SpawnNewEnemyDurationSec` | **25** с |
| `Bullet.LifeTimeSeconds` | **2** с |
| `Bullet.Speed` | **20** ед/с |
| `Bullet.Prefab` | `bullet.prefab` |
| `Bullet.EnemyPrefab` | `bullet_enemy.prefab` |
| `Laser.BeamEffectLifetimeSec` | **0.5** с |
| `Laser.LaserUpdateDurationSec` | **10** с |
| `Laser.LaserMaxShoots` | **3** заряда |
| `Ship.ThrustUnitsPerSecond` | **6** ед/с² |
| `Ship.MaxSpeed` | **15** ед/с |
| `Ship.Gun` | `UserGunData.asset` (MaxShoots=5, Reload=2с) |
| `LeaderboardId` | `"asteroids_highscores"` |

---

## Сводка баланса

| Сущность | Очки | Скорость | Пуль макс. | Перезарядка |
|----------|------|----------|-----------|-------------|
| Астероид большой | 1 | — | — | — |
| Астероид средний | 2 | — | — | — |
| Астероид малый | 3 | — | — | — |
| НЛО большой | 4 | 5 ед/с | 1 | 2 с |
| НЛО малый | 5 | 10 ед/с | 1 | 2 с |
| Корабль игрока | — | макс. 15 ед/с | 5 | 2 с |
| Лазер | — | — | 3 заряда | восст. 10 с |
| Пуля | — | 20 ед/с | — | время жизни 2 с |

---

*Схема данных проанализирована: 2026-03-27*
