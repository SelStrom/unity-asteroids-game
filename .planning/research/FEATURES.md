# Feature Landscape

**Домен:** Миграция 2D-аркады Asteroids с Unity 2022.3 + Built-in RP на Unity 6.3 + URP + гибридный DOTS
**Дата исследования:** 2026-04-02

## Table Stakes

Функции, без которых миграция считается неуспешной. Весь текущий геймплей должен работать 1:1.

| Функция | Почему обязательна | Сложность | Примечания |
|---------|-------------------|-----------|------------|
| Апгрейд проекта на Unity 6.3 | Базовое требование, без него невозможны URP и DOTS | Low | Автоматический апгрейд через Unity Hub, ручная проверка скриптов |
| Фикс shtl-mvvm: TMP как встроенный модуль | В Unity 6 TMP слит с uGUI (com.unity.ugui), пакет com.unity.textmeshpro удалён. shtl-mvvm имеет явную зависимость на него | Med | Нужен conditional reference: #if для версий 2022.3 vs 6.x, обратная совместимость обязательна |
| Миграция материалов/шейдеров на URP | Спрайты на Built-in шейдерах станут розовыми/чёрными | Low | Render Pipeline Converter (Window > Rendering) автоматизирует. Для 2D-спрайтов замена минимальна: Sprites-Default -> Sprite-Lit-Default |
| Настройка URP 2D Renderer Asset | URP требует Pipeline Asset + 2D Renderer. Без них рендеринг не работает | Low | Создать URP Asset, выбрать 2D Renderer, назначить в Project Settings > Graphics |
| Сохранение Physics2D коллизий | Игра использует OnCollisionEnter2D и Physics2D.RaycastNonAlloc для лазера | Low | Physics2D работает в URP без изменений, это не зависит от рендер-пайплайна |
| Сохранение Input System | Проект уже на новом Input System (1.19.0) с .inputactions | Low | Совместим с Unity 6.3, изменений не требуется |
| Миграция TMP Essential Resources | При апгрейде на Unity 6 нужен ре-импорт TMP ресурсов (шрифты, шейдеры) | Low | Появится popup при первом открытии — принять и ре-импортировать |
| Гибридный DOTS: IComponentData для игровой логики | Перенос MoveComponent, ThrustComponent и т.д. на ECS IComponentData | High | Ядро миграции. 8 систем, 5 типов сущностей. Требует Baker для каждого типа |
| Гибридный DOTS: SystemBase/ISystem для систем | Перенос MoveSystem, ThrustSystem и др. на ECS SystemBase или ISystem | High | Замена ручного цикла Model.Update на World.Update. Порядок через [UpdateBefore/After] |
| Гибридный DOTS: сохранение GameObject Views | UI и визуалы (ShipVisual, AsteroidVisual) остаются на MonoBehaviour + shtl-mvvm | Med | Companion components или managed references. SpriteRenderer на entities — companion component |
| WebGL-совместимость DOTS | Текущий проект собирается под WebGL. Burst/Jobs должны работать | Med | Burst поддерживает WebGL (WASM). Но нет многопоточности в WebGL — Jobs выполняются синхронно |

## Differentiators

Функции, которые даёт новый стек поверх 1:1 миграции. Не обязательны, но ценны.

| Функция | Ценность | Сложность | Примечания |
|---------|----------|-----------|------------|
| URP 2D Lighting | 2D-источники света для визуальных эффектов: свечение пуль, лазера, взрывов | Med | Не было возможности в Built-in RP. Sprite-Lit-Default материал уже поддерживает. Требует добавления 2D Light компонентов |
| Burst-компиляция ECS систем | Значительное ускорение числовых расчётов (движение, физика прицеливания UFO) | Low | Для простой аркады прирост академический, но демонстрирует возможности стека |
| Box2D v3 (новый 2D Physics API) | Улучшенная производительность, детерминизм, отладка физики | Low | Unity 6.3 добавляет low-level API на Box2D v3. Работает параллельно со старым API. Для аркады не критично |
| Shader Graph для 2D эффектов | Визуальный редактор шейдеров для кастомных эффектов (свечение, искажение) | Med | URP + Shader Graph — мощная связка. В Built-in RP Shader Graph ограничен |
| URP Post-Processing (Bloom, Vignette) | Глобальные визуальные эффекты: bloom на выстрелах, виньетка | Low | Встроен в URP Volume system. Kawase/Dual filtering bloom в Unity 6.3 |
| Sprite Atlas Analyzer | Инструмент оптимизации атласов спрайтов | Low | Новый в Unity 6.3. Полезен для оптимизации draw calls |
| SubScene для уровней | Стриминг и инкрементальная загрузка сцен через DOTS SubScene | Low | Для одноэкранной аркады избыточно, но полезно как архитектурный паттерн |
| Entities Query + SystemAPI | Типобезопасные запросы к компонентам без ручных словарей | Low | Заменяет кастомный Dictionary<IGameEntityModel, TNode> в BaseModelSystem<TNode> |

## Anti-Features

Функции, которые сознательно НЕ нужно внедрять при миграции.

| Анти-функция | Почему избегать | Что делать вместо |
|-------------|-----------------|-------------------|
| Полный DOTS (без GameObjects для визуалов) | Entities Graphics не поддерживает SpriteRenderer нативно — только как companion component. Полный DOTS для 2D спрайтов требует кастомного рендеринга или NSprites | Гибридный подход: ECS для логики, GameObjects для визуалов |
| DOTS 2D Physics (com.unity.2d.entities.physics) | Пакет в preview, не production-ready. Нет verified-версии для Unity 6.3 | Оставить стандартный Physics2D. Коллизии обрабатывать в MonoBehaviour, результаты передавать в ECS |
| Unity AI (Beta) | Beta в Unity 6.3, не стабильно для production pipeline | Игнорировать на этапе миграции |
| Netcode for Entities | Проект не мультиплеерный, онлайн только лидерборд через UGS | Оставить UGS Leaderboard API как есть |
| CoreCLR / .NET 9 runtime | В roadmap Unity 2026, но ещё не доступно в 6.3. Mono backend стабилен | Оставить Mono. Перейти на CoreCLR когда станет verified |
| Миграция shtl-mvvm на UI Toolkit | UI Toolkit ещё не полностью заменяет uGUI для runtime UI в играх. shtl-mvvm заточена под uGUI | Оставить uGUI + shtl-mvvm. UI Toolkit — отдельный будущий milestone |
| Миграция всех багов одновременно | 7 известных критических багов. Исправление при миграции увеличивает риск регрессий | Баги — отдельный milestone после миграции (как указано в PROJECT.md) |
| Новый Input System Actions Asset рефакторинг | Текущий Input System уже работает (v1.19.0 с .inputactions) | Оставить как есть, работает без изменений |

## Feature Dependencies

```
Unity 6.3 Upgrade
  ├── TMP Migration (shtl-mvvm fix)
  │     └── UI работоспособность (все экраны)
  ├── URP Migration
  │     ├── Material/Shader конвертация
  │     ├── 2D Renderer Asset настройка
  │     └── [опционально] 2D Lighting
  │           └── [опционально] Post-Processing (Bloom)
  └── Hybrid DOTS Migration
        ├── IComponentData определения
        ├── Baker авторинг компонентов
        ├── ISystem/SystemBase системы
        ├── Companion Objects (SpriteRenderer на entities)
        └── World lifecycle (замена Model.Update)
```

Ключевые зависимости:
- **Unity 6.3 Upgrade** обязан быть первым — всё остальное от него зависит
- **TMP fix** блокирует любую работу с UI после апгрейда
- **URP** и **DOTS** независимы друг от друга, но обе зависят от Unity 6.3
- **URP** проще и быстрее, поэтому логично делать перед DOTS
- **DOTS** — самая сложная часть, делать последней на стабильной базе

## MVP Recommendation

### Приоритет 1: Unity 6.3 + TMP совместимость
1. Апгрейд проекта на Unity 6.3
2. Фикс shtl-mvvm для TMP как встроенного модуля (с обратной совместимостью)
3. Ре-импорт TMP Essential Resources
4. Проверка компиляции и запуска

### Приоритет 2: URP миграция
1. Установка URP пакета, создание 2D Renderer Asset
2. Конвертация материалов через Render Pipeline Converter
3. Проверка визуального соответствия 1:1

### Приоритет 3: Гибридный DOTS
1. Определение IComponentData для каждого компонента (Move, Thrust, Rotate, Gun, Laser, LifeTime, ShootTo, MoveTo)
2. Baker авторинг для 5 типов сущностей
3. Перенос 8 систем на ISystem с Burst
4. Интеграция с GameObject Views через companion components
5. Замена Model lifecycle на World lifecycle

### Отложить:
- **2D Lighting / Post-Processing**: визуальные улучшения — отдельный milestone после стабильной миграции
- **Box2D v3 API**: работает параллельно со старым API, переход необязателен
- **Shader Graph эффекты**: декоративные, не влияют на функциональность

## Sources

- [Unity 6.3 LTS Release Notes](https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html)
- [Unity 6.3 LTS Blog Post](https://unity.com/blog/unity-6-3-lts-is-now-available)
- [URP Upgrade from Built-in](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/upgrading-from-birp.html)
- [URP 2D Sprite Preparation](https://docs.unity3d.com/6000.1/Documentation/Manual/urp/PrepShader.html)
- [Entities 1.4 Baker Documentation](https://docs.unity3d.com/Packages/com.unity.entities@1.4/manual/ecs-workflow-example-authoring-baking.html)
- [Entities Graphics Manual](https://docs.unity3d.com/6000.2/Documentation/Manual/com.unity.entities.graphics.html)
- [ECS Development Status March 2025](https://discussions.unity.com/t/ecs-development-status-milestones-march-2025/1615810)
- [TextMeshPro in Unity 6 Discussion](https://discussions.unity.com/t/textmesh-pro-in-unity-6/1580163)
- [Render Pipeline Feature Comparison](https://docs.unity3d.com/6000.3/Documentation/Manual/render-pipelines-feature-comparison.html)
- [DOTS 2D Physics Discussion](https://discussions.unity.com/t/any-2d-physics-solutions-with-dots/1654080)
- [Input System Migration Guide](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.18/manual/Migration.html)
