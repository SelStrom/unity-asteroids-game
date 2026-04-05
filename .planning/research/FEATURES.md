# Feature Landscape

**Domain:** Самонаводящиеся ракеты для аркадного шутера Asteroids (Unity 6.3, гибридный DOTS)
**Researched:** 2026-04-05

## Table Stakes

Фичи, которые игроки ожидают от системы самонаводящихся ракет. Без них механика ощущается сломанной.

| Feature | Why Expected | Complexity | Dependencies | Notes |
|---------|--------------|------------|-------------|-------|
| Запуск ракеты по кнопке (R) | Базовый ввод; без него оружие недоступно | Low | Input System (`PlayerActions`), `RocketData` компонент, `RocketShootEvent` буфер | Аналог GunData.Shooting -- флаг + обработка в EcsRocketSystem |
| Наведение на ближайшую цель | Суть "самонаводящейся" ракеты; без этого это просто пуля | High | `ShipPositionData` singleton, QueryBuilder для AsteroidTag/UfoTag/UfoBigTag, `RocketHomingData` компонент | Поиск ближайшего врага + поворот Direction к цели каждый кадр |
| Ограниченный боезапас | Без лимита ракеты обесценивают пули и лазер | Low | `RocketData.MaxShoots`, `RocketData.CurrentShoots` | По аналогии с LaserData -- начальный запас из конфига |
| Респавн ракет по таймеру | Игрок должен получать новые ракеты, иначе механика одноразовая | Low | `RocketData.ReloadRemaining`, `RocketData.ReloadDurationSec` | Инкрементальная перезарядка как у лазера (по одной) |
| Время жизни ракеты (LifeTime) | Ракета не должна летать вечно -- забивает экран, нарушает баланс | Low | `LifeTimeData` (уже есть) | Переиспользуем существующий LifeTimeData + EcsDeadByLifeTimeSystem |
| Коллизия ракеты с врагами | Ракета должна уничтожать цели | Med | `EcsCollisionHandlerSystem`, новый `RocketTag` + `PlayerRocketTag` | Расширить ProcessCollision для ракет; ракета = одноразовое попадание |
| Коллизия ракеты со случайными врагами по пути | Ракета летит к цели, но поражает любого врага на пути | Low | Collider2D на GameObject ракеты | Физика Unity 2D обеспечит это автоматически через OnCollisionEnter2D |
| HUD: количество доступных ракет | Игрок должен видеть боезапас | Low | ObservableBridgeSystem, MVVM ReactiveValue | Аналог отображения лазерных зарядов в HUD |
| Визуал ракеты (спрайт) | Ракета должна быть видна и отличима от пули | Low | SpriteRenderer, GameObjectPool | По PROJECT.md: уменьшенный спрайт корабля |
| Инверсионный след (частицы) | Визуальная обратная связь полета ракеты; без следа ракета неотличима от пули | Med | ParticleSystem на GameObject ракеты, URP совместимость | Rate Over Distance emission для непрерывного следа; нужна подходящая скорость эмиссии чтобы не было разрывов |
| Конфигурация через ScriptableObject | Все параметры ракеты из конфигов, не захардкожены | Low | `RocketConfig` : `BaseGameEntityData`, GameData | Скорость, поворот (град/сек), макс. боезапас, время перезарядки, время жизни, очки |

## Differentiators

Фичи, которые выделяют реализацию. Не ожидаемы, но повышают ценность.

| Feature | Value Proposition | Complexity | Dependencies | Notes |
|---------|-------------------|------------|-------------|-------|
| Плавный поворот с ограниченным turn rate | Ракета летит по дуге, а не мгновенно меняет направление -- визуально красиво и создает контргейм (враги могут "уклониться") | Med | `RocketHomingData.TurnRateDegreesPerSec`, math.atan2 + clamp delta angle | Ключевой параметр баланса: слишком быстрый поворот = просто пуля, слишком медленный = промахи. Рекомендация: 180-270 град/сек |
| Переключение цели при уничтожении текущей | Если цель умерла до попадания, ракета ищет новую | Med | Проверка HasComponent<DeadTag> на targetEntity каждый кадр | Без этого ракета теряет смысл при быстром уничтожении целей пулями |
| Ракета вращается визуально по направлению полета | Спрайт разворачивается в сторону движения | Low | atan2 от Direction -> Quaternion.Euler в Visual/Bridge | Значительно улучшает "ощущение" ракеты |
| Взрыв при попадании (VFX) | Визуальная награда за попадание | Low | Существующий ExplosionVFX + GameObjectPool | Переиспользовать паттерн взрыва астероидов/UFO |
| HUD: таймер перезарядки ракет | Игрок видит когда придет следующая ракета | Low | ObservableBridgeSystem, MVVM | Аналог таймера лазера |

## Anti-Features

Фичи, которые НЕ следует реализовывать.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| Управляемая игроком ракета (player-guided missile) | Ломает flow аркадного геймплея -- игрок перестает управлять кораблем | Полностью автоматическое наведение после запуска (fire-and-forget) |
| Множественные типы ракет | Явно out of scope в PROJECT.md; усложняет баланс и UI | Один тип ракеты с конфигурируемыми параметрами |
| Proportional Navigation / предиктивное прицеливание | Избыточная сложность для аркады; PN предназначена для симуляторов. Цели (астероиды) движутся линейно -- простой seek достаточен | Простой seek: каждый кадр поворачивать Direction к текущей позиции цели с ограниченным turn rate |
| Бесконечный поворот (мгновенная смена направления) | Ракета становится неотличима от "умной пули"; теряется визуальная дуга полета | Ограниченный turn rate (180-270 град/сек) -- ракета реально промахивается по быстрым целям |
| Тороидальное наведение (учет wrap-around при расчете расстояния) | Крайне сложная геометрия: нужно проверять 9 "фантомных" позиций цели; непропорциональная сложность для edge case | Наводиться на "прямую" позицию цели. Тороидальное перемещение ракеты через MoveSystem уже работает -- ракета пройдет через край экрана и продолжит наводиться |
| Ракеты для UFO (враждебные homing) | Радикально меняет баланс; требует отдельную систему уклонения | Только игрок стреляет ракетами |
| Lock-on индикатор на цели | Перегружает минималистичный UI Asteroids | Ракета просто летит к ближайшему; игрок видит это по траектории |

## Feature Dependencies

```
Input (кнопка R)
  -> RocketData компонент на Ship entity
    -> EcsRocketSystem (перезарядка + событие запуска)
      -> RocketShootEvent буфер
        -> Спавн ракеты (EntityFactory.CreateRocket + GameObject)
          -> RocketHomingData + MoveData + LifeTimeData на rocket entity
            -> EcsRocketHomingSystem (наведение: поиск цели + поворот Direction)
              -> EcsMoveSystem (уже есть -- двигает по Direction)
                -> GameObjectSyncSystem (уже есть -- синхронизирует Position)
                  -> CollisionHandler (расширить для RocketTag)

Визуал (параллельно):
  Спавн ракеты -> RocketVisual MonoBehaviour
    -> SpriteRenderer (уменьшенный корабль)
    -> ParticleSystem (инверсионный след)
    -> Вращение спрайта по Direction

HUD (параллельно):
  RocketData -> ObservableBridgeSystem -> MVVM ReactiveValue -> HudVisual
```

## MVP Recommendation

Приоритет реализации (строгий порядок по зависимостям):

1. **RocketData + RocketConfig** -- ECS компонент и ScriptableObject конфиг (аналог LaserData)
2. **Input binding** -- кнопка R в PlayerActions.inputactions
3. **EcsRocketSystem** -- перезарядка + генерация RocketShootEvent (аналог EcsLaserSystem)
4. **EntityFactory.CreateRocket** -- спавн entity с RocketTag + MoveData + LifeTimeData + RocketHomingData
5. **EcsRocketHomingSystem** -- ядро: поиск ближайшего врага + поворот Direction с ограниченным turn rate
6. **CollisionHandler** -- расширить для PlayerRocketTag (ракета + враг = оба уничтожены + очки)
7. **RocketVisual** -- спрайт + ParticleSystem trail + вращение по направлению
8. **HUD** -- отображение боезапаса и таймера перезарядки

**Defer:**
- Переключение цели при смерти текущей: реализовать в MVP, но если сложно -- fallback на "ракета продолжает лететь прямо"
- Взрыв VFX при попадании: уже есть паттерн, добавить после базовой механики
- Таймер перезарядки в HUD: после базового счетчика ракет

## Complexity Assessment

| Компонент | Сложность | Обоснование |
|-----------|-----------|-------------|
| RocketData + Config | Low | Копия LaserData с другими параметрами |
| EcsRocketSystem | Low | Копия EcsLaserSystem |
| Input binding | Low | Одно новое действие в InputActions |
| EntityFactory.CreateRocket | Low | Аналог CreateBullet + доп. компоненты |
| EcsRocketHomingSystem | **High** | Новая логика: поиск цели среди всех врагов (Query), расчет угла, clamp поворота. Должна быть корректной математически |
| CollisionHandler расширение | Low | +2 ветки if в ProcessCollision |
| RocketVisual + Trail | Med | Новый MonoBehaviour + ParticleSystem настройка + вращение спрайта |
| HUD интеграция | Low | По аналогии с лазером через ObservableBridge |

**Общая оценка: Medium.** Основная сложность сконцентрирована в EcsRocketHomingSystem. Все остальное -- расширение существующих паттернов.

## Key Design Decisions

### Seek vs Proportional Navigation
**Решение: простой Seek с ограниченным turn rate.**
Каждый кадр: вычислить угол к цели, повернуть Direction максимум на `TurnRate * deltaTime` градусов. Это дает визуально красивую дугу и достаточную точность для аркадных целей.

### Burst-совместимость EcsRocketHomingSystem
**Проблема:** Поиск ближайшего врага требует итерации по всем entity с AsteroidTag/UfoTag/UfoBigTag. Это managed query, но может быть Burst-совместимым если использовать только IComponentData.
**Решение:** Система без BurstCompile (как EcsCollisionHandlerSystem и EcsGunSystem). Количество entity в Asteroids невелико (десятки), производительность не проблема.

### PlayerRocketTag vs PlayerBulletTag
**Решение: отдельный RocketTag + PlayerRocketTag.** Позволяет дифференцировать поведение в CollisionHandler. В будущем ракеты могут иметь другие правила (например, не уничтожаться при попадании в малый астероид). В CollisionHandler добавить `IsPlayerProjectile()` helper, объединяющий PlayerBulletTag и PlayerRocketTag.

### Тороидальный wrap для ракеты
**Решение: используем существующий EcsMoveSystem.** MoveData на ракете + EcsMoveSystem = автоматический wrap. Наведение не учитывает wrap (anti-feature выше), но ракета физически проходит через край экрана и корректно наводится на цель в "прямом" пространстве.

### Хранение цели в компоненте
**Решение: Entity targetEntity в RocketHomingData.** Каждый кадр проверяем: если target == Entity.Null или имеет DeadTag -- ищем новую цель. Это дешевле полного поиска каждый кадр и позволяет переключение цели.

## Sources

- [Asteroids Deluxe -- Wikipedia](https://en.wikipedia.org/wiki/Asteroids_Deluxe) -- homing missiles как враг в оригинальном сиквеле
- [How to Create Homing Missiles in Unity -- GameDeveloper](https://www.gamedeveloper.com/business/how-to-create-homing-missiles-in-game-with-unity) -- target acquisition, turn rate балансировка
- [2D Homing Missile Algorithm -- GitHub yoyoberenguer](https://github.com/yoyoberenguer/Homing-missile) -- visual cone, speed balancing, orbit trap issue
- [Unity Proportional Navigation Collection -- GitHub](https://github.com/Woreira/Unity-Proportional-Navigation-Collection) -- PN guidance (отвергнуто как избыточное для аркады)
- [Homing Missile Phaser 3 -- Ourcade](https://blog.ourcade.co/posts/2020/make-homing-missile-seek-target-arcade-physics-phaser-3/) -- bounding box rotation issues
- [GDevelop Homing Projectile docs](https://wiki.gdevelop.io/gdevelop5/extensions/homing-projectile/) -- turn rate, max speed, lifetime parameters
- Анализ кодовой базы: `EcsGunSystem.cs`, `EcsLaserSystem.cs`, `EcsCollisionHandlerSystem.cs`, `EntityFactory.cs` -- паттерны для расширения
