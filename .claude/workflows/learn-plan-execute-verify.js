// ============================================================================
// УЧЕБНЫЙ WORKFLOW: plan → execute → verify с разными "усилиями" по этапам
// ----------------------------------------------------------------------------
// Цель — показать каркас, который ты сможешь запускать и разбирать.
// Идея: планирование на максимальном усилии (opus), исполнение на среднем
// (sonnet), механическое извлечение по схеме на дешёвой модели (haiku).
//
// Как запустить:
//   через Workflow tool с { name: "learn-plan-execute-verify",
//                           args: "Краткое описание задачи" }
// args придёт в скрипт как глобальная переменная `args` (строка).
// ============================================================================

// --- meta ОБЯЗАТЕЛЕН и должен быть ЧИСТЫМ литералом (без переменных/вызовов).
//     Заголовки фаз в meta.phases должны совпадать со строками в phase().
//     Поле model тут — только для отображения в прогрессе.
export const meta = {
  name: 'learn-plan-execute-verify',
  description: 'Учебный workflow: план (opus) → исполнение (sonnet) → проверка (sonnet) → сводка (haiku)',
  whenToUse: 'Когда учишься писать workflow и хочешь пощупать per-stage модели',
  phases: [
    { title: 'Plan',    detail: 'Декомпозиция задачи на шаги',        model: 'opus' },
    { title: 'Execute', detail: 'Проработка каждого шага',             model: 'sonnet' },
    { title: 'Verify',  detail: 'Адверсариальная проверка результата', model: 'sonnet' },
    { title: 'Summary', detail: 'Сборка финальной сводки по схеме',    model: 'haiku' },
  ],
}

// --- JSON Schema для структурированного вывода планировщика.
//     Когда передаёшь schema в agent(), агент ОБЯЗАН вызвать StructuredOutput,
//     а agent() вернёт уже провалидированный объект — парсить ничего не надо.
const PLAN_SCHEMA = {
  type: 'object',
  required: ['steps'],
  properties: {
    steps: {
      type: 'array',
      items: {
        type: 'object',
        required: ['id', 'title', 'detail'],
        properties: {
          id:     { type: 'integer' },
          title:  { type: 'string' },
          detail: { type: 'string', description: 'Что именно надо сделать на этом шаге' },
        },
      },
    },
  },
}

const VERDICT_SCHEMA = {
  type: 'object',
  required: ['ok', 'notes'],
  properties: {
    ok:    { type: 'boolean', description: 'Шаг проработан корректно?' },
    notes: { type: 'string',  description: 'Что не так / что подтверждено' },
  },
}

const SUMMARY_SCHEMA = {
  type: 'object',
  required: ['headline', 'bullets'],
  properties: {
    headline: { type: 'string' },
    bullets:  { type: 'array', items: { type: 'string' } },
  },
}

// Задача берётся из args; если ничего не передали — учебный дефолт.
const task = (typeof args === 'string' && args.trim())
  ? args.trim()
  : 'Учебная задача: описать, как добавить новый тип врага в игру Asteroids'

log(`Задача: ${task}`)

// ---------------------------------------------------------------------------
// ЭТАП 1 — PLAN. Max effort: opus. Здесь цена ошибки максимальна.
// ---------------------------------------------------------------------------
phase('Plan')
const plan = await agent(
  `Ты планировщик. Разбей задачу на 3-5 конкретных шагов.
Задача: ${task}
Верни список шагов через StructuredOutput.`,
  { phase: 'Plan', model: 'opus', schema: PLAN_SCHEMA }
)
log(`План готов: ${plan.steps.length} шаг(ов)`)

// ---------------------------------------------------------------------------
// ЭТАП 2+3 — EXECUTE → VERIFY через pipeline().
// pipeline = каждый шаг проходит ВСЕ стадии независимо, БЕЗ барьера:
// шаг #1 уже проверяется, пока шаг #3 ещё прорабатывается. Это дефолт.
// Стадия 1 (execute) на sonnet, стадия 2 (verify) тоже на sonnet.
// Обрати внимание: phase задаём в opts ЯВНО — внутри pipeline нельзя
// полагаться на глобальный phase() (возможны гонки между параллельными шагами).
// ---------------------------------------------------------------------------
const results = await pipeline(
  plan.steps,

  // стадия 1 — исполнение
  (step) => agent(
    `Проработай шаг плана детально.
Шаг #${step.id}: ${step.title}
Детали: ${step.detail}
Верни проработку обычным текстом.`,
    { label: `execute:#${step.id}`, phase: 'Execute', model: 'sonnet' }
  ),

  // стадия 2 — проверка. Получает (prevResult, originalItem, index).
  (execOutput, step) => agent(
    `Адверсариально проверь проработку шага. Старайся найти проблему.
Шаг: ${step.title}
Проработка: ${execOutput}
Верни вердикт через StructuredOutput.`,
    { label: `verify:#${step.id}`, phase: 'Verify', model: 'sonnet', schema: VERDICT_SCHEMA }
  ).then((verdict) => ({ step, execOutput, verdict }))
)

// pipeline возвращает массив; упавшие элементы становятся null — отфильтруем.
const clean = results.filter(Boolean)
const okCount = clean.filter(r => r.verdict.ok).length
log(`Проверено: ${okCount}/${clean.length} шагов прошли`)

// ---------------------------------------------------------------------------
// ЭТАП 4 — SUMMARY. Low effort: haiku. Чистая механика — сборка по схеме.
// Это и есть оправданный downgrade: не рассуждение, а форматирование.
// ---------------------------------------------------------------------------
phase('Summary')
const summary = await agent(
  `Собери краткую сводку по результатам.
Задача: ${task}
Результаты по шагам:
${clean.map(r => `- ${r.step.title}: ${r.verdict.ok ? 'OK' : 'ПРОБЛЕМА'} — ${r.verdict.notes}`).join('\n')}
Верни headline + bullets через StructuredOutput.`,
  { phase: 'Summary', model: 'haiku', schema: SUMMARY_SCHEMA }
)

// Возвращаемое значение workflow попадёт в результат Workflow tool.
return {
  task,
  stepsPlanned: plan.steps.length,
  stepsPassed: okCount,
  summary,
}
