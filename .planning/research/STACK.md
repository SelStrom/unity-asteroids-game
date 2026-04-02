# Technology Stack: Миграция на Unity 6.3 + URP + Hybrid DOTS

**Проект:** Asteroids
**Дата исследования:** 2026-04-02
**Режим:** Миграция существующего проекта (Unity 2022.3.60f1 -> Unity 6.3 LTS)

## Рекомендуемый стек

### Игровой движок

| Технология | Версия | Назначение | Обоснование |
|------------|--------|------------|-------------|
| Unity | 6.3 LTS (6000.3) | Игровой движок | LTS с поддержкой до декабря 2027. Включает Box2D v3 для Physics2D, Render Graph для URP, улучшенную 2D-интеграцию. Уверенность: HIGH |

### Рендеринг

| Технология | Версия | Назначение | Обоснование |
|------------|--------|------------|-------------|
| com.unity.render-pipelines.universal | 17.4.x (встроен в 6000.3) | URP с 2D Renderer | Версия привязана к редактору, устанавливается автоматически. 2D Renderer входит в URP: поддержка 2D-света, Sprite Masks, Sorting Groups. Уверенность: HIGH |

### DOTS / ECS

| Технология | Версия | Назначение | Обоснование |
|------------|--------|------------|-------------|
| com.unity.entities | 1.4.x | ECS-фреймворк (системы, компоненты, запросы) | Verified-пакет для Unity 6.3. Базовый пакет для гибридного подхода. Уверенность: HIGH |
| com.unity.physics | 1.4.x | ECS-физика (3D-движок с 2D-ограничениями) | Verified-пакет. **ВНИМАНИЕ:** это 3D-физика, см. раздел "Критические решения". Уверенность: HIGH (версия), MEDIUM (применимость для 2D) |
| com.unity.collections | 2.6.x | NativeArray, NativeHashMap и др. | Verified-пакет, транзитивная зависимость Entities. Уверенность: HIGH |
| com.unity.burst | 1.8.x | Burst-компилятор для ISystem | Verified-пакет. Уже есть транзитивно (1.8.19), обновится автоматически. Уверенность: HIGH |
| com.unity.mathematics | 1.2.x+ | Математика для Burst-совместимых систем | Транзитивная зависимость. Уверенность: HIGH |

### UI и привязки

| Технология | Версия | Назначение | Обоснование |
|------------|--------|------------|-------------|
| com.unity.ugui | 2.0.x (встроен в 6000.3) | uGUI + встроенный TextMeshPro | В Unity 6 TMP слит в UGUI. Пакет com.unity.textmeshpro **больше не существует** как отдельный. Уверенность: HIGH |
| com.shtl.mvvm | git (с фиксом) | MVVM-привязки | Требует обновления package.json и возможно asmdef. См. стратегию миграции TMP. Уверенность: HIGH |

### Ввод

| Технология | Версия | Назначение | Обоснование |
|------------|--------|------------|-------------|
| com.unity.inputsystem | 1.19.x | Система ввода | Verified для Unity 6.3. Текущая версия 1.19.0 совместима. Уверенность: HIGH |

### Сервисы (UGS)

| Технология | Версия | Назначение | Обоснование |
|------------|--------|------------|-------------|
| com.unity.services.authentication | 3.6.1 | Анонимная аутентификация | Verified для 6000.3. Минорное обновление с 3.6.0. Уверенность: HIGH |
| com.unity.services.leaderboards | 2.3.3 | Лидерборд | Verified для 6000.3. Та же версия что и сейчас. Уверенность: HIGH |
| com.unity.services.core | 1.16.x+ | Базовый SDK UGS | Verified для 6000.3. Уверенность: HIGH |

### Скриптинг

| Параметр | Значение | Примечание |
|----------|----------|------------|
| Язык | C# 9.0 | Без изменений, Unity 6 не поддерживает C# 10+. Уверенность: HIGH |
| Scripting Backend | Mono (dev) / IL2CPP (prod) | Mono для быстрой итерации, IL2CPP для релизных сборок. Уверенность: HIGH |
| .NET Profile | .NET Standard 2.1 | Без изменений. Уверенность: HIGH |

---

## Критические решения по миграции

### 1. TextMeshPro: Слияние в UGUI (КРИТИЧНО для shtl-mvvm)

**Факт:** Начиная с Unity 2023.2, пакет `com.unity.textmeshpro` слит в `com.unity.ugui`. В Unity 6 отдельного пакета TMP больше нет.

**Что сохраняется:**
- Пространство имён `TMPro` -- без изменений
- Ссылка в asmdef `"Unity.TextMeshPro"` -- продолжает работать (assembly forwarding)
- Все API (`TMP_Text`, `TMP_InputField` и т.д.) -- без изменений

**Что ломается:**
- В `package.json` зависимость `"com.unity.textmeshpro": "3.0.7"` -- **пакет не существует** в реестре Unity 6

**Стратегия фикса shtl-mvvm (обратная совместимость с Unity 2022.3+):**

```json
// package.json -- ДО
{
  "dependencies": {
    "com.unity.textmeshpro": "3.0.7",
    "com.unity.nuget.newtonsoft-json": "3.2.2"
  }
}

// package.json -- ПОСЛЕ
{
  "dependencies": {
    "com.unity.ugui": "1.0.0",
    "com.unity.nuget.newtonsoft-json": "3.2.2"
  }
}
```

**Обоснование:** `com.unity.ugui` 1.0.0 существует и в Unity 2022.3, и в Unity 6. В Unity 2022.3 TMP подтягивается через `com.unity.textmeshpro` в manifest проекта (уже есть). В Unity 6 TMP встроен в UGUI. Пространство имён `TMPro` и asmdef-ссылка `Unity.TextMeshPro` работают в обоих случаях.

**Asmdef `Shtl.Mvvm.asmdef`** -- менять **НЕ нужно**:
```json
{
  "references": ["Unity.TextMeshPro"]  // Работает и в 2022.3, и в Unity 6
}
```

**Уверенность:** MEDIUM -- логика верна на основании документации Unity, но требует практической проверки при первой сборке. Ссылка `Unity.TextMeshPro` в asmdef подтверждена множественными источниками как работающая.

### 2. Built-in RP -> URP: Миграция 2D-проекта

**Порядок миграции:**
1. Установить пакет URP через Package Manager
2. Создать URP Asset + 2D Renderer Data
3. Назначить URP Asset в Project Settings > Graphics
4. Запустить конвертер: Window > Rendering > Render Pipeline Converter
5. Выбрать "Built-In Render Pipeline 2D to URP 2D"
6. Initialize Converters -> Convert Assets

**Что конвертируется автоматически:**
- Материалы 2D (Sprites-Default -> URP 2D variants)
- Ссылки на материалы в компонентах

**Что потребует ручной работы:**
- Проверить все спрайты (SpriteRenderer) -- должны использовать URP-совместимые материалы
- Проверить ParticleSystem (эффекты взрывов) -- материалы частиц
- Проверить камеру -- добавить UniversalAdditionalCameraData
- Шейдер LineRenderer (эффект лазера) -- проверить совместимость

**Важно для 2D:** Режим освещения по умолчанию может поменяться (gamma -> linear). Проект использует простые спрайты без 2D-света, поэтому визуальные отличия минимальны, но нужно проверить яркость.

**Уверенность:** HIGH (процесс документирован Unity)

### 3. Гибридный DOTS: Архитектурный подход

**КРИТИЧЕСКОЕ ОГРАНИЧЕНИЕ: Нет ECS-поддержки 2D-физики**

Пакет `com.unity.physics` работает **только с 3D-физикой** (Havok / Unity Physics). Пакет `com.unity.2d.entities.physics` находится в **preview** и не является Verified для Unity 6.3.

**Рекомендованный подход для Asteroids:**

**Вариант A (рекомендуется): ECS для логики, GameObject для физики и рендеринга**
- Entities: хранение данных (позиция, скорость, здоровье, компоненты систем)
- ISystem / SystemBase: игровая логика (Move, Rotate, Thrust, Gun, Laser, Lifetime, ShootTo, MoveTo)
- MonoBehaviour: визуальное представление (SpriteRenderer, ParticleSystem, LineRenderer)
- Physics2D (GameObject): коллизии остаются на стороне Unity Physics2D
- Синхронизация: ECS -> GameObject (позиция, вращение через CompanionGameObject или ручной sync)

**Вариант B (альтернатива): ECS для логики, кастомная коллизионная логика**
- Полностью вычислять коллизии в ECS (AABB / Circle overlap) вместо Physics2D
- Плюс: нет зависимости от GameObject-физики
- Минус: нужно переписать всю коллизионную логику, включая лазерный raycast

**Рекомендация: Вариант A** -- минимизирует объём переписывания, сохраняет Physics2D.RaycastNonAlloc для лазера, хорошо ложится на существующую архитектуру.

**Уверенность:** MEDIUM -- подход гибридного DOTS с GameObject-физикой широко используется в сообществе, но 2D-специфика документирована слабо.

### 4. Entities Graphics и 2D-рендеринг

**Факт:** `com.unity.entities.graphics` **не поддерживает** URP 2D Renderer и SpriteRenderer. Он работает только с MeshRenderer + 3D-шейдерами.

**Решение:** НЕ использовать Entities Graphics. Рендеринг остаётся на стороне GameObject (SpriteRenderer, ParticleSystem, LineRenderer). ECS управляет только данными и логикой.

**Уверенность:** HIGH -- подтверждено Unity Discussions и документацией.

---

## Пакеты: Что меняется при миграции

### Добавляемые пакеты

| Пакет | Версия | Назначение |
|-------|--------|------------|
| com.unity.render-pipelines.universal | (встроен) | URP -- добавить через Package Manager |
| com.unity.entities | 1.4.x | ECS-фреймворк |
| com.unity.physics | 1.4.x | **Опционально** -- только если нужна 3D ECS-физика (для Asteroids вероятно НЕ нужен) |

### Обновляемые пакеты

| Пакет | Было | Станет | Примечание |
|-------|------|--------|------------|
| com.unity.ugui | 1.0.0 | 2.0.x | Обновится автоматически, теперь включает TMP |
| com.unity.burst | 1.8.19 | 1.8.x | Может обновиться минорно |
| com.unity.collections | 1.2.4 | 2.6.x | Мажорное обновление через зависимость Entities |
| com.unity.mathematics | 1.2.6 | 1.2.x+ | Минорное обновление |
| com.unity.services.authentication | 3.6.0 | 3.6.1 | Минорный патч |
| com.unity.feature.2d | 2.0.1 | Проверить | Метапакет, обновится под Unity 6 |
| com.shtl.mvvm | git (c7bda1c) | git (новый коммит) | Фикс package.json для TMP-совместимости |

### Удаляемые пакеты

| Пакет | Причина |
|-------|---------|
| com.unity.textmeshpro | 3.0.9 | Слит в UGUI, удалить из manifest.json |

### Без изменений

| Пакет | Версия | Примечание |
|-------|--------|------------|
| com.unity.inputsystem | 1.19.0 | Совместим |
| com.unity.services.leaderboards | 2.3.3 | Совместим |
| com.unity.services.core | 1.16.0 | Совместим |
| com.unity.timeline | 1.7.7 | Совместим |
| com.unity.test-framework | 1.1.33 | Совместим |

---

## Рассмотренные альтернативы

| Категория | Рекомендация | Альтернатива | Почему нет |
|-----------|-------------|-------------|------------|
| Рендер-пайплайн | URP с 2D Renderer | HDRP | HDRP не поддерживает 2D Renderer. Проект 2D -- только URP. |
| ECS-физика 2D | GameObject Physics2D | com.unity.physics (3D) | Потребуется эмулировать 2D через ограничения Z-оси. Усложнение без выгоды для простого 2D. |
| ECS-физика 2D | GameObject Physics2D | com.unity.2d.entities.physics | Preview-пакет, не Verified. Нестабилен, может сломаться. |
| ECS-рендеринг 2D | GameObject SpriteRenderer | Entities Graphics | Не поддерживает URP 2D Renderer и SpriteRenderer. |
| ECS-рендеринг 2D | GameObject SpriteRenderer | NSprites (community) | Сторонний пакет, дополнительная зависимость, нет гарантий поддержки. |
| Полный DOTS | Гибридный DOTS | Полный DOTS без GameObject | Нет 2D-физики, нет 2D-рендеринга в ECS, UI через UGUI требует GameObject. |
| Scripting backend | Mono (dev) + IL2CPP (prod) | Только IL2CPP | Mono быстрее для итерации в редакторе, IL2CPP только для билдов |

---

## Матрица совместимости

| Компонент | Unity 2022.3 | Unity 6.3 | Совместимость |
|-----------|-------------|-----------|---------------|
| C# 9.0 | Да | Да | Полная |
| com.unity.textmeshpro 3.0.9 | Да | **НЕТ** | Удалить, TMP теперь в UGUI |
| TMPro namespace | Да | Да | Полная |
| Unity.TextMeshPro asmdef | Да | Да | Полная (assembly forwarding) |
| com.unity.ugui 1.0.0 | Да | 2.0.x | Обратная совместимость |
| com.unity.inputsystem 1.19.0 | Да | Да | Полная |
| UGS Authentication 3.6.x | Да | Да | Полная |
| UGS Leaderboards 2.3.3 | Да | Да | Полная |
| Physics2D (MonoBehaviour) | Да | Да | Полная, Box2D v3 в Unity 6.3 |
| Object.FindObjectsOfType | Да | **Obsolete** | Заменить на FindObjectsByType |
| Enlighten GI | Да | **Удалён** | Заменить Progressive Lightmapper (не используется в проекте) |
| Built-in RP | Да | Да (но мигрируем) | URP предпочтителен |

---

## Установка

```bash
# Порядок миграции (шаг за шагом, НЕ всё сразу):

# Шаг 1: Открыть проект в Unity 6.3 через Hub
# Unity автоматически предложит обновить пакеты

# Шаг 2: Обновить manifest.json
# Удалить: "com.unity.textmeshpro": "3.0.9"
# URP добавится через Package Manager

# Шаг 3: Обновить shtl-mvvm (перед открытием проекта или сразу после)
# В shtl-mvvm/package.json:
#   заменить "com.unity.textmeshpro": "3.0.7" на "com.unity.ugui": "1.0.0"

# Шаг 4: Установить URP через Package Manager
# Window > Package Manager > Universal RP > Install

# Шаг 5: Настроить URP
# Create > Rendering > URP Asset (2D Renderer)
# Project Settings > Graphics > Scriptable Render Pipeline Settings = созданный Asset

# Шаг 6: Конвертировать материалы
# Window > Rendering > Render Pipeline Converter
# "Built-In Render Pipeline 2D to URP 2D" > Convert

# Шаг 7: Добавить Entities
# Package Manager > com.unity.entities > Install
```

---

## Что НЕ использовать

| Технология | Причина |
|------------|---------|
| com.unity.entities.graphics | Не поддерживает URP 2D Renderer. Для 2D-спрайтов бесполезен. |
| com.unity.2d.entities.physics | Preview-пакет, нестабилен, не Verified. |
| com.unity.physics (для 2D) | 3D-физика; эмуляция 2D через ограничения -- усложнение без пользы. |
| HDRP | Не поддерживает 2D Renderer. |
| Полный DOTS без GameObject | Нет поддержки 2D-рендеринга и 2D-физики в Entities. UI через UGUI требует GameObject. |
| C# 10+ | Unity 6 не поддерживает выше C# 9.0. |

---

## Оценка уверенности

| Область | Уверенность | Обоснование |
|---------|-------------|-------------|
| Версии пакетов (Entities, URP, UGS) | HIGH | Подтверждено через docs.unity3d.com/6000.3 released packages |
| TMP-миграция (namespace, asmdef) | MEDIUM | Множественные источники на Unity Discussions подтверждают, но требует практической проверки |
| TMP-миграция (package.json fix для shtl-mvvm) | MEDIUM | Логически верно, но нужно протестировать на обеих версиях Unity |
| URP 2D конвертация | HIGH | Документированный процесс через Render Pipeline Converter |
| Гибридный DOTS + GameObject Physics2D | MEDIUM | Широко используемый подход, но мало документации именно для 2D |
| Entities Graphics не для 2D | HIGH | Подтверждено Unity Discussions и документацией |

---

## Источники

- [Unity 6.3 LTS Release](https://unity.com/blog/unity-6-3-lts-is-now-available) -- общая информация о релизе
- [Unity 6.3 What's New](https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html) -- новые возможности
- [Unity 6.3 Upgrade Guide](https://docs.unity3d.com/6000.3/Documentation/Manual/UpgradeGuideUnity63.html) -- breaking changes 6.2 -> 6.3
- [Unity 6.0 Upgrade Guide](https://docs.unity3d.com/6000.3/Documentation/Manual/UpgradeGuideUnity6.html) -- breaking changes 2022 -> 6.0
- [Unity 6.3 Released Packages](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html) -- verified пакеты и версии
- [Render Pipeline Converter](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/features/rp-converter.html) -- конвертация Built-in -> URP
- [URP 17.4 Changelog](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.4/changelog/CHANGELOG.html) -- версия URP для 6000.3
- [Entities 1.4 Manual](https://docs.unity3d.com/Packages/com.unity.entities@1.4/manual/index.html) -- документация Entities
- [ECS Development Status Dec 2025](https://discussions.unity.com/t/ecs-development-status-december-2025/1699284) -- статус DOTS
- [TextMesh Pro in Unity 6](https://discussions.unity.com/t/textmesh-pro-in-unity-6/1580163) -- статус TMP
- [TMP and UGUI Merged](https://discussions.unity.com/t/textmesh-pro-and-ugui-merged-clarification-on-where-to-get-the-latest-tmp-for-urp/1603892) -- детали слияния
- [2D Physics with DOTS](https://discussions.unity.com/t/any-2d-physics-solutions-with-dots/1654080) -- ограничения 2D-физики в ECS
- [Entity Graphics + URP 2D](https://discussions.unity.com/t/is-entity-graphics-supposed-to-work-with-urps-2d-renderer-at-all/916046) -- ограничения Entities Graphics
- [Render Pipelines Strategy 2026](https://unity.com/topics/render-pipelines-strategy-for-2026) -- стратегия развития пайплайнов
