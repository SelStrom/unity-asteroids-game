# Scene & Prefabs

**Дата анализа:** 2026-03-27

---

## Главная сцена (`Assets/Scenes/Main.unity`)

### Корневые GameObject-ы

| Имя | Активен | Компоненты | Примечание |
|-----|:-------:|-----------|-----------|
| **Main Camera** | ✓ | Transform, Camera, AudioListener | Ортографическая камера |
| **UI** | ✓ | RectTransform, Canvas, CanvasScaler, GraphicRaycaster | Корневой Canvas |
| **Application** | ✓ | Transform, MonoBehaviour (Application-контроллер) | Точка входа игровой логики |
| **Game** | ✓ | Transform | Контейнер для игровых сущностей (пустой в редакторе) |
| **Pool** | ✓ | Transform | Контейнер пула объектов (пустой в редакторе) |
| **EventSystem** | ✓ | (UI EventSystem) | Обработка ввода UI |

---

### Main Camera

- **Tag:** `MainCamera`
- **Transform:** позиция (0, 0, −10)
- **Camera:**
  - Режим: Orthographic
  - Orthographic Size: **22.5** (охват по вертикали = 45 мировых единиц)
  - Clear Flags: Solid Color (2), фон: чёрный (0,0,0,0)
  - Near clip: 0.3, Far clip: 1000
  - Depth: −1
  - HDR: выкл., MSAA: выкл.
- **AudioListener** присутствует

---

### UI (Canvas)

- **Canvas:** RenderMode = Camera (1), привязан к `Main Camera`, PlaneDistance = 1
- **CanvasScaler:** ScaleMode = Scale With Screen Size (1), Reference Resolution = **1920×1080**, ScreenMatchMode = Match Width (0)
- Дочерние панели:

| Дочерний объект | Активен по умолч. | Тип | Описание |
|----------------|:-----------------:|-----|---------|
| **Hud** | ✗ (IsActive: 0) | RectTransform + HudVisual MonoBehaviour | HUD во время игры: координаты, угол, скорость, заряды лазера, таймер перезарядки |
| **Score** | ✗ (IsActive: 0) | RectTransform + ScoreScreen MonoBehaviour | Экран окончания игры: счёт, таблица лидеров, ввод имени |
| **TitleScreen** | ✓ | RectTransform + TitleScreen MonoBehaviour | Стартовый экран с кнопкой Start |

---

### Application

- **MonoBehaviour:** (guid: `d31b2fe0a867431d83fa2a223dc4daf7`)
- Сериализованные ссылки:
  - `_configs` → `GameData.asset`
  - `_poolContainer` → Transform объекта **Pool**
  - `_gameContainer` → Transform объекта **Game**
  - `_hudVisual` → MonoBehaviour объекта **Hud**
  - `_scoreVisual` → MonoBehaviour объекта **Score**
  - `_titleScreenView` → MonoBehaviour объекта **TitleScreen**

---

### Hud (панель HUD)

- Активен: **нет** (включается при старте игры)
- Якорь: левый верхний угол (AnchorMin: 0,1 / AnchorMax: 0,1), позиция (16, −16)
- MonoBehaviour `HudVisual` содержит ссылки:
  - `_coordinates` — TextMeshPro метка координат
  - `_rotationAngle` — TextMeshPro метка угла поворота
  - `_speed` — TextMeshPro метка скорости
  - `_laserShootCount` — TextMeshPro счётчик зарядов лазера
  - `_laserReloadTime` — TextMeshPro таймер перезарядки
- Дочерние элементы: несколько экземпляров `gui_text.prefab` с именами `coordinates`, `rotation_angle`, `speed_text`, `laser_shoot_count`

---

### Score (экран результатов)

- Активен: **нет**
- MonoBehaviour `ScoreScreen` содержит ссылки:
  - `_scoreText` — TextMeshPro с итоговым счётом
  - `_nameInputContainer` — контейнер ввода имени
  - `_nameInput` — TMP_InputField
  - `_submitButton` — кнопка Submit
  - `_leaderboardContainer` — контейнер таблицы лидеров
  - `_entryTemplate` — шаблон строки таблицы (`leaderboard_entry.prefab`)
  - `_entriesContainer` — контейнер строк
  - `_playerRankText` — текст с рангом игрока
  - `_changeNameButton` — кнопка смены имени
  - `_loadingIndicator` — индикатор загрузки (красный квадрат 30×30, якорь правый верхний угол)
  - `_restartButton` — кнопка Restart

---

## Префабы

### ship (`Assets/Media/prefabs/ship.prefab`)

- **Layer:** 7 (Player)
- **Tag:** Untagged

Компоненты:
- `Transform`: позиция (0,0,0), масштаб (1,1,1)
- `SpriteRenderer`: спрайт корабля (из атласа `39238117801b40c43856f62b7fdf50fe`), цвет белый, SortingLayer Default / Order 0, CastShadows: выкл.
- `Rigidbody2D`: BodyType = Kinematic (1), Mass = 0.5, LinearDrag = 1, AngularDrag = 1, GravityScale = **0**, UseFullKinematicContacts = true
- `PolygonCollider2D`: форма — треугольник: `(1, 0)`, `(−0.5, 0.5)`, `(−0.5, −0.5)`, IsTrigger = false
- `MonoBehaviour` (guid: `df4996ff456d4ad6a9767bee0f04c679`): ссылка на `_spriteRenderer`

---

### bullet (`Assets/Media/prefabs/bullet.prefab`)

- **Layer:** 9 (PlayerBullet)
- **Tag:** Untagged

Компоненты:
- `Transform`: позиция (0,0,0)
- `SpriteRenderer`: спрайт пули (атлас `39238117801b40c43856f62b7fdf50fe`)
- `Rigidbody2D`: Kinematic, Mass = 0.5, LinearDrag = 1, GravityScale = **0**
- `CircleCollider2D`: Radius = **0.2**, IsTrigger = false
- `MonoBehaviour` (guid: `aa1aa04866a5f26488aed44b690cc3a0`): ссылка на `_collider`

---

### bullet\_enemy (`Assets/Media/prefabs/bullet_enemy.prefab`)

Является вариантом префаба `bullet.prefab` с переопределёнными значениями:
- **Layer:** 10 (EnemyBullet) — переопределено через `m_Layer: 10`
- **Имя:** `bullet_enemy`
- Все остальные компоненты идентичны `bullet.prefab`

---

### asteroid\_big (`Assets/Media/prefabs/asteroid_big.prefab`)

- **Layer:** 8 (Asteroid)
- **Tag:** Untagged

Компоненты:
- `Transform`: позиция (0,0,0)
- `SpriteRenderer`: начальный спрайт — первый из `SpriteVariants` (задаётся из `AsteroidBigData`), атлас `39238117801b40c43856f62b7fdf50fe`
- `Rigidbody2D`: Kinematic, GravityScale = **0**
- `CircleCollider2D`: Radius = **2** (базовое значение для большого астероида)
- `MonoBehaviour` (guid: `94116fa4dbe14d54876156a1a7eb2e61`): ссылка на `_spriteRenderer`

---

### asteroid\_medium (`Assets/Media/prefabs/asteroid_medium.prefab`)

Является вариантом префаба `asteroid_big.prefab` с переопределениями:
- **Имя:** `asteroid_medium`
- `CircleCollider2D.Radius`: **1** (переопределено)
- `_spriteVariants`: 3 спрайта из `AsteroidMediumData`
- Спрайт `SpriteRenderer` — первый из вариантов Medium

---

### asteroid\_small (`Assets/Media/prefabs/asteroid_small.prefab`)

Является вариантом префаба `asteroid_big.prefab` с переопределениями:
- **Имя:** `asteroid_small`
- `CircleCollider2D.Radius`: **0.6** (переопределено)
- `_spriteVariants`: 3 спрайта из `AsteroidSmallData`

---

### ufo (`Assets/Media/prefabs/ufo.prefab`)

- **Layer:** 11 (Enemy)
- **Tag:** Untagged

Компоненты:
- `Transform`: позиция (0,0,0), без дочерних объектов
- `SpriteRenderer`: спрайт НЛО (guid `39238117801b40c43856f62b7fdf50fe`)
- `Rigidbody2D`: Kinematic, GravityScale = **0**
- `PolygonCollider2D`: форма НЛО (8 вершин, oldSize 3×2.5, newSize 0.47×0.32)
- `MonoBehaviour` (guid: `3952070e6e7445d480f3869cc50a5028`): контроллер НЛО (без сериализованных полей в инспекторе)

---

### ufo\_big (`Assets/Media/prefabs/ufo_big.prefab`)

- **Layer:** 11 (Enemy)
- Структура: родительский GameObject `ufo_big` + дочерний `image`

Компоненты корневого GameObject (`ufo_big`):
- `Rigidbody2D`: Kinematic, GravityScale = **0**
- `PolygonCollider2D`: форма большого НЛО (8 вершин, oldSize 3×2.5, newSize 3×2.5 — без масштабирования)
- `MonoBehaviour` (guid: `3952070e6e7445d480f3869cc50a5028`): тот же контроллер, что у `ufo`

Дочерний объект `image`:
- `Transform`: LocalScale = **(1.5, 1.5, 1.5)** — визуально в 1.5× больше
- `SpriteRenderer`: тот же спрайт НЛО, SortingLayer Default

---

### gui\_text (`Assets/Media/prefabs/gui/gui_text.prefab`)

Шаблон текстовой метки для HUD.

Компоненты:
- `RectTransform`: якорь центр (0.5, 0.5), SizeDelta = 200×50
- `CanvasRenderer`
- `TextMeshPro` (TMPro): текст по умолчанию `"Param: XX.value"`, шрифт размер 28, цвет белый, без переноса строк, выравнивание по левому краю

---

### leaderboard\_entry (`Assets/Media/prefabs/gui/leaderboard_entry.prefab`)

Шаблон строки таблицы лидеров.

Структура: корневой `leaderboard_entry` (HorizontalLayoutGroup) → дочерние:
- `rank_text` — TextMeshPro ранг, SizeDelta 50×30, preferredWidth 50, позиция (25, −15)
- *(прочие поля имени и очков — по аналогии, структура HorizontalLayoutGroup)*

Компоненты строки:
- `RectTransform` с горизонтальным раскладчиком
- `LayoutElement` для каждого текстового поля
- `TextMeshPro` для каждой ячейки

---

*Сцена и префабы проанализированы: 2026-03-27*
