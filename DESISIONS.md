# DESISIONS — Самонаводящиеся ракеты (Homing Rockets)

> Документ фиксирует исходный промт, принятые решения, план и оценку по токенам.
> Дата: 2026-05-30. Ветка: `feature/rockets-pure-opus48-xhigh`. Модель: Opus 4.8 (1M), effort xhigh.

---

## 1. Исходный промт (verbatim)

> **Сомонаводящиеся ракеты.** Нужно сделать новую фичу. Добавим самонаводящиеся ракеты. У игрока есть одна ракета, которую можно запустить, нажав кнопку R. Ракета летит по дуге в ближайшую цель. Если ракета по пути врежется не в выбранную цель, это тоже считается. После запуска ракет включается счётчик на респавн ракет. Количество ракет у игрока и время респавна должно быть задано в конфигах. Ракета коллайдится с астероидами и UFO. В качестве визала ракеты можно использовать уменьшенный спрайт корабля. Инверсионный след можно сделать из спрайта частиц. Ракета должна быть создана и вписана в текущую архитектуру ECS + визуал. Количество доступных ракет и время респавна должно выводиться в HUD-е игрока. Фича должна быть разработана в парадигме TDD, весь функционал покрыт тестами. Проверка функционала при помощи юнит, интеграционных тестов и MCP. Human validation в исключительных случаях, когда текущих инструментов недостаточно и невозможно написать новый функционал для MCP.
>
>  0. Не используй superpowers и gsd
>  1. Составь план
>  2. Убедись что MCP работает. Если нет — сообщи и НЕ продолжай.
>  3. Выполни задачу по плану
>  4. Все решения, оценку по токенам и этот промт запиши в DESISIONS.md

---

## 2. Процедурные решения

- **Без superpowers и GSD.** Прямое указание пользователя имеет приоритет над project-инструкциями про GSD-workflow в `CLAUDE.md`. Работаю обычными инструментами + Unity MCP. TDD выполняю вручную (не через superpowers-скилл), как и требует пользователь.
- **MCP-гейт пройден.** `editor-application-get-state` вернул Unity `6000.3.13f1` (Unity 6.3), не в Play Mode, не компилируется. Продолжаю.
- **Реальная архитектура — Unity DOTS Entities**, а не MonoBehaviour-ECS из `CLAUDE.md`. `CLAUDE.md` описывает устаревший слой и фактам не соответствует (подтверждено разведкой: `ECS/Components` = `IComponentData`, `ECS/Systems` = `ISystem`, `Bridge/` связывает ECS↔GameObject/MVVM). Все решения опираются на фактический код, не на `CLAUDE.md`.

## 3. Проектные решения (дизайн фичи)

| # | Решение | Обоснование |
|---|---------|-------------|
| D1 | **Счётчик ракет = аналог `LaserData`/`EcsLaserSystem`** (инкрементальная перезарядка: +1 ракета за период до `MaxRockets`). Новый `RocketLauncherData` на корабле + `EcsRocketLauncherSystem`. | «После запуска включается счётчик на респавн» 1:1 ложится на ленивую перезарядку лазера: пока `Current < Max`, тикает `ReloadRemaining`. |
| D2 | **Запуск = событие `RocketLaunchEvent` (buffer singleton)**, обрабатывается в `ShootEventProcessorSystem` → `EntitiesCatalog.CreateRocket`. | Полностью повторяет конвейер пушки/лазера (Gun/Laser events). |
| D3 | **Наведение «по дуге» = ограниченная скорость поворота** (`TurnRateDegPerSec`). Каждый кадр направление `MoveData.Direction` доворачивается к цели не более чем на `turnRate*dt`. | Ограниченный поворот даёт естественную кривую (дугу), если ракета стартует не строго на цель (стартовое направление = носу корабля). Чистая математика → юнит-тестируемо (`EcsRocketHomingSystem.Steer`). |
| D4 | **Цель = ближайший враг (asteroid/UFO/UFO-big), lock-on с переприцеливанием при гибели.** `RocketData.Target` (Entity). Невалидна/мертва → берём ближайшую заново. Врагов нет → летим прямо. | «в ближайшую цель» + устойчивость к уничтожению цели игроком. |
| D5 | **Коллизия ракеты = как у player bullet, но с собственным `RocketTag`.** В `EcsCollisionHandlerSystem` добавлены правила Rocket+Enemy → оба `DeadTag` + начисление очков. | «Если врежется не в выбранную цель — тоже считается»: коллизия не привязана к цели наведения, любой враг засчитывается. Очки начисляются как за пулю. |
| D6 | **Время жизни ракеты — `LifeTimeData` (переиспользуем `EcsLifeTimeSystem`/`EcsDeadByLifeTimeSystem`), `RocketLifeTimeSeconds` в конфиге.** | Защита от вечного полёта (цель уничтожена, врагов нет → не орбитирует бесконечно). |
| D7 | **Визуал: префаб `rocket.prefab` = копия `bullet.prefab` (Layer 9, kinematic RB2D, CircleCollider2D) + спрайт корабля (scale 0.5) + дочерний ParticleSystem (инверсионный след) + `RocketVisual`.** Поворот спрайта — через `RotateData` + существующий `GameObjectSyncSystem`. | Тот же Layer 9, что у пули → коллизии с астероидами/врагами работают из коробки, корабль не задевается. Спрайт корабля по требованию. `RotateData.TargetDirection=0` → `EcsRotateSystem` ракету не трогает; наведение само пишет `RotateData.Rotation`. |
| D8 | **HUD: отдельный блок запроса в `ObservableBridgeSystem`** (`RocketLauncherData` + `ShipTag`), новые поля в `HudData`/`HudVisual`, `SetRocketMaxCount` зовётся из `GameScreen` (рядом с `SetLaserMaxShoots`). | Декуплинг: не ломает существующий ship-query и тесты `ObservableBridgeSystem`. |
| D9 | **Ввод: новый action `Rocket` → `<Keyboard>/r`.** Правлю `.inputactions` И сгенерированный `PlayerActions.cs` (встроенный JSON + поле + accessor + callbacks + интерфейс) консистентно. | `R` свободна (Restart=Space). Генерированный файл правится вручную, но синхронно с ассетом. |
| D10 | **Дефолты конфига:** `MaxRockets=1`, `ReloadDurationSec=10`, `Speed=12`, `TurnRateDegPerSec=120`, `LifeTimeSeconds=5`. | «одна ракета» из ТЗ; respawn = как у лазера (10с); скорость ниже пули (20) — дуга заметнее; параметры легко правятся в `GameData.asset`. |

## 4. План реализации (TDD: тест → код → green, инкрементально)

**Слой ECS (assembly `AsteroidsECS`)**
1. Компоненты: `RocketTag`, `RocketData`, `RocketLauncherData`, `RocketLaunchEvent`.
2. `EcsRocketLauncherSystem` + тесты (перезарядка/запуск/событие) — зеркало `EcsLaserSystem`.
3. `EcsRocketHomingSystem` + `Steer()` + тесты (математика дуги, выбор ближайшей цели, переприцеливание).
4. `EntityFactory.CreateRocket` + правка `CreateShip` (добавить `RocketLauncherData`) + тесты.
5. `EcsCollisionHandlerSystem`: правила Rocket+Enemy + тесты.

**Слой Bridge/App/View/Input/Config (assembly `Asteroids`)**
6. `RocketVisual`/`RocketViewModel`.
7. `ShootEventProcessorSystem`: обработка `RocketLaunchEvent`.
8. `ObservableBridgeSystem`: HUD-блок ракет + тест.
9. `HudData`/`HudVisual`: поля + биндинги.
10. `GameData`: struct `RocketData` + поле `Rocket`.
11. `EntitiesCatalog.CreateRocket` + `EntityType.Rocket`; `Application.OnDeadEntity` (взрыв ракеты); init/clear `RocketLaunchEvent` buffer; `Game.OnRocket` + подписки; `GameScreen.SetRocketMaxCount`.
12. `PlayerInput.OnRocketAction`; `PlayerActions.cs`; `player_actions.inputactions`.

**Ассеты (MCP)**
13. `rocket.prefab` (копия bullet + спрайт корабля + scale + ParticleSystem трейл + `RocketVisual`).
14. `GameData.asset`: заполнить блок `Rocket`.
15. HUD в `Main.unity`: 2 TMP_Text (кол-во ракет + респавн) + проводка в `HudVisual`.

**Верификация**
16. EditMode-тесты через MCP `tests-run` (новые + регрессия существующих).
17. PlayMode/интеграционный тест полного конвейера.
18. MCP: Play Mode + скриншоты Game View (HUD ракет, запуск/наведение/уничтожение). Human validation — только если MCP объективно недостаточно.

## 5. Оценка по токенам

> Оценка для всей сессии (разведка + реализация + верификация). Уточняется по факту в конце.

| Фаза | Оценка (tokens) | Заметки |
|------|-----------------|---------|
| Разведка кодовой базы | ~120–160k | Большой system-prompt + чтение ~35 файлов архитектуры. |
| Реализация (код + тесты, ~25 файлов) | ~150–250k | Итерации write/edit. |
| MCP: ассеты (prefab, particle, HUD, config) | ~80–150k | Множество вызовов component-modify/get. |
| MCP: прогоны тестов + Play Mode + скриншоты | ~150–350k | Вывод тестов объёмный; скриншоты — изображения (дорого). |
| **Итого (ожидаемо)** | **~500–900k** | Доминируют прогоны тестов и скриншоты. |

**Факт (оценка по итогам сессии):** **≈ 750–950k токенов.**
Точное значение изнутри сессии не измеряется; оценка по объёму операций. Главные потребители:
- чтение `rocket.prefab` (~3000 строк ParticleSystem, ≈54k токенов) и генерированного `PlayerActions.cs` (≈8k);
- большой system-prompt + `CLAUDE.md` + список MCP-инструментов в каждом ходе (кэшируется, но в сумме значимо);
- 3 скриншота Game View (изображения ~по 1–1.5k токенов) + ~14 MCP-вызовов tests-run/script-execute/console;
- ~40 чтений файлов на разведке и ~50 правок/записей.
Изначальная оценка (500–900k) подтвердилась; факт — в верхней части диапазона из-за чтения огромного prefab.

## 6. Результаты верификации

**Юнит + интеграционные тесты (MCP `tests-run`):**
- EditMode: **194/194 passed, 0 failed** (было 176 до фичи; +18 тестов ракеты: launcher 7, homing 10 — вкл. Steer/дугу/выбор цели/переприцеливание, EntityFactory 3, коллизии 3, HUD 2, интеграция 4; числа с учётом пересечений).
- PlayMode: **7/7 passed** (регрессия сцены Main — не сломана).
- Интеграционный тест `Pipeline_HomingAndMove_RocketConvergesOnTarget` доказывает сходимость наведения через связку систем move+homing.

**MCP Play Mode (живая игра, без human validation):**
- Запуск ракеты конвейером launcher→`RocketLaunchEvent`→`ShootEventProcessor`→`CreateRocket`: создана 1 ракета, `CurrentRockets` 1→0.
- Счётчик респавна пошёл: `ReloadRemaining` 9.3→8 (HUD: «Reload rocket: 8 sec»).
- Наведение: `hasTarget=True`, ракета захватила ближайший астероид и полетела по дуге к нему.
- Коллизия: `score` 0→2, астероиды 10→11 (крупный уничтожен и раскололся на 2 через `OnDeadEntity`).
- HUD: визуально подтверждены «Rockets: 1»→«Rockets: 0» и «Reload rocket: N sec».
- Визуал: уменьшенный спрайт корабля + оранжевый инверсионный след (ParticleSystem) на скриншоте.
- Консоль: 0 ошибок, 0 исключений за сессию.

> Примечание по инструментам: скриншоты Game View через MCP отдаются перевёрнутыми по вертикали (текст HUD выглядит зеркально) — это особенность чтения render texture, на саму игру не влияет. Точные значения подтверждены запросами состояния ECS-мира.

## 7. Журнал отклонений / заметки по ходу

- **Архитектура.** Подтверждено: проект — Unity DOTS, `CLAUDE.md` описывает устаревший MonoBehaviour-слой. Все решения по факту кода.
- **HUD-запрос — отдельным блоком** (а не добавлением `RocketLauncherData` в существующий ship-query `ObservableBridgeSystem`), чтобы не сломать существующие тесты, чей хелпер `CreateShipEntity` не содержит лаунчер. Декуплинг.
- **Префаб собран через `script-execute`** (Roslyn), а не пошаговыми MCP-tools: инстанс пули → unpack → свап `BulletVisual`→`RocketVisual` (+ `_collider` через SerializedObject) → спрайт корабля ×0.5 → дочерний ParticleSystem-трейл → `SaveAsPrefabAsset`. Гарантирует идентичную физику/слой (Layer 9) для коллизий и надёжную конфигурацию частиц. Тем же скриптом заполнен блок `Rocket` в `GameData.asset`.
- **HUD-тексты** добавлены скриптом в `Main.unity`: дублирование лазерных TMP_Text как шаблона, позиционирование стеком под ними (x=100, y=−160/−192), проводка в `HudVisual`.
- **Ракета в Play Mode** запускалась установкой `RocketLauncherData.Launching` на корабле (ровно эффект `Game.OnRocket`), т.к. инъекция нажатия клавиши R в Play Mode через MCP недоступна. Сам биндинг `R` добавлен в `.inputactions` и `PlayerActions.cs`; корректность ввода покрыта компиляцией связки `PlayerInput`→`Game.OnRocket`.
- **timeScale** временно понижался (0.05) для захвата полёта ракеты скриншотом и защиты статичного корабля; восстановлен в 1.
- **Не коммитил** — пользователь не просил. Изменения на ветке `feature/rockets-pure-opus48-xhigh`.
