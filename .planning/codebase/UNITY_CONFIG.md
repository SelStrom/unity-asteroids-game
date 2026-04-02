# Unity Configuration

**Дата анализа:** 2026-03-27

---

## Информация о проекте

| Параметр | Значение |
|----------|----------|
| Компания | Home |
| Продукт | Asteroids |
| Unity версия | 2022.3.60f1 |
| Scripting Backend | Mono (Standalone: 0) |
| API Compatibility Level | .NET Standard 2.1 (6) |
| Active Input Handler | Input System Package (1) |
| Целевая платформа | Standalone (основная) |
| Разрешение по умолчанию | 1920×1080 |
| Цветовое пространство | Gamma (0) |
| Рендеринг | Built-in, Forward |
| Fullscreen Mode | Exclusive Fullscreen (1) |
| Splash Screen | Отключён (`m_ShowUnitySplashScreen: 0`) |
| Bundle ID | `com.DefaultCompany.2DProject` |
| Cloud Project ID | `b80d4dd7-4bb0-4c81-b29b-4e84466d4630` |

---

## Теги и слои

### Пользовательские теги

Пользовательские теги отсутствуют (`tags: []`). Используется только системный тег `MainCamera` (на камере).

### Слои

| Индекс | Имя |
|--------|-----|
| 0 | Default |
| 1 | TransparentFX |
| 2 | Ignore Raycast |
| 3 | *(пусто)* |
| 4 | Water |
| 5 | UI |
| 6 | *(пусто)* |
| **7** | **Player** |
| **8** | **Asteroid** |
| **9** | **PlayerBullet** |
| **10** | **EnemyBullet** |
| **11** | **Enemy** |
| 12–31 | *(пусто)* |

### Sorting Layers

Только один слой: `Default` (uniqueID: 0).

---

## Физика

Игра использует **Physics 2D** (2D-проект). Настройки из `ProjectSettings/Physics2DSettings.asset`:

| Параметр | Значение |
|----------|----------|
| Gravity | (0, −9.81) |
| Velocity Iterations | 8 |
| Position Iterations | 3 |
| Max Translation Speed | 100 |
| Max Rotation Speed | 360 |
| Simulation Mode | FixedUpdate (0) |
| Queries Hit Triggers | true |
| Auto Sync Transforms | false |
| Multithreading | false |

> Примечание: все игровые объекты используют `Rigidbody2D` с `GravityScale: 0` — гравитация не влияет на игровые сущности.

### Матрица коллизий (2D)

Декодировано из `m_LayerCollisionMatrix` файла `Physics2DSettings.asset`. Только пары с пользовательскими слоями:

| | Player (7) | Asteroid (8) | PlayerBullet (9) | EnemyBullet (10) | Enemy (11) |
|---|:---:|:---:|:---:|:---:|:---:|
| **Player (7)** | — | ✓ | — | ✓ | ✓ |
| **Asteroid (8)** | ✓ | — | ✓ | — | ✓ |
| **PlayerBullet (9)** | — | ✓ | — | — | ✓ |
| **EnemyBullet (10)** | ✓ | — | — | — | — |
| **Enemy (11)** | ✓ | ✓ | ✓ | — | — |

**Итог взаимодействий:**
- `Player` ↔ `Asteroid` — столкновение игрока с астероидом
- `Player` ↔ `EnemyBullet` — попадание вражеской пули в игрока
- `Player` ↔ `Enemy` — столкновение игрока с НЛО
- `Asteroid` ↔ `PlayerBullet` — попадание пули игрока в астероид
- `Asteroid` ↔ `Enemy` — столкновение НЛО с астероидом
- `PlayerBullet` ↔ `Enemy` — попадание пули игрока в НЛО

---

## Привязки ввода

Файл: `Assets/Input/player_actions.inputactions`
Action Map: `PlayerControls`
Input System: **New Input System Package** (`activeInputHandler: 1`)

| Действие | Тип | Клавиши | Описание |
|----------|-----|---------|----------|
| **Attack** | Button | `Space` | Выстрел пулей |
| **Rotate** | Value (Axis) | `A` (negative, +1) / `D` (positive, −1) | Вращение корабля; 1DAxis(minValue=1, maxValue=−1) — `A` = +1 (влево), `D` = −1 (вправо) |
| **Accelerate** | Value | `W` | Ускорение корабля |
| **Laser** | Button | `Q` | Выстрел лазером |
| **Back** | Button | `Escape` | Назад / пауза |
| **Restart** | Button | `Space` | Рестарт игры |

Control Schemes: отсутствуют (пустой массив).
`initialStateCheck: true` для `Rotate` и `Accelerate` — значение считывается сразу при старте.

---

## Качество

Файл: `ProjectSettings/QualitySettings.asset`
Текущий уровень качества: **5 (Ultra)** (`m_CurrentQuality: 5`)

| Уровень | Имя | Anti-Aliasing | VSync | Тени | LOD Bias |
|---------|-----|:---:|:---:|------|---------|
| 0 | Very Low | Нет | Нет | Нет | 0.3 |
| 1 | Low | Нет | Нет | Нет | 0.4 |
| 2 | Medium | Нет | 1x | Hard Only | 0.7 |
| 3 | High | Нет | 1x | 2 cascades | 1.0 |
| 4 | Very High | 2x MSAA | 1x | 2 cascades | 1.5 |
| 5 | **Ultra** *(активный)* | 2x MSAA | 1x | 4 cascades | 2.0 |

Платформенные умолчания: Android / iOS / tvOS → Medium (2), WebGL → High (3), Standalone → Ultra (5).

---

## Аудио

Файл: `ProjectSettings/AudioManager.asset`

| Параметр | Значение |
|----------|----------|
| Master Volume | 1.0 |
| Doppler Factor | 1.0 |
| Rolloff Scale | 1.0 |
| Speaker Mode | Stereo (2) |
| Sample Rate | Системный (0 = auto) |
| DSP Buffer Size | 1024 |
| Virtual Voice Count | 512 |
| Real Voice Count | 32 |
| Virtualize Effects | true |
| Disable Audio | false |

---

*Конфигурация проанализирована: 2026-03-27*
