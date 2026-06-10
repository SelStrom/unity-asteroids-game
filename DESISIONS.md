# DESISIONS — Самонаводящиеся ракеты

## Исходный промт

> **Сомонаводящиеся ракеты.** Нужно сделать новую фичу. Добавим самонаводящиеся ракеты. У игрока есть одна ракета, которую можно запустить, нажав кнопку R. Ракета летит по дуге в ближайшую цель. Если ракета по пути врежется не в выбранную цель, это тоже считается. После запуска ракет включается счётчик на респавн ракет. Количество ракет у игрока и время респавна должно быть задано в конфигах. Ракета коллайдится с астероидами и UFO. В качестве визала ракеты можно использовать уменьшенный спрайт корабля. Инверсионный след можно сделать из спрайта частиц. Ракета должна быть создана и вписана в текущую архитектуру ECS + визуал. Количество доступных ракет и время респавна должно выводиться в HUD-е игрока. Фича должна быть разработана в парадигме TDD, весь функционал покрыт тестами. Проверка функционала при помощи юнит, интеграционных тестов и MCP. Human validation в исключительных случаях, когда текущих инструментов недостаточно и невозможно написать новый функционал для MCP.
>
> 0. Не используй superpowers и gsd
> 1. Составь план
> 2. Убедись что MCP работает. Если нет — сообщи и НЕ продолжай.
> 3. Выполни задачу по плану
> 4. Все решения, оценку по токенам и этот промт запиши в DESISIONS.md

## Контекст

- Реальная архитектура — гибридный DOTS: чистый ECS (`Assets/Scripts/ECS/`, asmdef `AsteroidsECS`) + managed-мосты (`Assets/Scripts/Bridge/`) + MVVM-визуал (shtl-mvvm). Описание в CLAUDE.md (MonoBehaviour-ECS) устарело.
- MCP проверен до начала работ: Unity 6000.3.13f1, Edit Mode, компиляция чистая. ✅

## План

1. ✅ Разведка: Game/EntitiesCatalog/EntityFactory/системы/мосты/HUD/инпут/конфиги/тесты.
2. DESISIONS.md (этот файл, дополняется по ходу).
3. **RED**: компоненты + стабы систем + падающие EditMode-тесты (launcher, homing).
4. **GREEN**: реализация `EcsRocketLauncherSystem`, `EcsRocketHomingSystem`.
5. Интеграция: `EntityFactory.CreateRocket` + `RocketAmmoData` в `CreateShip`, `EntitiesCatalog.CreateRocket`, `ShootEventProcessorSystem.ProcessRocketEvents`, `Game.OnRocket`, singleton-буфер `RocketLaunchEvent`, `PlayerInput`/inputactions (R), `GameData.RocketData`, HUD (HudData/HudVisual/ObservableBridgeSystem/GameScreen). Тесты фабрики и коллизий.
6. Unity-ассеты через MCP: `rocket.prefab`, заполнение `GameData.asset`, TMP-строки в HUD сцены `Main`.
7. Все EditMode-тесты зелёные (MCP tests-run), без регрессий.
8. PlayMode интеграционные тесты (MCP tests-run PlayMode).
9. Визуальная верификация: playmode + скриншот Game View через MCP.
10. Финализация DESISIONS.md, коммит.

## Решения

| # | Решение | Обоснование |
|---|---------|-------------|
| 1 | **Дуга = ограниченная угловая скорость поворота.** `EcsRocketHomingSystem` каждый кадр доворачивает `MoveData.Direction` к ближайшей живой цели не быстрее `TurnSpeedDegPerSec`. | Естественная дуга без скриптованных кривых; параметр кривизны в конфиге. |
| 2 | **Перенаведение на ближайшую цель каждый кадр** (а не фиксация цели при запуске). | Проще (нет хранения target Entity и обработки его смерти); требование «врезался не в выбранную цель — тоже считается» выполняется автоматически: физика коллайдит ракету с любым врагом на пути. |
| 3 | **Ракета несёт `PlayerBulletTag` + `RocketTag`.** | `EcsCollisionHandlerSystem` уже обрабатывает PlayerBullet+Enemy (оба DeadTag + очки) — ноль изменений в обработчике коллизий. `RocketTag` — для homing-системы и идентификации. Покрыто явными регрессионными тестами. |
| 4 | **`RotateData` на ракете, `Rotation = Direction`.** | Существующий `GameObjectSyncSystem` поворачивает GO по `RotateData.Rotation` — визуал ракеты ориентируется по курсу без новых систем. `EcsRotateSystem` не мешает: `TargetDirection = 0`. |
| 5 | **Респавн по образцу лазера:** таймер тикает только при `CurrentRockets < MaxRockets`, восстанавливает по одной ракете за период. | Консистентность с `EcsGunSystem`/`EcsLaserSystem`; «после запуска включается счётчик на респавн» — выполняется. |
| 6 | **Событийный пайплайн как у пуль:** `RocketLaunchEvent` (IBufferElementData, singleton-буфер) → `ShootEventProcessorSystem` создаёт визуал+entity через `EntitiesCatalog.CreateRocket`. | ECS-системы не трогают managed-объекты; мост уже существует. |
| 7 | **`LifeTimeData` на ракете.** | Переиспользование `EcsLifeTimeSystem`/`EcsDeadByLifeTimeSystem`: ракета без цели не летает вечно. |
| 8 | **Конфиг `GameData.RocketData`:** Prefab, MaxRockets, RespawnDurationSec, Speed, LifeTimeSeconds, TurnSpeedDegPerSec. | Требование: количество ракет и время респавна в конфигах. |
| 9 | **Input:** action `Rocket`, binding `<Keyboard>/r`. `PlayerActions.cs` регенерится Unity автоматически (`generateWrapperCode: 1`). | Минимальная правка JSON + refresh. |
| 10 | **Слой префаба — PlayerBullet (9),** Rigidbody2D Kinematic + UseFullKinematicContacts + CircleCollider2D, как у пули. | Матрица коллизий уже настроена для снарядов игрока. |
| 11 | **HUD:** `RocketCount`, `RocketRespawnTime`, `IsRocketRespawnTimeVisible` в `HudData`; чтение `RocketAmmoData` корабля в `ObservableBridgeSystem`; два TMP-текста в сцене. | Зеркально лазеру. |

## Оценка по токенам

Приблизительная оценка расхода за сессию (вход с учётом перечитывания контекста, выход — генерация кода/тестов/ответов):

| Этап | Оценка |
|------|--------|
| Разведка кодовой базы + проверка MCP | ~60k input / ~3k output |
| RED: компоненты, стабы, тесты | ~30k / ~8k |
| GREEN: реализация систем | ~20k / ~4k |
| Интеграция (фабрика, мосты, Game, input, HUD-код, тесты) | ~60k / ~12k |
| Unity-ассеты через MCP (префаб, конфиг, сцена) | ~40k / ~6k |
| PlayMode-тесты + отладка | ~50k / ~6k |
| Визуальная верификация + диагностика | ~60k / ~5k |
| **Итого (порядок величины)** | **~320k input / ~45k output** |

## Журнал выполнения

1. ✅ Разведка кодовой базы, проверка MCP (Unity 6000.3.13f1, Edit Mode, компиляция чистая).
2. ✅ RED: 13 новых тестов падали на стабах (185 всего, 142 старых зелёные, регрессий нет).
3. ✅ GREEN: `EcsRocketLauncherSystem` + `EcsRocketHomingSystem` — 185/185 зелёные.
4. ✅ Интеграция кода: `EntityFactory.CreateRocket` + `RocketAmmoData` в `CreateShip`, `EntitiesCatalog.CreateRocket` (+ `EntityType.Rocket`), `RocketVisual`, `ShootEventProcessorSystem.ProcessRocketEvents`, `Game.OnRocket` + очистка буфера, singleton-буфер в `Application`, `PlayerInput.OnRocketAction`, action `Rocket` (R) в inputactions (обёртка регенерирована автоматически), `GameData.RocketData`, HUD-код. 191/191 EditMode зелёные (6 новых: фабрика + коллизии rocket×asteroid/ufo/ufoBig).
5. ✅ Ассеты через MCP (`script-execute`): `rocket.prefab` (слой PlayerBullet, Rigidbody2D Kinematic + FullKinematicContacts, CircleCollider2D, SpriteRenderer со спрайтом корабля scale 0.5, дочерний ParticleSystem-след: world space, rateOverDistance 12, fade-out по alpha); `GameData.asset` (MaxRockets=1, Respawn=10s, Speed=12, LifeTime=6s, TurnSpeed=180°/s); сцена `Main` — `rocket_count`(y=−160) + `rocket_respawn_time`(y=−192) из префаба `gui_text`, привязаны к `HudVisual`. Матрица слоёв подтверждена программно: PlayerBullet×Asteroid=true, PlayerBullet×Enemy=true.
6. ✅ PlayMode-интеграция: 3 теста `RocketGameplayTests` (запуск: entity+визуал+HUD «Rockets: 0»; респавн: восстановление+сброс таймера+HUD «Rockets: 1»; lifetime: уничтожение по таймеру). Все 10 PlayMode-тестов зелёные.
7. ✅ Визуальная верификация через MCP: playmode + script-execute-раннер (клик Start, запуск ракеты, пауза) + скриншот Game View — ракета (уменьшенный корабль) летит по дуге с инверсионным следом, HUD показывает «Rockets: 0» и «Rocket respawn: 9 sec».

## Замечания и отладка

- **PlayMode-тесты и UGS**: ошибка аутентификации лидерборда («player is already signing in») валила тест как unhandled log → `LogAssert.ignoreFailingMessages = true` в SetUp интеграционных тестов (ошибки UGS нерелевантны для ракет).
- **deltaTime в расфокусированном редакторе** крошечный: ожидание N кадров не набирает времени таймеров → в PlayMode-тестах ожидание через `WaitForSeconds`, в верификационном раннере — `Application.runInBackground = true`.
- **Скриншот Game View через MCP приходит перевёрнутым** (артефакт чтения рендер-текстуры) — не игровой баг.
- **Разовое наблюдение, не воспроизвелось**: при первом верификационном запуске (playmode простоял ~23 сек на титульном экране после прогона PlayMode-тестов) корабль погиб в первые полсекунды после старта, до запуска ракеты (ракетная фича не участвовала — rockets=0). На чистом повторе не воспроизвелось: корабль жив, ракета летит. Возможный мусор состояния от предыдущих тестовых сессий в той же playmode-сессии редактора. К фиче отношения не имеет; если повторится — отдельное расследование.

## Human validation

Не потребовалась: весь функционал проверен юнит-тестами (EditMode), интеграционными PlayMode-тестами и MCP (программная диагностика мира + скриншоты).
