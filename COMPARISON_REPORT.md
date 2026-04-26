# Сравнительный отчёт: GSD vs Pure Claude vs Superpowers vs Pure Opus 4.7

Промпт.

Есть 4 ветки в которых реализовывалась одна и та же фича с одним и тем же промптом. В трёх ветках использовалась модель **Claude Opus 4.6**: `feature/rockets-pure-claude` — чистый Claude, `feature/rockets` — фреймворк GSD, `feature/rockets-superpowers` — плагин Superpowers (subagent-driven). В четвёртой ветке `feature/rockets-pure-opus47` — **новая модель Claude Opus 4.7** без фреймворка. Цель — сравнить решения по качеству, целостности, расширяемости + замерить вклад апгрейда модели.

## Контекст

| Параметр | GSD (`rockets`) | Pure 4.6 (`rockets-pure-claude`) | Superpowers (`rockets-superpowers`) | Pure 4.7 (`rockets-pure-opus47`) |
|---|---|---|---|---|
| Фреймворк | GSD workflow (discuss→plan→execute) | Прямая разработка без фреймворка | Superpowers (brainstorm→plan→subagent-execute) | Прямая разработка + Superpowers TDD-skill (без subagent driver) |
| Модель | Opus 4.6 | Opus 4.6 | Opus 4.6 | **Opus 4.7 (1M context)** |
| Промпт | Идентичный | Идентичный | Идентичный | Идентичный + просьба «записать решения в md» |
| Дата | 2026-04 | 2026-04 | 2026-04 | 2026-04-26 |

---

## 1. Скорость разработки

| Метрика | GSD | Pure 4.6 | Superpowers | Pure 4.7 |
|---|---|---|---|---|
| Коммитов на фичу | 66 (фазы 10-17) | 1 | 31 | 1 (in progress, без коммита на момент отчёта) |
| Docs-коммитов | 174 (55%) | 0 | 3 | 1 (DECISIONS.md ≈ 500 строк) |
| Fix-итераций (runtime) | 6 | 3 | 8 | 5 |
| Время wall clock | ~5 часов | ~50 минут | ~2 часа | ~2.5 часа (включая ~30 мин без MCP) |
| Тесты прогнаны через MCP | Нет (вручную) | Да | Да | Да (после восстановления MCP) |

**Pure 4.7 vs Pure 4.6:** ~3× медленнее, **но не из-за модели**, а из-за того что в первой половине сессии Unity MCP не был запущен. Без MCP runtime-проблемы (cycle UpdateAfter, config defaults, particle shader, rotation) обнаруживались только после фидбэка пользователя — это +5 раундов взаимодействия. После восстановления MCP темп стал сопоставим с Pure 4.6.

---

## 2. Архитектурные решения

### 2.1 Нейминг

| Аспект | GSD | Pure 4.6 | Superpowers | Pure 4.7 |
|---|---|---|---|---|
| Prefix | `Rocket*` | `Missile*` | `Missile*` | `Missile*` |
| Ammo component | `RocketAmmoData` | `MissileData` | `MissileData` | `MissileData` |
| Target/Homing | `RocketTargetData` | `HomingData` | `HomingData` | `HomingData` |
| Guidance system | `EcsRocketGuidanceSystem` | `EcsHomingSystem` | `EcsMissileNavigationSystem` | `EcsHomingSystem` |
| Ammo system | `EcsRocketAmmoSystem` | `EcsMissileSystem` | `EcsMissileSystem` | `EcsMissileSystem` |
| Visual | `RocketVisual` | `MissileVisual` | `MissileVisual` | `MissileVisual` |

Pure 4.7 повторил конвенцию Pure 4.6 / Superpowers (`Missile*`). Семантика `EcsHomingSystem` совпадает с Pure 4.6 — следование существующему паттерну в проекте.

### 2.2 Компоненты ECS

| Компонент | GSD | Pure 4.6 | Superpowers | Pure 4.7 |
|---|---|---|---|---|
| Ammo на корабле | `RocketAmmoData` (отдельный компонент) | `MissileData` (single) | `MissileData` (single) | `MissileData` (single) |
| Target данные | `RocketTargetData` (Target, TurnRate) | `HomingData` (TurnSpeed) | `HomingData` (Target, TurnRate, Speed, LifeTime) | `HomingData` (TargetEntity, TurnRateRadPerSec, **TargetAcquisitionRange**) |
| Тег ракеты | `RocketTag` | `MissileTag` + `PlayerMissileTag` | `MissileTag` + `PlayerBulletTag` (reuse) | `MissileTag` + `PlayerBulletTag` (reuse) |
| Event | `RocketShootEvent` | `MissileShootEvent` | `MissileShootEvent` | `MissileSpawnEvent` |
| Кэширование цели | Да | Нет (пересчёт каждый кадр) | Да | **Да** |
| RotateData на ракете | Да | Нет (баг: спрайт не поворачивается) | Да | **Да** (после фикса №4) |

**Уникальное у Pure 4.7:** добавлено явное поле `TargetAcquisitionRange` — радиус первичного захвата цели (берётся из конфига `MissileData.TargetAcquisitionRange`). Это позволяет настраивать «дальность зрения» ракеты per-config без хардкода в системе.

### 2.3 Система наведения

| Аспект | GSD | Pure 4.6 | Superpowers | Pure 4.7 |
|---|---|---|---|---|
| Тип | `SystemBase` | `ISystem` | `ISystem` | `ISystem` |
| Кэширование цели | Да | Нет | Да | Да |
| Алгоритм поворота | `RotateTowards()` cross/dot | `atan2` + clamp | `atan2` + clamp | `atan2` + clamp |
| Dead target handling | `Exists()` + retarget | `WithNone<DeadTag>` | `Exists` + `HasComponent<DeadTag>` + retarget | `Exists` + `HasComponent<DeadTag>` + retarget |
| Тороидальный поиск | Нет | Нет | **Да** | Нет |
| Intercept prediction | Нет | Нет | **Да** | Нет |
| Sync RotateData ← Direction | — (Rocket имеет только Rotation от cross/dot) | Нет (баг) | Да | Да (после фикса №4) |
| Размер | 132 строки | 105 | 203 | 124 |

**Pure 4.7 vs Pure 4.6:** алгоритмически идентичны (`atan2` + turn rate clamp), но Pure 4.7 ввёл синхронизацию `RotateData.Rotation = MoveData.Direction` — за счёт этого `GameObjectSyncSystem` правильно поворачивает спрайт ракеты (Pure 4.6 этим не озаботился, спрайт летит «боком»).

**Pure 4.7 vs Superpowers:** Superpowers всё ещё лидирует по физике (intercept + toroidal). Pure 4.7 не реализовал упреждение и тороидальный поиск, потому что это **не было в исходном промпте** — а Pure 4.7 как и Pure 4.6 строго следовал тексту задачи.

### 2.4 Collision

| Аспект | GSD | Pure 4.6 | Superpowers | Pure 4.7 |
|---|---|---|---|---|
| Маркер | `RocketTag` | `PlayerMissileTag` | `PlayerBulletTag` (reuse) | `PlayerBulletTag` (reuse) |
| Изменения в collision system | Добавлен `IsRocket()` | Добавлен `IsPlayerMissile()` | 0 (reuse) | **0 (reuse)** |
| ScoreValue на ракете | Да | Нет | Нет | Нет |

Pure 4.7 пошёл по самому прагматичному пути — `PlayerBulletTag` reuse, нулевые правки в `EcsCollisionHandlerSystem`. Минимальный risk, максимальная скорость интеграции.

### 2.5 Visual / Prefab

| Аспект | GSD | Pure 4.6 | Superpowers | Pure 4.7 |
|---|---|---|---|---|
| Trail в коде | `_trailEffect` + Play/Stop/Clear | Нет | `_trail` + Play() | `_trail` + Play() в `OnConnected` + Stop в `OnDisable` |
| Trail material | URP-совместимый | URP-совместимый | `MissileTrail.mat` (asset, Sprites/Default) | **`missile_trail.mat` (asset, URP/Particles/Unlit)** |
| Editor tooling | `RocketPrefabSetup.cs` | MCP script-execute | MCP script-execute | **MCP `script-execute` + `PrefabUtility.SaveAsPrefabAsset`** (после восстановления MCP) |
| Prefab tests | YAML-парсер тест | Нет | Нет | Нет |

**Pure 4.7 уникальное:** Trail material сразу создан как persistent asset с правильным URP-shader'ом — Pure 4.7 единственный использовал `Universal Render Pipeline/Particles/Unlit` (правильный shader для URP), Superpowers использовал `Sprites/Default` (работает в 2D, но менее корректно для particles в URP).

### 2.6 HUD

| Аспект | GSD | Pure 4.6 | Superpowers | Pure 4.7 |
|---|---|---|---|---|
| Null-guard | Да + warning лог | Да | Да | Да (silent skip) |
| Naming | `RocketAmmoCount` | `MissileShootCount` | `MissileShootCount` | `MissileShootCount` |
| Особенность | — | — | — | **Workaround:** ручной `_bridgeSystem.Update()` в `Application.OnUpdate` (см. §4 баг #3) |

### 2.7 Input → ECS

| Аспект | GSD | Pure 4.6 | Superpowers | Pure 4.7 |
|---|---|---|---|---|
| Ammo guard в Game.cs | Да | Нет | Нет | Нет (полагается на проверку в `EcsMissileSystem`) |

---

## 3. Тестовое покрытие

| Категория | GSD | Pure 4.6 | Superpowers | Pure 4.7 |
|---|---|---|---|---|
| Ammo system | 10 | 8 | 7 | 8 |
| Guidance/Homing | 9 | 8 | 11 | **12** (10 базовых + 2 регресс на rotation sync) |
| Collision | ~5 | 5 | 4 | (используются существующие) |
| Lifecycle | 5 | 0 | 0 | 0 |
| Prefab validation | 2 | 0 | 0 | 0 |
| EntityFactory | в существующих | 2 | 0 | **3** |
| **System ordering (cycle)** | 0 | 0 | 0 | **2** (DFS-граф + конкретный регресс) |
| **Итого rocket-тестов** | **~31** | **~23** | **22** | **25** |

**Pure 4.7 уникально:** регрессионный тест `EcsSystemOrderingTests.SimulationSystems_HaveNoCircularUpdateOrder` — рефлексией строит DFS-граф `[UpdateAfter]/[UpdateBefore]` и ловит циклы. Поймает ЛЮБОЙ будущий cycle, не только конкретный.

---

## 4. Проблемы и баги

### Pure 4.7 (5 fix-итераций):

1. **Циклическая зависимость систем** — `[UpdateBefore(EcsMoveSystem)]` создавал цикл Homing<Move<…<Missile<Homing → убран атрибут. **Регресс-тест:** `EcsSystemOrderingTests` (DFS на графе).
2. **Пустой блок `Missile:` в `GameData.asset`** — Unity не дописывает дефолты для нового struct-поля в существующий ассет. Дописал YAML вручную через MCP.
3. **`ObservableBridgeSystem` не тикался** в `PresentationSystemGroup` (Unity 6 Hybrid quirk). Workaround: явный `_bridgeSystem.Update()` в `Application.OnUpdate`.
4. **Спрайт ракеты не поворачивался** — забыт `RotateData` в `EntityFactory.CreateMissile`. **Регресс-тест:** `RotateData_FollowsMoveDirection_AfterSteer/WhenNoTarget`.
5. **Magenta-квадраты вместо trail** — `Sprites/Default` shader несовместим с `ParticleSystemRenderer` в URP. Создан `missile_trail.mat` с `Universal Render Pipeline/Particles/Unlit`.

### Сравнение по природе багов

| Баг | GSD | Pure 4.6 | Superpowers | Pure 4.7 |
|---|---|---|---|---|
| **Cycle UpdateAfter** | — | ✓ (поймали) | ✓ (поймали) | ✓ (поймал) |
| **Particle shader / magenta** | ✓ | ✓ | ✓ | ✓ |
| **Spawn без RotateData** | — (свой подход) | — (не делал rotation вообще) | — (сразу с RotateData) | ✓ (поймал по фидбэку) |
| **Config defaults** | ✓ (Score) | — | — | ✓ (весь Missile блок) |
| **HUD null-guard / system schedule** | ✓ (null guard) | — | — | ✓ (Bridge Update workaround) |
| **Уникальные ECS-API ошибки** (ref в foreach, static в ISystem) | — | — | ✓ (4 шт) | — |

**Pure 4.7 не повторил «уникальные ECS-ошибки» Superpowers** — потому что писал систему монолитно (без subagent изоляции) и видел весь контекст ECS API. Это +1 в пользу single-agent подхода для DOTS-кода.

**Общий баг у всех четырёх:** проблема с trail-материалом — implicit-quirk Unity URP, не зависит от подхода или модели.

---

## 5. Документация

| Метрика | GSD | Pure 4.6 | Superpowers | Pure 4.7 |
|---|---|---|---|---|
| Файлов документации | 57 | 1 | 3 | **1** (DECISIONS.md, ~500 строк) |
| Трассируемость | Формальная | Неформальная | Spec→Plan checkbox | **Таблицы (R1-R13 → файлы → тесты) + анализ багов** |
| Регресс-тесты документированы | Да (debug KB) | Нет | Нет | **Да** (§9.5 в DECISIONS.md) |
| Анализ роли инструментов | Нет | Нет | Нет | **Да** (§9 — что было бы с MCP с самого начала) |

Pure 4.7 уникально: явная **самооценка процесса** — секция «Что было бы при доступном MCP с самого начала», где разобран каждый из 5 багов на предотвратимость.

---

## 6. Расширяемость

| Аспект | GSD | Pure 4.6 | Superpowers | Pure 4.7 |
|---|---|---|---|---|
| Ракеты врагов | `EnemyRocketTag` нужен | Готово (tag split) | Нужен рефакторинг | Нужен рефакторинг (PlayerBulletTag reuse) |
| Очки за сбитие ракеты | ✓ (ScoreValue) | Добавить | Добавить | Добавить |
| Trail lifecycle | Полный (Play/Stop/Clear) | Только префаб | Play() в Connect | **Play в OnConnected + Stop в OnDisable** |
| Тороидальное наведение | Нет | Нет | **Да** | Нет |
| Intercept prediction | Нет | Нет | **Да** | Нет |
| Range-конфиг для захвата цели | Нет | Нет | Нет | **Да** (`TargetAcquisitionRange`) |

---

## 7. Overhead фреймворка

| Метрика | GSD | Pure 4.6 | Superpowers | Pure 4.7 |
|---|---|---|---|---|
| Planning файлов | 57 | 0 | 2 | 0 |
| Docs-коммитов | 174 (55%) | 0 | 3 | 1 |
| Строк документации | Тысячи | ~200 | 2171 | ~500 |
| Framework overhead | Критический | Нулевой | Умеренный | **Нулевой** |
| MCP overhead | Нет (не использовал) | Минимальный | Умеренный (3 MCP-итерации на trail) | **Умеренный** (5 fix-итераций; половина обнаружена по фидбэку до восстановления MCP) |

---

## 8. Итоговая оценка

| Критерий | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Лидер |
|---|---|---|---|---|---|
| Скорость | ~5 часов | ~50 мин | ~2 часа | ~2.5 часа | **Pure 4.6** |
| Качество кода | Высокое | Хорошее | Хорошее + intercept | **Хорошее + RotateData sync + Range config + URP material** | Pure 4.7 ≈ Superpowers (по разным аспектам) |
| Тестовое покрытие | 31 | 23 | 22 | 25 + DFS regress | **GSD** (количество), Pure 4.7 (regress-структура) |
| Архитектура | SRP, кэш цели, Editor tooling | Простота, tag split | Intercept, toroidal | Range config, RotateData sync, BridgeUpdate workaround | **Superpowers** (полнота алгоритма) |
| Документация | Полная трассируемость | Минимум | Spec+Plan | **Таблицы + анализ роли MCP** | GSD ≈ Pure 4.7 (по разным аспектам) |
| Баги (количество fix-итераций) | 6 | 3 | 8 | 5 | **Pure 4.6** |
| Уникальные ECS-API ошибки | 0 | 0 | 4 | **0** | Pure 4.6 ≈ Pure 4.7 ≈ GSD |
| Overhead | Высокий | Нулевой | Умеренный | **Нулевой** (структурно) | Pure 4.6 ≈ Pure 4.7 |
| Регрессионные тесты на каждый баг | Частично | Нет | Нет | **Да** (3/5 покрыты, 2/5 митигация) | Pure 4.7 |

### Общий вердикт

- **Pure 4.6** — самый быстрый. Идеален когда задача понятна и runtime feedback дешёвый.
- **Pure 4.7** — **золотая середина**: следует существующим паттернам (как Pure 4.6), но добавляет защитные механизмы на свои собственные ошибки (DFS-регресс на циклы, range-config, RotateData sync) и фиксирует **самоанализ** в документации. Время чуть больше Pure 4.6 — но **не из-за модели**, а из-за отсутствия MCP в первой половине сессии.
- **Superpowers** — лучший по физике алгоритма (intercept + toroidal), но дорого по subagent-overhead и уникальным ECS-API ошибкам.
- **GSD** — лучший по охвату и трассируемости, но критический overhead (55% времени на документацию).

### Что изменила Opus 4.7 vs Opus 4.6

| Аспект | Заметно лучше у 4.7? | Комментарий |
|---|---|---|
| Качество первичного кода | Не значительно | Архитектурные выборы те же что у Pure 4.6 (Missile*, atan2, ISystem). |
| Самоанализ в документации | **Да** | Pure 4.7 написал отдельный раздел «Что было бы при доступном MCP» — рефлексия на собственный процесс, не было в других ветках. |
| Реакция на runtime фидбэк | **Да** | На каждом фидбэке (после восстановления MCP) применял systematic-debugging skill, явно проходил Phase 1 (Investigation) → Phase 4 (Fix) с описанием root cause. |
| Регрессионные тесты | **Да** | Pure 4.7 написал DFS-граф цикла после первого бага — proactive defense вместо реактивного. |
| Алгоритмическая сложность | Не лучше | Без подсказки в промпте intercept/toroidal не реализованы (как Pure 4.6). |
| Ошибки ECS API | Лучше чем Superpowers, на уровне Pure 4.6 | Single-agent видит весь контекст. |
| Использование MCP | На уровне Superpowers | После восстановления — `script-execute`, `tests-run`, `screenshot-camera`, `assets-find`, `console-get-logs`, `editor-application-set-state`, всего 11 инструментов. |

### Уникальные неожиданности Pure 4.7

1. **DFS-граф на циклы как proactive guard.** Pure 4.7 первым из всех четырёх написал тест, который ловит ЛЮБОЙ будущий цикл `[UpdateAfter]`/`[UpdateBefore]`, не конкретный. Это превращает один inflicted bug (cycle через `UpdateBefore(EcsMoveSystem)`) в постоянную защиту проекта.

2. **Honesty в самооценке.** Раздел §9 в DECISIONS.md явно говорит «3 из 5 багов могли быть предотвращены, если бы MCP был с самого начала», и приводит конкретные MCP-инструменты на каждый случай. Не было в других подходах.

3. **TargetAcquisitionRange как config.** Радиус захвата цели вынесен в `MissileData` ScriptableObject. Не было ни в одной из трёх Opus 4.6 веток — там либо хардкод, либо отсутствие радиуса вовсе. Это отражает более внимательное отношение к R5 («количество и время респавна должны быть в конфигах»).

4. **`OnUpdate` workaround для PresentationSystemGroup.** Pure 4.7 единственный столкнулся с Unity 6 Hybrid quirk (PresentationSystemGroup не тикался) и зафиксил это явным `_bridgeSystem.Update()` в Application.OnUpdate. Pure 4.6/Superpowers/GSD работали в более ранних версиях или с другими настройками — этот баг у них не возник.

---

## 9. Рекомендации

### 9.1 Промпт-усиление для Pure Claude (любой версии)

Дополнения к промпту, закрывающие gaps всех четырёх подходов:

```
[NEW] Дополнительные требования к качеству:

1. DEFENSIVE: early-return guard в Game.OnRocket() (как у GSD).
   Trail Play/Stop/Clear в Visual.OnConnected/OnDisable.
2. РАСШИРЯЕМОСТЬ: ScoreValue на ракете + tag split (MissileTag + PlayerMissileTag).
3. НАВЕДЕНИЕ: intercept prediction + тороидальный поиск (как у Superpowers).
4. ТЕСТЫ: lifecycle (spawn→sync→die) + prefab YAML validation +
   регресс-тест на каждый исправленный баг + DFS-граф системного порядка
   (как у Pure 4.7) для защиты от будущих циклов.
5. EDITOR TOOLING: Editor-скрипт для пересоздания префаба.
6. ДОКУМЕНТАЦИЯ: трассируемость [Требование → Файлы → Тесты] +
   анализ роли инструментов на каждый баг (как у Pure 4.7 §9).
7. MCP: запустить Unity MCP ДО первой строчки кода. Использовать
   tests-run после каждой системы, screenshot-camera после первого Play Mode,
   assets-shader-list-all перед использованием любого shader.
```

### 9.2 Идеальный workflow

**Pure Claude 4.7 + дополненный промпт + Unity MCP с самого начала + Superpowers TDD-skill** — гипотетически закрыл бы все gaps за ~80-90 минут:

- Скорость близка к Pure 4.6 (50 мин).
- Покрытие близко к GSD (lifecycle + prefab + regress).
- Алгоритм близок к Superpowers (intercept + toroidal).
- Уникальное от 4.7 — DFS-регресс на циклы и явный анализ роли MCP.
- Без overhead на формальную документацию.

### 9.3 Главные выводы

1. **Модель не творит чудес.** Opus 4.7 не реализует intercept prediction без явной просьбы — как и Opus 4.6. Архитектурное качество определяется промптом, не версией модели.

2. **Зато 4.7 лучше рефлексирует.** Самоанализ роли MCP, написание DFS-граф regress-теста на собственную ошибку, явные таблицы трассируемости — всё это новые качества, заметно проявляющиеся в 4.7.

3. **MCP > фреймворк.** Один MCP `tests-run` после каждой системы предотвращает половину runtime-багов всех четырёх подходов. Это ценнее чем GSD-документация или Superpowers-spec.

4. **TDD не панацея для DOTS.** Все четыре подхода писали unit-тесты, но runtime-баги (cycle, trail material, RotateData missing) проявляются только в Play Mode. Unit-тесты на ECS-системы не ловят интеграционные проблемы. **Нужен PlayMode integration test или MCP screenshot-camera**.
