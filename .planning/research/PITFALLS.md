# Pitfalls Research

**Domain:** Самонаводящиеся ракеты в гибридном DOTS 2D аркадной игре
**Researched:** 2026-04-05
**Confidence:** HIGH (основано на анализе кодовой базы + известные паттерны gamedev)

## Critical Pitfalls

### Pitfall 1: Тороидальный wrapping ломает homing-алгоритм

**What goes wrong:**
Ракета наводится на цель через `math.normalizesafe(targetPos - rocketPos)`, но на тороидальном экране кратчайший путь может идти через границу. Цель на расстоянии 1 юнит за правым краем будет считаться на расстоянии `gameArea.x - 1` в обратную сторону. Ракета летит длинным путём вместо короткого, визуально выглядит сломанной.

**Why it happens:**
`EcsMoveSystem.PlaceWithinGameArea` делает wrapping ПОСЛЕ перемещения. Homing-система читает raw-позиции из `MoveData.Position`. Разработчик копирует паттерн из `EcsShootToSystem`/`EcsMoveToSystem`, которые тоже НЕ учитывают wrapping (для прямолинейного полёта пули/UFO это менее заметно, для дуги ракеты -- критично).

**How to avoid:**
Вычислять shortest-path delta с учётом wrapping:
```csharp
float2 delta = targetPos - rocketPos;
if (math.abs(delta.x) > gameArea.x * 0.5f)
{
    delta.x -= math.sign(delta.x) * gameArea.x;
}
if (math.abs(delta.y) > gameArea.y * 0.5f)
{
    delta.y -= math.sign(delta.y) * gameArea.y;
}
```
Вынести в утилиту `ToroidalDelta(float2 a, float2 b, float2 gameArea)` -- пригодится и для target acquisition (расчёт расстояния до ближайшего врага).

**Warning signs:**
- Ракета летит "вокруг света" вместо через ближний край
- Тесты homing проходят только когда цель в центре экрана
- Разные результаты при позициях у краёв

**Phase to address:**
Фаза 1 (ECS-компоненты + RocketHomingSystem). `ToroidalDelta` должна быть реализована ДО homing-алгоритма, как фундамент.

---

### Pitfall 2: Осцилляция ракеты вокруг цели (jitter/orbiting)

**What goes wrong:**
Ракета с высокой скоростью поворота мгновенно направляется на цель, пролетает мимо, разворачивается, пролетает снова -- бесконечная осцилляция. При низкой скорости поворота ракета входит в стабильную орбиту вокруг цели и никогда не попадает.

**Why it happens:**
Наивный homing: `direction = normalize(target - position)` пересчитывается каждый кадр без ограничения угловой скорости поворота. Или обратная ошибка -- слишком малый turn rate при высокой линейной скорости.

**How to avoid:**
Использовать пропорциональную навигацию или ограниченный поворот:
1. Вычислить желаемое направление (с тороидальным delta)
2. Ограничить поворот за кадр: `maxTurnAngle = turnRate * deltaTime`
3. Повернуть текущее направление к желаемому на не более чем `maxTurnAngle` (через `math.atan2` + clamp дельты угла)
4. Баланс: `turnRate` должен позволять ракете описать дугу радиусом ~ 2-3 юнита при текущей скорости. Формула: `minTurnRadius = speed / turnRateRadians`.

**Warning signs:**
- Ракета "дрожит" около цели
- Ракета вращается вокруг медленной цели
- Быстрые цели (малый UFO) недосягаемы

**Phase to address:**
Фаза 1 (RocketHomingSystem). Критически важно: turn rate и линейная скорость должны быть конфигурируемыми с первого дня. Тесты должны проверять дугу при разных углах к цели.

---

### Pitfall 3: Burst-несовместимость в homing-логике

**What goes wrong:**
Homing-система требует доступ к позициям ВСЕХ потенциальных целей для target acquisition. Попытка использовать managed-типы (`Dictionary`, `List<Entity>`, `GameObject`, делегаты) в `[BurstCompile]` ISystem -- ошибка компиляции или fallback на Mono (x10-x100 замедление).

**Why it happens:**
В текущей кодовой базе `EcsShootToSystem` и `EcsMoveToSystem` используют singleton `ShipPositionData` -- одна цель, данные уже в ECS. Для ракеты цель -- "ближайший враг", что требует перебора ВСЕХ врагов. Разработчик пытается использовать EntityManager queries или managed коллекции внутри Burst-job.

**How to avoid:**
Два подхода:
1. **Предпочтительный:** `EcsRocketHomingSystem` как `SystemBase` (managed, без `[BurstCompile]`), по аналогии с `ObservableBridgeSystem`, `GameObjectSyncSystem`. Target acquisition через `SystemAPI.Query` с фильтрами `WithAny<AsteroidTag, UfoTag, UfoBigTag>`. Для 20-50 врагов Burst не нужен.
2. **Альтернативный (если профайлер покажет проблему):** Собрать позиции врагов в `NativeArray<float2>` в первом проходе, итерировать во втором. Burst-совместимо, но сложнее.

Учитывая текущую архитектуру (max ~20-30 entity на экране), вариант 1 проще и безопаснее.

**Warning signs:**
- `InvalidOperationException` или `NotSupportedException` из Burst
- System fallback на managed execution (предупреждение в Console)
- Попытка кэшировать `EntityQuery` результаты в managed-поле ISystem

**Phase to address:**
Фаза 1 (ECS-компоненты). Решение Burst/non-Burst принять ДО написания системы. Зафиксировать в архитектуре.

---

### Pitfall 4: Target acquisition гонится за мёртвыми entity

**What goes wrong:**
Ракета захватывает цель, цель уничтожается (получает `DeadTag`), но ракета продолжает наводиться на entity, который уже помечен мёртвым или уничтожен. Результат: `InvalidOperationException`, полёт к (0,0), или мгновенное переключение на новую цель с визуальным рывком.

**Why it happens:**
`DeadEntityCleanupSystem` работает в `LateSimulationSystemGroup`. Между моментом добавления `DeadTag` и фактическим уничтожением entity проходит минимум 1 кадр. Если homing-система обновляется раньше cleanup -- она видит entity с `DeadTag`, но ещё существующий. Если entity уже уничтожен -- `EntityManager.Exists()` вернёт false, но без проверки -- crash.

**How to avoid:**
1. В `EcsRocketHomingSystem` при поиске целей: `.WithNone<DeadTag>()` в EntityQuery
2. При наличии текущей цели: проверять `EntityManager.Exists(target) && !EntityManager.HasComponent<DeadTag>(target)` каждый кадр
3. Хранить targetEntity в `RocketData` компоненте как `Entity` (unmanaged, совместим со struct IComponentData)
4. При потере цели -- два варианта: (a) искать новую ближайшую, (b) лететь прямо по последнему направлению. Вариант (a) предпочтительнее для game feel

**Warning signs:**
- Ракета резко меняет направление в момент уничтожения цели
- `InvalidOperationException: Entity does not exist` в логах
- Ракета летит к позиции (0,0) после уничтожения цели

**Phase to address:**
Фаза 1 (RocketHomingSystem). Паттерн target validation -- часть core homing logic. Обязательные unit-тесты: "цель умирает во время наведения".

---

### Pitfall 5: Ракета не учтена в EcsCollisionHandlerSystem

**What goes wrong:**
Ракета сталкивается с врагом, но ничего не происходит -- коллизия не обработана. Или ракета уничтожает корабль игрока. Или ракета-ракета / ракета-пуля коллизия вызывает undefined behavior (пара entity не матчится ни одному if-блоку в `ProcessCollision`).

**Why it happens:**
`EcsCollisionHandlerSystem.ProcessCollision` использует жёсткие проверки по тегам: `IsPlayerBullet`, `IsEnemyBullet`, `IsShip`, `IsEnemy`, `IsAsteroid`, `IsUfoAny`. Новый `RocketTag` не попадает ни в одну категорию. Коллизия ракеты с врагом просто игнорируется -- ни один if-блок не сработает.

**How to avoid:**
1. Создать `RocketTag : IComponentData`
2. Расширить `ProcessCollision` в `EcsCollisionHandlerSystem`:
   - Rocket + Enemy (Asteroid/Ufo/UfoBig) => MarkDead обоих, AddScore за врага
   - Rocket + Ship => игнорировать (ракета игрока не вредит себе)
   - Rocket + EnemyBullet => дизайн-решение: ракета уничтожает пулю? или неуязвима?
   - Rocket + PlayerBullet => игнорировать (friendly fire отключен)
3. Добавить Physics layer "Rocket" в Unity Project Settings
4. Настроить Layer Collision Matrix: Rocket сталкивается с Asteroid, Enemy, но НЕ с Player, PlayerBullet
5. Добавить helper `IsRocket(ref EntityManager em, Entity entity)` по паттерну существующих

**Warning signs:**
- Ракета пролетает сквозь врагов без эффекта
- Ракета уничтожает корабль игрока
- Дублирование очков при одновременной пуле и ракете на одну цель (обе MarkDead, обе AddScore)

**Phase to address:**
Фаза 2 (коллизии). Сразу после базового движения ракеты. Дублирование score предотвращается проверкой `DeadTag` перед `AddScore` (уже реализовано -- `MarkDead` проверяет `!HasComponent<DeadTag>`).

---

### Pitfall 6: Респавн ракет при смерти корабля / рестарте игры

**What goes wrong:**
Таймер респавна ракет продолжает работать после смерти корабля. При рестарте количество ракет не сбрасывается, или сбрасывается, но активные ракеты в полёте остаются на экране. Или: ECS-компонент амуниции привязан к Ship entity, Ship уничтожен -- данные потеряны.

**Why it happens:**
В текущей архитектуре `Game.Stop()` вызывает `EntitiesCatalog.ReleaseAllGameEntities()`, который уничтожает все entity через `_entityManager.DestroyEntity(entity)`. Если `RocketAmmoData` хранится на Ship entity -- данные уничтожаются вместе с кораблём. Если респавн через отдельный singleton entity -- его тоже нужно чистить. Если через `ActionScheduler` (как спавн врагов) -- scheduled action может остаться в очереди.

**How to avoid:**
1. `RocketAmmoData` как IComponentData на Ship entity (текущие ракеты, макс. ракеты, таймер респавна). Умирает с кораблём -- это ОК, при рестарте Ship создаётся заново с начальными значениями.
2. Перезарядка через ECS-систему `EcsRocketReloadSystem` (по аналогии с `EcsGunSystem`/`EcsLaserSystem`), НЕ через ActionScheduler.
3. Активные ракеты в полёте: имеют GameObjectRef + RocketTag -- чистятся через `ReleaseAllGameEntities`.
4. Тест: Start -> Launch Rocket -> Die -> Restart -> проверить rockets == maxRockets, нет летающих ракет.

**Warning signs:**
- После рестарта у игрока 0 ракет (или больше максимума)
- Ракеты от предыдущей игры летают на новом экране
- Console error при попытке release entity который уже уничтожен

**Phase to address:**
Фаза 3 (респавн и жизненный цикл). Требует понимания полного lifecycle -- реализовать после коллизий.

---

### Pitfall 7: Particle trail ракеты не чистится при уничтожении / re-use из пула

**What goes wrong:**
Ракета уничтожается (попала в цель или lifetime истёк), частицы инверсионного следа мгновенно исчезают вместе с GameObject. Визуально некрасиво -- след обрывается. Или хуже: при повторном использовании из пула старые частицы "вспыхивают" в позиции предыдущего полёта.

**Why it happens:**
Текущий паттерн VFX (взрывы): отдельный pooled GameObject с ParticleSystem, возвращается в пул по `OnParticleSystemStopped`. Trail ракеты -- дочерний объект ракеты. При Release ракеты в pool дочерний ParticleSystem деактивируется (`SetActive(false)`). При повторном Get -- `SetActive(true)` может возобновить старые частицы.

**How to avoid:**
1. Trail как **отдельный pooled объект**, НЕ дочерний элемент ракеты
2. При уничтожении ракеты: отвязать trail (`SetParent(null)`), остановить emission (`Stop()`), дать доиграть оставшиеся частицы, вернуть в пул по `OnParticleSystemStopped`
3. При Get из пула: `ParticleSystem.Clear()` + `ParticleSystem.Play()` для чистого старта
4. Альтернатива: `TrailRenderer` вместо ParticleSystem -- проще lifecycle, автоматический fade. Но `TrailRenderer.Clear()` обязателен при Get из пула.

**Warning signs:**
- Частицы мгновенно пропадают при попадании ракеты (trail обрывается)
- "Призрачные" частицы висят после уничтожения
- При повторном запуске ракеты видны старые частицы из предыдущего полёта

**Phase to address:**
Фаза 4 (визуал). Отдельная фаза после механики, т.к. trail -- чисто визуальный элемент.

---

### Pitfall 8: Ракета не имеет LifeTimeData -- летает вечно при промахе

**What goes wrong:**
Ракета потеряла цель (все враги уничтожены, или цель была единственная и умерла). Ракета летит прямо по последнему направлению. Без `LifeTimeData` она никогда не уничтожится -- будет бесконечно оборачиваться через тороидальный экран.

**Why it happens:**
Разработчик сосредоточен на homing-механике и забывает про edge case "нет целей". В текущей кодовой базе пули имеют `LifeTimeData`, астероиды и UFO -- нет (они бессмертны). Ракета по характеру ближе к пуле, но это легко упустить.

**How to avoid:**
1. Добавить `LifeTimeData` к ракете при создании в `EntityFactory.CreateRocket()`
2. `EcsDeadByLifeTimeSystem` уже обрабатывает `LifeTimeData` + `LifeTimeData.TimeRemaining <= 0` => `DeadTag`. Ракета подхватится автоматически.
3. Время жизни из конфига: `RocketLifeTimeSec` (рекомендация: 3-5 секунд -- достаточно для пролёта через экран)

**Warning signs:**
- Ракеты накапливаются на экране при промахах
- Entity count растёт без ограничений
- Performance degradation после длительной игры

**Phase to address:**
Фаза 1 (EntityFactory + компоненты). Добавлять `LifeTimeData` вместе с другими компонентами ракеты.

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Хардкод скорости/turnRate ракеты в системе (как `20f` в ShootToSystem) | Быстрая реализация | Невозможно балансировать без перекомпиляции | Никогда -- текущий антипаттерн, не повторять |
| RocketHomingSystem как SystemBase вместо ISystem | Простой доступ к EntityQuery, managed types | Невозможен BurstCompile, медленнее при сотнях entity | Допустимо при < 50 ракет на экране (наш случай) |
| Target acquisition каждый кадр | Всегда актуальная цель | O(rockets * enemies) каждый кадр | Допустимо при < 10 ракет * 30 врагов = 300 итераций |
| Один collision layer для пуль и ракет | Меньше настроек Layer Matrix | Нельзя различить поведение пули и ракеты при коллизии | Допустимо если collision response идентичный (оба = MarkDead + Score) |
| Пропустить тороидальный delta для target acquisition | Проще код | Ракета выбирает "ближнюю" цель по Евклидову расстоянию, а не тороидальному | Никогда -- целеполагание будет некорректным у краёв экрана |

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| CollisionBridge | Забыть вызвать `_collisionBridge.RegisterMapping()` для GO ракеты | Добавить в `EntitiesCatalog.CreateRocket()` по аналогии с `CreateBullet()` |
| EcsCollisionHandlerSystem | Не добавить rocket-specific collision rules | Расширить `ProcessCollision` с `IsRocket()` helper ПЕРЕД тестированием коллизий |
| ObservableBridgeSystem + HUD | Попытка добавить HUD-данные ракет в Ship-query | Расширить существующий query с `RefRO<RocketAmmoData>` на Ship entity (Ship уже имеет Thrust, Laser и пр.) |
| GameObjectSyncSystem | Ракета с `RotateData` синхронизируется через первый foreach (с вращением) | Это корректно -- ракета ДОЛЖНА иметь RotateData для визуального поворота по направлению полёта |
| DeadEntityCleanupSystem | Callback `_onDeadEntity` не обрабатывает RocketTag | Проверить нужен ли post-death эффект для ракеты (взрыв). Если да -- расширить обработчик |
| Input System | Забыть добавить действие "Rocket" в `.inputactions` asset | Добавить Rocket action (кнопка R) в `player_actions.inputactions`, пересгенерировать `PlayerActions.cs` |
| EntityType enum | Забыть добавить `Rocket` значение | Ведёт к невозможности определить тип entity через `TryGetEntityType()`. Crash при дробления астероида если попала ракета |
| ShootEventProcessorSystem | Попытка обработать запуск ракеты как `GunShootEvent` | Создать отдельный `RocketLaunchEvent : IBufferElementData` с собственной обработкой, или запускать ракету напрямую из `Game` |
| RocketAmmoData на Ship | Ship entity уничтожается при рестарте, данные теряются | Это ОК -- при `CreateShip()` создаётся новый entity с начальными значениями. Не хранить состояние ракет вне Ship |

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| ParticleSystem на каждую ракету без pooling | Frame drops при 5+ ракет с trails | Пулить trail-объекты отдельно от ракет, `ParticleSystem.Clear()` при reuse | > 10 одновременных trails |
| `EntityQuery.ToEntityArray` / `ToComponentDataArray` каждый кадр | GC allocation 40+ bytes каждый кадр на query | Использовать `foreach` по `SystemAPI.Query`, не аллоцировать managed массивы | Всегда -- аллокации каждый кадр гарантированно вызовут GC stutter на WebGL |
| Trail Renderer / ParticleSystem с высоким vertex count | GPU overdraw, FPS drop на WebGL | Ограничить: `TrailRenderer.time = 0.3s`, `minVertexDistance = 0.1f`. Для ParticleSystem: `maxParticles = 50` | WebGL на слабых GPU |
| Пересчёт `ToroidalDelta` для ВСЕХ врагов для КАЖДОЙ ракеты | CPU spike | Для < 10 ракет * < 50 врагов = 500 вычислений float2 -- не проблема. Оптимизировать только по профайлеру | > 500 пар (не наш случай) |

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| Ракета мгновенно поворачивает на 180 градусов | Выглядит нефизично, нет ощущения "ракеты" | Ограниченный turn rate (~180-270 deg/sec), плавная дуга. Скорость чуть ниже пули для визуальной читаемости |
| Ракеты слишком мощные -- обесценивают пули и лазер | Игрок использует только ракеты, геймплей упрощается | Ограниченный запас (2-3 штуки), длинный respawn (5-10 сек), очки за kill ракетой = как за пулю |
| Нет обратной связи при запуске ракеты | Игрок не уверен, запустилась ли ракета | Немедленное уменьшение счётчика в HUD + визуальный/звуковой эффект запуска |
| HUD не показывает таймер респавна | Игрок не знает, когда будет следующая ракета | Числовой таймер или прогресс-бар рядом со счётчиком ракет (по аналогии с LaserReloadTime) |
| Ракета не отличается визуально от пули | Игрок не видит разницы между оружием | Уменьшенный спрайт корабля (как в ТЗ) + инверсионный след -- визуально уникальна |
| Запуск ракеты при 0 доступных -- нет обратной связи | Игрок жмёт R и ничего не происходит, думает что баг | Звуковой "denied" или мигание пустого счётчика ракет в HUD |

## "Looks Done But Isn't" Checklist

- [ ] **Homing через тороидальный край:** Ракета у правого края экрана, цель у левого -- delta < gameArea/2, ракета летит через ближний край
- [ ] **EcsCollisionHandlerSystem:** Добавлен `RocketTag` для ВСЕХ комбинаций (Rocket+Asteroid, Rocket+Ufo, Rocket+UfoBig, Rocket+EnemyBullet). Ship+Rocket игнорируется
- [ ] **Physics Layer "Rocket":** Создан в Project Settings, настроена Layer Collision Matrix (Rocket vs Asteroid/Enemy = collide; Rocket vs Player/PlayerBullet = ignore)
- [ ] **EntityType enum:** Добавлено значение `Rocket`
- [ ] **LifeTimeData:** Ракета имеет время жизни для самоуничтожения при промахе (3-5 сек)
- [ ] **Lifecycle -- Game.Stop():** Активные ракеты уничтожаются через `ReleaseAllGameEntities()`
- [ ] **Lifecycle -- Restart:** Счётчик ракет = maxRockets, нет летающих ракет от предыдущей игры
- [ ] **Pool -- trail cleanup:** `ParticleSystem.Clear()` или `TrailRenderer.Clear()` при Get из пула
- [ ] **Pool -- trail detach:** Trail отвязывается от ракеты при уничтожении, доигрывает отдельно
- [ ] **Input:** Действие "Rocket" (кнопка R) добавлено в `player_actions.inputactions`, `PlayerActions.cs` пересгенерирован
- [ ] **HUD:** Счётчик ракет и таймер респавна отображаются, привязаны через ObservableBridgeSystem
- [ ] **Score:** Убийство ракетой начисляет очки (через ScoreValue на враге, как для пуль)
- [ ] **CollisionBridge.RegisterMapping:** Вызывается в `EntitiesCatalog.CreateRocket()` для GO ракеты
- [ ] **WebGL:** ParticleSystem trail работает на WebGL (нет compute shader зависимостей)
- [ ] **DeadTag filter:** Homing query использует `.WithNone<DeadTag>()` для исключения мёртвых целей
- [ ] **Entity validation:** Ракета проверяет `EntityManager.Exists(target)` каждый кадр перед наведением

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Тороидальный wrapping не учтён | LOW | Заменить `target - position` на `ToroidalDelta()` в одном месте (homing system). Одна утилита, 10 строк |
| Осцилляция ракеты | LOW | Добавить turn rate clamp -- одна переменная в конфиге, 5-10 строк в системе |
| Burst-несовместимость | MEDIUM | Переписать ISystem на SystemBase (или наоборот). Структурное изменение, но локализованное в одном файле |
| Мёртвые entity в таргетинге | LOW | Добавить `.WithNone<DeadTag>()` в query + проверку `Exists`. 3 строки |
| Collision handler не обрабатывает ракеты | LOW | Добавить if-блоки в `ProcessCollision` по аналогии с PlayerBullet. ~15 строк |
| Респавн не сбрасывается | MEDIUM | Зависит от архитектуры хранения ammo. Если на Ship entity -- минимально. Если ActionScheduler -- рефакторинг |
| Trail не чистится | LOW | Разделить trail и ракету на отдельные pooled объекты, добавить `Clear()` |
| Нет LifeTimeData | LOW | Одна строка в `EntityFactory.CreateRocket()`. `EcsDeadByLifeTimeSystem` подхватит автоматически |

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| Тороидальный wrapping | Phase 1 (ECS + RocketHomingSystem) | Тест: ракета у края, цель за противоположным краем. `ToroidalDelta.length < gameArea/2` |
| Осцилляция | Phase 1 (RocketHomingSystem) | Тест: ракета с turnRate + speed из конфига. Visual inspection дуги в PlayMode |
| Burst-несовместимость | Phase 1 (архитектурное решение) | Компиляция без Burst warnings/errors. SystemBase для homing -- OK |
| Мёртвые entity target | Phase 1 (target validation) | Тест: уничтожить цель ракеты, verify ракета не crashится, ищет новую или летит прямо |
| LifeTimeData | Phase 1 (EntityFactory) | Тест: ракета без цели уничтожается через N секунд |
| Collision handler | Phase 2 (коллизии) | Тест: ракета + каждый тип врага = оба dead + score начислен |
| Layer Collision Matrix | Phase 2 (коллизии) | Тест: ракета НЕ сталкивается с кораблём и пулями игрока |
| Респавн lifecycle | Phase 3 (интеграция) | Тест: Start -> shoot all -> die -> restart -> rockets == maxRockets |
| Trail cleanup/detach | Phase 4 (визуал) | Тест: запустить ракету -> уничтожить -> Get из пула -> нет старых частиц |
| Multiple targeting | Phase 1 (design decision) | Game feel review: запустить 3 ракеты при 5 врагах -- поведение адекватное |

## Sources

- Анализ кодовой базы: `EcsMoveSystem.cs` (тороидальный wrapping, строки 39-50), `EcsCollisionHandlerSystem.cs` (tag-based dispatch, 8 if-блоков), `EcsShootToSystem.cs`/`EcsMoveToSystem.cs` (targeting паттерн без wrapping), `DeadEntityCleanupSystem.cs` (lifecycle, LateSimulationSystemGroup)
- Паттерны entity lifecycle: `EntitiesCatalog.cs` (create/release flow, RegisterMapping), `EntityFactory.cs` (component composition для каждого типа entity)
- Существующие антипаттерны в проекте: хардкод `20f` в ShootToSystem:17, хардкод `10f` макс. скорости осколков, хардкод `30` для raycast buffer
- Unity DOTS архитектура: Burst compatibility constraints (managed types запрещены), SystemBase vs ISystem, `.WithNone<T>()` для фильтрации query
- Gamedev: proportional navigation, turn rate limiting, toroidal distance computation -- общеизвестные паттерны homing missile в аркадных играх

---
*Pitfalls research for: самонаводящиеся ракеты в гибридном DOTS 2D аркадной игре*
*Researched: 2026-04-05*
