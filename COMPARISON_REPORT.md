# Сравнительный отчёт: GSD vs Pure Claude vs Superpowers

Промпт.

Есть 3 ветки в которых реализовывалась одна и та же фича с одним и тем же промптом с одной и той же моделью opus. В ветке feature/rockets-pure-claude использовался чистый claude, в feature/rockets — фреймворк GSD, в feature/rockets-superpowers — плагин Superpowers (subagent-driven development). Моя цель понять, кто справился лучше. Сравни решения в этих ветках на качество, целостность и расширяемость. Дай подробный отчет по принятым решениям при имплементации кода и количеству проблем, которые были решены во всех случаях. Так же для ясности дай сравнительную характеристику скорости разработки. Составь отчет.

## Контекст

Одна и та же фича (самонаводящиеся ракеты) реализована тремя подходами с одной моделью (Claude Opus 4.6, 1M context):

| Параметр | GSD (`feature/rockets`) | Pure Claude (`feature/rockets-pure-claude`) | Superpowers (`feature/rockets-superpowers`) |
|---|---|---|---|
| Фреймворк | GSD workflow (discuss→plan→execute) | Прямая разработка без фреймворка | Superpowers plugin (brainstorm→plan→subagent-execute) |
| Модель | Claude Opus 4.6 | Claude Opus 4.6 | Claude Opus 4.6 |
| Промпт | Идентичный | Идентичный | Идентичный |

---

## 1. Скорость разработки

| Метрика | GSD | Pure Claude | Superpowers |
|---|---|---|---|
| Коммитов на фичу ракет | 66 (фазы 10-17) | 1 | 31 |
| Коммитов всего (включая миграцию) | 315 | 1 (+ 4 от предыдущей миграции) | 287 (включая миграцию) |
| Docs-коммитов | 174 (55%) | 0 | 3 (spec + plan + report) |
| Fix-коммитов на ракеты | 6 | 0 (3 бага исправлены до коммита) | 8 |
| Время на ракеты (wall clock) | ~5 часов (21:19 → 02:05) | ~1 сессия (~50 минут) | ~2 часа (15:18 → 17:16) |
| Дней работы (всего с миграцией) | 6 дней | 1 сессия | 1 сессия (2 часа) |

**Вывод:** Pure Claude — самый быстрый (~50 мин). Superpowers — средний (~2 часа, ~2.5x медленнее Pure Claude). GSD — самый медленный (~5 часов, ~6x медленнее Pure Claude). Superpowers потратил время на brainstorm → spec → plan (20 мин) + subagent dispatch overhead, но значительно меньше чем GSD на документацию.

---

## 2. Архитектурные решения

### 2.1 Нейминг

| Аспект | GSD | Pure Claude | Superpowers |
|---|---|---|---|
| Prefix | `Rocket*` | `Missile*` | `Missile*` |
| Ammo component | `RocketAmmoData` | `MissileData` | `MissileData` |
| Target/Homing | `RocketTargetData` | `HomingData` | `HomingData` |
| Guidance system | `EcsRocketGuidanceSystem` | `EcsHomingSystem` | `EcsMissileNavigationSystem` |
| Ammo system | `EcsRocketAmmoSystem` | `EcsMissileSystem` | `EcsMissileSystem` |
| Visual | `RocketVisual` | `MissileVisual` | `MissileVisual` |

**Оценка:** GSD разделил ammo и target данные в два компонента — лучше для SRP. Pure Claude и Superpowers объединили ammo данные в один `MissileData` (как у `GunData`/`LaserData` — следует существующему паттерну). Superpowers назвал навигационную систему `EcsMissileNavigationSystem` — наиболее семантически точное имя из трёх.

### 2.2 Компоненты ECS

| Компонент | GSD | Pure Claude | Superpowers |
|---|---|---|---|
| Ammo на корабле | `RocketAmmoData` (MaxAmmo, ReloadDurationSec, CurrentAmmo, ReloadRemaining, Shooting, Direction, ShootPosition) | `MissileData` (идентичная структура, MaxShoots вместо MaxAmmo) | `MissileData` (MaxShoots, ReloadDurationSec, CurrentShoots, ReloadRemaining, Shooting, Direction, ShootPosition) |
| Target данные | `RocketTargetData` (Target: Entity, TurnRateDegPerSec) | `HomingData` (TurnSpeed) | `HomingData` (Target: Entity, TurnRateDegPerSec, Speed, LifeTime) |
| Тег ракеты | `RocketTag` | `MissileTag` + `PlayerMissileTag` | `MissileTag` + `PlayerBulletTag` (reuse) |
| Event | `RocketShootEvent` | `MissileShootEvent` | `MissileShootEvent` |

**Ключевые отличия:**
- **Кэширование цели:** GSD и Superpowers хранят `Entity Target` — кэшируют текущую цель. Pure Claude пересчитывает каждый кадр.
- **HomingData:** Superpowers включил `Speed` и `LifeTime` прямо в `HomingData` — самый полный компонент из трёх, все данные наведения в одном месте.
- **Теги:** GSD — единый `RocketTag`. Pure Claude — `MissileTag` + `PlayerMissileTag` (максимальная гибкость). Superpowers — `MissileTag` + `PlayerBulletTag` (переиспользование существующего тега коллизий — прагматично, но менее расширяемо).

### 2.3 Система наведения

| Аспект | GSD | Pure Claude | Superpowers |
|---|---|---|---|
| Тип системы | `SystemBase` (managed) | `ISystem` (unmanaged) | `ISystem` (unmanaged) |
| Порядок | `[UpdateAfter(EcsMoveSystem)]` | `[UpdateAfter(EcsMissileSystem)]` | `[UpdateAfter(EcsMissileSystem)]` |
| Кэширование цели | Да (`Target` entity ref) | Нет (пересчёт каждый кадр) | Да (`Target` entity ref в `HomingData`) |
| Алгоритм поворота | `RotateTowards()` — cross/dot product | `atan2` + angle diff + clamp | `atan2` + angle diff + clamp |
| Dead target handling | Проверка `Exists()` + retarget | `WithNone<DeadTag>()` в запросе | `Exists()` + `HasComponent<DeadTag>()` + retarget |
| Упреждение (intercept) | Нет | Нет | Да — intercept point на основе closing speed |
| Тороидальный поиск | Нет | Нет | Да — `ToroidalDistanceSq()` + тороидальная коррекция направления |
| Размер файла | 132 строки | 105 строк | 203 строки |

**Superpowers значительно лучше по алгоритму наведения:** Единственная реализация с intercept prediction (упреждением) и тороидальным поиском цели. Ракета летит не к текущей позиции цели, а к предсказанной точке пересечения. Тороидальный поиск корректно находит ближайшую цель через края экрана. Это самый полный и физически корректный алгоритм из трёх.

### 2.4 Collision

| Аспект | GSD | Pure Claude | Superpowers |
|---|---|---|---|
| Маркер | `RocketTag` (единый) | `PlayerMissileTag` (отдельный от `MissileTag`) | `PlayerBulletTag` (reuse) |
| Метод проверки | `IsRocket()` | `IsPlayerMissile()` | Через существующую `PlayerBulletTag` систему |
| ScoreValue на ракете | Да (Score=50 на самой ракете) | Нет | Нет |
| Изменения в collision system | Добавлен `IsRocket()` чек | Добавлен `IsPlayerMissile()` чек | Нет изменений — `PlayerBulletTag` уже обрабатывается |

**По расширяемости:** Pure Claude лучше (разделение `MissileTag`/`PlayerMissileTag`).
**По ScoreValue:** GSD лучше (Score на ракете).
**По прагматичности:** Superpowers лучше — переиспользование `PlayerBulletTag` позволило не трогать collision system вообще. Минимальный diff, нулевой риск регрессии.

### 2.5 Visual / Prefab

| Аспект | GSD | Pure Claude | Superpowers |
|---|---|---|---|
| Trail в коде | `_trailEffect` SerializeField + Play/Stop/Clear в OnConnected/OnDisable | Нет — trail только на префабе | `_trail` SerializeField + Play() в OnConnected |
| Trail cleanup | Явный `Stop(StopEmittingAndClear)` в `OnDisable()` | Нет обработки жизненного цикла trail | Нет обработки в OnDisable |
| Editor tooling | `RocketPrefabSetup.cs` — Editor-скрипт для setup | Создание через MCP script-execute | Создание через MCP script-execute |
| Prefab tests | `RocketPrefabSerializeFieldTests.cs` — парсит YAML | Нет | Нет |
| Создание Trail | Код в Editor-скрипте | MCP script-execute (ad-hoc) | MCP script-execute (итеративно, 3 попытки) |
| Материал Trail | URP-совместимый | URP-совместимый | `MissileTrail.mat` — Sprites/Default как asset |

**GSD лучше:** Trail lifecycle (Play/Stop/Clear), Editor-скрипт, YAML-тесты.
**Superpowers частично:** Trail управляется в OnConnected (Play), но нет Stop/Clear в OnDisable — при пулинге может glitch. Создание материала как отдельного asset — хороший подход (решает проблему purple squares на уровне архитектуры).

### 2.6 HUD

| Аспект | GSD | Pure Claude | Superpowers |
|---|---|---|---|
| Null-guard | Да + warning лог | Да (null check) | Да (null check) |
| Naming | `RocketAmmoCount`, `RocketReloadTime` | `MissileShootCount`, `MissileReloadTime` | `MissileShootCount`, `MissileReloadTime` |

**Паритет.** Все три корректно обрабатывают null SerializeField.

### 2.7 Input → ECS

| Аспект | GSD | Pure Claude | Superpowers |
|---|---|---|---|
| Ammo guard в Game.cs | Да (`if (rocketAmmo.CurrentAmmo <= 0) return`) | Нет (проверка только в EcsMissileSystem) | Нет (проверка только в EcsMissileSystem) |

**GSD лучше:** Early exit в `Game.OnRocket()` предотвращает бесполезное обращение к ECS (чтение RotateData/MoveData), когда ракет нет. Pure Claude и Superpowers полагаются на проверку в ECS-системе — корректно, но менее эффективно.

---

## 3. Тестовое покрытие

| Категория тестов | GSD | Pure Claude | Superpowers |
|---|---|---|---|
| Ammo system | 10 | 8 | 7 |
| Guidance/Homing | 9 | 8 | 11 |
| Collision (rocket) | ~5 (из 20 Rocket-related) | 5 | 4 |
| Lifecycle | 5 | 0 | 0 |
| Prefab validation | 2 | 0 | 0 |
| EntityFactory | в существующих | 2 | 0 |
| **Итого rocket-тестов** | **~31** | **~23** | **22** |

**GSD лучше по покрытию:** Lifecycle тесты (spawn→sync→die) и Prefab YAML валидация — категории, которые ни Pure Claude, ни Superpowers не покрыли.

**Superpowers лучше по навигации:** 11 тестов навигации (больше всех) — покрывают тороидальный поиск, упреждение, переключение цели, LifeTime. Это отражает более сложный алгоритм наведения.

---

## 4. Проблемы и баги

### GSD (6 rocket-related fixes):
1. `fix(config): set Rocket Score=50` — забыл выставить Score в GameData.asset
2. `fix: заменить Default-Particle на URP-материал` — фиолетовые квадраты (общий баг)
3. `fix: null-guard для rocket SerializeField в HudVisual` — NullRef при отсутствии TMP_Text
4. `fix(14): ошибки компиляции, trail PS и GameData.asset` — компиляционные ошибки
5. `fix(13): create rocket prefab and assign to GameData` — префаб не был привязан
6. Множественные merge conflicts (STATE.md) — 5+ merge fixes

### Pure Claude (3 бага, исправлены до коммита):
1. **Циклическая зависимость систем** — `[UpdateBefore(EcsMoveSystem)]` создавал цикл → убран атрибут
2. **Missing script на Main Camera** — не связан с фичей, предсуществовал
3. **Фиолетовые квадраты (trail)** — null материал на ParticleSystem → создан URP-материал

### Superpowers (8 fix-коммитов):
1. **Циклическая зависимость систем** — `[UpdateBefore(EcsMoveSystem)]` + `[UpdateAfter(EcsMissileSystem)]` создавали цикл через всю цепочку → убран `[UpdateBefore]`
2. **Duplicate PlayerActions.cs** — Unity перегенерировал новый файл, старый в Generated/ создавал конфликт → удалён старый
3. **ref параметры в foreach** — `RefRW<>` нельзя передавать по ref из foreach SystemAPI.Query → убраны ref
4. **SystemAPI.Query в static методе** — не работает в ISystem → метод сделан нестатическим
5. **Trail не виден** — Default-Particle.mat (Additive) невидим в 2D → заменён на Sprites/Default как asset
6. **playOnAwake Trail** — частицы эмиттили при instantiate до connect → отключён, Play() в OnConnected
7. **Missile инстансы в сцене** — replaceGameObjectWithPrefab оставил PrefabInstance → удалены script-execute
8. **Ошибки компиляции** — обновление сгенерированных файлов после изменения Input System

**Вывод:** Superpowers обнаружил больше уникальных багов (8), но 4 из них — типичные ошибки при работе с Unity ECS (ref в foreach, static method, system ordering, generated files). GSD имел 6 багов, многие self-inflicted. Pure Claude — 3 бага, самый чистый первый проход.

Общий баг у всех трёх: **фиолетовые квадраты trail** (runtime-материал не переживает сериализацию).

---

## 5. Документация и трассируемость

| Метрика | GSD | Pure Claude | Superpowers |
|---|---|---|---|
| Planning артефакты | RESEARCH.md, PLAN.md, VERIFICATION.md, STATE.md, CONTEXT.md × 8 фаз | DECISIONS.md (1 файл) | Spec (330 строк) + Plan (1841 строка) + Report |
| Файлов документации | 57 | 1 | 3 |
| Строк документации | Тысячи | ~200 | ~2170 |
| Трассируемость требований | Формальная (REQUIREMENTS → ROADMAP → PLAN → VERIFICATION) | Неформальная (решения в DECISIONS.md) | Средняя (Spec → Plan с checkbox tasks) |
| Аудит milestone | Да (16/19 satisfied, 3 human needed) | Нет | Нет |
| Debug knowledge base | Да (rocket-trail-missing, hudvisual-nullref) | Нет | Нет |

**GSD — формально лучший** по документации. Полная трассируемость от требования до верификации.

**Superpowers — средний уровень:** Один spec-документ (дизайн) + один plan-документ (пошаговые задачи с checkbox). Достаточно для воспроизведения, но нет формальной верификации. Plan на 1841 строк — детальный, с кодом в каждом шаге, но не проверяется автоматически.

**Pure Claude — минимальный:** Один DECISIONS.md. Достаточно для понимания, но не для аудита.

---

## 6. Расширяемость

| Аспект | GSD | Pure Claude | Superpowers |
|---|---|---|---|
| Ракеты врагов | Нужен `EnemyRocketTag` (нет готового разделения) | `MissileTag` + `PlayerMissileTag` уже разделены | `MissileTag` + `PlayerBulletTag` — нужен рефакторинг для enemy missiles |
| Разные типы ракет | `RocketTargetData.TurnRateDegPerSec` на сущности | `HomingData.TurnSpeed` — аналогично | `HomingData` с TurnRate + Speed + LifeTime — наиболее полный |
| Очки за сбитие ракеты | `ScoreValue` уже на ракете | Нужно добавить | Нужно добавить |
| Trail lifecycle | Управляется в коде (Play/Stop) | Только на префабе — при пулинге может glitch | Play() в OnConnected, но нет Stop/Clear |
| Тороидальное наведение | Нет | Нет | Есть — корректно работает через wrap |
| Intercept prediction | Нет | Нет | Есть — ракета летит к предсказанной точке |

**По алгоритму:** Superpowers — единственный с intercept prediction и тороидальным наведением. При масштабировании (больше врагов, больший экран) это критически важно.

**По тегам:** Pure Claude — лучшая подготовка к enemy missiles.

**По lifecycle:** GSD — полный контроль Trail (Play/Stop/Clear).

---

## 7. Overhead фреймворка

| Метрика | GSD | Pure Claude | Superpowers |
|---|---|---|---|
| Planning файлов | 57 | 0 | 2 (spec + plan) |
| Docs-коммитов | 174 (55%) | 0 | 3 (~10%) |
| Research/Plan циклов | 8 × (RESEARCH + PLAN + VERIFY) | 0 | 1 × (brainstorm + spec + plan) |
| Строк документации | Тысячи | ~200 (DECISIONS.md) | 2171 (spec 330 + plan 1841) |
| Debug sessions | 2 формальных | 0 | 0 |
| Merge conflicts | 5+ (STATE.md) | 0 | 0 |
| Framework overhead | Критический — 55% времени на docs | Нулевой | Умеренный — 20 мин на spec/plan |

**GSD:** Максимальный overhead. 57 planning файлов, 174 docs коммита, 5+ merge conflicts.

**Superpowers:** Умеренный overhead. 2 файла (spec + plan), ~20 минут на подготовку. Plan на 1841 строк — избыточно детальный для одной фичи, но не требует обслуживания (написал и забыл).

**Pure Claude:** Нулевой overhead. 1 DECISIONS.md.

---

## 8. Итоговая оценка

| Критерий | GSD | Pure Claude | Superpowers | Победитель |
|---|---|---|---|---|
| **Скорость** | ~5 часов | ~50 минут | ~2 часа | **Pure Claude** (6x vs GSD, 2.5x vs SP) |
| **Качество кода** | Выше (ammo guard, trail lifecycle, ScoreValue) | Хорошее, следует паттернам | Хорошее + intercept + toroidal | **Superpowers** (алгоритм наведения) |
| **Тестовое покрытие** | 31 тест (lifecycle + prefab YAML) | 23 теста | 22 теста (11 навигация) | **GSD** (шире покрытие) |
| **Архитектура** | Кэширование цели, Editor tooling | Проще, готов к enemy missiles | Target caching + intercept + toroidal | **Superpowers** (полнота алгоритма) |
| **Документация** | Полная трассируемость | Минимальная, но достаточная | Spec + Plan, средний уровень | **GSD** (формальная верификация) |
| **Расширяемость** | ScoreValue, trail mgmt | Разделение тегов | Intercept, toroidal, полный HomingData | **Паритет** (разные аспекты) |
| **Баги** | 6 fixes (часть self-inflicted) | 3 бага (1 архитектурный) | 8 fixes (4 ECS-специфичных) | **Pure Claude** (минимум багов) |
| **Overhead** | 57 файлов, 174 docs коммита | 0 | 2 файла, 3 docs коммита | **Pure Claude** (нулевой) |
| **Воспроизводимость** | Высокая (формальный процесс) | Низкая (зависит от контекста) | Средняя (spec + plan воспроизводимы) | **GSD** (формальный процесс) |
| **Физическая корректность** | Нет intercept, нет toroidal | Нет intercept, нет toroidal | Intercept + toroidal | **Superpowers** (единственный) |

### Общий вердикт

**Pure Claude** — лучший по соотношению результат/время. Рабочая фича за 50 минут, минимум багов, нулевой overhead. Идеален для фич с понятной архитектурой.

**Superpowers** — лучший по качеству алгоритма. Единственный реализовал intercept prediction и тороидальное наведение — ракета летит к предсказанной точке, а не к текущей позиции цели, и корректно работает через edges экрана. За это заплатил 2x временем относительно Pure Claude и 8 багфиксами. Plan на 1841 строк создал детальный roadmap, но subagent overhead и MCP-итерации (3 попытки на Trail) увеличили время.

**GSD** — лучший по покрытию и документации. 31 тест, lifecycle и prefab YAML валидация, полная трассируемость. Но 6x медленнее Pure Claude, 55% времени на документацию.

### Неожиданные результаты

1. **Superpowers — единственный с физически корректным наведением.** Ни GSD, ни Pure Claude не реализовали intercept prediction и тороидальный поиск. Spec-документ (brainstorm → design) явно указал эти требования, и subagent их реализовал. Это показывает ценность этапа проектирования — он выявляет требования, которые не очевидны из промпта.

2. **Superpowers допустил больше всех багов (8).** Subagent-driven подход изолирует контекст каждого subagent, что приводит к потере глобальной картины. Баги типа "ref в foreach" и "SystemAPI.Query в static" — ошибки, которые опытный разработчик не допустит, но subagent без полного контекста Unity ECS API — допускает.

3. **PlayerBulletTag reuse — прагматичный shortcut.** Superpowers переиспользовал существующий `PlayerBulletTag` вместо создания нового тега. Нулевые изменения в collision system — минимальный risk. Но менее расширяемо для enemy missiles.

---

## 9. Рекомендации по усилению Pure Claude

### Проблемы, которые нужно закрыть

GSD и Superpowers выиграли в разных критериях. Все преимущества можно закрыть **расширением промпта**.

### Решение: дополненный промпт

Ниже — модифицированный промпт, который закрывает gaps. Добавления выделены комментариями `[NEW]`.

```
Самонаводящиеся ракеты. Нужно сделать новую фичу. Добавим самонаводящиеся
ракеты. У игрока есть одна ракета, которую можно запустить, нажав кнопку R.
Ракета летит по дуге в ближайшую цель. Если ракета по пути врежется не в
выбранную цель, это тоже считается. После запуска ракет включается счетчик
на респавн ракет. Количество ракет у игрока и время респавна должно быть
задано в конфигах. Ракета коллайдится с астеройдами и UFO. В качестве визала
ракеты можно использовать уменьшенный спрайт корабля. Инверсионный след
можно сделать из спрайта частиц. Ракета должна быть создана и вписана в
текущую архитектуру ECS + визуал. Количество доступных ракет и время респавна
должно выводится в HUD-e игрока. Фича должна быть разработана в парадигме
TDD, весь функционал покрыт тестами. Проверка функционала при помощи юнит,
интеграционных тестов и MCP. Human validation в исключительных случаях, когда
текущих инструментов не достаточно и невозможно написать новый функционал
для MCP. Веди отчет в md файле в которой пиши какие ты решения принял и
почему, а еще какие скилы использовал.

[NEW] Дополнительные требования к качеству:

1. DEFENSIVE CODING: Добавь early-return guard в Game.OnRocket() — если
   ракет нет, не читай RotateData/MoveData. Для Visual-компонентов с
   ParticleSystem — управляй lifecycle (Play/Stop/Clear) в OnConnected
   и OnDisable, не полагайся на авто-поведение префаба.

2. РАСШИРЯЕМОСТЬ: Добавь ScoreValue компонент на сущность ракеты (для
   будущих сценариев, когда враги могут сбивать ракеты). Разделяй теги
   (MissileTag + PlayerMissileTag) для будущих ракет врагов.

3. НАВЕДЕНИЕ: Реализуй intercept prediction (упреждение) — ракета должна
   лететь к предсказанной позиции цели, а не к текущей. Поиск ближайшей
   цели должен учитывать тороидальную геометрию экрана.

4. ТЕСТЫ LIFECYCLE: Помимо unit-тестов систем, напиши:
   - Lifecycle тесты (spawn→sync→move→die полный цикл)
   - Prefab validation тесты (парсинг YAML для проверки что SerializeField
     привязаны и все fileID существуют)
   - Regression тест на каждый исправленный баг

5. EDITOR TOOLING: Создай Editor-скрипт для настройки ракетного префаба
   (trail параметры, GameData значения) — чтобы любой член команды мог
   пересоздать настройку одной кнопкой.

6. ДОКУМЕНТАЦИЯ: В отчёте решений добавь раздел "Трассируемость" — таблица
   [Требование → Файлы → Тесты] для каждого аспекта фичи.
```

### Что дают эти добавления

| Gap | Источник gap | Добавление в промпт | Ожидаемый эффект |
|---|---|---|---|
| Ammo guard | GSD | п.1 (defensive coding) | Идентичный GSD-качеству input handling |
| Trail lifecycle | GSD | п.1 (ParticleSystem lifecycle) | Play/Stop/Clear в коде, не только на префабе |
| ScoreValue | GSD | п.2 (расширяемость) | Ракета как полноценная scoreable entity |
| Intercept prediction | Superpowers | п.3 (наведение) | Физически корректное наведение |
| Тороидальный поиск | Superpowers | п.3 (наведение) | Корректная работа через edges |
| Lifecycle тесты | GSD | п.4 (spawn→die цикл) | +5-8 тестов, покрывающих интеграцию |
| Prefab YAML тесты | GSD | п.4 (prefab validation) | Защита от broken SerializeField |
| Editor tooling | GSD | п.5 | Повторяемая настройка префаба |
| Трассируемость | GSD | п.6 | Лёгкий аудит без overhead GSD |

### Оценка overhead

Эти добавления увеличат время Pure Claude примерно на **20-30 минут** (с ~50 до ~80 минут):
- Intercept + toroidal наведение: +10 мин (более сложный алгоритм + тесты)
- Editor-скрипт: +5 мин
- Lifecycle + Prefab тесты: +10 мин
- Defensive coding + ScoreValue: +2 мин
- Таблица трассируемости: +3 мин

Итого: **~80 минут** вместо ~50, при этом закрываются ВСЕ gaps со всеми тремя подходами. Это всё ещё **1.5x быстрее Superpowers** (~2 часа) и **~4x быстрее GSD** (~5 часов).

### Вывод

Каждый фреймворк привнёс уникальные качества: GSD — lifecycle тесты и трассируемость, Superpowers — intercept prediction и тороидальное наведение. Но ни одно из этих преимуществ не требует фреймворка — достаточно **явно указать в промпте**.

Разница между подходами:
- **GSD** заставляет проходить чеклисты через формальный процесс → гарантирует покрытие, но дорого
- **Superpowers** выносит требования в spec-документ → brainstorm выявляет неочевидные аспекты (intercept), но subagent overhead
- **Pure Claude** полагается на промпт → быстро, но качество зависит от полноты промпта

**Идеальный workflow: Pure Claude + дополненный промпт с чеклистом качества.** Скорость Pure Claude + покрытие GSD + алгоритмическое качество Superpowers, без overhead на документацию.
