# Project Research Summary

**Project:** Asteroids
**Domain:** Миграция 2D-аркады с Unity 2022.3 + Built-in RP на Unity 6.3 + URP + гибридный DOTS
**Researched:** 2026-04-02
**Confidence:** MEDIUM

## Executive Summary

Asteroids -- 2D-аркада на Unity, использующая архитектуру Model-ViewModel-View через библиотеку shtl-mvvm, с WebGL как целевой платформой. Миграция включает три ортогональных направления: обновление движка (Unity 2022.3 -> 6.3), смена рендер-пайплайна (Built-in RP -> URP с 2D Renderer) и переход игровой логики на ECS (MonoBehaviour Model -> Entities). Исследование показало, что полный DOTS невозможен для 2D-проекта: Entities Graphics не поддерживает SpriteRenderer и WebGL, а DOTS Physics 2D отсутствует как verified-пакет. Единственный жизнеспособный подход -- гибридный DOTS, где ECS управляет данными и логикой, а GameObjects отвечают за рендеринг, физику и UI.

Рекомендуемый подход -- строгая последовательная миграция в 5 фаз: сначала апгрейд движка с критическими фиксами (shtl-mvvm, FindObjectsOfType), затем переход на URP, и только на стабильной базе -- поэтапная миграция на гибридный DOTS. Такой порядок минимизирует риск регрессий: каждая фаза завершается рабочей игрой, которую можно протестировать. URP и DOTS независимы друг от друга, но URP проще и быстрее, поэтому идет первой.

Главные риски: (1) shtl-mvvm сломается при первой сборке из-за удаленного пакета com.unity.textmeshpro -- требуется фикс библиотеки с обратной совместимостью; (2) визуал станет розовым после включения URP без конвертации материалов; (3) Physics2D может незаметно изменить поведение коллизий; (4) WebGL несовместим с Entities Graphics и System.Threading -- гибридный подход и Burst Jobs обязательны. Все риски управляемы при правильной последовательности и тестировании после каждой фазы.

## Key Findings

### Recommended Stack

Unity 6.3 LTS с com.unity.entities 1.4.x для ECS, URP 17.4.x с 2D Renderer для рендеринга, существующий Physics2D (Box2D v3) для коллизий. TMP слит в com.unity.ugui 2.0.x -- отдельный пакет удален. Все UGS-пакеты (Authentication, Leaderboards) совместимы без изменений. Подробности в [STACK.md](STACK.md).

**Core technologies:**
- **Unity 6.3 LTS (6000.3):** Игровой движок -- LTS с поддержкой до декабря 2027, включает Box2D v3 и Render Graph
- **com.unity.entities 1.4.x:** ECS-фреймворк -- verified-пакет, основа гибридного DOTS
- **URP 17.4.x с 2D Renderer:** Рендер-пайплайн -- замена Built-in RP, поддержка 2D Lighting и Shader Graph
- **com.unity.ugui 2.0.x:** UI + встроенный TextMeshPro -- заменяет отдельный TMP-пакет
- **com.shtl.mvvm (git):** MVVM-биндинги -- требует фикса package.json для совместимости с Unity 6

**Критические версионные ограничения:**
- C# 9.0 (Unity 6 не поддерживает 10+)
- com.unity.textmeshpro **удален** из реестра Unity 6 -- нужно удалить из manifest.json
- com.unity.collections обновится с 1.2.4 до 2.6.x (мажорное обновление через зависимость Entities)

### Expected Features

Подробности в [FEATURES.md](FEATURES.md).

**Must have (table stakes):**
- Апгрейд проекта на Unity 6.3 с сохранением компиляции
- Фикс shtl-mvvm для TMP-совместимости с обратной совместимостью Unity 2022.3+
- Миграция материалов/шейдеров на URP (все спрайты видимы)
- Сохранение Physics2D коллизий (OnCollisionEnter2D, RaycastNonAlloc)
- Гибридный DOTS: IComponentData для 8 компонентов, ISystem для 8 систем
- WebGL-совместимость всего стека (Burst Jobs, без System.Threading)

**Should have (differentiators):**
- URP 2D Lighting (свечение пуль, лазера, взрывов)
- Burst-компиляция чистых ECS-систем (Move, Rotate, Thrust)
- URP Post-Processing (Bloom, Vignette)
- SystemAPI.Query для типобезопасных запросов вместо ручных словарей

**Defer (v2+):**
- 2D Lighting и Post-Processing -- визуальные улучшения после стабильной миграции
- Box2D v3 low-level API -- работает параллельно со старым, переход необязателен
- Shader Graph эффекты -- декоративные
- SubScene для уровней -- избыточно для одноэкранной аркады
- UI Toolkit -- shtl-mvvm заточена под uGUI, миграция UI -- отдельный будущий milestone

### Architecture Approach

Архитектура "логика в Entities, рендеринг на GameObjects" с тремя слоями: ECS World (данные + логика), Bridge Layer (синхронизация Entity -> Transform, Input -> ECS, ECS -> ObservableValue), MonoBehaviour Layer (SpriteRenderer, Physics2D, uGUI + shtl-mvvm). Подробности в [ARCHITECTURE.md](ARCHITECTURE.md).

**Major components:**
1. **ECS World** -- IComponentData (MoveData, RotateData, ThrustData, GunData, LaserData, LifeTimeData, ShootToData, MoveToData) + ISystem/SystemBase (8 систем игровой логики)
2. **Bridge Layer** -- GameObjectSyncSystem (Entity -> Transform), InputBridgeSystem (PlayerInput -> ECS), ObservableBridgeSystem (ECS -> MVVM), CollisionBridgeSystem (Physics2D -> DeadTag), EntityViewRegistry (Entity <-> GameObject маппинг + пул)
3. **MonoBehaviour Layer** -- Visual-компоненты (SpriteRenderer, ParticleSystem), UI-экраны (uGUI + shtl-mvvm), Physics2D коллайдеры, PlayerInput

**Key patterns:**
- Managed component GameObjectRef (IComponentData class) для ссылки Entity -> GameObject
- ICleanupComponentData для автоматического возврата GO в пул при уничтожении entity
- Однонаправленная синхронизация ECS -> Transform (main thread, ~200 объектов -- приемлемо)
- ObservableBridge для прозрачной интеграции ECS-данных с shtl-mvvm биндингами

### Critical Pitfalls

Подробности в [PITFALLS.md](PITFALLS.md).

1. **shtl-mvvm ломается из-за TMP-зависимости** -- заменить `"com.unity.textmeshpro"` на `"com.unity.ugui": "1.0.0"` в package.json библиотеки; проверить assembly forwarding для `Unity.TextMeshPro` asmdef-ссылки
2. **Entities Graphics несовместим с WebGL** -- не использовать вообще; рендеринг только через GameObjects с SpriteRenderer
3. **Розовые материалы после URP** -- обязательный бэкап (git branch) перед конвертацией; Render Pipeline Converter для 2D; ручная проверка ParticleSystem
4. **Physics2D регрессии** -- тестировать коллизии, raycast, инерцию после каждого этапа; отдельно проверять WebGL
5. **System.Threading ломает WebGL** -- все Jobs через Burst; async через Awaitable вместо Task; никакого System.Threading

## Implications for Roadmap

### Phase 1: Unity 6.3 Upgrade + Critical Fixes
**Rationale:** Все остальное зависит от успешного апгрейда движка. Самая блокирующая зависимость.
**Delivers:** Проект компилируется и запускается на Unity 6.3 с полной функциональностью 1:1.
**Addresses:** Апгрейд проекта, фикс shtl-mvvm (TMP), ре-импорт TMP Essentials, замена FindObjectsOfType, тестирование Physics2D.
**Avoids:** Pitfall 1 (TMP dependency), Pitfall 4 (Physics2D regression), Pitfall 6 (FindObjectsOfType).

### Phase 2: URP Migration
**Rationale:** URP независима от DOTS, проще и быстрее. Дает стабильную визуальную базу для дальнейшей работы.
**Delivers:** Проект рендерится через URP 2D Renderer. Все спрайты, частицы, UI корректны.
**Uses:** com.unity.render-pipelines.universal 17.4.x, Render Pipeline Converter, URP Asset + 2D Renderer Data.
**Avoids:** Pitfall 3 (розовые материалы) -- конвертация через официальный инструмент + ручная проверка ParticleSystem.

### Phase 3: ECS Foundation (Components + Systems)
**Rationale:** Самая сложная часть миграции. Начинается с чистого нового кода (IComponentData, ISystem) без затрагивания существующей архитектуры.
**Delivers:** Полный набор ECS-компонентов и систем, работающих изолированно (можно тестировать без GameObjects).
**Implements:** ECS World -- компоненты, архетипы, EntityFactory, 8 игровых систем с правильным порядком обновления.
**Avoids:** Pitfall 2 (Entities Graphics WebGL) -- архитектурное решение "без Entities Graphics" принимается до начала.

### Phase 4: Bridge Layer + Integration
**Rationale:** Соединяет ECS World с существующим визуальным и UI-слоем. Зависит от Phase 3.
**Delivers:** Полностью работающая игра на гибридном DOTS. Старый Model-слой заменен на ECS.
**Implements:** GameObjectSyncSystem, InputBridgeSystem, ObservableBridgeSystem, CollisionBridgeSystem, EntityViewRegistry.
**Avoids:** Pitfall 5 (WebGL threading) -- все Jobs через Burst, тестирование WebGL после интеграции.

### Phase 5: Optimization + Visual Enhancements
**Rationale:** Оптимизация на работающей базе. Опционально, но завершает миграцию.
**Delivers:** Burst-компилированные системы, 2D Lighting, Post-Processing (Bloom).
**Uses:** [BurstCompile] для чистых ISystem (Move, Rotate, Thrust, LifeTime), URP 2D Light, Volume system.

### Phase Ordering Rationale

- **Phase 1 -> Phase 2:** URP требует Unity 6.3. После апгрейда визуал на Built-in RP еще работает, но конвертация на URP -- логичный следующий шаг.
- **Phase 2 -> Phase 3:** URP не зависит от DOTS, но дает стабильный рендеринг до начала архитектурных изменений. DOTS-миграция не должна решать проблемы рендеринга одновременно.
- **Phase 3 -> Phase 4:** ECS-компоненты и системы создаются как новый код рядом с существующим. Bridge и интеграция подключают их к живой игре только когда ECS-слой проверен.
- **Phase 4 -> Phase 5:** Оптимизация и украшения -- только на работающей игре. Burst-компиляция может выявить проблемы с blittable-типами, которые проще фиксить на стабильной базе.
- **WebGL-тестирование:** Обязательно после каждой фазы, не только в конце.

### Research Flags

Фазы, требующие углубленного исследования при планировании:
- **Phase 1 (Unity 6.3 Upgrade):** Фикс shtl-mvvm требует проверки assembly forwarding `Unity.TextMeshPro` на практике. versionDefines vs простая замена зависимости -- нужна валидация.
- **Phase 3 (ECS Foundation):** Маппинг существующих 8 систем на ECS требует детального анализа каждой системы. Тороидальная обертка, предиктивное прицеливание UFO -- нетривиальная логика.
- **Phase 4 (Bridge + Integration):** Паттерн CollisionBridge (Physics2D -> ECS) слабо документирован для 2D. Задержка коллизий в 1 кадр из-за синхронизации Transform -- нужна оценка влияния.

Фазы со стандартными паттернами (исследование при планировании необязательно):
- **Phase 2 (URP Migration):** Хорошо задокументированный процесс через Render Pipeline Converter. Официальные гайды покрывают 100% задач.
- **Phase 5 (Optimization):** Burst-компиляция и URP 2D Lighting -- стандартные, хорошо документированные возможности.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Версии пакетов подтверждены через docs.unity3d.com/6000.3; матрица совместимости верифицирована |
| Features | HIGH | Четкое разделение на table stakes / differentiators / anti-features; зависимости между фичами определены |
| Architecture | MEDIUM | Гибридный DOTS-подход широко используется, но 2D-специфика (SpriteRenderer + Physics2D + ECS) слабо документирована. Конкретные паттерны (GameObjectRef, ObservableBridge) -- авторские решения, не из официальных гайдов |
| Pitfalls | MEDIUM-HIGH | Критические питфоллы подтверждены официальной документацией и форумами Unity. Physics2D-регрессии -- LOW confidence (только пользовательские отчеты) |

**Overall confidence:** MEDIUM

### Gaps to Address

- **Assembly forwarding Unity.TextMeshPro в Unity 6:** Множественные источники утверждают что работает, но требует практической проверки при первой сборке. Если не работает -- потребуется правка asmdef в shtl-mvvm.
- **Задержка коллизий в 1 кадр:** При гибридном подходе ECS обновляет позицию, затем GameObjectSyncSystem пишет в Transform, затем Physics2D обрабатывает коллизии на следующем FixedUpdate. Для аркады при 60 FPS это приемлемо, но нужна практическая проверка.
- **DOTS overhead для ~50 сущностей:** ECS архитектура добавляет сложность без measurable performance gain при малом количестве объектов. Это осознанный trade-off ради обучения и архитектурной чистоты, но стоит замерить до/после.
- **com.unity.collections мажорное обновление (1.2 -> 2.6):** Транзитивная зависимость от Entities. Может потребовать изменений если проект использует NativeArray/NativeHashMap напрямую.
- **WebGL + Physics2D float precision:** Различия в обработке denormalized float между WASM и нативными платформами. Нет надежного способа предотвратить кроме тестирования.

## Sources

### Primary (HIGH confidence)
- [Unity 6.3 LTS Release](https://unity.com/blog/unity-6-3-lts-is-now-available) -- общая информация о релизе
- [Unity 6.3 What's New](https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html) -- новые возможности
- [Unity 6.0 Upgrade Guide](https://docs.unity3d.com/6000.3/Documentation/Manual/UpgradeGuideUnity6.html) -- breaking changes
- [Unity 6.3 Released Packages](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html) -- verified пакеты
- [Render Pipeline Converter](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/features/rp-converter.html) -- конвертация Built-in -> URP
- [Entities 1.4 Manual](https://docs.unity3d.com/Packages/com.unity.entities@1.4/manual/index.html) -- документация Entities
- [Unity 6.3 Web Technical Limitations](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-technical-overview.html) -- ограничения WebGL
- [Burst Multithreading on Web](https://docs.unity3d.com/6000.5/Documentation/Manual/web-multithreading-burst.html) -- Burst Jobs на WebGL

### Secondary (MEDIUM confidence)
- [TextMesh Pro in Unity 6](https://discussions.unity.com/t/textmesh-pro-in-unity-6/1580163) -- статус TMP слияния
- [ECS Development Status Dec 2025](https://discussions.unity.com/t/ecs-development-status-december-2025/1699284) -- статус DOTS
- [2D Physics with DOTS](https://discussions.unity.com/t/any-2d-physics-solutions-with-dots/1654080) -- отсутствие DOTS 2D Physics
- [Entities Graphics + WebGL](https://discussions.unity.com/t/webgl-platform-support-for-entities-graphics/918881) -- отсутствие WebGL поддержки
- [Entity Graphics + URP 2D](https://discussions.unity.com/t/is-entity-graphics-supposed-to-work-with-urps-2d-renderer-at-all/916046) -- ограничения для 2D
- [needle-mirror/com.unity.entities](https://github.com/needle-mirror/com.unity.entities/releases) -- версии пакетов

### Tertiary (LOW confidence)
- [Physics2D Issues after Unity 6 Upgrade](https://discussions.unity.com/t/2d-physics-in-unity-6-issues/949606) -- пользовательские отчеты о регрессиях, нет официального подтверждения

---
*Research completed: 2026-04-02*
*Ready for roadmap: yes*
