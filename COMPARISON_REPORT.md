# Сравнительный отчёт: GSD vs Pure Claude

Промпт.

Есть 2 ветки в которых реализовывалась одна и та же фича с одним и тем же промптом с одной и той же моделью opus. В одной ветке feature/rockets-pure-claude использовался чистый claude, в другой feature/rockets использовался    
фреймворк gsd. Моя цель понять, кто справился лучше. Сравни решения в этих ветках на качество, целостность и расширяемость. Дай подробный отчет по принятым решениям при имплементации кода и количеству проблем, который были       
решены в обоих случаях. Так же для ясности дай сравнительную характестистику скорости разработки. Составь отчет

## Контекст

Одна и та же фича (самонаводящиеся ракеты) реализована двумя подходами с одной моделью (Claude Opus 4.6, 1M context):

| Параметр | GSD (`feature/rockets`) | Pure Claude (`feature/rockets-pure-claude`) |
|---|---|---|
| Фреймворк | GSD workflow (discuss→plan→execute) | Прямая разработка без фреймворка |
| Модель | Claude Opus 4.6 | Claude Opus 4.6 |
| Промпт | Идентичный | Идентичный |

---

## 1. Скорость разработки

| Метрика | GSD | Pure Claude |
|---|---|---|
| Коммитов на фичу ракет (фазы 10-17) | 66 | 1 |
| Коммитов всего (включая миграцию) | 315 | 1 (+ 4 от предыдущей миграции) |
| Docs-коммитов | 174 (55%) | 0 |
| Fix-коммитов на ракеты | 6 | 0 (3 бага исправлены до коммита) |
| Время на ракеты (wall clock) | ~5 часов (21:19 → 02:05) | ~1 сессия (~50 минут) |
| Дней работы (всего с миграцией) | 6 дней | 1 сессия |

**Вывод:** Pure Claude выполнил фичу в **~6x быстрее** по wall clock. GSD потратил >50% времени на документацию (RESEARCH.md, PLAN.md, VERIFICATION.md, STATE.md на каждую фазу).

---

## 2. Архитектурные решения

### 2.1 Нейминг

| Аспект | GSD | Pure Claude |
|---|---|---|
| Prefix | `Rocket*` | `Missile*` |
| Ammo component | `RocketAmmoData` | `MissileData` |
| Target/Homing | `RocketTargetData` | `HomingData` |
| Guidance system | `EcsRocketGuidanceSystem` | `EcsHomingSystem` |
| Ammo system | `EcsRocketAmmoSystem` | `EcsMissileSystem` |
| Visual | `RocketVisual` | `MissileVisual` |

**Оценка:** GSD разделил ammo и target данные в два компонента — лучше для SRP. Pure Claude объединил ammo данные в один `MissileData` (как у `GunData`/`LaserData` — следует существующему паттерну).

### 2.2 Компоненты ECS

| Компонент | GSD | Pure Claude |
|---|---|---|
| Ammo на корабле | `RocketAmmoData` (MaxAmmo, ReloadDurationSec, CurrentAmmo, ReloadRemaining, Shooting, Direction, ShootPosition) | `MissileData` (идентичная структура, только MaxShoots вместо MaxAmmo) |
| Target данные | `RocketTargetData` (Target: Entity, TurnRateDegPerSec) | `HomingData` (TurnSpeed) |
| Тег ракеты | `RocketTag` | `MissileTag` + `PlayerMissileTag` |
| Event | `RocketShootEvent` | `MissileShootEvent` |

**Ключевое отличие:** GSD хранит `Entity Target` в `RocketTargetData` — кэширует текущую цель. Pure Claude пересчитывает ближайшую цель каждый кадр.

- GSD подход **эффективнее** (не пересчитывает дистанцию каждый кадр), но требует валидации target (target может умереть).
- Pure Claude подход **проще и надёжнее** (нет stale references), но O(N) каждый кадр. При 1 ракете и ~20 врагах — разницы нет.

### 2.3 Система наведения

| Аспект | GSD | Pure Claude |
|---|---|---|
| Тип системы | `SystemBase` (managed) | `ISystem` (unmanaged) |
| Порядок | `[UpdateAfter(EcsMoveSystem)]` | `[UpdateAfter(EcsMissileSystem)]` |
| Кэширование цели | Да (`Target` entity ref) | Нет (пересчёт каждый кадр) |
| Алгоритм поворота | `RotateTowards()` — cross/dot product | `atan2` + angle diff + clamp |
| Dead target handling | Проверка `Exists()` + retarget | `WithNone<DeadTag>()` в запросе |

**Оценка:** Оба алгоритма корректны. GSD использует более классический подход (cross/dot), Pure Claude — тригонометрический (atan2). Оба дают одинаковый визуальный результат.

### 2.4 Collision

| Аспект | GSD | Pure Claude |
|---|---|---|
| Маркер | `RocketTag` (единый) | `PlayerMissileTag` (отдельный от `MissileTag`) |
| Метод проверки | `IsRocket()` | `IsPlayerMissile()` |
| ScoreValue на ракете | Да (Score=50 на самой ракете) | Нет |

**GSD лучше:** добавил `ScoreValue` на ракету — потенциально полезно для будущих механик (враг сбивает ракету → получает очки). Pure Claude вообще не даёт очки за уничтожение ракеты — проще, но менее расширяемо.

**Pure Claude лучше:** `PlayerMissileTag` отделён от `MissileTag` — готов к будущим ракетам врагов.

### 2.5 Visual / Prefab

| Аспект | GSD | Pure Claude |
|---|---|---|
| Trail в коде | `_trailEffect` SerializeField + Play/Stop/Clear в OnConnected/OnDisable | Нет — trail только на префабе |
| Trail cleanup | Явный `Stop(StopEmittingAndClear)` в `OnDisable()` | Нет обработки жизненного цикла trail |
| Editor tooling | `RocketPrefabSetup.cs` — Editor-скрипт для setup | Создание через MCP script-execute |
| Prefab tests | `RocketPrefabSerializeFieldTests.cs` — парсит YAML | Нет |

**GSD значительно лучше:** Правильная обработка ParticleSystem lifecycle (Play/Stop/Clear), Editor-скрипт для повторяемой настройки, и YAML-тесты для валидации префаба.

### 2.6 HUD

| Аспект | GSD | Pure Claude |
|---|---|---|
| Null-guard | Да + warning лог | Да (null check) |
| Naming | `RocketAmmoCount`, `RocketReloadTime` | `MissileShootCount`, `MissileReloadTime` |

**Паритет.** Оба корректно обрабатывают null SerializeField.

### 2.7 Input → ECS

| Аспект | GSD | Pure Claude |
|---|---|---|
| Ammo guard в Game.cs | Да (`if (rocketAmmo.CurrentAmmo <= 0) return`) | Нет (проверка только в EcsMissileSystem) |

**GSD лучше:** Early exit в `Game.OnRocket()` предотвращает бесполезное обращение к ECS (чтение RotateData/MoveData), когда ракет нет.

---

## 3. Тестовое покрытие

| Категория тестов | GSD | Pure Claude |
|---|---|---|
| Ammo system | 10 | 8 |
| Guidance/Homing | 9 | 8 |
| Collision (rocket) | ~5 (из 20 Rocket-related) | 5 |
| Lifecycle | 5 | 0 |
| Prefab validation | 2 | 0 |
| EntityFactory | в существующих | 2 |
| **Итого rocket-тестов** | **~31** | **~23** |

**GSD лучше по покрытию:** Lifecycle тесты (spawn→sync→die) и Prefab YAML валидация — категории, которые Pure Claude не покрыл.

---

## 4. Проблемы и баги

### GSD (6 rocket-related fixes):
1. `fix(config): set Rocket Score=50` — забыл выставить Score в GameData.asset
2. `fix: заменить Default-Particle на URP-материал` — фиолетовые квадраты (идентичный баг)
3. `fix: null-guard для rocket SerializeField в HudVisual` — NullRef при отсутствии TMP_Text
4. `fix(14): ошибки компиляции, trail PS и GameData.asset` — компиляционные ошибки
5. `fix(13): create rocket prefab and assign to GameData` — префаб не был привязан
6. Множественные merge conflicts (STATE.md) — 5+ merge fixes

### Pure Claude (3 бага, исправлены до коммита):
1. **Циклическая зависимость систем** — `[UpdateBefore(EcsMoveSystem)]` создавал цикл → убран атрибут
2. **Missing script на Main Camera** — не связан с фичей, предсуществовал
3. **Фиолетовые квадраты (trail)** — null материал на ParticleSystem → создан URP-материал

**Вывод:** GSD обнаружил больше багов (6 vs 3), но многие были self-inflicted (забыл привязать префаб, забыл Score, merge conflicts). Pure Claude допустил 1 архитектурный баг (циклическая зависимость) и 1 общий с GSD (URP trail).

---

## 5. Документация и трассируемость

| Метрика | GSD | Pure Claude |
|---|---|---|
| Planning артефакты | RESEARCH.md, PLAN.md, VERIFICATION.md, STATE.md, CONTEXT.md × 8 фаз | DECISIONS.md (1 файл) |
| Трассируемость требований | Формальная (REQUIREMENTS → ROADMAP → PLAN → VERIFICATION) | Неформальная (решения в DECISIONS.md) |
| Аудит milestone | Да (16/19 satisfied, 3 human needed) | Нет |
| Debug knowledge base | Да (rocket-trail-missing, hudvisual-nullref) | Нет |

**GSD значительно лучше** по документации. Каждое решение трассируется от требования до верификации. Pure Claude полагается на один DECISIONS.md — достаточно для понимания, но не для формального аудита.

---

## 6. Расширяемость

| Аспект | GSD | Pure Claude |
|---|---|---|
| Ракеты врагов | Нужен `EnemyRocketTag` (нет готового разделения) | `MissileTag` + `PlayerMissileTag` уже разделены — проще добавить `EnemyMissileTag` |
| Разные типы ракет | `RocketTargetData.TurnRateDegPerSec` на сущности — каждая ракета может иметь свою скорость | `HomingData.TurnSpeed` — аналогично |
| Очки за сбитие ракеты | `ScoreValue` уже на ракете | Нужно добавить |
| Trail lifecycle | Управляется в коде (Play/Stop) | Только на префабе — при пулинге может glitch |

**GSD чуть лучше:** ScoreValue на ракете и управление trail в коде дают больше контроля. Pure Claude лучше подготовлен к enemy missiles через разделение тегов.

---

## 7. Overhead фреймворка

| Метрика | GSD overhead |
|---|---|
| .planning/ файлов | 57 файлов |
| docs-коммитов | 174 (55% от всех) |
| Research фазы | 8 × RESEARCH.md |
| Plan фазы | 8 × PLAN.md (некоторые с 2-3 планами) |
| Verification | 8 × VERIFICATION.md |
| Context/State | 8 × CONTEXT.md + STATE.md |
| Debug sessions | 2 формальных debug sessions |
| Merge conflicts | 5+ merge conflicts из-за STATE.md |

Pure Claude: 0 overhead файлов, 1 DECISIONS.md.

---

## 8. Итоговая оценка

| Критерий | GSD | Pure Claude | Победитель |
|---|---|---|---|
| **Скорость** | ~5 часов | ~50 минут | **Pure Claude** (6x) |
| **Качество кода** | Чуть выше (ammo guard, trail lifecycle, ScoreValue) | Хорошее, следует паттернам проекта | **GSD** (незначительно) |
| **Тестовое покрытие** | 31 тест (lifecycle + prefab YAML) | 23 теста | **GSD** |
| **Архитектура** | Кэширование цели, Editor tooling | Проще, готов к enemy missiles | **Паритет** |
| **Документация** | Полная трассируемость | Минимальная, но достаточная | **GSD** |
| **Расширяемость** | ScoreValue, trail mgmt | Разделение тегов | **Паритет** |
| **Баги** | 6 fixes (часть self-inflicted) | 3 бага (1 архитектурный) | **Pure Claude** |
| **Overhead** | 57 planning файлов, 174 docs коммита | 0 | **Pure Claude** |
| **Воспроизводимость** | Высокая (формальный процесс) | Низкая (зависит от контекста разговора) | **GSD** |

### Общий вердикт

**Pure Claude** победил по соотношению результат/время. Получил рабочую фичу за 1 сессию с минимальным overhead. Код чистый, следует паттернам проекта, 23 теста покрывают все ключевые сценарии.

**GSD** дал более robustный результат: больше тестов, лучшая документация, формальная верификация. Но заплатил за это 6x замедлением и 55% времени на документацию, которая для фичи такого масштаба избыточна.

**Рекомендация:** Для фич уровня "добавить новое оружие" (чёткие требования, понятная архитектура) — Pure Claude оптимален. GSD оправдан для системных миграций, архитектурных переделок и командной работы, где трассируемость критична.

---

## 9. Рекомендации по усилению Pure Claude

### Проблемы, которые нужно закрыть

GSD победил в 3 критериях:
1. **Качество кода** — ammo guard в input, trail lifecycle в Visual, ScoreValue на ракете
2. **Тестовое покрытие** — lifecycle тесты, prefab YAML валидация (31 vs 23)
3. **Документация** — формальная трассируемость требований

Все три можно закрыть **расширением промпта**, не добавляя фреймворк.

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

3. ТЕСТЫ LIFECYCLE: Помимо unit-тестов систем, напиши:
   - Lifecycle тесты (spawn→sync→move→die полный цикл)
   - Prefab validation тесты (парсинг YAML для проверки что SerializeField
     привязаны и все fileID существуют)
   - Regression тест на каждый исправленный баг

4. EDITOR TOOLING: Создай Editor-скрипт для настройки ракетного префаба
   (trail параметры, GameData значения) — чтобы любой член команды мог
   пересоздать настройку одной кнопкой.

5. ДОКУМЕНТАЦИЯ: В отчёте решений добавь раздел "Трассируемость" — таблица
   [Требование → Файлы → Тесты] для каждого аспекта фичи.
```

### Что дают эти добавления

| Gap | Добавление в промпт | Ожидаемый эффект |
|---|---|---|
| Ammo guard | п.1 (defensive coding) | Идентичный GSD-качеству input handling |
| Trail lifecycle | п.1 (ParticleSystem lifecycle) | Play/Stop/Clear в коде, не только на префабе |
| ScoreValue | п.2 (расширяемость) | Ракета как полноценная scoreable entity |
| Lifecycle тесты | п.3 (spawn→die цикл) | +5-8 тестов, покрывающих интеграцию |
| Prefab YAML тесты | п.3 (prefab validation) | Защита от broken SerializeField |
| Editor tooling | п.4 | Повторяемая настройка префаба |
| Трассируемость | п.5 | Лёгкий аудит без overhead GSD |

### Оценка overhead

Эти добавления увеличат время Pure Claude примерно на **15-20 минут** (с ~50 до ~70 минут):
- Editor-скрипт: +5 мин
- Lifecycle + Prefab тесты: +10 мин
- Defensive coding + ScoreValue: +2 мин
- Таблица трассируемости: +3 мин

Итого: **~70 минут** вместо ~50, при этом закрываются все gaps с GSD. Это всё ещё **4x быстрее** GSD (~5 часов).

### Вывод

Фреймворк GSD не даёт магического качества — его преимущества сводятся к конкретным практикам (defensive coding, lifecycle tests, prefab validation, Editor tooling), которые можно перенести в промпт за 5 строк. Разница в том, что GSD **заставляет** проходить эти чеклисты через формальный процесс, а Pure Claude нужно **явно попросить**.

Идеальный workflow: **Pure Claude + дополненный промпт с чеклистом качества**. Скорость Pure Claude + покрытие GSD, без 55% overhead на документацию.
