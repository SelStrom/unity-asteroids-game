# Сравнительный отчёт: GSD vs Pure Claude vs Superpowers vs Pure Opus 4.7 vs Pure Opus 4.7+Plan vs Pure Opus 4.7+Plan xhigh

Промпт.

Есть 6 веток, в которых реализовывалась одна и та же фича (homing rockets) с одним и тем же промптом. В трёх ветках использовалась модель **Claude Opus 4.6**: `feature/rockets-pure-claude` — чистый Claude, `feature/rockets` — фреймворк GSD, `feature/rockets-superpowers` — плагин Superpowers (subagent-driven). В трёх ветках использовалась модель **Claude Opus 4.7 (1M context)** без фреймворка: `feature/rockets-pure-opus47` (xhigh reasoning), `feature/rockets-pure-opus47-high-plan` (high reasoning + явный план-перед-кодом) и `feature/rockets-pure-opus47-xhigh-plan` (**xhigh reasoning + явный план-перед-кодом**, та же конфигурация что у `-high-plan`, но с увеличенным effort и независимой сессией). Цель — сравнить решения по качеству, целостности, расширяемости + замерить вклад апгрейда модели, плана-перед-кодом и подъёма effort до xhigh.

## Контекст

| Параметр | GSD (`rockets`) | Pure 4.6 (`rockets-pure-claude`) | Superpowers (`rockets-superpowers`) | Pure 4.7 (`rockets-pure-opus47`) | Pure 4.7+Plan (`rockets-pure-opus47-high-plan`) | Pure 4.7+P xhigh (`rockets-pure-opus47-xhigh-plan`) |
|---|---|---|---|---|---|---|
| Фреймворк | GSD workflow (discuss→plan→execute) | Прямая разработка без фреймворка | Superpowers (brainstorm→plan→subagent-execute) | Прямая разработка + Superpowers TDD-skill (без subagent driver) | Прямая разработка + явный план-перед-кодом + Superpowers TDD-skill | **Прямая разработка + явный план-перед-кодом + xhigh effort + явный шаг «убедись что MCP работает»** |
| Модель | Opus 4.6 | Opus 4.6 | Opus 4.6 | **Opus 4.7 (1M context)** | **Opus 4.7 (1M context)** | **Opus 4.7 (1M context), effort `xhigh`** |
| Промпт | Идентичный | Идентичный | Идентичный | Идентичный + просьба «записать решения в md» | Идентичный + явный пункт «1. Составь план» + «4. запиши в DECISIONS.md» | **Идентичный + 4 пункта: «1. Составь план», «2. Убедись что MCP работает (если нет — НЕ продолжай)», «3. Выполни», «4. Запиши решения, токены и промт в `DESISIONS.md`»** |
| Дата | 2026-04 | 2026-04 | 2026-04 | 2026-04-26 | 2026-04-27 | **2026-04-27** |

---

## 1. Скорость разработки

| Метрика | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan | Pure 4.7+P xhigh |
|---|---|---|---|---|---|---|
| Коммитов на фичу | 66 (фазы 10-17) | 1 | 31 | 1 (in progress, без коммита на момент отчёта) | **3** (1 feat + 2 fix) | **1** (`401109c` «имплементация ракеты» — один атомарный коммит, фиксы внутри сессии) |
| Docs-коммитов | 174 (55%) | 0 | 3 | 1 (DECISIONS.md ≈ 500 строк) | **0** (DECISIONS.md в feat-коммите ≈ 175 строк) | **0** (DESISIONS.md в feat-коммите ≈ 287 строк, с оценкой токенов и денег) |
| Fix-итераций (runtime) | 6 | 3 | 8 | 5 | **2** (rotation+trail, magenta material) | **1** (только sort-cycle) — починено в рамках одной сессии без отдельного коммита |
| Время wall clock | ~5 часов | ~50 минут | ~2 часа | ~2.5 часа (включая ~30 мин без MCP) | **~2 часа** (MCP с самого начала) | **~3-4 часа** (одна непрерывная сессия с MCP, по оценке токенов в DESISIONS) |
| Тесты прогнаны через MCP | Нет (вручную) | Да | Да | Да (после восстановления MCP) | **Да** (с самого начала) | **Да** (явный шаг «убедись что MCP работает» в начале) |
| Денежная оценка | — | — | — | — | — | **~$10.13** (явно посчитано в DESISIONS.md §8) |

**Pure 4.7+P xhigh vs Pure 4.7+Plan:** один коммит вместо трёх. xhigh-вариант поймал sort-cycle через `console-get-logs` MCP и починил в той же сессии до коммита; high-plan-вариант поймал rotation/trail и magenta-material **после** коммита, в отдельных fix-коммитах. По итогу баг-список меньше (1 vs 2), но это не из-за модели: high-plan использовал **другой архитектурный путь** (aggregated RocketData) и наткнулся на иные runtime-проблемы (RotateData missing, magenta). xhigh-plan выбрал разделённый дизайн (Launcher + Homing + Event + Tag) — он ближе к существующему паттерну Gun/Laser и ECS-граф собрался почти корректно. Почти — потому что один цикл всё-таки возник (`[UpdateAfter(EcsShipPositionUpdateSystem)]` транзитивно создавал Homing → Move → ShipPosUpdate → Homing).

**Pure 4.7+P xhigh vs Pure 4.7+Plan по plan-coverage:** план-перед-кодом не предотвратил sort-cycle здесь — модель не успела увидеть транзитивную цепочку зависимостей (Homing → Move → ShipPosUpdate → Homing) до запуска. Урок: **xhigh effort повышает качество одной мысли, но не гарантирует прохода всей графовой проверки**. Спасает MCP-runtime feedback (`console-get-logs` → IndexOutOfRangeException в `ComponentSystemSorter.FindExactCycleInSystemGraph`).

**Pure 4.7+P xhigh vs Pure 4.6:** ~4× медленнее в wall clock — но в обмен на 287-строчную DESISIONS.md с оценкой токенов, регрессионные тесты на каждый компонент и архитектурно более гибкий дизайн (Launcher отдельно от Homing, легко добавить EnemyRocketTag).

---

## 2. Архитектурные решения

### 2.1 Нейминг

| Аспект | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan | Pure 4.7+P xhigh |
|---|---|---|---|---|---|---|
| Prefix | `Rocket*` | `Missile*` | `Missile*` | `Missile*` | **`Rocket*`** | **`Rocket*`** |
| Ammo component | `RocketAmmoData` | `MissileData` | `MissileData` | `MissileData` | **`RocketData`** (агрегирован: arsenal + Direction + ShootPosition) | **`RocketLauncherData`** (только арсенал: MaxRockets/CurrentRockets/RespawnDurationSec/RespawnRemaining + Launching/LaunchPosition/LaunchDirection) |
| Target/Homing | `RocketTargetData` | `HomingData` | `HomingData` | `HomingData` | **`RocketHomingData`** (Target, TurnRateRadPerSec) | **`RocketHomingData`** (Target, TurnRateRadPerSec) |
| Event | `RocketShootEvent` | `MissileShootEvent` | `MissileShootEvent` | `MissileSpawnEvent` | **`RocketShootEvent`** | **`RocketLaunchEvent`** (буфер на singleton + поиск цели в Bridge) |
| Guidance system | `EcsRocketGuidanceSystem` | `EcsHomingSystem` | `EcsMissileNavigationSystem` | `EcsHomingSystem` | **`EcsRocketHomingSystem`** | **`EcsRocketHomingSystem`** |
| Ammo system | `EcsRocketAmmoSystem` | `EcsMissileSystem` | `EcsMissileSystem` | `EcsMissileSystem` | **`EcsRocketSystem`** | **`EcsRocketLauncherSystem`** |
| Visual | `RocketVisual` | `MissileVisual` | `MissileVisual` | `MissileVisual` | **`RocketVisual`** | **`RocketVisual`** |

**Pure 4.7+P xhigh уникальное:** третья из трёх Opus-4.7 веток, использующая семантику `Rocket*` (вторая после GSD и Pure 4.7+Plan). Plan-перед-кодом снова возвращает к буквальному прочтению промпта. Дополнительно — единственная ветка, где **`Launcher`** выделен в отдельный концепт (`RocketLauncherData`, `EcsRocketLauncherSystem`) по аналогии с принятым в проекте паттерном Gun/Laser. Это маркирует «корабль = носитель оружия», а не «корабль ≡ ракета».

### 2.2 Компоненты ECS

| Компонент | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan | Pure 4.7+P xhigh |
|---|---|---|---|---|---|---|
| Ammo на корабле | `RocketAmmoData` (отдельный компонент) | `MissileData` (single) | `MissileData` (single) | `MissileData` (single) | **`RocketData` (single, агрегированный)** | **`RocketLauncherData` (только арсенал + Launching флаг)** |
| Target данные | `RocketTargetData` (Target, TurnRate) | `HomingData` (TurnSpeed) | `HomingData` (Target, TurnRate, Speed, LifeTime) | `HomingData` (TargetEntity, TurnRateRadPerSec, **TargetAcquisitionRange**) | `RocketHomingData` (Target, TurnRateRadPerSec) — **без диапазона** | **`RocketHomingData` (Target, TurnRateRadPerSec) — без диапазона** |
| Тег ракеты | `RocketTag` | `MissileTag` + `PlayerMissileTag` | `MissileTag` + `PlayerBulletTag` (reuse) | `MissileTag` + `PlayerBulletTag` (reuse) | **`RocketTag`** (одиночный, без reuse) | **`RocketTag`** (одиночный, без reuse) |
| Event-механизм | Обычный component | Обычный component | Обычный component | EntityCommandBuffer | Через `RocketData.Shooting` flag | **`RocketLaunchEvent` singleton-буфер** + поиск цели в managed Bridge |
| Кэширование цели | Да | Нет (пересчёт каждый кадр) | Да | Да | **Да** | **Да** + last-known-direction если цель умерла |
| RotateData на ракете | Да | Нет (баг: спрайт не поворачивается) | Да | Да (после фикса №4) | **Да** (после фикса №1) | **Да** (включён сразу при `EntityFactory.CreateRocket`) |

**Pure 4.7+P xhigh уникальное:** **разделённый** дизайн вместо аггрегированного: `RocketLauncherData` живёт на корабле и хранит только арсенал + флаг намерения выстрелить (`Launching=true`), `RocketHomingData` — на ракете. Между ними — **`RocketLaunchEvent` singleton-буфер**, который читает managed-side `ShootEventProcessorSystem` (см. §2.7). Это позволило вынести поиск ближайшего врага в managed-код (где доступен `_catalog`) без нарушения Burst-ограничений.

**Pure 4.7+P xhigh vs Pure 4.7+Plan:** Pure 4.7+Plan агрегирует всё в `RocketData` (включая `Direction` и `ShootPosition`). Pure 4.7+P xhigh разделяет арсенал и ракету. Tradeoff:
- **+** Pure 4.7+P xhigh: компоненты более когерентны (Single Responsibility): launcher знает арсенал, homing — про цель.
- **+** Pure 4.7+P xhigh: легче добавить EnemyRocketTag и enemy launcher (другая система, тот же тег ракеты).
- **−** Pure 4.7+P xhigh: 4 компонента + 1 event + 1 тег вместо 1 агрегата.
- **+** Pure 4.7+Plan: один `SetComponentData` атомарно записывает «всё что нужно для выстрела».
- **−** Pure 4.7+Plan: труднее эволюционировать, если арсенал и ракета разойдутся в требованиях.

**Pure 4.7+P xhigh слабее:** так же нет `TargetAcquisitionRange` — захватывается ближайшая цель из всей сцены без ограничения дальности.

### 2.3 Система наведения

| Аспект | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan | Pure 4.7+P xhigh |
|---|---|---|---|---|---|---|
| Тип | `SystemBase` | `ISystem` | `ISystem` | `ISystem` | **`ISystem`** | **`ISystem`** |
| Кэширование цели | Да | Нет | Да | Да | **Да** | **Да** |
| Алгоритм поворота | `RotateTowards()` cross/dot | `atan2` + clamp | `atan2` + clamp | `atan2` + clamp | **2D rotation matrix (cos/sin) + cross sign** | **2D rotation matrix (cos/sin) + signed angle через cross/dot** |
| Dead/lost target handling | `Exists()` + retarget | `WithNone<DeadTag>` | `Exists` + `HasComponent<DeadTag>` + retarget | `Exists` + `HasComponent<DeadTag>` + retarget | **`Exists` + `HasComponent<DeadTag>` + retarget в том же кадре** | **`Exists` + `HasComponent<DeadTag>` + НЕ retarget — летит по последнему `MoveData.Direction` (last-known direction)** |
| Тороидальный поиск | Нет | Нет | **Да** | Нет | Нет | Нет |
| Intercept prediction | Нет | Нет | **Да** | Нет | Нет | Нет |
| Sync RotateData ← Direction | — (Rocket имеет только Rotation от cross/dot) | Нет (баг) | Да | Да (после фикса №4) | **Да** (после фикса №1) | **Да** (заложено в начальный план) |
| Размер | 132 строки | 105 | 203 | 124 | **152** | **~120** |

**Pure 4.7+P xhigh vs Pure 4.7+Plan по dead-target:** обе ветки кэшируют `Target`, обе используют `Exists + HasComponent<DeadTag>`. Но они расходятся в действии:
- **Pure 4.7+Plan:** немедленный retarget (поиск нового ближайшего врага в том же кадре).
- **Pure 4.7+P xhigh:** retarget **отсутствует** — ракета продолжает лететь по последнему `MoveData.Direction` до коллизии или таймаута `LifeTimeData`. Решение мотивировано буквальной интерпретацией ТЗ: «если врежется не в выбранную цель — тоже считается».

Tradeoff: Pure 4.7+Plan агрессивнее (ракета всегда «горячая»), Pure 4.7+P xhigh — детерминированнее (поведение предсказуемо: «потеряли — летим по прямой»). Для одиночной ракеты в небольшой сцене разницы почти нет, но при многих ракетах подход xhigh снижает нагрузку (нет N×M-перебора каждый кадр).

**Pure 4.7+P xhigh алгоритмически:** аналогично Pure 4.7+Plan использует **прямую матрицу вращения 2D** (одно `cos` + одно `sin` за тик) вместо `atan2 + clamp + cos/sin`. Подтверждает гипотезу из 5-way отчёта: при xhigh-effort и плане модель естественно тяготеет к более точному паттерну.

**Pure 4.7+P xhigh слабее:** так же как Pure 4.7+Plan — нет `TargetAcquisitionRange`, нет тороидального поиска, нет intercept prediction. Pure 4.7+P xhigh строго следовал тексту промпта (в плане раздел 2 явно говорит «полёт по дуге через ограничение угловой скорости — каноническая реализация homing-missile, альтернативы [proportional navigation, lead targeting] сложнее и не требуются по ТЗ»).

### 2.4 Collision

| Аспект | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan | Pure 4.7+P xhigh |
|---|---|---|---|---|---|---|
| Маркер | `RocketTag` | `PlayerMissileTag` | `PlayerBulletTag` (reuse) | `PlayerBulletTag` (reuse) | **`RocketTag`** (отдельный) | **`RocketTag`** (отдельный) |
| Изменения в collision system | Добавлен `IsRocket()` | Добавлен `IsPlayerMissile()` | 0 (reuse) | 0 (reuse) | **Добавлен `IsRocket()` + Rocket+Enemy ветка** | **Добавлен `IsPlayerProjectile()` хелпер: `HasComponent<PlayerBulletTag> \|\| HasComponent<RocketTag>` — обобщает обе ветки в одну** |
| ScoreValue на ракете | Да | Нет | Нет | Нет | **Нет** (берётся ScoreValue врага) | **Нет** (берётся ScoreValue врага) |
| Rocket+Ship | — | — | — | — | **Безопасно игнорируется** (явная ветка) | **Безопасно игнорируется** (явная ветка) |
| Rocket+EnemyBullet | — | — | — | — | — | **Безопасно игнорируется** (своя ракета не сталкивается с пулей врага) |

**Pure 4.7+P xhigh уникальное (vs все остальные):** **`IsPlayerProjectile`** обобщающий хелпер. Семантика — «снаряд игрока, неважно какого типа». Эффект: было 4 if-ветки в `EcsCollisionHandlerSystem` (PlayerBullet+Enemy A/B order, Rocket+Enemy A/B order), стало 2 if-ветки. Снижение объёма коллизионной логики при сохранении строгого разделения тегов на уровне ECS-данных.

**Pure 4.7+P xhigh vs Pure 4.7+Plan:** оба пошли по явному `RocketTag` пути (а не reuse `PlayerBulletTag` как у Pure 4.7/Superpowers). Но Pure 4.7+Plan дублировал ветки PlayerBullet/Rocket в коллизионном хендлере, Pure 4.7+P xhigh — обобщил их через хелпер. Это более DRY-вариант того же архитектурного выбора. Возможный регресс: если в будущем PlayerBullet и Rocket разойдутся по правилам коллизии (например, ракета должна оглушать а не убивать), хелпер придётся развернуть обратно в две ветки. Но для текущего ТЗ — выигрыш по читаемости и объёму.

### 2.5 Visual / Prefab

| Аспект | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan | Pure 4.7+P xhigh |
|---|---|---|---|---|---|---|
| Trail в коде | `_trailEffect` + Play/Stop/Clear | Нет | `_trail` + Play() | `_trail` + Play() в `OnConnected` + Stop в `OnDisable` | **Только в префабе** (ParticleSystem-child, без управления из кода) | **`_trail.Clear()` в `OnConnected`** + ParticleSystem с `playOnAwake=true` в префабе |
| Trail material | URP-совместимый | URP-совместимый | `MissileTrail.mat` (asset, Sprites/Default) | `missile_trail.mat` (asset, URP/Particles/Unlit) | **`Default-ParticleSystem.mat`** (Unity built-in extra resource) | **`Default-ParticleSystem.mat`** (built-in extra resource) |
| Editor tooling | `RocketPrefabSetup.cs` | MCP script-execute | MCP script-execute | MCP `script-execute` + `PrefabUtility.SaveAsPrefabAsset` (после восстановления MCP) | **MCP `script-execute` + `PrefabUtility.LoadPrefabContents/SaveAsPrefabAsset`** | **Прямая запись YAML файла `rocket.prefab` + `.meta` с фиксированным GUID, затем `assets-refresh`** (после неуспеха `mcp__assets-copy`) |
| Prefab tests | YAML-парсер тест | Нет | Нет | Нет | Нет | Нет |
| Префабная инспектор-настройка HUD | в сцене | в сцене | в сцене | в сцене | в сцене | **отложено: `if (_rocketCount != null)` + явный TODO в DESISIONS.md** |

**Pure 4.7+P xhigh уникальное:** префаб написан **напрямую как YAML-файл** — `Assets/Media/prefabs/rocket.prefab`. Причина: `mcp__ai-game-developer__assets-copy` упал с `NullReferenceException`. xhigh-вариант проанализировал стоимость альтернативы (15+ atomic MCP-операций: gameobject-create + component-add × N + component-modify × M) и выбрал детерминированный путь — записать YAML напрямую, потом `assets-refresh`. Unity распознал префаб с первого раза.

**Pure 4.7+P xhigh частичный trail-управление:** в коде есть `_trail.Clear()` при `OnConnected`, но `Play/Stop` нет — полагается на `playOnAwake=true` префаба. Это **компромисс** между Pure 4.7+Plan (только префаб, никакого кода) и Pure 4.7 (полный Play/Stop в коде). Tradeoff проявился: при повторных выстрелах остался **визуальный артефакт TrailRenderer** — «протянутый» сегмент следа от точки исчезновения старой ракеты к точке спавна новой (см. §4 баг «Trail artifact at re-fire»). Решено пользователем: оставить как known issue, не фиксить.

**Pure 4.7+P xhigh уникальное (HUD setup):** единственная ветка, где сцена не настроена полностью — биндинги HUD-полей `_rocketCount` и `_rocketRespawnTime` оставлены `null`, защищены через `if (field != null)` в `HudVisual`. В DESISIONS.md явно указано: «открыть Main.unity, создать TMP_Text детей, перетащить в инспектор». Это **scoped honesty** — открытое признание границ MCP-возможностей, аналогичное «невыполненным опциональным элементам» у Pure 4.7+Plan, но конкретнее.

### 2.6 HUD

| Аспект | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan | Pure 4.7+P xhigh |
|---|---|---|---|---|---|---|
| Null-guard | Да + warning лог | Да | Да | Да (silent skip) | **Да (silent skip)** | **Да (silent skip)** + явный TODO в DESISIONS.md |
| Naming | `RocketAmmoCount` | `MissileShootCount` | `MissileShootCount` | `MissileShootCount` | **`RocketShootCount`** | **`RocketCount` / `RocketRespawnTime` / `IsRocketRespawnVisible`** (отдельная семантика «respawn», не «reload») |
| Особенность | — | — | — | **Workaround:** ручной `_bridgeSystem.Update()` в `Application.OnUpdate` (см. §4 баг #3) | **`SetRocketMaxShoots()` через `ObservableBridgeSystem`** — `IsRocketReloadTimeVisible` пересчитывается из (current<max) | **`SetRocketMaxCount()` через `ObservableBridgeSystem`** — Bridge читает `RocketLauncherData` напрямую, обновляет `HudData.RocketCount/RespawnTime/IsRocketRespawnVisible` |

**Pure 4.7+P xhigh vs Pure 4.7+Plan:** оба используют `ObservableBridgeSystem` вместо workaround Pure 4.7. Различие — в наименовании HUD-полей: xhigh-вариант ввёл отдельную семантику «respawn» (не «reload»), что точнее соответствует промпту «после запуска ракет включается счётчик на респавн ракет». Это пример того, как plan + xhigh effort выводит к точному переводу терминологии промпта в код.

### 2.7 Input → ECS

| Аспект | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan | Pure 4.7+P xhigh |
|---|---|---|---|---|---|---|
| Ammo guard в Game.cs | Да | Нет | Нет | Нет (полагается на проверку в `EcsMissileSystem`) | **Нет** (полагается на проверку `Shooting && CurrentShoots>0` в `EcsRocketSystem`) | **Нет** (полагается на проверку `Launching && CurrentRockets>0` в `EcsRocketLauncherSystem`) |
| Поиск ближайшей цели | в момент спавна (`EntitiesCatalog.SpawnRocket`) | в системе наведения (Job каждый кадр) | в системе наведения (cached) | в `MissileSpawnSystem` (managed) | в Game.OnRocket (managed Game) | **в managed Bridge — `ShootEventProcessorSystem.ProcessRocketEvents`**, после прочтения `RocketLaunchEvent` |
| Action key | Space (overload) | R | R | R | R | **R** (новый action в `player_actions.inputactions`, биндинг `<Keyboard>/r`) |

**Pure 4.7+P xhigh уникальное:** **поиск ближайшей цели вынесен в managed Bridge** — единственная из 6 веток. Логика: `EcsRocketLauncherSystem` (Burst-friendly ISystem) только генерирует `RocketLaunchEvent` в singleton-буфер; затем `ShootEventProcessorSystem` (managed, имеет `_catalog` и доступ к Unity API) читает буфер и через `EntityQuery<AsteroidTag/UfoTag/UfoBigTag, MoveData>` находит ближайшего врага без `DeadTag`, после чего вызывает `_catalog.CreateRocket(pos, dir, target)`.

Преимущества:
- Burst-friendly Launcher не требует доступа к managed `_catalog`.
- Поиск ближайшего энемия (linear scan EntityQuery) удобнее писать в Bridge без Burst-ограничений (можно использовать LINQ или managed-сущности).
- Унифицировано с уже существующей логикой пушки и лазера в `ShootEventProcessorSystem`.

Недостаток: дополнительный hop через event-буфер (1 кадр задержки между нажатием R и созданием ракеты в сцене). Для геймплея незаметно, для тестов — добавляет необходимость явно тикать `ShootEventProcessorSystem` после установки события (см. §4 баг «MCP вручную дёрнул sysMan.Update()»).

---

## 3. Тестовое покрытие

| Категория | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan | Pure 4.7+P xhigh |
|---|---|---|---|---|---|---|
| Ammo system | 10 | 8 | 7 | 8 | **7** (`EcsRocketSystemTests`) | **9** (`EcsRocketLauncherSystemTests`: reload/cooldown/launch/no-ammo/reset Launching/multi-rocket) |
| Guidance/Homing | 9 | 8 | 11 | 12 (10 базовых + 2 регресс на rotation sync) | **10** (8 базовых + 2 регресс на RotateData sync после фикса №1) | **7** (`EcsRocketHomingSystemTests`: поворот к цели, ограничение углом, потеря цели → прямая, нулевой turnRate) |
| Collision | ~5 | 5 | 4 | (используются существующие) | **4** (Rocket+Asteroid, Rocket+Ufo, обратный порядок, Rocket+Ship безопасно) | **7** (`RocketCollisionTests`: Rocket+Asteroid/Ufo/UfoBig в обоих порядках, Rocket+Ship игнор, Rocket+EnemyBullet игнор) |
| Lifecycle | 5 | 0 | 0 | 0 | 0 | 0 |
| Prefab validation | 2 | 0 | 0 | 0 | 0 | 0 |
| Component defaults | в существующих | в существующих | в существующих | в существующих | в существующих | **5** (`RocketComponentTests`: defaults, добавление в entity, буфер событий) |
| EntityFactory | в существующих | 2 | 0 | 3 | **2** (`HasInitialRocketData`, `CreateRocket_HasCorrectComponents`) | **+1** (CreateRocket all components + значения; обновлены 3 теста CreateShip с новыми параметрами) |
| Bridge / HUD | в существующих | 0 | 0 | 1 | **2** (`PushesRocketData_ToHudData`, `RocketReloadTimeHidden_WhenAmmoFull`) | в существующих расширениях `ObservableBridgeSystemTests` |
| **System ordering (cycle)** | 0 | 0 | 0 | **2** (DFS-граф + конкретный регресс) | **0** (цикл не возник — DFS-guard не понадобился) | **0** (цикл возник, поправлен; DFS-guard не написан — фикс одним удалением `[UpdateAfter]`, регресс-тест не добавлен) |
| **Итого rocket-тестов** | **~31** | **~23** | **22** | **25** | **~25** | **~28** |
| **Общий test suite** | (фазы 1-9: 188+) | n/a | n/a | n/a | **188/188 passed** | **194/194 passed** |

**Pure 4.7+P xhigh vs Pure 4.7+Plan:** **+3 rocket-теста** (`RocketComponentTests` 5 шт., больше тестов на launcher и коллизии). Дополнительные сценарии: defaults компонентов отдельным файлом, явные тесты «buffer-event ставится / собирается», а также явный тест «Rocket+EnemyBullet безопасно игнорируется» (этого не было ни у одной другой ветки).

**Pure 4.7+P xhigh минус:** sort-cycle поправлен **без регресс-теста**. Pure 4.7 в аналогичной ситуации написал DFS-граф проверку (2 теста), Pure 4.7+P xhigh — пропустил, ограничившись комментарием в DESISIONS.md «Урок: при добавлении ECS-системы… проверять ВСЮ цепочку транзитивных зависимостей». Без теста этот урок может быть забыт при следующем расширении системного графа. **Это — наиболее заметный пропуск xhigh-варианта по сравнению с другими 4.7-ветками.**

**Pure 4.7+P xhigh плюс:** общий тест-сьют **194/194** (на 6 тестов больше, чем у Pure 4.7+Plan 188/188). Это включает не только новые rocket-тесты, но и поддержку расширенного `EntityFactory.CreateShip` (теперь принимает `RocketLauncherData` параметром).

---

## 4. Проблемы и баги

### Pure 4.7+P xhigh (1 fix-итерация в рамках одной сессии):

1. **ECS sort cycle** — `[UpdateAfter(EcsShipPositionUpdateSystem)]` в `EcsRocketHomingSystem` транзитивно создавал цикл `Homing → Move → ShipPosUpdate → Homing` (потому что `EcsMoveSystem [UpdateBefore(EcsShipPositionUpdateSystem)]`). Unity ECS падал с `IndexOutOfRangeException` в `ComponentSystemSorter.FindExactCycleInSystemGraph` при сортировке систем.
   - **Поймали через:** `mcp__ai-game-developer__console-get-logs` — exception висел в консоли при первом запуске PlayMode.
   - **Фикс:** удалить `[UpdateAfter(EcsShipPositionUpdateSystem)]`, оставить только `[UpdateBefore(EcsMoveSystem)]`. Knowledge of `ShipPositionData` в Homing не нужно — он читает позицию цели напрямую через `MoveData`.
   - **Регресс-тест:** **не добавлен** (комментарий в DESISIONS.md §3.8 «Урок: проверять цепочку транзитивных зависимостей» вместо кода).
   - Поправлено в той же сессии до коммита — **в git нет отдельного fix-коммита**.

2. **MCP `assets-copy` упал с NullReferenceException** при попытке копировать существующий префаб как стартовую точку для `rocket.prefab`. Обошли прямой записью YAML-файла + `assets-refresh`. Не влияет на runtime — это был только инструмент построения префаба.

3. **Trail artifact at re-fire** (known issue, не починен) — при втором и последующих запусках ракеты в HUD/сцене виден визуальный артефакт от `TrailRenderer` («протянутый» сегмент следа от точки исчезновения предыдущей ракеты к точке спавна новой). Текущая защита через `_trail.Clear()` в `RocketVisual.OnConnected` недостаточна. **Гипотезы:** (а) `Clear()` вызывается ДО переустановки `transform.position` через `GameObjectSyncSystem`, (б) `emitting=true` устанавливается до синхронизации позиции из ECS `MoveData`. **Решено пользователем: оставить как known issue, не фиксить.** Возможный фикс — выключить `emitting`, дать кадр на синхронизацию, потом `Clear()` + `emitting=true`. Документировано в DESISIONS.md §9 «Что требует ручной доработки».

4. **MCP не смог дотикать `ShootEventProcessorSystem` в EditMode/Paused** — после того как через `script-execute` установили `RocketLaunchEvent` в буфер, нужно было вручную дёрнуть `sysMan.Update()` через тот же `script-execute`, чтобы Bridge обработал событие и создал ракету. **Workaround:** добавили шаг ручного `Update()` в проверочный скрипт. **Влияние на production: нулевое** (в обычной игре системы тикают сами через PlayerLoop).

### Сравнение по природе багов

| Баг | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan | Pure 4.7+P xhigh |
|---|---|---|---|---|---|---|
| **Cycle UpdateAfter** | — | ✓ (поймали) | ✓ (поймали) | ✓ (поймал) | **— (не возник благодаря плану)** | **✓ (возник несмотря на план; поймал через `console-get-logs` без отдельного коммита)** |
| **Particle shader / magenta** | ✓ | ✓ | ✓ | ✓ | **✓** | **— (избежан — сразу взял `Default-ParticleSystem.mat` через built-in)** |
| **Spawn без RotateData** | — (свой подход) | — (не делал rotation вообще) | — (сразу с RotateData) | ✓ (поймал по фидбэку) | **✓ (поймал по фидбэку)** | **— (RotateData в плане CreateRocket с самого начала)** |
| **Config defaults** | ✓ (Score) | — | — | ✓ (весь Missile блок) | **— (заполнил через SerializedObject script-execute сразу)** | **— (GameData.asset с дефолтами заполнен сразу через SerializedObject)** |
| **HUD null-guard / system schedule** | ✓ (null guard) | — | — | ✓ (Bridge Update workaround) | **— (использовал существующий Bridge pipeline)** | **— (использовал ObservableBridgeSystem) + ✓ (HUD биндинги отложены — null-guard required)** |
| **Уникальные ECS-API ошибки** (ref в foreach, static в ISystem) | — | — | ✓ (4 шт) | — | **—** | **—** |
| **Trail artifact at re-fire** | — | n/a | — | — | — | **✓ (known issue, не починен)** |
| **MCP assets-copy NRE** | — | — | — | — | — | **✓ (обошли YAML)** |

**Pure 4.7+P xhigh: 2/8 потенциальных багов проявились (sort-cycle + trail artifact). MCP NRE и MCP scheduler tick — инструментальные, не блокирующие production.**

**Самый интересный observable у Pure 4.7+P xhigh:** план-перед-кодом + xhigh effort **не предотвратили** sort-cycle. План в DESISIONS.md описывал «`[UpdateBefore(EcsMoveSystem)]`», но добавил **избыточный** `[UpdateAfter(EcsShipPositionUpdateSystem)]` (предположительно «чтобы Homing видел свежую позицию корабля»). Транзитивная цепочка `Homing → Move → ShipPosUpdate → Homing` стала видна только в runtime. **Урок:** проактивная проверка графа зависимостей перед написанием кода полезна, но **не является заменой** runtime-feedback от Unity ECS sort algorithm. План + MCP > план без MCP.

**Сравнение с Pure 4.7+Plan:** Pure 4.7+Plan не наткнулся на sort-cycle, потому что выбрал агрегированный `RocketData` (только один атрибут `[UpdateBefore(EcsMoveSystem)]`). Pure 4.7+P xhigh выбрал разделённый дизайн (Launcher отдельно от Homing) и поставил атрибут на «не ту» границу графа. **Архитектурное решение влияет на pattern-cycle vulnerability.**

**Общий баг у пяти из шести:** проблема с trail-материалом — implicit-quirk Unity URP, не зависит от подхода или модели. **Pure 4.7+P xhigh — единственная ветка, избежавшая этого**, потому что план явно предписал `Default-ParticleSystem.mat` через `AssetDatabase.GetBuiltinExtraResource` (заимствовано напрямую из Pure 4.7+Plan §2.5).

---

## 5. Документация

| Метрика | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan | Pure 4.7+P xhigh |
|---|---|---|---|---|---|---|
| Файлов документации | 57 | 1 | 3 | 1 (DECISIONS.md, ~500 строк) | **1** (DECISIONS.md, ~175 строк) | **1** (DESISIONS.md, ~287 строк) |
| Трассируемость | Формальная | Неформальная | Spec→Plan checkbox | Таблицы (R1-R13 → файлы → тесты) + анализ багов | **Структурированно по разделам (Архитектура → Алгоритм → Конфиги → Ввод → HUD → Visual → TDD → MCP)** | **10 разделов: Промт → План → Решения → Архитектура → Тесты → Файлы → MCP → Токены → Известные TODO → Соблюдение CLAUDE.md** |
| Регресс-тесты документированы | Да (debug KB) | Нет | Нет | Да (§9.5 в DECISIONS.md) | **Косвенно** (§7 TDD-покрытие перечисляет тесты) | **Косвенно** (§5 TDD-покрытие перечисляет тесты, без явных регресс-меток) |
| Анализ роли инструментов | Нет | Нет | Нет | Да (§9 — что было бы с MCP с самого начала) | **Да (§8)** — MCP-валидация: какие шаги через какие инструменты | **Да (§7)** — таблица MCP-проверка с Status/Result для каждого инструмента + явный fail (`assets-copy` NRE) |
| Невыполненные опциональные пункты | — | — | — | — | **Да (§Невыполненные опциональные элементы)** — явно перечислены отложенные задачи | **Да (§9)** — 4 явных known TODO с pythagorean-уровня деталями (включая artifact с гипотезой и предлагаемым фиксом) |
| **Оценка токенов / стоимости** | — | — | — | — | — | **Да (§8)** — единственная ветка с явной таблицей токенов по фазам + денежной оценкой (~$10.13) и ремарками о caching и retry overhead |
| Соблюдение CLAUDE.md правил | Неявно | Неявно | Неявно | Неявно | Неявно | **Да (§10)** — явный чеклист: язык, стиль, минимальный scope, TDD, MCP, регресс на фиксе |

**Pure 4.7+P xhigh уникальное:**
1. **§8 «Оценка по токенам и денежная оценка ~$10».** Единственная ветка из 6, явно посчитавшая стоимость фичи. Включает разбивку по фазам (контекст / исследование / план / TDD / реализация / интеграция / префаб / MCP / запись DESISIONS), грубую оценку cache-hit savings и средние метрики (стоимость на файл/строку).
2. **§7 «MCP-проверка».** Структурированная таблица: какие MCP-инструменты использовались, результат каждого вызова (OK/FAIL), и явное описание единственного провала (`assets-copy NullReferenceException`). Это более документированный self-audit, чем у Pure 4.7+Plan.
3. **§9 «Что требует ручной доработки».** Не просто «есть TODO», а с гипотезами, ссылками на код (`Assets/Scripts/View/RocketVisual.cs:26-30`) и предлагаемыми фиксами. Артефакт TrailRenderer описан с двумя гипотезами и двумя возможными решениями.
4. **§10 «Соблюдение CLAUDE.md».** Чеклист соответствия глобальным правилам. Это уникально — ни одна другая ветка не делает explicit-self-check. (Глобальные правила, включая «при багфиксе всегда писать регрессионный тест», получили ✅ в чеклисте — что **противоречит реальности**: на sort-cycle регресс-теста нет. Это потенциальный риск самопроверки.)

**Pure 4.7+P xhigh vs Pure 4.7+Plan:** примерно в 1.6× длиннее (287 vs 175 строк), без формальной трассируемости R1-R13, но с тремя уникальными разделами (токены, MCP-таблица, чеклист CLAUDE.md). Это **более реflective**, но также демонстрирует риск self-claim accuracy: § 10.6 («регрессионные тесты при фиксе») формально проставлен ✅, тогда как фактически на sort-cycle регресс-теста нет.

---

## 6. Расширяемость

| Аспект | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan | Pure 4.7+P xhigh |
|---|---|---|---|---|---|---|
| Ракеты врагов | `EnemyRocketTag` нужен | Готово (tag split) | Нужен рефакторинг | Нужен рефакторинг (PlayerBulletTag reuse) | **Готово к tag split** (RocketTag отдельный, легко добавить EnemyRocketTag) | **Готово к tag split** (RocketTag отдельный, разделённый Launcher позволяет добавить enemy launcher без изменения Homing) |
| Очки за сбитие ракеты | ✓ (ScoreValue) | Добавить | Добавить | Добавить | **Добавить** (ScoreValue нет — сейчас берётся ScoreValue врага) | **Добавить** (ScoreValue нет — сейчас берётся ScoreValue врага через DeadTag) |
| Trail lifecycle | Полный (Play/Stop/Clear) | Только префаб | Play() в Connect | **Play в OnConnected + Stop в OnDisable** | **Только префаб** (regress если будут пуллить ракеты) | **Частичный: Clear() в Connect, есть known artifact при re-fire** |
| Тороидальное наведение | Нет | Нет | **Да** | Нет | Нет | Нет |
| Intercept prediction | Нет | Нет | **Да** | Нет | Нет | Нет |
| Range-конфиг для захвата цели | Нет | Нет | Нет | **Да** (`TargetAcquisitionRange`) | Нет | Нет |
| Конфиги ракеты | `RocketAmmoConfig` ScriptableObject | Нет | Нет | Да (`MissileData` struct в `GameData`) | **Да** (`RocketData` struct в `GameData`: `Prefab`, `MaxShoots`, `ReloadDurationSec`, `Speed`, `LifeTimeSeconds`, `TurnRateDegPerSec`) | **Да** (`RocketData` struct в `GameData`: `Prefab`, `MaxRockets`, `RespawnDurationSec`, `Speed`, `TurnRateDegPerSec`, `LifeTimeSec`, `Score`) |
| Параллелизм Launch + Homing | NA | NA | NA | NA | Один компонент → нет | **Разделение Launcher/Homing → параллельный Launcher даёт enemy launcher без изменения Homing** |

**Pure 4.7+P xhigh уникальное:** разделённый Launcher/Homing **архитектурно поддерживает enemy rockets без рефакторинга** — нужно только добавить `EnemyRocketLauncherSystem` на UFO-entity, а `EcsRocketHomingSystem` будет работать одинаково для player+enemy ракет. Pure 4.7+Plan также готов к tag split, но из-за aggregated `RocketData` потребует разделения контейнера на (player) launcher + (rocket-only) data.

---

## 7. Overhead фреймворка

| Метрика | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan | Pure 4.7+P xhigh |
|---|---|---|---|---|---|---|
| Planning файлов | 57 | 0 | 2 | 0 | **0** (план был в conversation, не в файлах) | **0** (план в conversation + §2 DESISIONS.md) |
| Docs-коммитов | 174 (55%) | 0 | 3 | 1 | **0** (DECISIONS.md в feat-коммите) | **0** (DESISIONS.md в feat-коммите) |
| Строк документации | Тысячи | ~200 | 2171 | ~500 | **~175** | **~287** |
| Framework overhead | Критический | Нулевой | Умеренный | Нулевой | **Нулевой** | **Нулевой** |
| MCP overhead | Нет (не использовал) | Минимальный | Умеренный (3 MCP-итерации на trail) | Умеренный (5 fix-итераций; половина обнаружена по фидбэку до восстановления MCP) | **Минимальный** (2 fix-итерации, MCP с самого начала) | **Минимальный** (1 fix-итерация sort-cycle, 1 обход MCP NRE; явный шаг «убедись что MCP работает» в начале) |

---

## 8. Итоговая оценка

| Критерий | GSD | Pure 4.6 | Superpowers | Pure 4.7 | Pure 4.7+Plan | Pure 4.7+P xhigh | Лидер |
|---|---|---|---|---|---|---|---|
| Скорость | ~5 часов | ~50 мин | ~2 часа | ~2.5 часа | **~2 часа** | ~3-4 часа | **Pure 4.6** |
| Качество кода | Высокое | Хорошее | Хорошее + intercept | Хорошее + RotateData sync + Range config + URP material | **Хорошее + 2D rotation matrix + RotateData sync + agreggated RocketData** | **Хорошее + 2D rotation matrix + разделённый Launcher/Homing + IsPlayerProjectile DRY** | Pure 4.7+P xhigh ≈ Pure 4.7+Plan ≈ Pure 4.7 ≈ Superpowers |
| Тестовое покрытие | 31 | 23 | 22 | 25 + DFS regress | **25 + RotateData regress** | **28 (без DFS regress, без RotateData regress — на cycle и rotation тестов нет)** | **GSD** (количество и lifecycle); Pure 4.7+P xhigh (количество без регрессов) |
| Архитектура | SRP, кэш цели, Editor tooling | Простота, tag split | Intercept, toroidal | Range config, RotateData sync, BridgeUpdate workaround | Aggregated RocketData, прагматичный tag split, built-in trail material | **Разделённый Launcher/Homing/Event/Tag, IsPlayerProjectile хелпер, поиск цели в managed Bridge, last-known direction** | **Superpowers** (полнота алгоритма) / **Pure 4.7+P xhigh** (модульность) |
| Документация | Полная трассируемость | Минимум | Spec+Plan | Таблицы + анализ роли MCP | Компактно по разделам + раздел «невыполнено» | **287 строк: 10 разделов + токены + MCP-таблица + чеклист CLAUDE.md** | GSD (полнота) ≈ Pure 4.7+P xhigh (детализация и self-audit) |
| Баги (количество fix-итераций) | 6 | 3 | 8 | 5 | **2** | **1 (sort-cycle) + 1 known issue** | **Pure 4.7+P xhigh** (по числу production-блокеров) ≈ **Pure 4.7+Plan** (по полностью починенным) |
| Уникальные ECS-API ошибки | 0 | 0 | 4 | 0 | **0** | **0** | Все кроме Superpowers |
| Overhead | Высокий | Нулевой | Умеренный | Нулевой (структурно) | **Нулевой** | **Нулевой** | Pure 4.6 ≈ Pure 4.7 ≈ Pure 4.7+Plan ≈ Pure 4.7+P xhigh |
| Регрессионные тесты на каждый баг | Частично | Нет | Нет | Да (3/5 покрыты, 2/5 митигация) | **Да (2/2 покрыты)** | **Нет (0/1 — sort-cycle без регресс-теста)** | Pure 4.7+Plan |
| Прозрачность процесса | Низкая | Низкая | Средняя | Высокая (DECISIONS.md) | Высокая | **Самая высокая (токены + $$ + явный MCP-таблица + чеклист CLAUDE.md)** | **Pure 4.7+P xhigh** |
| Honest scope (известные TODO в продукте) | Implicit | Implicit | Implicit | Implicit | Явный, без артефактов в коде | **Явный, с известным runtime-артефактом, оставленным умышленно** | **Pure 4.7+P xhigh** (по полноте признания) |

### Общий вердикт

- **Pure 4.6** — самый быстрый. Идеален когда задача понятна и runtime feedback дешёвый.
- **Pure 4.7** — следует существующим паттернам (как Pure 4.6), но добавляет защитные механизмы на свои собственные ошибки (DFS-регресс на циклы, range-config, RotateData sync) и фиксирует **самоанализ** в документации.
- **Pure 4.7+Plan** — золотая середина по числу полностью починенных багов: единственная ветка, где план-перед-кодом измеримо снизил количество багов (2 vs 5 у Pure 4.7), убрал три категории runtime-проблем (cycle, config defaults, bridge schedule) и сократил DECISIONS.md в 3 раза. Использовал самые прагматичные решения: built-in trail material, отдельный `RocketTag`, агрегированный `RocketData`. **Регрессионные тесты на 100% багов.**
- **Pure 4.7+P xhigh** — **самая модульная архитектура** и **самая прозрачная документация** из шести вариантов. Разделение `RocketLauncherData` / `RocketHomingData` / `RocketLaunchEvent` / `RocketTag` плюс `IsPlayerProjectile` хелпер дают наилучшую готовность к расширению (enemy rockets, multi-rocket arsenal). DESISIONS.md уникальна по самоанализу: токены ($$), таблица MCP-инструментов, чеклист CLAUDE.md. **Слабые места:** sort-cycle всё-таки возник (план не предотвратил), регресс-тест на него не написан, TrailRenderer-артефакт оставлен как known issue по решению пользователя. **xhigh effort не дал измеримого выигрыша по сравнению с high-plan** — ни по скорости (3-4 ч vs ~2 ч), ни по числу багов (1 vs 2 production-блокера). Зато дал **более глубокую саморефлексию** в документации.
- **Superpowers** — лучший по физике алгоритма (intercept + toroidal), но дорого по subagent-overhead и уникальным ECS-API ошибкам.
- **GSD** — лучший по охвату и трассируемости, но критический overhead (55% времени на документацию).

### Что изменили план-перед-кодом, Opus 4.7 и xhigh effort

| Аспект | Заметно лучше у Pure 4.7+P xhigh? | Комментарий |
|---|---|---|
| Скорость | **Нет** vs Pure 4.7+Plan (~3-4ч vs ~2ч) | xhigh effort требует больше cycles на одну мысль; план + xhigh не быстрее, чем план + high. |
| Качество первичного кода | **Да** по модульности (разделённые компоненты) | Архитектурные выборы — лучшая готовность к будущему расширению (enemy rockets). |
| Глубина саморефлексии | **Да** vs все 5 предыдущих веток | Токены/$$, MCP-таблица, чеклист CLAUDE.md — уникальные разделы документации. |
| Реакция на runtime фидбэк | На уровне Pure 4.7+Plan | На обоих фидбэках (sort-cycle, MCP NRE) применил debugging-skill. |
| Регрессионные тесты | **Хуже** vs Pure 4.7+Plan | 0/1 — sort-cycle не покрыт регресс-тестом. |
| Проактивная защита от собственных ошибок | На уровне Pure 4.7+Plan | DFS-guard не написан, как и у Pure 4.7+Plan. |
| Алгоритмическая сложность | Не лучше | Без подсказки в промпте intercept/toroidal не реализованы (как у всех 4.7-веток). |
| Ошибки ECS API | На уровне Pure 4.7 / Pure 4.7+Plan / Pure 4.6 | Single-agent видит весь контекст, ECS-API не путает. |
| Использование MCP | Не лучше Pure 4.7+Plan | MCP с самого начала (явный шаг «убедись что работает»), но `assets-copy` NRE добавил один обход через ручной YAML. |
| Атомарность коммитов | **Хуже** vs Pure 4.7+Plan (3 коммита) | Один коммит «имплементация ракеты» включает feat + 1 фикс sort-cycle — нарушает «один коммит = одна логическая единица». |
| Прозрачность стоимости | **Да** vs все 5 предыдущих веток | Единственная ветка с явной оценкой токенов и долларов. |

### Уникальные неожиданности Pure 4.7+P xhigh

1. **Поиск ближайшей цели в managed Bridge.** Единственная из 6 веток, где `EcsRocketLauncherSystem` лишь генерирует event в singleton-буфер, а `ShootEventProcessorSystem` (managed-side) вычисляет ближайшего врага и вызывает `_catalog.CreateRocket`. Это сохраняет Burst-friendliness Launcher и переиспользует уже существующий event-pipeline проекта (как Gun, Laser).

2. **`IsPlayerProjectile` хелпер вместо дублирования.** Объединяет `PlayerBulletTag || RocketTag` в одной ветке `EcsCollisionHandlerSystem`. Уменьшил 4 if-ветки до 2 при сохранении строгого разделения тегов. Семантически точно: «снаряд игрока» — единое правило коллизии, реализация — два разных класса данных.

3. **Last-known direction вместо retarget.** При смерти/исчезновении цели ракета летит по `MoveData.Direction`, не пытаясь найти новую цель. Решение мотивировано буквальным прочтением ТЗ «если врежется не в выбранную цель — тоже считается». Pure 4.7+Plan делает retarget — обе интерпретации валидны, xhigh выбрал детерминированную.

4. **Префаб как ручной YAML после `assets-copy` NRE.** Единственная ветка, столкнувшаяся с MCP `assets-copy` багом и обошедшая его прямой записью `.prefab` + `.meta` файлов с фиксированным GUID, потом `assets-refresh`. Прагматизм > MCP-purity.

5. **Sort-cycle всё-таки возник, несмотря на xhigh + plan.** Промежуточный атрибут `[UpdateAfter(EcsShipPositionUpdateSystem)]` в `EcsRocketHomingSystem` создавал транзитивный цикл `Homing → Move → ShipPosUpdate → Homing`. Pure 4.7+Plan этого избежал благодаря выбору aggregated RocketData (один атрибут на границе графа). xhigh-выбор разделённого дизайна расширил поверхность графа, и план не уловил транзитивную цепочку. Поправлено через MCP `console-get-logs`. **Урок: xhigh + plan не заменяют MCP runtime-feedback.**

6. **Денежная оценка фичи в DESISIONS.md.** Единственная ветка с явной таблицей токенов по 9 фазам и грубой оценкой ~$10.13 без cache savings. Полезное self-disclosure, которое отсутствует у Pure 4.7 (DECISIONS.md там тоже большой, но цены не считал).

7. **Раздел «Соблюдение CLAUDE.md правил» с явными ✅.** Единственная ветка с такой формальной самопроверкой. Полезно, но потенциально опасно: пункт «регрессионные тесты при фиксе» проставлен ✅, тогда как на sort-cycle регресс-тест не написан. Self-claim требует валидации reviewer'ом.

8. **TrailRenderer artifact at re-fire — known issue, оставленный по решению пользователя.** Единственная ветка с **намеренно** не починенным runtime-багом, документированным в DESISIONS.md §9.4 с двумя гипотезами и предлагаемыми фиксами. Это «scoped honesty» в чистом виде: лучше явный TODO с гипотезой, чем тихое оставление.

9. **Один атомарный коммит «имплементация ракеты».** Все фиксы внутри сессии (sort-cycle, MCP NRE) попали в один и тот же коммит вместе с feat. Pure 4.7+Plan делал 3 коммита (feat + 2 fix). Tradeoff: чище git log в xhigh-варианте, но теряется audit-trail отдельных фиксов.

10. **Семантика «respawn», а не «reload» в HUD.** Единственная ветка с буквальным переводом промпта «после запуска ракет включается счётчик на респавн ракет» в код: `RocketRespawnTime`, `IsRocketRespawnVisible`. У всех остальных — «reload»/«ammo».

---

## 9. Рекомендации

### 9.1 Промпт-усиление для Pure Claude (любой версии)

Дополнения к промпту, закрывающие gaps всех шести подходов:

```
[NEW] Дополнительные требования к качеству:

1. ОБЯЗАТЕЛЬНО: явный пункт «1. Составь план» (как у Pure 4.7+Plan).
   Это снижает количество runtime-багов в 2-3 раза.
2. ОБЯЗАТЕЛЬНО: пункт «убедись что MCP работает; если нет — НЕ продолжай»
   (как у Pure 4.7+P xhigh). Защищает от 30 минут работы вслепую.
3. DEFENSIVE: early-return guard в Game.OnRocket() (как у GSD).
   Trail Play/Stop/Clear в Visual.OnConnected/OnDisable (для пуллинга).
4. РАСШИРЯЕМОСТЬ: ScoreValue на ракете + tag split
   (RocketTag + PlayerRocketTag/EnemyRocketTag).
5. НАВЕДЕНИЕ: intercept prediction + тороидальный поиск (как у Superpowers).
6. ТЕСТЫ: lifecycle (spawn→sync→die) + prefab YAML validation +
   регресс-тест на каждый исправленный баг + DFS-граф системного порядка
   (как у Pure 4.7) для защиты от будущих циклов.
   ВАЖНО: «при багфиксе всегда писать регрессионный тест» — пункт CLAUDE.md.
   xhigh-вариант проставил ✅ в self-check, но фактически пропустил.
   Reviewer должен валидировать соответствие самозаявлений коду.
7. EDITOR TOOLING: Editor-скрипт для пересоздания префаба;
   при отказе MCP `assets-copy` — fallback на прямой YAML
   (как у Pure 4.7+P xhigh).
8. ДОКУМЕНТАЦИЯ: трассируемость [Требование → Файлы → Тесты] +
   анализ роли инструментов на каждый баг (как у Pure 4.7 §9) +
   раздел «Невыполненные опциональные элементы» (как у Pure 4.7+Plan) +
   оценка токенов и стоимости (как у Pure 4.7+P xhigh) +
   чеклист соответствия CLAUDE.md (как у Pure 4.7+P xhigh).
9. MCP: запустить Unity MCP ДО первой строчки кода. Использовать
   tests-run после каждой системы, screenshot-camera после первого Play Mode,
   assets-shader-list-all перед использованием любого shader,
   console-get-logs после первого Play Mode для проверки sort-cycle.
10. АРХИТЕКТУРА: рассмотреть оба варианта компонентного дизайна
    (aggregated vs разделённый); выбрать с учётом будущей готовности
    к enemy-варианту фичи (как Pure 4.7+P xhigh с Launcher/Homing).
```

### 9.2 Идеальный workflow

**Pure Claude 4.7 + явный план-перед-кодом + Unity MCP с самого начала + Superpowers TDD-skill** — фактически реализован в `Pure 4.7+Plan` и `Pure 4.7+P xhigh`. Достижения:

| Цель | Pure 4.7+Plan | Pure 4.7+P xhigh |
|---|---|---|
| Скорость ~2 часа | ✅ | ❌ (~3-4ч из-за xhigh effort) |
| ≤2 fix-итерации | ✅ | ✅ (1 + 1 known issue) |
| ≥25 rocket-тестов | ✅ (~25) | ✅ (~28) |
| 0 уникальных ECS-API ошибок | ✅ | ✅ |
| Без overhead на формальную документацию | ✅ | ✅ (но 287 vs 175 строк — больше, но всё ещё в 1 коммите) |
| Регресс-тесты на каждый баг | ✅ (2/2) | ❌ (0/1 — sort-cycle не покрыт) |
| Готовность к enemy rockets | Нужен split aggregated → launcher | ✅ (уже разделено) |
| Прозрачность стоимости | ❌ | ✅ |

**Рекомендация:** для **MVP** — Pure 4.7+Plan (быстрее, регресс-тесты на каждый баг). Для **production-кода с расширением (enemy rockets, multi-rocket)** — Pure 4.7+P xhigh (модульная архитектура), **но добавить регресс-тест на sort-cycle и убрать known TrailRenderer issue**. Чтобы достичь покрытия GSD (31 тест) и алгоритма Superpowers (intercept+toroidal), нужны явные требования в промпте — модель сама не выходит за рамки задачи.

### 9.3 Главные выводы

1. **План-перед-кодом — самый дешёвый buff.** Один пункт «Составь план» в промпте измеримо снизил fix-итерации с 5 до 2 у Pure 4.7. Не требует фреймворка, не требует subagent — только дисциплины reasoning.

2. **xhigh effort повышает качество саморефлексии, но не скорость.** Pure 4.7+P xhigh даёт более глубокую DESISIONS.md (токены, $$, чеклист) и более модульную архитектуру (Launcher отдельно от Homing), но wall clock в 1.5-2× больше, чем у Pure 4.7+Plan. **Выбирать xhigh нужно когда важна saturation на одну мысль** (архитектурные решения, оценка стоимости), не когда важна скорость.

3. **План + xhigh всё ещё не заменяют MCP runtime feedback.** Pure 4.7+P xhigh, несмотря на план и xhigh effort, сделал ошибку с транзитивным `[UpdateAfter]`. Поймал — только через `console-get-logs`. **Урок:** plan защищает от очевидных проблем, runtime — от транзитивных.

4. **Модель не творит чудес.** Opus 4.7+xhigh не реализует intercept prediction без явной просьбы — как и Opus 4.7 / Opus 4.6. Архитектурное качество определяется промптом, не версией модели и не effort'ом.

5. **Зато 4.7 лучше рефлексирует, а xhigh ещё больше.** Самоанализ роли MCP, написание DFS-граф regress-теста на собственную ошибку, явные таблицы трассируемости, раздел «невыполнено», оценка токенов, чеклист CLAUDE.md — всё это новые качества. xhigh усиливает их, но не заменяет дисциплину reviewer'а: пункт ✅ «регрессионные тесты при фиксе» в xhigh-варианте формально проставлен, фактически — нет (на sort-cycle).

6. **MCP > фреймворк.** Один MCP `tests-run` после каждой системы предотвращает половину runtime-багов всех шести подходов. Это ценнее чем GSD-документация или Superpowers-spec. Для Pure 4.7+P xhigh именно `console-get-logs` поймал sort-cycle — без MCP это был бы блокер.

7. **TDD не панацея для DOTS.** Все шесть подходов писали unit-тесты, но runtime-баги (cycle, trail material, RotateData missing, TrailRenderer artifact at re-fire) проявляются только в Play Mode. Unit-тесты на ECS-системы не ловят интеграционные проблемы. **Нужен PlayMode integration test или MCP screenshot-camera + console-get-logs**.

8. **Proactive guards триггерятся фактическими багами.** Pure 4.7 написал DFS-граф после cycle-бага. Pure 4.7+Plan не написал — потому что цикл не возник. Pure 4.7+P xhigh не написал — несмотря на то что цикл возник (но на нашем уровне — фикс одной строкой, регресс-тест посчитан излишним). Это нормально: defensive code должен быть оправдан ценой написания, и здесь модель приняла решение пропустить тест.

9. **Прагматизм vs полнота — обе стратегии работают, добавляется третья: модульность.** Pure 4.7+Plan выбрал минимализм (built-in trail material, агрегированный RocketData, нет range-config). Pure 4.7 выбрал defensive engineering (URP-specific material, разделённые компоненты, range-config). Pure 4.7+P xhigh выбрал **модульность под расширение** (разделённые Launcher/Homing/Event/Tag, IsPlayerProjectile хелпер, поиск цели в managed Bridge). Все три достигли работающей фичи. Выбор зависит от долгосрочного контекста: minimal — для MVP/прототипа, defensive — для production с защитой от регрессов, modular — для production с готовностью к расширению фичи (enemy rockets, multi-rocket).

10. **Архитектурный выбор влияет на pattern-cycle vulnerability.** Pure 4.7+Plan не наткнулся на sort-cycle из-за aggregated дизайна (один `[UpdateBefore]`). Pure 4.7+P xhigh столкнулся с ним из-за разделённого дизайна (несколько систем, больше графовых рёбер). Это не значит, что разделение хуже — это значит, что разделение **требует более тщательной проверки графа зависимостей**. Урок для будущих ECS-проектов: tooling для DFS-проверки графа должен быть стандартом разработки, а не реактивной защитой от уже произошедшего бага.
