# Сравнительный отчёт: GSD vs Pure Claude vs Superpowers vs Pure Opus 4.7 vs Pure Opus 4.7+Plan

Промпт.

Есть 5 веток, в которых реализовывалась одна и та же фича (homing rockets) с одним и тем же промптом. В трёх ветках использовалась модель **Claude Opus 4.6**: `feature/rockets-pure-claude` — чистый Claude, `feature/rockets` — фреймворк GSD, `feature/rockets-superpowers` — плагин Superpowers (subagent-driven). В двух ветках использовалась модель **Claude Opus 4.7 (1M context)** без фреймворка: `feature/rockets-pure-opus47` (xhigh reasoning) и `feature/rockets-pure-opus47-high-plan` (xhigh reasoning + явный план-перед-кодом). Цель — сравнить решения по качеству, целостности, расширяемости + замерить вклад апгрейда модели и плана-перед-кодом.

## Контекст

| Параметр | GSD (`rockets`) | Pure 4.6 (`rockets-pure-claude`) | Superpowers (`rockets-superpowers`) | Pure 4.7 (`rockets-pure-opus47`) | Pure 4.7+Plan (`rockets-pure-opus47-high-plan`) |
|---|---|---|---|---|---|
| Фреймворк | GSD workflow (discuss→plan→execute) | Прямая разработка без фреймворка | Superpowers (brainstorm→plan→subagent-execute) | Прямая разработка + Superpowers TDD-skill (без subagent driver) | Прямая разработка + явный план-перед-кодом + Superpowers TDD-skill |
| Модель | Opus 4.6 | Opus 4.6 | Opus 4.6 | **Opus 4.7 (1M context)** | **Opus 4.7 (1M context)** |
| Промпт | Идентичный | Идентичный | Идентичный | Идентичный + просьба «записать решения в md» | Идентичный + явный пункт «1. Составь план» + «4. запиши в DECISIONS.md» |
| Дата | 2026-04 | 2026-04 | 2026-04 | 2026-04-26 | 2026-04-27 |

---

## 1. Скорость разработки

| Метрика | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan |
|---|---|---|---|---|---|
| Коммитов на фичу | 66 (фазы 10-17) | 1 | 31 | 1 (in progress, без коммита на момент отчёта) | **3** (1 feat + 2 fix) |
| Docs-коммитов | 174 (55%) | 0 | 3 | 1 (DECISIONS.md ≈ 500 строк) | **0** (DECISIONS.md в feat-коммите ≈ 175 строк) |
| Fix-итераций (runtime) | 6 | 3 | 8 | 5 | **2** (rotation+trail, magenta material) |
| Время wall clock | ~5 часов | ~50 минут | ~2 часа | ~2.5 часа (включая ~30 мин без MCP) | **~2 часа** (MCP с самого начала) |
| Тесты прогнаны через MCP | Нет (вручную) | Да | Да | Да (после восстановления MCP) | **Да** (с самого начала) |

**Pure 4.7+Plan vs Pure 4.7:** на 30 мин быстрее и в 2.5× меньше fix-итераций. Не из-за модели — модель та же, — а из-за двух эффектов: (а) Unity MCP был запущен с самого начала, (б) явный план-перед-кодом выявил три потенциальных проблемы до того, как они стали runtime-багами (правильное расположение `[UpdateAfter]`/`[UpdateBefore]`, заполнение GameData через SerializedObject, использование существующего Bridge-pipeline без workaround).

**Pure 4.7+Plan vs Pure 4.6:** ~2.4× медленнее — но в обмен на полноценную DECISIONS.md, RotateData sync, регрессионный тест на rotation, а также 2 фикса на runtime-баги (Pure 4.6 не наткнулся на них, потому что не имплементировал rotation вообще).

---

## 2. Архитектурные решения

### 2.1 Нейминг

| Аспект | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan |
|---|---|---|---|---|---|
| Prefix | `Rocket*` | `Missile*` | `Missile*` | `Missile*` | **`Rocket*`** |
| Ammo component | `RocketAmmoData` | `MissileData` | `MissileData` | `MissileData` | **`RocketData`** (агрегирован: arsenal + Direction + ShootPosition) |
| Target/Homing | `RocketTargetData` | `HomingData` | `HomingData` | `HomingData` | **`RocketHomingData`** (Target, TurnRateRadPerSec) |
| Guidance system | `EcsRocketGuidanceSystem` | `EcsHomingSystem` | `EcsMissileNavigationSystem` | `EcsHomingSystem` | **`EcsRocketHomingSystem`** |
| Ammo system | `EcsRocketAmmoSystem` | `EcsMissileSystem` | `EcsMissileSystem` | `EcsMissileSystem` | **`EcsRocketSystem`** |
| Visual | `RocketVisual` | `MissileVisual` | `MissileVisual` | `MissileVisual` | **`RocketVisual`** |

**Pure 4.7+Plan уникальное:** единственная Opus-4.7 ветка, использующая семантику `Rocket*` (вторая после GSD). Промпт говорит «ракета», и `Rocket*` — точный буквальный перевод. Pure 4.7 (без плана) выбрал `Missile*` следуя оружейной классике — план-перед-кодом возвращает к буквальному прочтению промпта.

### 2.2 Компоненты ECS

| Компонент | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan |
|---|---|---|---|---|---|
| Ammo на корабле | `RocketAmmoData` (отдельный компонент) | `MissileData` (single) | `MissileData` (single) | `MissileData` (single) | **`RocketData` (single, агрегированный)** |
| Target данные | `RocketTargetData` (Target, TurnRate) | `HomingData` (TurnSpeed) | `HomingData` (Target, TurnRate, Speed, LifeTime) | `HomingData` (TargetEntity, TurnRateRadPerSec, **TargetAcquisitionRange**) | `RocketHomingData` (Target, TurnRateRadPerSec) — **без диапазона** |
| Тег ракеты | `RocketTag` | `MissileTag` + `PlayerMissileTag` | `MissileTag` + `PlayerBulletTag` (reuse) | `MissileTag` + `PlayerBulletTag` (reuse) | **`RocketTag`** (одиночный, без reuse) |
| Event | `RocketShootEvent` | `MissileShootEvent` | `MissileShootEvent` | `MissileSpawnEvent` | **`RocketShootEvent`** |
| Кэширование цели | Да | Нет (пересчёт каждый кадр) | Да | Да | **Да** |
| RotateData на ракете | Да | Нет (баг: спрайт не поворачивается) | Да | Да (после фикса №4) | **Да** (после фикса №1) |

**Pure 4.7+Plan уникальное:** `RocketData` агрегирует и арсенал (`MaxShoots`, `CurrentShoots`, `ReloadDurationSec`, `ReloadRemaining`), и параметры выстрела (`Shooting`, `Direction`, `ShootPosition`). Это позволяет `Game.OnRocket` атомарно записать «всё что нужно» одним `SetComponentData`, а `EcsRocketSystem` прочитать в следующем тике. У Pure 4.7 же `MissileData` отдельно, а `Direction`/`Position` шли через event-buffer.

**Pure 4.7+Plan слабее:** нет `TargetAcquisitionRange` — радиус «зрения» не вынесен в конфиг. Фактически означает: ракета захватывает ближайшую цель из всей сцены без ограничения дальности.

### 2.3 Система наведения

| Аспект | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan |
|---|---|---|---|---|---|
| Тип | `SystemBase` | `ISystem` | `ISystem` | `ISystem` | **`ISystem`** |
| Кэширование цели | Да | Нет | Да | Да | **Да** |
| Алгоритм поворота | `RotateTowards()` cross/dot | `atan2` + clamp | `atan2` + clamp | `atan2` + clamp | **2D rotation matrix (cos/sin) + cross sign** |
| Dead target handling | `Exists()` + retarget | `WithNone<DeadTag>` | `Exists` + `HasComponent<DeadTag>` + retarget | `Exists` + `HasComponent<DeadTag>` + retarget | **`Exists` + `HasComponent<DeadTag>` + retarget в том же кадре** |
| Тороидальный поиск | Нет | Нет | **Да** | Нет | Нет |
| Intercept prediction | Нет | Нет | **Да** | Нет | Нет |
| Sync RotateData ← Direction | — (Rocket имеет только Rotation от cross/dot) | Нет (баг) | Да | Да (после фикса №4) | **Да** (после фикса №1) |
| Размер | 132 строки | 105 | 203 | 124 | **152** |

**Pure 4.7+Plan уникальное (алгоритмически):** единственная ветка, использующая **прямую матрицу вращения 2D** вместо `atan2` + clamp. Текущее направление поворачивается на `min(angle, TurnRate*dt)` радиан, знак — из 2D-кросспроизведения:

```csharp
var cross = currentDir.x * desiredDir.y - currentDir.y * desiredDir.x;
var sign = cross >= 0f ? 1f : -1f;
var step = maxStep * sign;
var newDir = new float2(
    currentDir.x * cos(step) - currentDir.y * sin(step),
    currentDir.x * sin(step) + currentDir.y * cos(step));
```

Преимущество: один `cos`+один `sin` вместо `atan2`+`atan2`+`clamp`+`cos`+`sin`. Нет деградации точности от двойного atan2-преобразования. Технически корректнее для частоты 60 fps.

**Pure 4.7+Plan слабее Pure 4.7 / Superpowers:** нет `TargetAcquisitionRange`, нет тороидального поиска, нет intercept prediction. Pure 4.7+Plan строго следовал тексту промпта без выхода за его рамки (как и Pure 4.6).

### 2.4 Collision

| Аспект | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan |
|---|---|---|---|---|---|
| Маркер | `RocketTag` | `PlayerMissileTag` | `PlayerBulletTag` (reuse) | `PlayerBulletTag` (reuse) | **`RocketTag`** (отдельный) |
| Изменения в collision system | Добавлен `IsRocket()` | Добавлен `IsPlayerMissile()` | 0 (reuse) | 0 (reuse) | **Добавлен `IsRocket()` + Rocket+Enemy ветка** |
| ScoreValue на ракете | Да | Нет | Нет | Нет | **Нет** (берётся ScoreValue врага) |
| Rocket+Ship | — | — | — | — | **Безопасно игнорируется** (явная ветка) |

**Pure 4.7+Plan vs Pure 4.7:** Pure 4.7 пошёл по reuse-пути (PlayerBulletTag); Pure 4.7+Plan — по явному `RocketTag` пути. Это +1 строка в `EcsCollisionHandlerSystem` (helper `IsRocket`), но даёт ясную семантику: ракета — отдельный класс снарядов, а не «улучшенная пуля». Промпт явно различает «пули» и «ракеты», и план-перед-кодом отразил это в ECS-теге.

### 2.5 Visual / Prefab

| Аспект | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan |
|---|---|---|---|---|---|
| Trail в коде | `_trailEffect` + Play/Stop/Clear | Нет | `_trail` + Play() | `_trail` + Play() в `OnConnected` + Stop в `OnDisable` | **Только в префабе** (ParticleSystem-child, без управления из кода) |
| Trail material | URP-совместимый | URP-совместимый | `MissileTrail.mat` (asset, Sprites/Default) | `missile_trail.mat` (asset, URP/Particles/Unlit) | **`Default-ParticleSystem.mat`** (Unity built-in extra resource) |
| Editor tooling | `RocketPrefabSetup.cs` | MCP script-execute | MCP script-execute | MCP `script-execute` + `PrefabUtility.SaveAsPrefabAsset` (после восстановления MCP) | **MCP `script-execute` + `PrefabUtility.LoadPrefabContents/SaveAsPrefabAsset`** |
| Prefab tests | YAML-парсер тест | Нет | Нет | Нет | Нет |

**Pure 4.7+Plan уникальное:** trail-материал — **встроенный Unity `Default-ParticleSystem.mat`** через `AssetDatabase.GetBuiltinExtraResource<Material>()`. Не нужно создавать новый asset, не нужно выбирать shader, не возникнет проблемы с URP/Built-in несовместимостью. Самое прагматичное решение из пяти подходов.

**Pure 4.7+Plan слабее:** trail управляется только префабом — нет программного `Play()`/`Stop()` цикла. Это работает (ParticleSystem с `playOnAwake=true` запускается сам), но не даёт контроля при пуллинге. Достаточно для MVP-сценария, но менее расширяемо.

### 2.6 HUD

| Аспект | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan |
|---|---|---|---|---|---|
| Null-guard | Да + warning лог | Да | Да | Да (silent skip) | **Да (silent skip)** |
| Naming | `RocketAmmoCount` | `MissileShootCount` | `MissileShootCount` | `MissileShootCount` | **`RocketShootCount`** |
| Особенность | — | — | — | **Workaround:** ручной `_bridgeSystem.Update()` в `Application.OnUpdate` (см. §4 баг #3) | **`SetRocketMaxShoots()` через `ObservableBridgeSystem`** — `IsRocketReloadTimeVisible` пересчитывается из (current<max) |

**Pure 4.7+Plan vs Pure 4.7:** не возникло проблемы с PresentationSystemGroup tick — потому что `ObservableBridgeSystem` уже работал в проекте (для Score/Ship), просто были добавлены новые поля. Workaround не понадобился.

### 2.7 Input → ECS

| Аспект | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan |
|---|---|---|---|---|---|
| Ammo guard в Game.cs | Да | Нет | Нет | Нет (полагается на проверку в `EcsMissileSystem`) | **Нет** (полагается на проверку `Shooting && CurrentShoots>0` в `EcsRocketSystem`) |

---

## 3. Тестовое покрытие

| Категория | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan |
|---|---|---|---|---|---|
| Ammo system | 10 | 8 | 7 | 8 | **7** (`EcsRocketSystemTests`) |
| Guidance/Homing | 9 | 8 | 11 | 12 (10 базовых + 2 регресс на rotation sync) | **10** (8 базовых + 2 регресс на RotateData sync после фикса №1) |
| Collision | ~5 | 5 | 4 | (используются существующие) | **4** (Rocket+Asteroid, Rocket+Ufo, обратный порядок, Rocket+Ship безопасно) |
| Lifecycle | 5 | 0 | 0 | 0 | 0 |
| Prefab validation | 2 | 0 | 0 | 0 | 0 |
| EntityFactory | в существующих | 2 | 0 | 3 | **2** (`HasInitialRocketData`, `CreateRocket_HasCorrectComponents`) |
| Bridge / HUD | в существующих | 0 | 0 | 1 | **2** (`PushesRocketData_ToHudData`, `RocketReloadTimeHidden_WhenAmmoFull`) |
| **System ordering (cycle)** | 0 | 0 | 0 | **2** (DFS-граф + конкретный регресс) | **0** (цикл не возник — DFS-guard не понадобился) |
| **Итого rocket-тестов** | **~31** | **~23** | **22** | **25** | **~25** |
| **Общий test suite** | (фазы 1-9: 188+) | n/a | n/a | n/a | **188/188 passed** |

**Pure 4.7+Plan vs Pure 4.7:** одинаковое количество rocket-тестов (~25), но **без DFS-graph guard'а** — потому что план-перед-кодом правильно расположил `[UpdateAfter]`/`[UpdateBefore]` с самого начала, и цикл не возник. Это иллюстрирует tradeoff: proactive guard полезен только если есть фактическая ошибка для защиты от регрессии.

---

## 4. Проблемы и баги

### Pure 4.7+Plan (2 fix-итерации):

1. **Спрайт ракеты не поворачивался + не было trail** — двойной фикс одним коммитом (`422d792`):
   - `RotateData` забыт в `EntityFactory.CreateRocket` → добавил.
   - `EcsRocketHomingSystem` не обновлял `RotateData.Rotation` → добавил финальный pass: `rotate.ValueRW.Rotation = move.ValueRO.Direction`.
   - **Регресс-тесты:** `Steer_SyncsRotateData_WithUpdatedDirection`, `Steer_SyncsRotateData_EvenWhenNoTarget`.
   - Trail добавлен в префаб через `script-execute` с `PrefabUtility.LoadPrefabContents/SaveAsPrefabAsset`.

2. **Magenta-квадраты вместо trail** (`45bed92`) — `Sprites/Default` shader без главной текстуры рендерится как Unity error material. Заменён на встроенный `Default-ParticleSystem.mat` через `AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat")` с fallback на процедурную круговую текстуру.

### Сравнение по природе багов

| Баг | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan |
|---|---|---|---|---|---|
| **Cycle UpdateAfter** | — | ✓ (поймали) | ✓ (поймали) | ✓ (поймал) | **— (не возник благодаря плану)** |
| **Particle shader / magenta** | ✓ | ✓ | ✓ | ✓ | **✓** |
| **Spawn без RotateData** | — (свой подход) | — (не делал rotation вообще) | — (сразу с RotateData) | ✓ (поймал по фидбэку) | **✓ (поймал по фидбэку)** |
| **Config defaults** | ✓ (Score) | — | — | ✓ (весь Missile блок) | **— (заполнил через SerializedObject script-execute сразу)** |
| **HUD null-guard / system schedule** | ✓ (null guard) | — | — | ✓ (Bridge Update workaround) | **— (использовал существующий Bridge pipeline)** |
| **Уникальные ECS-API ошибки** (ref в foreach, static в ISystem) | — | — | ✓ (4 шт) | — | **—** |

**Pure 4.7+Plan: 2/6 потенциальных багов проявились.** План-перед-кодом предотвратил 3 категории багов, которые проявлялись у Pure 4.7 без плана:
- Cycle UpdateAfter (проверил граф зависимостей до написания кода).
- Config defaults (использовал SerializedObject + FindPropertyRelative с самого начала).
- Bridge schedule (изучил существующий ObservableBridgeSystem и его место в SystemGroup до интеграции).

Оставшиеся 2 бага — runtime-only (RotateData visible только при play, magenta только в URP scene rendering). Их нельзя поймать без play-time проверки или явного знания «магия Unity» для этих случаев.

**Общий баг у всех пяти:** проблема с trail-материалом — implicit-quirk Unity URP, не зависит от подхода или модели.

---

## 5. Документация

| Метрика | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan |
|---|---|---|---|---|---|
| Файлов документации | 57 | 1 | 3 | 1 (DECISIONS.md, ~500 строк) | **1** (DECISIONS.md, ~175 строк) |
| Трассируемость | Формальная | Неформальная | Spec→Plan checkbox | Таблицы (R1-R13 → файлы → тесты) + анализ багов | **Структурированно по разделам (Архитектура → Алгоритм → Конфиги → Ввод → HUD → Visual → TDD → MCP)** |
| Регресс-тесты документированы | Да (debug KB) | Нет | Нет | Да (§9.5 в DECISIONS.md) | **Косвенно** (§7 TDD-покрытие перечисляет тесты) |
| Анализ роли инструментов | Нет | Нет | Нет | Да (§9 — что было бы с MCP с самого начала) | **Да (§8)** — MCP-валидация: какие шаги через какие инструменты |
| Невыполненные опциональные пункты | — | — | — | — | **Да (§Невыполненные опциональные элементы)** — явно перечислены отложенные задачи |

**Pure 4.7+Plan vs Pure 4.7:** ~3× компактнее (175 vs 500 строк), без формальной таблицы трассируемости R1-R13, но с явным разделом «невыполненные опциональные элементы» (которого не было ни у одной другой ветки) — честное признание того, что не сделано: PlayMode-тест, юнит-тест на CreateRocket в EntitiesCatalog. Это «scoped honesty» — описание границ MVP вместо претензии на полноту.

---

## 6. Расширяемость

| Аспект | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan |
|---|---|---|---|---|---|
| Ракеты врагов | `EnemyRocketTag` нужен | Готово (tag split) | Нужен рефакторинг | Нужен рефакторинг (PlayerBulletTag reuse) | **Готово к tag split** (RocketTag отдельный, легко добавить EnemyRocketTag) |
| Очки за сбитие ракеты | ✓ (ScoreValue) | Добавить | Добавить | Добавить | **Добавить** (ScoreValue нет — сейчас берётся ScoreValue врага) |
| Trail lifecycle | Полный (Play/Stop/Clear) | Только префаб | Play() в Connect | **Play в OnConnected + Stop в OnDisable** | **Только префаб** (regress если будут пуллить ракеты) |
| Тороидальное наведение | Нет | Нет | **Да** | Нет | Нет |
| Intercept prediction | Нет | Нет | **Да** | Нет | Нет |
| Range-конфиг для захвата цели | Нет | Нет | Нет | **Да** (`TargetAcquisitionRange`) | Нет |
| Конфиги ракеты | `RocketAmmoConfig` ScriptableObject | Нет | Нет | Да (`MissileData` struct в `GameData`) | **Да** (`RocketData` struct в `GameData`: `Prefab`, `MaxShoots`, `ReloadDurationSec`, `Speed`, `LifeTimeSeconds`, `TurnRateDegPerSec`) |

---

## 7. Overhead фреймворка

| Метрика | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan |
|---|---|---|---|---|---|
| Planning файлов | 57 | 0 | 2 | 0 | **0** (план был в conversation, не в файлах) |
| Docs-коммитов | 174 (55%) | 0 | 3 | 1 | **0** (DECISIONS.md в feat-коммите) |
| Строк документации | Тысячи | ~200 | 2171 | ~500 | **~175** |
| Framework overhead | Критический | Нулевой | Умеренный | Нулевой | **Нулевой** |
| MCP overhead | Нет (не использовал) | Минимальный | Умеренный (3 MCP-итерации на trail) | Умеренный (5 fix-итераций; половина обнаружена по фидбэку до восстановления MCP) | **Минимальный** (2 fix-итерации, MCP с самого начала) |

---

## 8. Итоговая оценка

| Критерий | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan | Лидер |
|---|---|---|---|---|---|---|
| Скорость | ~5 часов | ~50 мин | ~2 часа | ~2.5 часа | **~2 часа** | **Pure 4.6** |
| Качество кода | Высокое | Хорошее | Хорошее + intercept | Хорошее + RotateData sync + Range config + URP material | **Хорошее + 2D rotation matrix + RotateData sync + agreggated RocketData** | Pure 4.7+Plan ≈ Pure 4.7 ≈ Superpowers |
| Тестовое покрытие | 31 | 23 | 22 | 25 + DFS regress | **25 + RotateData regress** | **GSD** (количество) |
| Архитектура | SRP, кэш цели, Editor tooling | Простота, tag split | Intercept, toroidal | Range config, RotateData sync, BridgeUpdate workaround | **Aggregated RocketData, прагматичный tag split, built-in trail material** | **Superpowers** (полнота алгоритма) / **Pure 4.7+Plan** (минимализм) |
| Документация | Полная трассируемость | Минимум | Spec+Plan | Таблицы + анализ роли MCP | **Компактно по разделам + раздел «невыполнено»** | GSD (полнота) ≈ Pure 4.7 (трассируемость) |
| Баги (количество fix-итераций) | 6 | 3 | 8 | 5 | **2** | **Pure 4.7+Plan** |
| Уникальные ECS-API ошибки | 0 | 0 | 4 | 0 | **0** | Pure 4.6 ≈ Pure 4.7 ≈ Pure 4.7+Plan ≈ GSD |
| Overhead | Высокий | Нулевой | Умеренный | Нулевой (структурно) | **Нулевой** | Pure 4.6 ≈ Pure 4.7 ≈ Pure 4.7+Plan |
| Регрессионные тесты на каждый баг | Частично | Нет | Нет | Да (3/5 покрыты, 2/5 митигация) | **Да (2/2 покрыты)** | Pure 4.7+Plan (по покрытию своих багов) |

### Общий вердикт

- **Pure 4.6** — самый быстрый. Идеален когда задача понятна и runtime feedback дешёвый.
- **Pure 4.7** — следует существующим паттернам (как Pure 4.6), но добавляет защитные механизмы на свои собственные ошибки (DFS-регресс на циклы, range-config, RotateData sync) и фиксирует **самоанализ** в документации.
- **Pure 4.7+Plan** — **новая золотая середина**: единственная ветка, где план-перед-кодом измеримо снизил количество багов (2 vs 5 у Pure 4.7), убрал три категории runtime-проблем (cycle, config defaults, bridge schedule) и сократил DECISIONS.md в 3 раза. Не ввёл proactive DFS-guard — потому что цикл не возник, и нечего было защищать. Использовал самые прагматичные решения: built-in trail material, отдельный `RocketTag`, агрегированный `RocketData`.
- **Superpowers** — лучший по физике алгоритма (intercept + toroidal), но дорого по subagent-overhead и уникальным ECS-API ошибкам.
- **GSD** — лучший по охвату и трассируемости, но критический overhead (55% времени на документацию).

### Что изменили план-перед-кодом и Opus 4.7

| Аспект | Заметно лучше у Pure 4.7+Plan? | Комментарий |
|---|---|---|
| Скорость | **Да** vs Pure 4.7 (~2ч vs ~2.5ч), нет vs Pure 4.6 (~50 мин) | План-перед-кодом ускоряет за счёт меньшего количества fix-итераций. |
| Качество первичного кода | Не значительно vs другие 4.7 | Архитектурные выборы прагматичнее (Rocket*, аггрегированный RocketData), но не «лучше» по абсолютной шкале. |
| Самоанализ в документации | Сопоставим с Pure 4.7 | Раздел §8 «MCP-валидация» аналогичен §9 у Pure 4.7. |
| Реакция на runtime фидбэк | Хорошая | На обоих фидбэках (rotation+trail, magenta) применил systematic-debugging skill. |
| Регрессионные тесты | **Да** | 2/2 регрессии покрыты (RotateData sync — двумя тестами). |
| Проактивная защита от собственных ошибок | **Нет** vs Pure 4.7 | DFS-guard не написан — потому что цикл не возник. Это иллюстрирует, что proactive guards триггерятся фактическим багом. |
| Алгоритмическая сложность | Не лучше | Без подсказки в промпте intercept/toroidal не реализованы. |
| Ошибки ECS API | На уровне Pure 4.7 / Pure 4.6 | Single-agent видит весь контекст. |
| Использование MCP | Лучше Pure 4.7 (с самого начала) | `script-execute`, `tests-run`, `screenshot-camera`, `assets-find`, `console-get-logs`, `editor-application-set-state`, `assets-refresh`, `scene-save`, `gameobject-component-add` — больше инструментов, но меньше «рекавери» MCP. |

### Уникальные неожиданности Pure 4.7+Plan

1. **2D rotation matrix вместо atan2.** Единственная ветка, использующая прямую матрицу вращения для homing. Алгоритмически эквивалентно `atan2 + clamp + cos/sin`, но за один cos+sin вместо трёх. Проявление: «свежий взгляд» на стандартный паттерн через явное планирование.

2. **Aggregated `RocketData` вместо event payload.** `Game.OnRocket` пишет всё в один компонент (`Direction`, `ShootPosition`, `Shooting=true`), а `EcsRocketSystem` читает в следующем тике. Это **проще** event-buffer pipeline у других подходов, но **уменьшает гибкость** — нельзя одним кадром стрельнуть несколько раз. Для R5 («одна ракета у игрока») этого достаточно.

3. **`Default-ParticleSystem.mat` как trail material.** Самое прагматичное решение: вместо создания нового URP-shader-material использовали встроенный Unity asset через `AssetDatabase.GetBuiltinExtraResource`. Работает в любом render pipeline, не требует дизайна shader'а.

4. **DECISIONS.md в feat-коммите.** Документация написана сразу с кодом, а не отдельным docs-коммитом. Это уменьшает шум в git log и делает «одно изменение = один коммит» более полным.

5. **Раздел «Невыполненные опциональные элементы» в DECISIONS.md.** Уникальная честность: явно перечислены задачи, которые не сделаны (particle trail оригинально был отложен — потом сделан в фиксе; PlayMode-тест выстрела ракеты — не написан; юнит-тест на CreateRocket в EntitiesCatalog — отложен). Не было ни в одной из четырёх предыдущих веток.

---

## 9. Рекомендации

### 9.1 Промпт-усиление для Pure Claude (любой версии)

Дополнения к промпту, закрывающие gaps всех пяти подходов:

```
[NEW] Дополнительные требования к качеству:

1. ОБЯЗАТЕЛЬНО: явный пункт «1. Составь план» (как у Pure 4.7+Plan).
   Это снижает количество runtime-багов в 2-3 раза.
2. DEFENSIVE: early-return guard в Game.OnRocket() (как у GSD).
   Trail Play/Stop/Clear в Visual.OnConnected/OnDisable (для пуллинга).
3. РАСШИРЯЕМОСТЬ: ScoreValue на ракете + tag split (RocketTag + PlayerRocketTag/EnemyRocketTag).
4. НАВЕДЕНИЕ: intercept prediction + тороидальный поиск (как у Superpowers).
5. ТЕСТЫ: lifecycle (spawn→sync→die) + prefab YAML validation +
   регресс-тест на каждый исправленный баг + DFS-граф системного порядка
   (как у Pure 4.7) для защиты от будущих циклов.
6. EDITOR TOOLING: Editor-скрипт для пересоздания префаба.
7. ДОКУМЕНТАЦИЯ: трассируемость [Требование → Файлы → Тесты] +
   анализ роли инструментов на каждый баг (как у Pure 4.7 §9) +
   раздел «Невыполненные опциональные элементы» (как у Pure 4.7+Plan).
8. MCP: запустить Unity MCP ДО первой строчки кода. Использовать
   tests-run после каждой системы, screenshot-camera после первого Play Mode,
   assets-shader-list-all перед использованием любого shader.
```

### 9.2 Идеальный workflow

**Pure Claude 4.7 + явный план-перед-кодом + Unity MCP с самого начала + Superpowers TDD-skill** — фактически реализован в `Pure 4.7+Plan`. Достижения:

- Скорость ~2 часа (близко к Pure 4.6 50 мин, но с полноценной DECISIONS.md).
- 2 fix-итерации (vs 5 у Pure 4.7, 8 у Superpowers, 6 у GSD).
- 25 rocket-тестов (близко к 31 у GSD, выше Pure 4.6 23 и Superpowers 22).
- 0 уникальных ECS-API ошибок (как Pure 4.6 / Pure 4.7 / GSD).
- Без overhead на формальную документацию.

Чтобы достичь покрытия GSD (31 тест) и алгоритма Superpowers (intercept+toroidal), нужны явные требования в промпте — модель сама не выходит за рамки задачи.

### 9.3 Главные выводы

1. **План-перед-кодом — самый дешёвый buff.** Один пункт «Составь план» в промпте измеримо снизил fix-итерации с 5 до 2 у Pure 4.7. Не требует фреймворка, не требует subagent — только дисциплины reasoning.

2. **Модель не творит чудес.** Opus 4.7+Plan не реализует intercept prediction без явной просьбы — как и Opus 4.7 / Opus 4.6. Архитектурное качество определяется промптом, не версией модели.

3. **Зато 4.7 лучше рефлексирует.** Самоанализ роли MCP, написание DFS-граф regress-теста на собственную ошибку, явные таблицы трассируемости, раздел «невыполнено» — всё это новые качества, заметно проявляющиеся в 4.7.

4. **MCP > фреймворк.** Один MCP `tests-run` после каждой системы предотвращает половину runtime-багов всех пяти подходов. Это ценнее чем GSD-документация или Superpowers-spec.

5. **TDD не панацея для DOTS.** Все пять подходов писали unit-тесты, но runtime-баги (cycle, trail material, RotateData missing) проявляются только в Play Mode. Unit-тесты на ECS-системы не ловят интеграционные проблемы. **Нужен PlayMode integration test или MCP screenshot-camera**.

6. **Proactive guards триггерятся фактическими багами.** Pure 4.7 написал DFS-граф после cycle-бага. Pure 4.7+Plan не написал — потому что цикл не возник. Это нормально: defensive code должен быть оправдан фактической ошибкой, иначе становится over-engineering.

7. **Прагматизм vs полнота — обе стратегии работают.** Pure 4.7+Plan выбрал минимализм (built-in trail material, агрегированный RocketData, нет range-config). Pure 4.7 выбрал defensive engineering (URP-specific material, разделённые компоненты, range-config). Оба достигли работающей фичи. Выбор зависит от долгосрочного контекста: minimal — для MVP/прототипа, defensive — для production-кода с расширением функциональности.
