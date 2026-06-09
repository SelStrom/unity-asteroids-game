# DECISIONS.md

Append-only журнал архитектурных решений по проекту Asteroids.
Каждый раздел соответствует одной фиче или этапу разработки.

> **Примечание об имени файла:** в исходной формулировке задачи файл назван `DESISIONS.md` (опечатка). Используется корректное имя `DECISIONS.md`.

---

## Фича: Самонаводящиеся ракеты

**Дата:** 2026-06-01
**Ветка:** `feature/rockets-pure-opus48-harness-workflow`
**Метод:** workflow `harness-cycle` (понять → спланировать → реализовать → проверить → синтез), затем ручная пост-верификация через Unity MCP.

---

### 1. Итоги решения

> Раздел отражает **фактическое** состояние кода в рабочем дереве на момент записи
> (сверено чтением исходников и `git diff`, а не намерениями плана).

#### ECS-компоненты, теги, события

| Артефакт | Тип | Реальные поля / поведение |
|---|---|---|
| `RocketTag` | `IComponentData` (tag) | Маркер сущности-ракеты |
| `RocketLauncherData` | `IComponentData` (на корабле) | `MaxRockets`, `CurrentRockets`, `RespawnDurationSec`, `RespawnRemaining`, `Shooting`, `Direction (float2)`, `ShootPosition (float2)`. **Механика — респавн по таймеру, НЕ cooldown и НЕ «заряды».** |
| `HomingData` | `IComponentData` (на ракете) | Только `Speed` и `TurnSpeed`. Цель НЕ кэшируется (нет `targetEntity`, нет радиуса поиска). |
| `RocketLaunchEvent` | `IBufferElementData` (singleton-буфер) | `ShooterEntity`, `Position (float2)`, `Direction (float2)` |

#### Системы

| Система | Статус | Поведение |
|---|---|---|
| `EcsRocketLauncherSystem` (ISystem) | новая | Респавн: пока `CurrentRockets < MaxRockets`, тикает `RespawnRemaining`; по достижении 0 — `CurrentRockets += 1` и сброс таймера. Выстрел: при `Shooting && CurrentRockets > 0` — `CurrentRockets -= 1` и добавление `RocketLaunchEvent` в singleton-буфер. `UpdateAfter(EcsLaserSystem)`. |
| `EcsHomingSystem` (ISystem) | новая | Каждый кадр для каждой ракеты ищет **ближайшего** врага (`AsteroidTag`/`UfoBigTag`/`UfoTag`) по `distancesq`, поворачивает направление движения к цели не более чем на `TurnSpeed * dt` (дуга через знак 2D-cross), пишет результат в `MoveData.Direction` и `RotateData.Rotation`. `UpdateBefore(EcsMoveSystem)`. |
| `EcsCollisionHandlerSystem` (ISystem) | правка | Добавлена ветка `Rocket + Enemy` (оба порядка A/B): `MarkDead` ракете и врагу + начисление `ScoreValue` врага. Коллизия с **любым** врагом по пути засчитывается (требование «врежется не в выбранную цель — тоже считается» выполнено через общий `IsEnemy`). |
| `ShootEventProcessorSystem` (SystemBase, мост) | правка | `ProcessRocketEvents()`: вычитывает singleton-буфер `RocketLaunchEvent` и на главном потоке вызывает `EntitiesCatalog.CreateRocket(position, direction)` (создание managed-визуала). |
| `ObservableBridgeSystem` (SystemBase, мост) | правка | Запрос корабля расширен на `RefRO<RocketLauncherData>`; пишет в HUD: `RocketCount`, `RocketRespawnTime`, `IsRocketRespawnTimeVisible (= CurrentRockets < MaxRockets)`. |

#### Визуал, конфиг, HUD, Input

| Артефакт | Реальное состояние |
|---|---|
| `RocketVisual` (MonoBehaviour, + `RocketViewModel`) | View ракеты, спрайт = `_configs.Ship.MainSprite` (уменьшенный спрайт корабля), репорт коллизии через `OnCollision`. |
| `Assets/Media/prefabs/rocket.prefab` | **Создан** (guid `28c0c9f2f5ac4a3abf7019f4838c75fb`), назначен в `GameData.asset → Rocket.Prefab`. |
| `GameData.RocketData` (struct в ScriptableObject) | Поля: `Prefab`, `MaxRockets`, `RespawnDurationSec`, `Speed`, `TurnSpeed`, `LifeTimeSeconds`. **Поля `Score` нет.** В `GameData.asset`: MaxRockets=3, Respawn=10, Speed=10, TurnSpeed=180, LifeTime=5. |
| HUD-строки | В `Main.unity` присутствуют TMP-объекты `'Rockets: 3'` и `'Rocket respawn: 0 sec'`; привязка в `HudVisual` (`_rocketCount`, `_rocketRespawnTime`, видимость respawn-строки по `IsRocketRespawnTimeVisible`). |
| Действие `Rocket` | Добавлено в `player_actions.inputactions` (кнопка **R**, тип Button), регенерирован `PlayerActions.cs`; `PlayerInput.OnRocketAction`; `Game.OnRocket` (по образцу `OnLaser`): пишет `Shooting/Direction/ShootPosition` в `RocketLauncherData` корабля. |
| Корабль | `EntityFactory.CreateShip` добавляет `RocketLauncherData` (дефолты `rocketMax=3`, `rocketRespawnSec=10`, реально прокидываются из конфига через `EntitiesCatalog`). `EntityType.Rocket` добавлен в enum каталога. |
| Singleton | `RocketLaunchEvent` инициализируется в `Application.InitializeEcsSingletons`. |

#### Ключевые проектные решения

- **Поиск ближайшей цели каждый кадр, без кэширования Entity** — цель может быть уничтожена другой системой; покадровый поиск дешёв при десятках врагов и исключает невалидные хэндлы.
- **Переиспользование `MoveData`/`LifeTimeData`** — тороидальный wrap и гарантированное уничтожение ракеты (`LifeTimeSeconds=5`) штатными системами, без дублирования логики.
- **`RotateData.Rotation` как ориентация для визуала** — `GameObjectSyncSystem` уже конвертирует Vector2-направление в угол; отдельный компонент не нужен.
- **Спавн визуала через мост `ShootEventProcessorSystem`** — ECS-система пишет событие в буфер, managed-мост на главном потоке создаёт GameObject через `EntitiesCatalog` (тот же паттерн, что у пуль/лазера).

#### Верификация (что реально проверено инструментами)

| Проверка | Результат |
|---|---|
| Компиляция проекта | ✅ (EditMode-сюита собралась) |
| EditMode тесты (вся сюита) | ✅ **196 / 196 GREEN** (Unity MCP `tests-run`, 7.6 c) |
| `rocket.prefab` назначен в конфиг | ✅ (сверка guid в `GameData.asset`) |
| HUD-строки ракет в сцене | ✅ (`Rockets: 3`, `Rocket respawn: 0 sec` в `Main.unity`) |
| Связность пути spawn→homing→collision→HUD | ✅ (сверено чтением исходников end-to-end) |
| Playmode-скриншот **геймплея** ракеты | ⚠️ НЕ получен — см. open-items |

#### Открытые вопросы / дефекты (честно)

1. **Live-геймплей не верифицирован визуально.** Game View в playmode показывает TitleScreen; старт игры и запуск ракеты (клавиша R), полёт по дуге и попадание требуют **инъекции пользовательского ввода**, чего текущие Unity MCP-инструменты не предоставляют. Это документированный «исключительный случай» → требуется **human validation** живого геймплея (старт игры, нажать R, увидеть дугу/попадание/декремент HUD/респавн).
2. **Качество RED-gate для `EcsHomingSystem`-тестов.** При первичной adversarial-проверке часть homing-тестов проходила и на «сломанной» версии (тавтологичность / незащищённость от no-op для уже выровненных направлений). Тесты зелёные, но их доказательная сила по части «поворот к цели» ниже остальных — кандидат на усиление (старт с заведомо несовпадающего направления, инициализация `RotateData` отлично от `Direction`).
3. **Слой коллизий.** В коде `EcsCollisionHandlerSystem` различение идёт по ECS-тегам (`RocketTag`/`*Tag`), а не по Physics2D-маске. Отдельного физического слоя для ракет не вводилось (ранее ошибочно задокументированное «отдельный слой вне маски лазера» — неверно и удалено).

> **Примечание о scope.** Рабочее дерево содержит полную вертикаль фичи (данные + системы + view + HUD + input + тесты, 18+ изменённых/новых файлов) — это сознательно полный срез, а не только data-слой.

---

### 2. Оценка затрат по токенам

Источник — телеметрия workflow `harness-cycle` + ручная пост-верификация.

| Этап | Токены |
|---|---|
| Workflow `harness-cycle` (21 субагент: понять/план/реализация/верификация/синтез) | **~1 500 000** (subagent_tokens, 846 tool-uses, ~29 мин) |
| Ручная пост-верификация (аудит дерева, прогон тестов через MCP, playmode, переписывание этого документа) | ~120 000 |
| **Итого (порядок)** | **~1.6 млн токенов** |

> Замечание: значительная доля токенов workflow ушла на adversarial-верификацию, которая дала **ложно-негативный** вердикт из-за падения Unity MCP-bridge в момент прогона верификатора (`Response data is null`). Реальное состояние кода оказалось лучше вердикта (196/196 GREEN при работающем MCP).

---

### 3. Исходный промт пользователя (дословно)

```
Сомонаводящиеся ракеты. Нужно сделать новую фичу. Добавим самонаводящиеся
ракеты. У игрока есть одна ракета, которую можно запустить, нажав кнопку R.
Ракета летит по дуге в ближайшую цель. Если ракета по пути врежется не в
выбранную цель, это тоже считается. После запуска ракет включается счётчик
на респавн ракет. Количество ракет у игрока и время респавна должно быть
задано в конфигах. Ракета коллайдится с астероидами и UFO. В качестве визала
ракеты можно использовать уменьшенный спрайт корабля. Инверсионный след можно
сделать из спрайта частиц. Ракета должна быть создана и вписана в текущую
архитектуру ECS + визуал. Количество доступных ракет и время респавна должно
выводиться в HUD-е игрока. Фича должна быть разработана в парадигме TDD, весь
функционал покрыт тестами. Проверка функционала при помощи юнит, интеграционных
тестов и MCP. Human validation в исключительных случаях, когда текущих
инструментов недостаточно и невозможно написать новый функционал для MCP.
```

---
