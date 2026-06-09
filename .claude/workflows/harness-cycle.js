// ============================================================================
// WORKFLOW: harness-цикл (понять → спланировать → реализовать → проверить → синтез)
// ----------------------------------------------------------------------------
// Каждый этап — отдельный субагент из .claude/agents/, у каждого model + effort
// заданы во frontmatter по рекомендованным параметрам. Workflow подключает их
// через opts.agentType (единственный способ задать per-step reasoning/effort,
// т.к. inline-agent() поля effort не имеет — только model).
//
// Рекомендованные параметры по этапам:
//   Понять      → harness-researcher   : sonnet / high   (сбор фактов)
//   Спланировать→ harness-planner      : opus   / max    (макс. цена ошибки)
//   Реализовать → harness-implementer  : sonnet / high   (генерация ≠ механика)
//   Проверить   → harness-verifier     : opus   / high   (адверсариальная)
//   Синтез      → harness-synthesizer  : haiku  / low    (механика по схеме)
//
// Структура: Понять и Спланировать — последовательны (план зависит от контекста).
// Реализовать→Проверить — pipeline по задачам плана (каждая задача проверяется,
// как только проработана, без барьера). Синтез — финальная сборка.
//
// Запуск: Workflow tool { name: "harness-cycle", args: "описание задачи" }
// ============================================================================

export const meta = {
  name: 'harness-cycle',
  description: 'Harness-цикл с субагентом на каждый этап; model+effort заданы во frontmatter субагентов',
  whenToUse: 'Полный цикл понять→спланировать→реализовать→проверить→синтез с настроенными усилиями по этапам',
  phases: [
    { title: 'Understand', detail: 'harness-researcher: собрать контекст',        model: 'sonnet' },
    { title: 'Plan',       detail: 'harness-planner: декомпозиция (effort:max)',   model: 'opus' },
    { title: 'Implement',  detail: 'harness-implementer: проработка задач',        model: 'sonnet' },
    { title: 'Verify',     detail: 'harness-verifier: адверсариальная проверка',   model: 'opus' },
    { title: 'Synthesize', detail: 'harness-synthesizer: сборка сводки (low)',     model: 'haiku' },
  ],
}

// --- Схемы структурированного вывода --------------------------------------
const CONTEXT_SCHEMA = {
  type: 'object',
  required: ['facts', 'constraints', 'openQuestions'],
  properties: {
    facts:         { type: 'array', items: { type: 'string' } },
    constraints:   { type: 'array', items: { type: 'string' } },
    openQuestions: { type: 'array', items: { type: 'string' } },
  },
}

const PLAN_SCHEMA = {
  type: 'object',
  required: ['tasks'],
  properties: {
    tasks: {
      type: 'array',
      items: {
        type: 'object',
        required: ['id', 'title', 'detail', 'done'],
        properties: {
          id:     { type: 'integer' },
          title:  { type: 'string' },
          detail: { type: 'string' },
          done:   { type: 'string', description: 'Критерий готовности задачи' },
        },
      },
    },
  },
}

const VERDICT_SCHEMA = {
  type: 'object',
  required: ['ok', 'notes'],
  properties: {
    ok:    { type: 'boolean' },
    notes: { type: 'string' },
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
  : 'Учебная задача: добавить новый тип врага в игру Asteroids'

log(`Harness-цикл по задаче: ${task}`)

// ---------------------------------------------------------------------------
// ЭТАП 1 — ПОНЯТЬ. sonnet / high (через субагент harness-researcher).
// ---------------------------------------------------------------------------
phase('Understand')
const context = await agent(
  `Собери контекст по задаче. Задача: ${task}`,
  { phase: 'Understand', agentType: 'harness-researcher', schema: CONTEXT_SCHEMA }
)
log(`Контекст: ${context.facts.length} фактов, ${context.constraints.length} ограничений, ${context.openQuestions.length} вопросов`)

// ---------------------------------------------------------------------------
// ЭТАП 2 — СПЛАНИРОВАТЬ. opus / max (через harness-planner).
// План зависит от контекста — поэтому последовательно, после этапа 1.
// ---------------------------------------------------------------------------
phase('Plan')
const plan = await agent(
  `Задача: ${task}

Контекст от исследователя:
Факты:
${context.facts.map((f) => `- ${f}`).join('\n')}
Ограничения:
${context.constraints.map((c) => `- ${c}`).join('\n')}
Открытые вопросы:
${context.openQuestions.map((q) => `- ${q}`).join('\n')}

Разбей задачу на атомарные задачи с критериями готовности.`,
  { phase: 'Plan', agentType: 'harness-planner', schema: PLAN_SCHEMA }
)
log(`План: ${plan.tasks.length} задач(и)`)

// ---------------------------------------------------------------------------
// ЭТАП 3+4 — РЕАЛИЗОВАТЬ → ПРОВЕРИТЬ через pipeline (без барьера).
// implement: sonnet/high (harness-implementer)
// verify:    opus/high   (harness-verifier)
// Каждая задача проверяется, как только проработана.
// phase задаём в opts ЯВНО — внутри pipeline нельзя полагаться на phase().
// ---------------------------------------------------------------------------
const results = await pipeline(
  plan.tasks,

  // стадия реализации
  (t) => agent(
    `Проработай задачу.
#${t.id}: ${t.title}
Детали: ${t.detail}
Критерий готовности: ${t.done}`,
    { label: `impl:#${t.id}`, phase: 'Implement', agentType: 'harness-implementer' }
  ),

  // стадия проверки — получает (prevResult, originalTask)
  (implOutput, t) => agent(
    `Адверсариально проверь проработку задачи против критерия готовности.
Задача: ${t.title}
Критерий: ${t.done}
Проработка:
${implOutput}`,
    { label: `verify:#${t.id}`, phase: 'Verify', agentType: 'harness-verifier', schema: VERDICT_SCHEMA }
  ).then((verdict) => ({ task: t, implOutput, verdict }))
)

const clean = results.filter(Boolean)
const passed = clean.filter((r) => r.verdict.ok).length
log(`Проверка: ${passed}/${clean.length} задач прошли`)

// ---------------------------------------------------------------------------
// ЭТАП 5 — СИНТЕЗ. haiku / low (через harness-synthesizer). Механика по схеме.
// ---------------------------------------------------------------------------
phase('Synthesize')
const summary = await agent(
  `Собери итоговую сводку по harness-циклу.
Задача: ${task}
Результаты по задачам:
${clean.map((r) => `- ${r.task.title}: ${r.verdict.ok ? 'OK' : 'ПРОБЛЕМА'} — ${r.verdict.notes}`).join('\n')}`,
  { phase: 'Synthesize', agentType: 'harness-synthesizer', schema: SUMMARY_SCHEMA }
)

return {
  task,
  stages: {
    understand: 'harness-researcher  — sonnet / high',
    plan:       'harness-planner     — opus   / max',
    implement:  'harness-implementer — sonnet / high',
    verify:     'harness-verifier    — opus   / high',
    synthesize: 'harness-synthesizer — haiku  / low',
  },
  tasksPlanned: plan.tasks.length,
  tasksPassed: passed,
  summary,
}
