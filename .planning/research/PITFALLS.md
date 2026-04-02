# Pitfalls Research

**Domain:** Миграция Unity 2022.3 LTS -> Unity 6.3 + URP + Hybrid DOTS
**Researched:** 2026-04-02
**Confidence:** MEDIUM-HIGH (верифицировано через официальную документацию и community-опыт)

## Critical Pitfalls

### Pitfall 1: shtl-mvvm ломается в Unity 6 из-за зависимости на com.unity.textmeshpro

**What goes wrong:**
Пакет `com.shtl.mvvm` (package.json) объявляет жесткую зависимость `"com.unity.textmeshpro": "3.0.7"`. Начиная с Unity 2023.2 TextMeshPro объединен в пакет `com.unity.ugui` и отдельный пакет `com.unity.textmeshpro` **удален** из реестра Unity 6. При попытке resolve зависимостей UPM не найдет пакет и **заблокирует импорт shtl-mvvm**. Весь UI проекта (5 экранов, HUD, лидерборд) станет неработоспособным.

Дополнительно: `.asmdef` файл библиотеки (`Shtl.Mvvm.asmdef`) ссылается на assembly `"Unity.TextMeshPro"`. В Unity 6 TMP-код находится в assembly `"Unity.ugui"` (или `"Unity.TextMeshPro"` как forwarded). Нужно проверить, работает ли assembly forwarding автоматически или требуется ручная правка.

**Why it happens:**
TMP был отдельным пакетом в Unity 2020-2022. Многие библиотеки объявляют его как зависимость напрямую. Unity 6 объединила TMP в ugui, но пакеты третьих сторон (и собственные) не обновились автоматически.

**How to avoid:**
1. В `package.json` библиотеки shtl-mvvm заменить жесткую зависимость на условную, используя `versionDefines` в `.asmdef`:
   ```json
   // package.json: убрать "com.unity.textmeshpro" из dependencies
   // Вместо этого использовать условный define в asmdef
   "versionDefines": [
     {
       "name": "com.unity.textmeshpro",
       "expression": "3.0.0",
       "define": "SHTL_TMP_PACKAGE"
     },
     {
       "name": "com.unity.ugui",
       "expression": "2.0.0",
       "define": "SHTL_TMP_UGUI"
     }
   ]
   ```
2. В C#-коде использовать `#if UNITY_6000_0_OR_NEWER` для переключения между `using TMPro` (работает в обоих случаях, namespace сохранен) и ссылками на assembly.
3. В `.asmdef` добавить `"Unity.ugui"` как reference (в Unity 6 TMP живет там) и оставить `"Unity.TextMeshPro"` для обратной совместимости через `defineConstraints` или `versionDefines`.
4. Ключевое требование: **обратная совместимость с Unity 2022.3+** — фикс должен работать на обеих версиях.

**Warning signs:**
- Ошибки компиляции `Assembly 'Shtl.Mvvm' references 'Unity.TextMeshPro' which could not be found` при открытии проекта в Unity 6
- UPM показывает `com.shtl.mvvm` как broken package
- Розовые/отсутствующие компоненты на всех Canvas-объектах

**Phase to address:**
Фаза 1 (Unity 6.3 Upgrade) — **самый первый шаг**, без него проект не соберется.

---

### Pitfall 2: Entities Graphics не поддерживает WebGL — невозможно рендерить ECS-сущности

**What goes wrong:**
Пакет `com.unity.entities.graphics` (ранее Hybrid Renderer) **не поддерживает WebGL платформу**. Если геймплейные сущности (астероиды, пули, корабль, UFO) переведены на ECS и их рендеринг зависит от Entities Graphics — WebGL билд перестанет работать. Проект **обязан** поддерживать WebGL.

**Why it happens:**
Entities Graphics использует compute shaders и indirect rendering для батчинга ECS-сущностей. WebGL 2.0 не поддерживает compute shaders, а WebGPU пока экспериментальный. Unity не реализовала fallback для WebGL.

**How to avoid:**
1. **Гибридный подход обязателен:** ECS — только для логики и данных. Визуализация — через обычные GameObjects с SpriteRenderer/ParticleSystem.
2. Использовать паттерн "ECS data, GameObject presentation": ISystem обновляет компоненты, отдельная система синхронизирует позиции из ECS в Transform GameObjects.
3. **Не использовать `com.unity.entities.graphics` вообще** — рендерить через существующую систему Visual + shtl-mvvm bindings.
4. Текущая архитектура (Model -> ViewModel -> Visual) уже является гибридной. DOTS заменяет Model-слой, ViewModel/Visual остаются на GameObjects.

**Warning signs:**
- Build error при сборке WebGL с подключенным `com.unity.entities.graphics`
- Черный экран в WebGL билде при наличии Entity-based рендеринга
- Ошибки в консоли: `Entities Graphics is not supported on this platform`

**Phase to address:**
Фаза 3 (Hybrid DOTS) — архитектурное решение должно быть принято **до начала** миграции на ECS.

---

### Pitfall 3: Built-in RP материалы становятся розовыми после переключения на URP

**What goes wrong:**
Все материалы, использующие Built-in шейдеры (Sprites-Default, Particles/Standard Unlit, UI/Default), перестают рендериться после установки URP Pipeline Asset в Graphics Settings. Спрайты астероидов, корабля, UFO, эффекты частиц — всё станет розовым (magenta = missing shader).

**Why it happens:**
URP использует собственные шейдеры, несовместимые с Built-in RP. `Sprites-Default` -> `Universal Render Pipeline/2D/Sprite-Lit-Default`. Переключение происходит глобально при установке URP Asset, и **нет возможности откатить** без ручного сохранения backup.

**How to avoid:**
1. **Бэкап проекта перед конвертацией** — git commit/branch обязателен.
2. Использовать **Render Pipeline Converter** (Window > Rendering > Render Pipeline Converter) для автоматической конвертации 2D материалов.
3. Создать URP Pipeline Asset с **2D Renderer** (не Forward/Deferred — это 3D рендереры).
4. Проверить все префабы после конвертации — ParticleSystem на эффектах взрывов может потребовать ручной замены материала.
5. Проверить UI-материалы — Canvas/uGUI обычно работает без изменений, но кастомные UI-шейдеры ломаются.

**Warning signs:**
- Розовые (magenta) спрайты в Scene/Game view
- Warnings в консоли: `Shader 'Sprites/Default' is not supported`
- ParticleSystem эффекты невидимы или некорректны

**Phase to address:**
Фаза 2 (URP Migration) — выполнять только после стабильной работы проекта на Unity 6.3.

---

### Pitfall 4: Physics2D поведение изменяется незаметно при апгрейде на Unity 6

**What goes wrong:**
Unity 6 содержит изменения в Physics2D, которые могут привести к отличающемуся поведению коллизий и физики. Множество разработчиков сообщают о "странном поведении физики" после апгрейда. В проекте Asteroids физика критична: коллизии корабля/пуль/астероидов, тороидальный экран, raycast лазера, движение с инерцией.

Дополнительный риск: в WebGL билде физика может давать **другие результаты** из-за различий в обработке denormalized floating-point чисел (WebAssembly использует полную точность, нативные платформы — нет).

**Why it happens:**
Unity 6 включает внутренние оптимизации и рефакторинг Physics2D engine. Поведение floating-point операций на разных платформах не гарантировано идентичным.

**How to avoid:**
1. **Тестировать физику после каждого этапа миграции** — корабль, астероиды, коллизии, лазер raycast.
2. Сравнивать поведение до и после: скорость корабля, инерция, углы столкновений.
3. Проверить `RaycastNonAlloc` — API стабилен, но поведение Physics2D.defaultContactOffset и другие параметры могли измениться.
4. Проверить WebGL билд отдельно — физика **будет** отличаться от Editor.

**Warning signs:**
- Объекты "проваливаются" сквозь друг друга
- Лазер не попадает в астероиды (raycast изменил точность)
- Астероиды ведут себя иначе при фрагментации
- Различия между Editor и WebGL билдом

**Phase to address:**
Фаза 1 (Unity 6.3 Upgrade) — тестирование физики обязательно после апгрейда.

---

### Pitfall 5: C# threading код ломает WebGL с DOTS

**What goes wrong:**
При добавлении DOTS (Job System, Burst) разработчики склонны использовать `System.Threading` API. В WebGL managed threads **полностью запрещены**. Burst-compiled Jobs работают многопоточно на WebGL (начиная с Burst 1.8.26), но **обычный C#** System.Threading — нет.

В текущем коде UGS сервис использует async/await (корутины + `WaitUntil`). При миграции на DOTS можно случайно ввести зависимость от threading.

**Why it happens:**
Разработчики привыкли к `Task.Run()`, `async/await`, `CancellationTokenSource` на desktop. WebGL — AOT-платформа с single-threaded GC, и `System.Threading` не работает.

**How to avoid:**
1. **Все Job System код — через Burst.** Burst-compiled Jobs работают многопоточно на WebGL.
2. Для async-операций использовать `Awaitable` API (Unity 6) вместо `Task`.
3. Никакого `System.Threading.Timer`, `Thread`, `ThreadPool` — заменять на корутины/Awaitable.
4. Тестировать **каждый** DOTS-паттерн в WebGL билде, а не только в Editor.

**Warning signs:**
- `NotSupportedException: System.Threading` в WebGL runtime
- WebGL билд зависает при инициализации UGS
- Jobs молча fallback на main thread (нет ошибки, но производительность как без DOTS)

**Phase to address:**
Фаза 3 (Hybrid DOTS) — архитектурное правило с первого дня.

---

### Pitfall 6: FindObjectsOfType API удален — компиляция провалится

**What goes wrong:**
`Object.FindObjectsOfType` и `Object.FindObjectOfType` помечены как **obsolete** в Unity 6.0 и генерируют ошибки компиляции (не warnings). Если код проекта или shtl-mvvm используют эти API — сборка сломается.

**Why it happens:**
Unity заменила эти методы на `Object.FindObjectsByType` / `Object.FindFirstObjectByType` / `Object.FindAnyObjectByType` с явным параметром `FindObjectsSortMode`.

**How to avoid:**
1. Поискать все вхождения `FindObjectsOfType` / `FindObjectOfType` в коде проекта и shtl-mvvm.
2. Заменить на `FindObjectsByType(FindObjectsSortMode.None)` (если порядок не важен — быстрее) или `FindObjectsSortMode.InstanceID` (если важен детерминизм).
3. Проверить shtl-mvvm library code — если библиотека использует эти API, фикс нужен **в библиотеке**.

**Warning signs:**
- `CS0619: 'Object.FindObjectOfType<T>()' is obsolete` при компиляции
- Ошибки в пакетах из PackageCache, которые нельзя редактировать напрямую

**Phase to address:**
Фаза 1 (Unity 6.3 Upgrade) — исправить при первой компиляции.

---

## Technical Debt Patterns

Shortcuts that seem reasonable but create long-term problems.

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Обернуть весь DOTS код в `#if !UNITY_WEBGL` | Быстро работает в Editor | Два code path, невозможно тестировать DOTS на WebGL | Никогда — Burst Jobs работают на WebGL, используй гибридный подход |
| Скопировать shtl-mvvm в Assets/ вместо фикса библиотеки | Мгновенное решение TMP проблемы | Форк без обновлений, два источника правды | Только как временная мера на 1-2 дня |
| Пропустить URP конвертацию и оставить Built-in | "Работает же" | Несовместимость с новыми фичами Unity 6, 2D Lighting, Shader Graph | Никогда — URP обязателен для DOTS rendering pipeline |
| Использовать MonoBehaviour Systems рядом с ECS | Знакомый паттерн | Две системы обновления, сложная отладка, GC vs native memory | Только для UI/Visual слоя (это и есть гибридный подход) |
| Оставить Mono backend вместо IL2CPP | Быстрее итерации | Burst на WebGL требует IL2CPP; DOTS-оптимизации требуют AOT | Только при разработке в Editor; production build — IL2CPP |

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| UGS Authentication | API пакетов `com.unity.services.*` может требовать обновления для Unity 6 | Обновить до последних версий пакетов **до** апгрейда Unity; проверить совместимость на странице UGS Release Notes |
| shtl-mvvm git package | Git URL без тега/ветки (`#v1.0.0`) — UPM кеширует и не обновляет | Указать конкретную ветку или тег: `"https://github.com/SelStrom/shtl-mvvm.git#unity6-compat"` |
| Input System | Upgrade на Unity 6 может потребовать регенерации `PlayerActions` | Переоткрыть `.inputactions` asset и пересохранить generated C# class |
| TextMeshPro данные | TMP Essential Resources / Examples импортированы в Assets/ | При апгрейде Unity может потребовать повторного импорта TMP Essentials; в Unity 6 путь к ресурсам изменен |
| ParticleSystem + URP | Particle materials используют Built-in шейдеры | Render Pipeline Converter **не конвертирует** ParticleSystem materials автоматически — проверить вручную `VfxBlowPrefab` |

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| DOTS на маленьком проекте: overhead ECS > benefit | Более медленная загрузка, сложнее код, нулевой прирост FPS | Замерять **до и после** миграции; для 50 сущностей DOTS overhead может превышать выигрыш | С первого дня — проект имеет ~20-50 одновременных сущностей |
| GC на WebGL: сборка мусора раз в кадр | Статтеры при аллокациях, заметные на слабых устройствах | Минимизировать аллокации (CONCERNS.md #11-15 уже документирует проблемы); DOTS NativeArrays помогут | При активном геймплее на мобильных браузерах |
| Burst compilation time для WebGL | Время сборки WebGL увеличивается в 2-5x | Использовать `[BurstCompile]` только на hot path; Profile guided compilation | При каждом WebGL билде |

## "Looks Done But Isn't" Checklist

- [ ] **Unity 6 Upgrade:** Проект открывается без ошибок, **но** физика может вести себя иначе — проверить все коллизии
- [ ] **TMP Migration:** Текст отображается, **но** `TMP_InputField` на экране Score может потерять стили/шрифты — проверить ввод имени
- [ ] **URP Conversion:** Спрайты видимы, **но** ParticleSystem эффект взрыва (`VfxBlowPrefab`) может быть невидим — проверить стоп-калбэк `OnParticleSystemStopped`
- [ ] **URP 2D Renderer:** Rendering работает, **но** 2D Lighting по умолчанию включен и может изменить визуал — если не нужен, использовать Sprite-Unlit-Default
- [ ] **DOTS Hybrid:** ECS системы работают, **но** тороидальный экран (`PlaceWithinGameArea`) должен быть портирован точно — проверить wrap на обеих границах
- [ ] **WebGL Build:** Editor работает, **но** WebGL билд — отдельная платформа с другой физикой и без threading — тестировать отдельно после **каждого** этапа
- [ ] **UGS Services:** Лидерборд работает в Editor, **но** WebGL имеет CORS-ограничения — проверить Submit/Load scores в WebGL билде
- [ ] **shtl-mvvm обратная совместимость:** Фикс работает на Unity 6, **но** проверить, что библиотека по-прежнему работает на Unity 2022.3 (CI/CD на обеих версиях)

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| shtl-mvvm TMP dependency | LOW | Правка package.json + asmdef в отдельном репозитории, новый git tag, обновление URL в manifest.json |
| Розовые материалы после URP | LOW | `git checkout` для отката; повторный запуск Render Pipeline Converter с правильными настройками |
| Physics2D regression | MEDIUM | Сравнение поведения через запись геймплея; ручная подстройка Physics2D Project Settings |
| Entities Graphics WebGL fail | HIGH | Если уже написан ECS rendering — полный рефакторинг на GameObject presentation. Поэтому решение гибридного подхода принимается **заранее** |
| WebGL threading crash | MEDIUM | Замена System.Threading на Awaitable/Coroutines; может потребовать рефакторинга async-кода UGS |
| FindObjectsOfType compilation | LOW | Поиск и замена по проекту; 5-10 минут работы |

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| shtl-mvvm TMP dependency | Phase 1: Unity 6.3 Upgrade | Проект компилируется, UI отображается, shtl-mvvm bindings работают |
| FindObjectsOfType API | Phase 1: Unity 6.3 Upgrade | Zero compilation errors |
| Physics2D regression | Phase 1: Unity 6.3 Upgrade | Ручное тестирование: коллизии, raycast, wrap, инерция |
| Розовые материалы URP | Phase 2: URP Migration | Все спрайты видимы, ParticleSystem работает, UI корректен |
| URP 2D Renderer setup | Phase 2: URP Migration | 2D Renderer Asset назначен; ортографическая камера (size 22.5) сохранена |
| Entities Graphics WebGL | Phase 3: Hybrid DOTS | Архитектурное решение: ECS data + GameObject visual; WebGL билд работает |
| Threading в WebGL | Phase 3: Hybrid DOTS | WebGL билд запускается без System.Threading ошибок; Jobs через Burst |
| DOTS overhead на малом масштабе | Phase 3: Hybrid DOTS | Benchmark: FPS до/после миграции не деградирует |

## Sources

- [Unity 6.0 Upgrade Guide (official docs)](https://docs.unity3d.com/6000.3/Documentation/Manual/UpgradeGuideUnity6.html) — HIGH confidence
- [Unity 6.3 Web Technical Limitations](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-technical-overview.html) — HIGH confidence
- [Unity Render Pipeline Converter (official docs)](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/features/rp-converter.html) — HIGH confidence
- [Unity Conditional Compilation / Scripting Symbols](https://docs.unity3d.com/6000.2/Documentation/Manual/platform-dependent-compilation.html) — HIGH confidence, `UNITY_6000_0_OR_NEWER` для version checks
- [TextMeshPro merged into ugui (Unity Forums)](https://discussions.unity.com/t/textmesh-pro-in-unity-6/1580163) — MEDIUM confidence
- [TMP Development Thread (Unity Forums)](https://discussions.unity.com/t/2023-2-latest-development-on-textmesh-pro/917387) — MEDIUM confidence
- [Entities Graphics WebGL (Unity Forums)](https://discussions.unity.com/t/webgl-platform-support-for-entities-graphics/918881) — MEDIUM confidence, подтверждено отсутствие WebGL поддержки
- [WebGL + DOTS Threading (Unity Forums)](https://discussions.unity.com/t/webgl-and-dots/913496) — MEDIUM confidence
- [Burst Multithreading on Web (official docs)](https://docs.unity3d.com/6000.5/Documentation/Manual/web-multithreading-burst.html) — HIGH confidence, Burst Jobs работают на WebGL с версии 1.8.26
- [Physics2D Issues after Unity 6 Upgrade (Unity Forums)](https://discussions.unity.com/t/2d-physics-in-unity-6-issues/949606) — LOW confidence (user reports, no official confirmation)
- [UGS Release Notes](https://docs.unity.com/ugs/en-us/manual/overview/manual/release-notes) — MEDIUM confidence
- `com.shtl.mvvm` package.json и asmdef — LOCAL (прямая инспекция кода), HIGH confidence

---
*Pitfalls research for: Unity 2022.3 -> Unity 6.3 + URP + Hybrid DOTS Migration*
*Researched: 2026-04-02*
