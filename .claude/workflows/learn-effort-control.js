// ============================================================================
// УЧЕБНЫЙ WORKFLOW v2: два способа управлять "усилием" в workflow
// ----------------------------------------------------------------------------
// Показывает разницу между:
//   (A) ТИР МОДЕЛИ  — opts.model: opus/sonnet/haiku — доступен инлайн в agent()
//   (B) REASONING/EFFORT — задаётся НЕ инлайн, а через субагент-файл
//       (.claude/agents/deep-planner.md) с frontmatter `effort: max`,
//       который зовётся через opts.agentType.
//
// Почему так: у inline-вызова agent() в opts есть только `model`, поля `effort`
// нет. Единственный способ задать reasoning конкретному шагу — вынести его в
// субагент-файл и подключить через agentType.
//
// ВАЖНО про грубую ручку: общий /effort сессии всё равно наследуется ВСЕМИ
// агентами этого workflow по умолчанию. agentType+frontmatter переопределяет
// effort точечно только для шага Plan.
//
// Запуск: Workflow tool с { name: "learn-effort-control", args: "твоя задача" }
// ============================================================================

export const meta = {
  name: 'learn-effort-control',
  description: 'Учебный workflow: per-step effort через agentType + frontmatter vs per-step model инлайн',
  whenToUse: 'Когда учишься разделять две оси усилия — model и reasoning/effort — в workflow',
  phases: [
    { title: 'Plan',    detail: 'Планирование через субагент deep-planner (effort: max)', model: 'opus' },
    { title: 'Execute', detail: 'Исполнение шагов — инлайн model: sonnet',                 model: 'sonnet' },
    { title: 'Summary', detail: 'Сборка сводки — инлайн model: haiku',                      model: 'haiku' },
  ],
}

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
          detail: { type: 'string' },
        },
      },
    },
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

const task = (typeof args === 'string' && args.trim())
  ? args.trim()
  : 'Учебная задача: описать, как добавить новый тип врага в игру Asteroids'

log(`Задача: ${task}`)

// ---------------------------------------------------------------------------
// СПОСОБ B — REASONING/EFFORT через субагент-файл.
// agentType: 'deep-planner' подключает .claude/agents/deep-planner.md,
// у которого во frontmatter `effort: max` и `model: opus`.
// Здесь мы НЕ передаём ни model, ни effort инлайн — их даёт файл субагента.
// schema всё равно работает: к системному промпту субагента допишется
// инструкция вызвать StructuredOutput.
// ---------------------------------------------------------------------------
phase('Plan')
const plan = await agent(
  `Разбей задачу на шаги. Задача: ${task}`,
  { phase: 'Plan', agentType: 'deep-planner', schema: PLAN_SCHEMA }
)
log(`План готов (effort:max через subagent): ${plan.steps.length} шаг(ов)`)

// ---------------------------------------------------------------------------
// СПОСОБ A — ТИР МОДЕЛИ инлайн. Никакого effort, только model.
// Эти шаги унаследуют reasoning-уровень из /effort текущей сессии.
// ---------------------------------------------------------------------------
phase('Execute')
const executed = await parallel(
  plan.steps.map((step) => () =>
    agent(
      `Проработай шаг детально.
Шаг #${step.id}: ${step.title}
Детали: ${step.detail}`,
      { label: `execute:#${step.id}`, phase: 'Execute', model: 'sonnet' }
    ).then((text) => ({ step, text }))
  )
)
const clean = executed.filter(Boolean)
log(`Проработано шагов: ${clean.length}`)

// ---------------------------------------------------------------------------
// SUMMARY — инлайн model: haiku (low). Чистая механика сборки по схеме.
// ---------------------------------------------------------------------------
phase('Summary')
const summary = await agent(
  `Собери краткую сводку.
Задача: ${task}
Проработки:
${clean.map((r) => `- ${r.step.title}: ${r.text.slice(0, 200)}`).join('\n')}
Верни headline + bullets через StructuredOutput.`,
  { phase: 'Summary', model: 'haiku', schema: SUMMARY_SCHEMA }
)

return {
  task,
  effortControl: {
    plan:    'effort:max + model:opus — через субагент deep-planner (agentType)',
    execute: 'model:sonnet инлайн; reasoning наследуется из /effort сессии',
    summary: 'model:haiku инлайн; reasoning наследуется из /effort сессии',
  },
  stepsPlanned: plan.steps.length,
  summary,
}
