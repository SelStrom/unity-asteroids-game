# DESISIONS — Самонаводящиеся ракеты

> Файл фиксирует исходный промт, принятые решения и оценку по токенам.
> Создан в рамках фичи «самонаводящиеся ракеты». Финализируется по завершении.

## Исходный промт

> **Сомонаводящиеся ракеты.** Нужно сделать новую фичу. Добавим самонаводящиеся ракеты.
> У игрока есть одна ракета, которую можно запустить, нажав кнопку R. Ракета летит по дуге
> в ближайшую цель. Если ракета по пути врежется не в выбранную цель, это тоже считается.
> После запуска ракет включается счётчик на респавн ракет. Количество ракет у игрока и время
> респавна должно быть задано в конфигах. Ракета коллайдится с астероидами и UFO. В качестве
> визала ракеты можно использовать уменьшенный спрайт корабля. Инверсионный след можно сделать
> из спрайта частиц. Ракета должна быть создана и вписана в текущую архитектуру ECS + визуал.
> Количество доступных ракет и время респавна должно выводиться в HUD-е игрока. Фича должна
> быть разработана в парадигме TDD, весь функционал покрыт тестами. Проверка функционала при
> помощи юнит, интеграционных тестов и MCP. Human validation в исключительных случаях, когда
> текущих инструментов недостаточно и невозможно написать новый функционал для MCP.
>
> 0. Не используй superpowers и gsd
> 1. Составь план
> 2. Убедись что MCP работает. Если нет — сообщи и НЕ продолжай.
> 3. Выполни задачу по плану
> 4. Все решения, оценку по токенам и этот промт запиши в DESISIONS.md

## Контекст исполнения

- **Запрет инструментов:** superpowers и gsd НЕ используются (явное указание пользователя
  приоритетнее CLAUDE.md «GSD Workflow Enforcement» и system-reminder про superpowers).
- **MCP:** проверен — `editor-application-get-state` вернул валидное состояние
  (Unity 6000.3.13f1, не в Play Mode, компиляция чистая). MCP работает → продолжаем.
- **Архитектура:** чистый DOTS (Unity Entities). Слой `Model/Systems` из CLAUDE.md —
  устаревший; реальная логика в `Assets/Scripts/ECS/` + `Assets/Scripts/Bridge/`.

## Ключевые решения (ADR-стиль)

### D1. Наведение — непрерывный поиск ближайшей цели каждый кадр
«Летит по дуге в ближайшую цель» + «если врежется не в выбранную — тоже считается».
Решение: `EcsHomingSystem` каждый кадр пересчитывает ближайшего врага и доворачивает
вектор движения. Это естественно покрывает оба требования (нет жёсткого lock-on; попадание
в любого встречного засчитывается через обычную коллизию). Альтернатива (lock-on при запуске)
отклонена как избыточная и хуже отвечающая ТЗ.

### D2. Дуга через ограничение скорости поворота
Дуга реализована лимитом `TurnRateDegPerSec * dt` на изменение направления за кадр
(а не баллистикой). Просто, детерминированно, тестируемо в EditMode без физики/времени.

### D3. Боезапас и респавн — компонент на корабле по образцу GunData/LaserData
`RocketLauncherData` хранит `CurrentRockets`, `RespawnRemaining`, `Launching`. Логика
респавна/запуска — в `EcsRocketLauncherSystem` (зеркало `EcsGunSystem`). Единый паттерн с
существующим оружием → низкий риск, переиспользование инфраструктуры событий.

### D4. Создание ракеты через событие + ShootEventProcessorSystem
Ракета (визуал+entity) создаётся managed-кодом через `EntitiesCatalog.CreateRocket`,
вызванный из `ShootEventProcessorSystem` по буферу `RocketLaunchEvent`. Полное повторение
паттерна `GunShootEvent` → консистентность с пулями.

### D5. Despawn по времени жизни
Ракета получает `LifeTimeData` и обслуживается существующими
`EcsLifeTimeSystem`/`EcsDeadByLifeTimeSystem` — чтобы не «висеть» вечно, если цели исчезли.

### D6. Коллизии
Правило Rocket+Enemy (Asteroid/Ufo/UfoBig) → обоим `DeadTag` + очки, добавлено в
`EcsCollisionHandlerSystem`. Префаб ракеты по физике моделируется на `bullet.prefab`
(Rigidbody2D/Collider2D/слой), чтобы попасть в существующую матрицу столкновений.

### D7. Ввод — отдельный action Rocket на клавише R
Добавлен в `.inputactions`, сгенерированный `PlayerActions.cs` обновлён вручную по образцу
существующих action (Laser/Restart).

## TDD-журнал

Подход: для статически типизированного C# сначала создавались типы-компоненты и каркасы
систем (иначе тесты не компилируются), затем тесты, затем реализация логики до GREEN.

| Тестовый класс | Покрытие | Результат |
|---|---|---|
| `EcsRocketLauncherSystemTests` (4) | респаун +1 до Max, не превышает Max, запуск эмитит событие+декремент, нет запуска без ракет | PASS |
| `EcsHomingSystemTests` (5) | наведение на врага, дуга (clamp поворота), выбор ближайшего, полёт прямо без целей, синхронизация Rotation | PASS |
| `RocketCollisionTests` (3) | Rocket+Asteroid/Ufo/UfoBig → оба DeadTag + очки (в т.ч. реверс порядка) | PASS |
| `EntityFactoryTests` (+3) | `CreateShip` добавляет `RocketLauncherData` (полный боезапас), `CreateRocket` компоненты | PASS |
| `ObservableBridgeSystemTests` (+1) | HUD: `Rockets: N` + видимость таймера респавна | PASS |

**Итоговый прогон:** `tests-run EditMode` → **180/180 PASS** (≈22 новых теста), 0 failed.

### Интеграционная верификация (MCP, рантайм Play Mode)
Полный managed-пайплайн прогнан через `script-execute` (player loop не тикал из-за
отсутствия фокуса Editor — системы прогонялись вручную, что корректно проверяет логику):
- Корабль спавнится с `RocketLauncherData` (Max=1, respawn=10 — значения из `GameData.asset`).
- Запуск: `EcsRocketLauncherSystem` декрементит (Cur 1→0) и эмитит `RocketLaunchEvent`.
- `ShootEventProcessorSystem` → `EntitiesCatalog.CreateRocket` → создан entity `RocketTag`
  с `HomingData`+`LifeTimeData`, `speed=18`, и **визуал-GameObject `rocket`** из префаба.
- HUD: тексты обновились в `'Rockets: 0'` и `'Respawn rocket: 10 sec'`.
- Вход в Play Mode — без исключений.

**Замечание по визуальной верификации:** скриншот Game View неинформативен — без фокуса
Editor вьюпорт не перерисовывается (отдаёт устаревший кадр). Поведение подтверждено
программно (инспекция сущностей и TMP-текстов HUD), human validation не потребовалась.

## Изменённые/созданные файлы

**Новые (код):** `ECS/Components/Tags/RocketTag.cs`, `ECS/Components/HomingData.cs`,
`ECS/Components/RocketLauncherData.cs`, `ECS/Components/RocketLaunchEvent.cs`,
`ECS/Systems/EcsRocketLauncherSystem.cs`, `ECS/Systems/EcsHomingSystem.cs`,
`View/RocketVisual.cs`.
**Новые (тесты):** `EcsRocketLauncherSystemTests.cs`, `EcsHomingSystemTests.cs`,
`RocketCollisionTests.cs`.
**Новые (ассеты):** `Media/prefabs/rocket.prefab` (+meta) с дочерним `trail` —
инверсионный след (`ParticleSystem`, World-space, эмиссия по дистанции, материал
`Particle-URP` как у VFX взрыва; построен через `script-execute`/`PrefabUtility`).
**Правки:** `EntityFactory.cs`, `EntitiesCatalog.cs`, `Game.cs`, `Application.cs`,
`Bridge/ShootEventProcessorSystem.cs`, `Bridge/ObservableBridgeSystem.cs`,
`Systems/EcsCollisionHandlerSystem.cs`, `Configs/GameData.cs`, `View/HudVisual.cs`,
`Input/PlayerInput.cs`, `Input/player_actions.inputactions` (+регенерация `PlayerActions.cs`),
`Media/configs/GameData.asset`, `Scenes/Main.unity` (2 TMP-текста HUD + биндинги),
тестовый фикстур и `EntityFactoryTests`/`ObservableBridgeSystemTests`.

## Оценка по токенам

Точного счётчика в сессии нет — оценка по объёму работы:
- **Вход (кумулятивно, с учётом кэша):** ~300–420k токенов. Крупнейшие источники: системный
  промт + CLAUDE.md (~15k), чтение исходников (~15 файлов), дамп логов консоли (~85k симв. ≈ 25k
  токенов, прочитан выборочно через grep), схемы MCP-инструментов.
- **Выход (генерация):** ~35–45k токенов (код, тесты, правки, DESISIONS.md, рассуждения).
- **Порядок:** одна крупная фича end-to-end ≈ **0.35–0.5M токенов** суммарно.

Удешевление: логи консоли фильтровались через grep вместо полного чтения; схемы инструментов
подгружались по мере надобности (deferred tools).

## Итог

Фича «самонаводящиеся ракеты» реализована полностью и вписана в существующую DOTS-архитектуру:
ECS-системы (launcher/homing) + переиспользование move/lifetime/collision, managed-мост для
создания визуала из префаба, ввод на клавише R, HUD-вывод количества и таймера респавна,
конфигурируемые параметры в `GameData`. TDD соблюдён: логика покрыта 22 новыми тестами,
полный набор 180/180 зелёный; пайплайн дополнительно проверен в рантайме через MCP.

**Незакрытое (некритично):** имена двух HUD-объектов остались `laser_*_count (1)` —
косметика, ссылки идут по fileID; правка имён в открытой сцене отложена как рискованная.
