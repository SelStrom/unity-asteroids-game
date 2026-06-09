# Workflow Tool — встроенный справочник API + паттерны

> Источник: описание инструмента Workflow в системном промпте Claude Code.
> Официальной публичной спеки на этот JS-API нет — это и есть эталон.

---

## Что такое workflow и когда он нужен

Workflow — JS-скрипт, детерминированно оркестрирующий множество субагентов.
Берись за него, когда задача требует:
- **полноты** — декомпозировать и закрыть параллельно;
- **уверенности** — независимые перспективы + адверсариальная проверка перед коммитом;
- **масштаба**, который не влезает в один контекст (миграции, аудиты, широкие свипы).

Скрипт запускается в фоне: вызов сразу возвращает task ID, по завершении приходит
`<task-notification>`. Живой прогресс — команда `/workflows`.

**Один вызов = одна well-scoped fan-out фаза.** Для больших работ — несколько
workflow подряд (understand → design → implement → review), читая результат
каждого перед следующим.

---

## Обязательный блок `meta`

Каждый скрипт начинается с `export const meta = {...}` — ЧИСТЫЙ литерал
(никаких переменных, вызовов, спредов, интерполяции).

```js
export const meta = {
  name: 'find-flaky-tests',                 // required
  description: 'Find flaky tests and fix',  // required, одна строка
  whenToUse: '...',                         // optional, показывается в списке
  phases: [                                 // optional, по одной на каждый phase()
    { title: 'Scan', detail: '...' },
    { title: 'Fix',  detail: '...', model: 'opus' }, // model — если фаза с override
  ],
}
// тело скрипта начинается здесь
```

Заголовки в `meta.phases` сопоставляются с `phase()` ТОЧНО по строке.
`phase()` без записи в meta получает собственную группу в прогрессе.

---

## API тела скрипта

### `agent(prompt, opts?) → Promise`
Спавнит субагента.
- Без `schema` → возвращает финальный текст (строку).
- С `schema` (JSON Schema) → агент вынужден вызвать StructuredOutput, возвращается
  провалидированный объект (парсить не надо).
- Возвращает `null`, если пользователь пропустил агента (фильтруй `.filter(Boolean)`).

`opts`:
| поле | смысл |
|---|---|
| `label` | переопределяет отображаемую метку |
| `phase` | явно привязывает агента к группе прогресса (нужно внутри pipeline/parallel — глобальный `phase()` там даёт гонки) |
| `schema` | JSON Schema → структурированный вывод |
| `model` | `'opus'`/`'sonnet'`/`'haiku'`. **По умолчанию НЕ указывать** — наследует модель главного цикла. Override только при явной уверенности |
| `isolation: 'worktree'` | свежий git worktree. ДОРОГО (~200-500ms + диск). Только если агенты параллельно мутируют файлы и конфликтовали бы |
| `agentType` | кастомный тип субагента (напр. `'Explore'`) вместо дефолтного; композится со schema |

### `pipeline(items, stage1, stage2, ...) → Promise<any[]>`  ← ДЕФОЛТ
Каждый item проходит ВСЕ стадии независимо, БЕЗ барьера между стадиями.
Item A может быть на стадии 3, пока item B ещё на стадии 1.
Wall-clock = самая медленная одиночная цепочка, не сумма-по-стадиям.
Каждый callback получает `(prevResult, originalItem, index)`.
Стадия, кинувшая исключение, роняет этот item в `null` и пропускает его остаток.

### `parallel(thunks) → Promise<any[]>`  ← БАРЬЕР
Запускает задачи конкурентно, ЖДЁТ всех. Thunk, кинувший исключение,
становится `null` (сам вызов не реджектится) → `.filter(Boolean)` перед использованием.
Использовать ТОЛЬКО когда реально нужны все результаты вместе.

### `phase(title)` / `log(message)`
`phase` — начать новую фазу (группировка прогресса).
`log` — строка-нарратор над деревом прогресса.

### `args`
Значение, переданное в Workflow как `args`, дословно (undefined если не задано).
Передавай массивы/объекты как настоящий JSON, НЕ как строку.

### `budget`
`{ total: number|null, spent(): number, remaining(): number }`
Цель из директивы пользователя вида «+500k». `total` = null если не задана.
Пул общий между главным циклом и всеми workflow. `total` — ЖЁсткий потолок:
по достижении дальнейшие `agent()` кидают. Для динамических циклов гвардить на `budget.total`
(иначе `remaining()` = Infinity и цикл крутится до cap 1000).

### `workflow(nameOrRef, args?) → Promise`
Запускает другой workflow инлайн. Вложенность — только 1 уровень.

---

## Барьер vs pipeline — правило выбора

**По умолчанию pipeline.** Барьер (`parallel` между стадиями) оправдан ТОЛЬКО когда
стадия N нуждается в кросс-item контексте ВСЕХ результатов стадии N-1:
- dedup/merge по всему набору перед дорогой downstream-работой;
- early-exit если суммарный счёт = 0;
- промпт стадии N ссылается на «другие находки» для сравнения.

Барьер НЕ оправдан «надо сначала flatten/map/filter» (делай это внутри стадии pipeline),
«стадии концептуально раздельны», «так чище». Smell-test: если средний transform между
двумя `parallel` не имеет кросс-item зависимости — это pipeline.

---

## Лимиты
- Конкурентность: `min(16, cpu-2)` агентов одновременно (остальные в очереди).
- Всего на run: cap 1000 агентов (backstop от runaway).
- Скрипт = чистый JS, НЕ TypeScript (типы/интерфейсы/generics не парсятся).
- Запрещены: `Date.now()`, `Math.random()`, безаргументный `new Date()` (ломают resume).
  Таймстемпы — через `args` или ставить после возврата; рандом — варьировать промпт по индексу.
- Нет доступа к ФС и Node API. Стандартные built-ins (JSON/Math/Array) — есть.

---

## Паттерны

### Канонический pipeline: review → verify (каждый dimension верифицируется сразу)
```js
const results = await pipeline(
  DIMENSIONS,
  d => agent(d.prompt, { label: `review:${d.key}`, phase: 'Review', schema: FINDINGS }),
  review => parallel(review.findings.map(f => () =>
    agent(`Adversarially verify: ${f.title}`, { phase: 'Verify', schema: VERDICT })
      .then(v => ({ ...f, verdict: v }))))
)
const confirmed = results.flat().filter(Boolean).filter(f => f.verdict?.isReal)
```

### Барьер когда нужен dedup по всем находкам
```js
const all = await parallel(DIMENSIONS.map(d => () => agent(d.prompt, { schema: FINDINGS })))
const deduped = dedupeByFileAndLine(all.filter(Boolean).flatMap(r => r.findings))
const verified = await parallel(deduped.map(f => () => agent(verifyPrompt(f), { schema: VERDICT })))
```

### Loop-until-count (накопить до цели)
```js
const bugs = []
while (bugs.length < 10) {
  const r = await agent('Find bugs.', { schema: BUGS })
  bugs.push(...r.bugs); log(`${bugs.length}/10`)
}
```

### Loop-until-budget (масштаб под «+500k»; гвард на budget.total!)
```js
const bugs = []
while (budget.total && budget.remaining() > 50_000) {
  const r = await agent('Find bugs.', { schema: BUGS })
  bugs.push(...r.bugs)
}
```

### Loop-until-dry + diverse-lens panel (находить, пока 2 раунда не пусто)
```js
const seen = new Set(), confirmed = []; let dry = 0
while (dry < 2) {
  const found = (await parallel(FINDERS.map(f => () =>
    agent(f.prompt, { phase: 'Find', schema: BUGS })))).filter(Boolean).flatMap(r => r.bugs)
  const fresh = found.filter(b => !seen.has(key(b)))   // dedup vs seen, НЕ vs confirmed!
  if (!fresh.length) { dry++; continue }
  dry = 0; fresh.forEach(b => seen.add(key(b)))
  const judged = await parallel(fresh.map(b => () =>
    parallel(['correctness','security','repro'].map(lens => () =>
      agent(`Judge "${b.desc}" via ${lens} lens — real?`, { phase: 'Verify', schema: VERDICT })))
      .then(vs => ({ b, real: vs.filter(Boolean).filter(v => v.real).length >= 2 }))))
  confirmed.push(...judged.filter(v => v.real).map(v => v.b))
}
```

### Прочие quality-паттерны (компонуй под задачу)
- **Adversarial verify** — N независимых скептиков на находку, каждому промпт «опровергни»; убиваем при большинстве «refuted».
- **Perspective-diverse verify** — каждому верификатору своя линза (correctness/security/perf/repro), а не N одинаковых.
- **Judge panel** — N независимых попыток с разных углов → параллельные судьи → синтез из победителя.
- **Multi-modal sweep** — агенты ищут разными способами (by-container/by-content/by-entity/by-time).
- **Completeness critic** — финальный агент «что пропущено?»; найденное → следующий раунд.
- **No silent caps** — если ограничил покрытие (top-N, sampling) — `log()` что отброшено.

Масштабируй под запрос: «найди баги» → пара finder'ов, single-vote.
«тщательно проаудируй» → большой пул, 3-5 голосов, стадия синтеза.

---

## Resume
Результат содержит `runId`. Перезапуск: `Workflow({ scriptPath, resumeFromRunId })`.
Самый длинный неизменённый префикс `agent()`-вызовов возвращается из кэша мгновенно;
первый изменённый/новый вызов и всё после — выполняется заново.
Тот же скрипт + те же args → 100% cache hit. Только в рамках той же сессии;
перед resume останови прошлый run (`TaskStop`).

## Итерация по скрипту
Каждый вызов Workflow персистит скрипт в файл под session-директорией и возвращает путь.
Чтобы дорабатывать — редактируй ЭТОТ файл (Write/Edit) и вызывай `Workflow({ scriptPath })`,
вместо повторной отправки полного `script`.
