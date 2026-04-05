# Project Research Summary

**Project:** Asteroids -- Самонаводящиеся ракеты (v1.2.0)
**Domain:** Новый тип оружия для 2D аркадного шутера на гибридном DOTS (Unity 6.3)
**Researched:** 2026-04-05
**Confidence:** HIGH

## Executive Summary

Самонаводящиеся ракеты -- третий тип оружия для Asteroids, дополняющий пули и лазер. Исследование показало, что реализация целиком укладывается в существующие паттерны проекта: ECS-компоненты для данных, ISystem для логики, Event Buffer для спавна, CollisionBridge для физики, MVVM для HUD. Новых пакетов или архитектурных решений не требуется -- все необходимые модули уже в manifest.json после миграции v1.1.0. Основная сложность сконцентрирована в одном месте: EcsRocketHomingSystem (поиск ближайшей цели + плавный поворот с ограниченным turn rate).

Рекомендуемый подход: seek-алгоритм с ограниченной скоростью поворота (180-270 град/сек). Каждый кадр ракета поворачивает Direction к ближайшему врагу, EcsMoveSystem двигает по обновленному направлению. Proportional Navigation отвергнута как избыточная для аркады. EcsRocketHomingSystem реализуется без BurstCompile (managed query для target acquisition) -- при масштабе 5 ракет и 50 врагов производительность не проблема.

Ключевой риск -- тороидальная геометрия: наивный `target - position` дает некорректный вектор у краев экрана. Решение: утилита `ToroidalDelta`, вычисляющая кратчайший путь через wrap. Это фундамент, который нужно заложить до написания homing-логики. Остальные риски (осцилляция, мертвые цели, lifecycle при рестарте) решаются стандартными паттернами проекта и имеют низкую стоимость исправления.

## Key Findings

### Recommended Stack

Новых пакетов не требуется. Все модули (Entities 1.4.5, Input System 1.19.0, URP 17.0.5, ParticleSystem, Physics2D, shtl-mvvm) уже установлены. Ракета расширяет стек горизонтально: новые IComponentData, ISystem, MonoBehaviour по установленным паттернам. Подробности в [STACK.md](STACK.md).

**Новые элементы стека:**
- `RocketAmmoData` (IComponentData): боезапас на Ship entity -- аналог GunData/LaserData
- `RocketHomingData` (IComponentData): параметры наведения (TurnRateDegPerSec) на entity ракеты
- `RocketLaunchEvent` (IBufferElementData): событие запуска -- аналог GunShootEvent/LaserShootEvent
- `EcsRocketHomingSystem` (ISystem, без Burst): ядро -- поиск цели + поворот Direction
- `EcsRocketAmmoSystem` (ISystem): перезарядка + генерация событий
- `ParticleSystem` (Simulation Space: World, Rate over Distance): инверсионный след

### Expected Features

Подробности в [FEATURES.md](FEATURES.md).

**Must have (table stakes):**
- Запуск ракеты по кнопке R
- Наведение на ближайшую цель (seek с ограниченным turn rate)
- Ограниченный боезапас (2-3 штуки)
- Инкрементальная перезарядка по таймеру
- Время жизни ракеты (самоуничтожение при промахе)
- Коллизия ракеты с врагами (уничтожение + очки)
- HUD: счетчик доступных ракет
- Визуал: спрайт (уменьшенный корабль) + инверсионный след
- Конфигурация через ScriptableObject (без магических чисел)

**Should have (differentiators):**
- Плавная дуга полета (визуально красиво, создает контргейм)
- Вращение спрайта по направлению движения
- Переключение цели при уничтожении текущей
- Взрыв VFX при попадании (переиспользовать существующий)
- HUD: таймер перезарядки

**Defer (anti-features -- НЕ реализовывать):**
- Управляемая игроком ракета (ломает arcade flow)
- Proportional Navigation (избыточно)
- Тороидальное наведение с 9 фантомными позициями (непропорциональная сложность)
- Ракеты для UFO (радикально меняет баланс)
- Lock-on индикатор (перегружает минималистичный UI)

### Architecture Approach

Ракеты интегрируются в существующий гибридный DOTS по тому же паттерну, что пули и лазер. Данные в ECS (IComponentData), логика в ISystem, визуал на GameObject с MonoBehaviour, связь через GameObjectRef + CollisionBridge + ObservableBridgeSystem. Ключевое архитектурное решение: EcsRocketHomingSystem обновляет MoveData.Direction, а существующий EcsMoveSystem двигает ракету -- разделение ответственности, переиспользование Burst-системы движения и тороидальной телепортации. Подробности в [ARCHITECTURE.md](ARCHITECTURE.md).

**Major components:**
1. `EcsRocketAmmoSystem` -- перезарядка боезапаса + генерация RocketLaunchEvent (на Ship entity)
2. `EcsRocketHomingSystem` -- поиск ближайшей цели + поворот Direction к ней (на Rocket entity)
3. `RocketVisual + RocketViewModel` -- спрайт, ParticleSystem trail, OnCollisionEnter2D (GameObject слой)
4. `GameObjectSyncSystem` (расширение) -- третий query для синхронизации position + rotation из MoveData.Direction

### Critical Pitfalls

Подробности в [PITFALLS.md](PITFALLS.md).

1. **Тороидальный wrapping ломает homing** -- наивный `target - position` дает неверный вектор у краев. Реализовать `ToroidalDelta()` до homing-логики. Стоимость: LOW (10 строк).
2. **Осцилляция/орбита вокруг цели** -- без ограничения turn rate ракета дрожит или вращается. Обязательный clamp поворота за кадр. Стоимость: LOW.
3. **Target acquisition по мертвым entity** -- цель может получить DeadTag между кадрами. `.WithNone<DeadTag>()` в query + `EntityManager.Exists()` проверка. Стоимость: LOW.
4. **Collision handler не знает про ракеты** -- без расширения ProcessCollision ракета пролетает сквозь врагов. Добавить `IsPlayerRocket()` + if-блоки. Стоимость: LOW.
5. **ParticleSystem trail не чистится при reuse из пула** -- старые частицы "вспыхивают". `ParticleSystem.Clear()` при Get, Stop() + detach при Release. Стоимость: LOW.

## Implications for Roadmap

Based on research, suggested phase structure:

### Phase 1: ECS Core -- данные + логика наведения

**Rationale:** Фундамент всей фичи. Все остальные фазы зависят от ECS-компонентов и систем. Критически важно заложить ToroidalDelta и корректный homing с первого дня.
**Delivers:** Рабочие ECS-компоненты (RocketAmmoData, RocketHomingData, RocketLaunchEvent, PlayerRocketTag), EcsRocketAmmoSystem (перезарядка), EcsRocketHomingSystem (наведение), EntityFactory.CreateRocket(), утилита ToroidalDelta.
**Addresses:** Наведение на ближайшую цель, ограниченный боезапас, перезарядка, время жизни, переключение цели.
**Avoids:** Pitfall 1 (тороидальный wrapping), Pitfall 2 (осцилляция), Pitfall 3 (Burst-несовместимость), Pitfall 4 (мертвые entity), Pitfall 8 (нет LifeTimeData).

### Phase 2: Collision Integration

**Rationale:** После ECS-ядра ракеты должны взаимодействовать с миром. Коллизии -- критический gameplay-элемент.
**Delivers:** Расширенный EcsCollisionHandlerSystem с PlayerRocketTag + Enemy rules. Physics layer "Rocket" в Project Settings.
**Addresses:** Коллизия ракеты с врагами, начисление очков.
**Avoids:** Pitfall 5 (ракета не учтена в collision handler).

### Phase 3: Bridge Layer + Entity Lifecycle

**Rationale:** Связь ECS с GameObject слоем. Без этого ракета существует только в ECS, невидима для игрока.
**Delivers:** ShootEventProcessorSystem.ProcessRocketEvents(), EntitiesCatalog.CreateRocket(), GameObjectSyncSystem (третий query для ракет), RocketVisual + RocketViewModel.
**Addresses:** Спавн ракеты, визуализация позиции и вращения, lifecycle при рестарте.
**Avoids:** Pitfall 6 (респавн при рестарте), Pitfall 7 (trail cleanup).

### Phase 4: Input + Game Integration

**Rationale:** Подключение к игровому процессу. Требует готовый pipeline спавна из Phase 3.
**Delivers:** Input Action "Rocket" (R key), PlayerInput.OnRocketAction, Game.OnRocket() handler, очистка буферов при рестарте.
**Addresses:** Запуск ракеты по кнопке R.

### Phase 5: Config + Prefab + Visual Polish

**Rationale:** Конфигурация и визуальная доводка. Prefab не нужен до момента интеграции.
**Delivers:** GameData.RocketData struct, Rocket prefab (SpriteRenderer + Collider2D + ParticleSystem), ScriptableObject с параметрами.
**Addresses:** Визуал ракеты (спрайт), инверсионный след, конфигурация через ScriptableObject, взрыв VFX при попадании.

### Phase 6: HUD

**Rationale:** Финальная фаза -- UI информирование игрока. Зависит от RocketAmmoData (Phase 1) и визуальной сцены (Phase 5).
**Delivers:** HudData расширения (RocketCount, RocketReloadTime), HudVisual bindings, ObservableBridgeSystem расширение.
**Addresses:** HUD: количество ракет, таймер перезарядки.

### Phase Ordering Rationale

- Phase 1 первая -- все остальное зависит от ECS-компонентов. TDD возможен в EditMode без сцены.
- Phase 2 сразу после -- коллизии валидируют, что ракеты "работают" в игровом мире.
- Phase 3 связывает ECS и GameObject -- ракета становится видимой.
- Phase 4 дает игроку контроль -- первый playtest возможен.
- Phase 5-6 -- polish и UX, могут идти параллельно или в любом порядке.
- Каждая фаза тестируется изолированно. Фазы 1-4 обеспечивают MVP. Фазы 5-6 завершают feature.

### Research Flags

Фазы, требующие углубленного исследования при планировании:
- **Phase 1 (EcsRocketHomingSystem):** Единственная фаза с новой логикой (не копия существующего). Алгоритм homing + ToroidalDelta + target acquisition. Рекомендуется `/gsd-research-phase` для валидации математики и edge cases.

Фазы со стандартными паттернами (пропустить research-phase):
- **Phase 2:** Расширение EcsCollisionHandlerSystem -- по образцу PlayerBulletTag.
- **Phase 3:** Bridge Layer -- по образцу CreateBullet в EntitiesCatalog.
- **Phase 4:** Input + Game -- по образцу OnLaser.
- **Phase 5:** Config + Prefab -- по образцу BulletData/LaserData.
- **Phase 6:** HUD -- по образцу LaserShootCount/LaserReloadTime.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Все пакеты уже в проекте, версии проверены, API стабилен с Entities 1.0 |
| Features | HIGH | Table stakes и differentiators определены из ТЗ + анализ аналогов (Asteroids Deluxe) |
| Architecture | HIGH | Все паттерны подтверждены работающим кодом v1.1.0 (Gun, Laser, Bullet) |
| Pitfalls | HIGH | Основано на анализе кодовой базы + известные gamedev-паттерны homing missiles |

**Overall confidence:** HIGH

### Gaps to Address

- **Баланс turn rate vs speed:** Рекомендация 180-270 град/сек -- теоретическая. Требует playtesting и итераций. Оба параметра в конфиге -- легко настроить.
- **Physics Layer "Rocket":** Нужно ли выделять отдельный layer или переиспользовать PlayerBullet layer? Зависит от дизайн-решения: пуля врага уничтожает ракету? Решить при планировании Phase 2.
- **Trail implementation:** ParticleSystem vs TrailRenderer -- оба варианта жизнеспособны. ParticleSystem дает больше контроля, TrailRenderer проще в lifecycle. Решить при планировании Phase 5.
- **Ракета + вражеская пуля:** Дизайн-решение: ракета неуязвима к пулям или уничтожается? Влияет на collision matrix. Решить при планировании Phase 2.

## Sources

### Primary (HIGH confidence)
- Кодовая база проекта v1.1.0: EntityFactory.cs, EcsCollisionHandlerSystem.cs, EcsGunSystem.cs, EcsLaserSystem.cs, ShootEventProcessorSystem.cs, ObservableBridgeSystem.cs, GameObjectSyncSystem.cs, EntitiesCatalog.cs, Game.cs, HudVisual.cs, PlayerInput.cs
- Packages/manifest.json -- точные версии пакетов
- Unity Entities 1.4.5 API: IComponentData, ISystem, SystemAPI.Query, IBufferElementData

### Secondary (MEDIUM confidence)
- [Asteroids Deluxe -- Wikipedia](https://en.wikipedia.org/wiki/Asteroids_Deluxe) -- homing missiles как враг в оригинальном сиквеле
- [How to Create Homing Missiles in Unity -- GameDeveloper](https://www.gamedeveloper.com/business/how-to-create-homing-missiles-in-game-with-unity) -- target acquisition, turn rate
- [2D Homing Missile Algorithm -- GitHub yoyoberenguer](https://github.com/yoyoberenguer/Homing-missile) -- orbit trap issue
- [GDevelop Homing Projectile docs](https://wiki.gdevelop.io/gdevelop5/extensions/homing-projectile/) -- turn rate, lifetime parameters

### Tertiary (LOW confidence)
- Баланс параметров (turn rate 180-270 deg/sec, lifetime 3-5 sec, max rockets 2-3) -- теоретические рекомендации, требуют playtesting

---
*Research completed: 2026-04-05*
*Ready for roadmap: yes*
