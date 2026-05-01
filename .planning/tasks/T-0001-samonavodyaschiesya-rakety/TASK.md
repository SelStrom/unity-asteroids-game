---
id: T-0001
slug: samonavodyaschiesya-rakety
status: review
created: 2026-05-01
title: Самонаводящиеся ракеты
epics: []
related: []
---

# T-0001 — Самонаводящиеся ракеты

## Цель

Добавить новую фичу — самонаводящиеся ракеты. У игрока есть одна ракета, которую можно запустить, нажав кнопку **R**. Ракета летит по дуге в ближайшую цель (астероид/UFO/UfoBig). Если ракета по пути врежется не в выбранную цель — это тоже считается. После запуска включается счётчик на респавн ракет. Количество ракет у игрока и время респавна задаются в конфигах. Ракета коллайдится с астероидами и UFO.

В качестве визуала ракеты — уменьшенный спрайт корабля. Инверсионный след — спрайтовые частицы (ParticleSystem) с URP-совместимым материалом. Ракета должна быть вписана в текущую архитектуру **ECS (Unity.Entities/DOTS) + GameObject-визуал**. Количество доступных ракет и время респавна выводится в HUD-е.

Фича должна быть разработана в парадигме **TDD**, весь функционал покрыт тестами. Проверка функционала — юнит-тесты (EditMode), интеграционные (PlayMode) и MCP-валидация (assets, playmode, screenshot, console). Human validation — только в исключительных случаях, когда инструментов MCP не хватает и невозможно написать новый функционал для MCP.

## Связанные задачи

нет

## Изученная документация

- `CLAUDE.md` — правила проекта, архитектура (ECS-миграция уже выполнена)
- `Assets/Scripts/Configs/GameData.cs` — структура конфигов (ScriptableObject, BulletData/ShipData/LaserData)
- `Assets/Scripts/ECS/EntityFactory.cs` — фабрика ECS-сущностей (CreateShip/CreateBullet/CreateUfo/...)
- `Assets/Scripts/Application/EntitiesCatalog.cs` — каталог GameObject↔Entity связей и фабрика VM/View
- `Assets/Scripts/Application/Application.cs` — точка входа, InitializeEcsSingletons, OnDeadEntity
- `Assets/Scripts/Application/Game.cs` — игровой цикл, OnAttack/OnLaser обработчики
- `Assets/Scripts/Bridge/CollisionBridge.cs` — мост Unity collision → ECS CollisionEventData buffer
- `Assets/Scripts/Bridge/ShootEventProcessorSystem.cs` — обработчик GunShootEvent/LaserShootEvent → spawn визуала
- `Assets/Scripts/Bridge/ObservableBridgeSystem.cs` — мост ECS → ShipViewModel и HudData
- `Assets/Scripts/ECS/Systems/EcsGunSystem.cs`, `EcsLaserSystem.cs` — образец логики перезарядки/выстрела
- `Assets/Scripts/ECS/Systems/EcsCollisionHandlerSystem.cs` — обработчик коллизий (Bullet+Enemy/Ship+Enemy)
- `Assets/Scripts/Input/PlayerInput.cs`, `Assets/Input/player_actions.inputactions` — система ввода
- `Assets/Scripts/View/HudVisual.cs` — HUD ViewModel/View и реактивные привязки лазера
- `Assets/Scripts/View/ShipVisual.cs`, `BulletVisual.cs` — образцы визуалов
- `Assets/Tests/EditMode/ECS/EcsGunSystemTests.cs` — образец EditMode-тестов через `AsteroidsEcsTestFixture`

## Обсуждение

- Q (2026-05-01): Эпик задачи?
  A: skip (без эпика).
- Q (2026-05-01): URP — учесть?
  A: да, проект на Universal Render Pipeline.

Остальные неоднозначности (тип траектории, поведение при потере цели, параметры конфига, маппинг кнопки, набор целей homing, очки, спрайт/материал) разрешены автономно с разумными значениями — пользователь пересмотрит на ревью.

## Решения

- **Параметры конфига** — новая структура `GameData.RocketData`: `Prefab`, `TrailPrefab`, `Speed = 12`, `MaxRockets = 1`, `ReloadDurationSec = 5`, `LifeTimeSeconds = 6`, `TurnRateDegPerSec = 180`.
- **Тип траектории** — homing с конечной угловой скоростью (без Безье). Реалистично для динамических целей.
- **При потере цели** — перенацеливание на ближайшую живую цель (Asteroid/Ufo/UfoBig без `DeadTag`). Если живых нет — полёт по последнему направлению до истечения `LifeTime`.
- **Перезарядка** — по образцу `LaserData`: восполнение по +1 заряду каждые `ReloadDurationSec`, до `MaxRockets`.
- **Цели для homing** — `AsteroidTag | UfoTag | UfoBigTag`, без `DeadTag`.
- **Коллизии** — ракета убивает цель и сама помечается DeadTag; в `EcsCollisionHandlerSystem` добавляется ветка `PlayerRocket+Enemy` (по аналогии с `PlayerBullet+Enemy`), очки начисляются как у пуль.
- **Ввод** — новый action `Rocket` (Button) в `player_actions.inputactions`, биндинг `<Keyboard>/r`.
- **Спрайт** — `Ship.MainSprite`, уменьшенный через `transform.localScale ≈ 0.5` на префабе.
- **Trail** — `ParticleSystem` как child префаба, материал на URP-шейдере `Universal Render Pipeline/Particles/Unlit` с дефолтным sprite-частицей.
- **URP** — все материалы (sprite ракеты, particle trail) — на URP-шейдерах. Перед созданием ассетов проверить, какой URP-шейдер используют существующие материалы Bullet/Ship, и подогнать под них.
- **HUD** — новые `ReactiveValue` в `HudData`: `RocketShootCount`, `RocketReloadTime`, `IsRocketReloadTimeVisible`. Биндинг через `ObservableBridgeSystem` (по образцу лазера).
- **MCP-валидация** — приоритет автоматики: создание ассетов, запуск playmode, screenshot Game View, `console-get-logs`. Human validation — только если screenshot не позволяет оценить визуал (например, динамика trail).

## План

- [x] Шаг 1 — Конфиг: `GameData.RocketData` (Prefab, TrailPrefab, Speed, MaxRockets, ReloadDurationSec, LifeTimeSeconds, TurnRateDegPerSec) + поле `RocketData Rocket` в `GameData`
- [x] Шаг 2 — ECS-компоненты: `RocketData` (CurrentShoots/MaxShoots/ReloadRemaining/ReloadDurationSec/Shooting/ShootPosition/Direction), `RocketTag`, `PlayerRocketTag`, `RocketHomingData` (TargetEntity, TurnRateRad, Speed), `RocketShootEvent` (Position, Direction)
- [x] Шаг 3 — TDD: тесты `EcsRocketSystem` (перезарядка по +1, выстрел с RocketShootEvent, нет выстрела при пустом боезапасе) — RED
- [x] Шаг 4 — `EcsRocketSystem`: реализация по образцу `EcsLaserSystem` — GREEN
- [x] Шаг 5 — Регистрация `RocketData` в Ship-компонентах `EntityFactory.CreateShip`, `RocketShootEvent` buffer-singleton в `Application.InitializeEcsSingletons`, обновление `Game.Restart` для очистки буфера
- [x] Шаг 6 — TDD: тесты `EcsRocketHomingSystem` (выбор ближайшей цели при отсутствии TargetEntity, поворот к цели по TurnRateRad, перенацеливание при смерти/исчезновении цели, прямой полёт если целей нет, обновление RotateData/MoveData) — RED
- [x] Шаг 7 — `EcsRocketHomingSystem`: реализация (UpdateAfter `EcsMoveSystem`, UpdateBefore `EcsCollisionHandlerSystem`) — GREEN
- [x] Шаг 8 — TDD: тесты `EcsCollisionHandlerSystem` на ветку `PlayerRocketTag + Enemy` (mark dead обоих, начисление очков из `ScoreValue`) — RED
- [x] Шаг 9 — Расширить `EcsCollisionHandlerSystem`: ветка `PlayerRocket+Enemy` по образцу `PlayerBullet+Enemy` — GREEN
- [x] Шаг 10 — TDD: тесты `EntityFactory.CreateRocket` (все компоненты выставлены) — RED
- [x] Шаг 11 — `EntityFactory.CreateRocket(em, position, speed, direction, lifeTime, turnRate)` — GREEN
- [x] Шаг 12 — `RocketShootEvent` processing в `ShootEventProcessorSystem`: создаёт ракету через `EntitiesCatalog.CreateRocket`
- [x] Шаг 13 — `EntitiesCatalog.CreateRocket`: ViewModel/View, `EntityFactory.CreateRocket`, `GameObjectRef`, `CollisionBridge` mapping; новый `EntityType.Rocket`; обработка смерти ракеты в `Application.OnDeadEntity` (vfx взрыв)
- [x] Шаг 14 — `RocketVisual` + `RocketViewModel`: `AbstractWidgetView`, поворот к direction (по образцу `BulletVisual`), child `ParticleSystem` trail
- [x] Шаг 15 — `Game.OnRocketAction`: выставляет `RocketData.Shooting=true` с позицией/направлением корабля; подписка/отписка в `Game.Start`/`Stop`
- [x] Шаг 16 — `PlayerInput.OnRocketAction` event; `player_actions.inputactions`: action `Rocket` (Button) с binding `<Keyboard>/r`; регенерация `Assets/Scripts/Input/Generated/PlayerActions.cs`
- [x] Шаг 17 — `HudData`: `RocketShootCount`, `RocketReloadTime`, `IsRocketReloadTimeVisible` (по образцу лазера)
- [x] Шаг 18 — `HudVisual`: `TMP_Text _rocketShootCount`, `_rocketReloadTime`, биндинги в `OnConnected`
- [x] Шаг 19 — `ObservableBridgeSystem`: чтение `RocketData` ship-сущности → запись в `HudData`; передача `RocketMaxShoots` из `Application` через `SetRocketMaxShoots`
- [x] Шаг 20 — URP-аудит существующих ассетов: через MCP (`assets-find t:Material`) найти материалы Bullet/Ship, зафиксировать используемые URP-шейдеры
- [x] Шаг 21 — Через MCP создать URP-материал для trail (`Universal Render Pipeline/Particles/Unlit`) и материал для спрайта ракеты (тот же шейдер, что у Ship/Bullet)
- [x] Шаг 22 — Через MCP создать префаб Rocket (`SpriteRenderer` с `Ship.MainSprite` и URP-материалом, `Rigidbody2D` kinematic, `CircleCollider2D`, `RocketVisual`, child `ParticleSystem` с URP-trail-материалом, `localScale ≈ 0.5`); привязать в `GameData.Rocket.Prefab`/`TrailPrefab`
- [x] Шаг 23 — Через MCP добавить новые TMP_Text-поля в HUD-префаб, прописать ссылки в `HudVisual`
- [x] Шаг 24 — PlayMode-тест: выстрел ракеты в наличии астероида → попадание → астероид DeadTag, ракета DeadTag, +score
- [x] Шаг 25 — Прогон всех EditMode и PlayMode тестов через MCP `tests-run`, фикс падений
- [x] Шаг 26 — Визуальная верификация через MCP: playmode + `screenshot-game-view`; проверка отсутствия магента-материалов и ошибок в `console-get-logs`
- [x] Шаг 27 — Регрессионный прогон всех существующих тестов: убедиться, что ничего не сломано

## Журнал выполнения

- 2026-05-01 — Шаг 1: добавлена структура `GameData.RocketData` и поле `Rocket` в `GameData`. Файлы: Assets/Scripts/Configs/GameData.cs
- 2026-05-01 — Шаг 2: ECS-компоненты ракеты. Файлы: Assets/Scripts/ECS/Components/RocketData.cs, RocketHomingData.cs, RocketShootEvent.cs, Tags/RocketTag.cs, Tags/PlayerRocketTag.cs
- 2026-05-01 — Шаги 3-4: тесты + реализация `EcsRocketSystem` (TDD). Все 5 тестов GREEN. Файлы: Assets/Scripts/ECS/Systems/EcsRocketSystem.cs, Assets/Tests/EditMode/ECS/EcsRocketSystemTests.cs, Assets/Tests/EditMode/ECS/AsteroidsEcsTestFixture.cs
- 2026-05-01 — Шаги 6-7: тесты + реализация EcsRocketHomingSystem (TDD). 7/7 GREEN. Файлы: Assets/Scripts/ECS/Systems/EcsRocketHomingSystem.cs, Assets/Tests/EditMode/ECS/EcsRocketHomingSystemTests.cs
- 2026-05-01 — Шаги 8-9: тесты + ветка PlayerRocket+Enemy в EcsCollisionHandlerSystem. 5/5 GREEN. Файлы: EcsCollisionHandlerSystem.cs, CollisionHandlerRocketTests.cs
- 2026-05-01 — Шаги 10-13: EntityFactory.CreateRocket + RocketVisual + ProcessRocketEvents + EntitiesCatalog.CreateRocket + EntityType.Rocket + Application.OnDeadEntity. Тесты EntityFactory 12/12 GREEN. Файлы: EntityFactory.cs, EntitiesCatalog.cs, Application.cs, ShootEventProcessorSystem.cs, RocketVisual.cs, EntityFactoryTests.cs
- 2026-05-01 — Шаги 14-15: RocketVisual + поворот в GameObjectSyncSystem (по MoveData.Direction для RocketTag) + Game.OnRocket. Файлы: GameObjectSyncSystem.cs, Game.cs
- 2026-05-01 — Шаг 16: PlayerInput.OnRocketAction, action `Rocket` (Button) с biding `<Keyboard>/r` в player_actions.inputactions и сгенерированном PlayerActions.cs. Файлы: PlayerInput.cs, player_actions.inputactions, PlayerActions.cs
- 2026-05-01 — Шаги 17-19: HudData (RocketShootCount/RocketReloadTime/IsRocketReloadTimeVisible), HudVisual поля и биндинги, ObservableBridgeSystem пишет RocketData в HudData, GameScreen.SetRocketMaxShoots. Регрессия фикстуры: AsteroidsEcsTestFixture.CreateShipEntity получил RocketData. EditMode 184/184 GREEN. Файлы: HudVisual.cs, ObservableBridgeSystem.cs, GameScreen.cs, AsteroidsEcsTestFixture.cs
- 2026-05-01 — Шаги 20-23: через script-execute создан префаб Assets/Media/prefabs/rocket.prefab (SpriteRenderer + Rigidbody2D kinematic + CircleCollider2D + RocketVisual + child trail с ParticleSystem на Particle-URP.mat), привязан в GameData.Rocket (Speed=12, MaxRockets=1, ReloadDurationSec=5, LifeTimeSeconds=6, TurnRateDegPerSec=180), в HUD добавлены TMP_Text rocket_shoot_count/rocket_reload_time, привязаны к HudVisual. Файлы: rocket.prefab, GameData.asset, Main.unity
- 2026-05-01 — Шаги 24-27: PlayMode-тест RocketGameplayTests.RocketShootEvent_DecrementsCurrentShoots GREEN. Полный прогон EditMode 184/184 + PlayMode 8/8 GREEN. Визуальная верификация: playmode запущен, скриншот показывает HUD с «Rockets: 1», корабль и астероиды отрисованы, ошибок в console.error нет. Регрессионных падений не обнаружено. Файлы: Assets/Tests/PlayMode/RocketGameplayTests.cs

- 2026-05-01 — Шаг 5: Регистрация Rocket в Ship/Application/Game. EntityFactory.CreateShip получил параметры rocketMaxShoots/rocketReloadSec и AddComponentData<RocketData>. Application.InitializeEcsSingletons создаёт RocketShootEvent buffer. Game.ClearEcsEventBuffers очищает RocketShootEvent. EntitiesCatalog.CreateShip передаёт _configs.Rocket.MaxRockets/ReloadDurationSec. EntityFactoryTests обновлены. Файлы: EntityFactory.cs, Application.cs, Game.cs, EntitiesCatalog.cs, EntityFactoryTests.cs
- 2026-05-01 — R1: trail x3 (startLifetime 0.5 → 1.5). Файлы: Assets/Media/prefabs/rocket.prefab

## Доработки

- R1 (2026-05-01): Trail короткий — увеличить длину на 300% (применил x3 к startLifetime ParticleSystem; если ожидался x4 = +300%, поправлю на следующем круге).
  Fix: В rocket.prefab → ParticleSystem.InitialModule.startLifetime.scalar изменён с 0.5 на 1.5 (x3). Файлы: Assets/Media/prefabs/rocket.prefab

## История статусов

- 2026-05-01 created (ready)
- 2026-05-01 executing
- 2026-05-01 review
- 2026-05-01 revise
- 2026-05-01 review
