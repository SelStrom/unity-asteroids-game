// Холостой прогон workflow-скрипта: реальные хуки заменены заглушками.
// Проверяется валидация args, раскладка матрицы по моделям и уровням effort,
// обезличивание, аудит фактических параметров и вся агрегация.
// Запуск: node .claude/workflows/rockets-bench-matrix.dryrun.mjs
import { readFileSync } from 'node:fs'
import assert from 'node:assert/strict'

const SRC = new URL('./rockets-bench-matrix.js', import.meta.url).pathname
const body = readFileSync(SRC, 'utf8').replace(/^export const meta/m, 'const meta')
const AsyncFunction = Object.getPrototypeOf(async function () {}).constructor
const FAKE_BASE = 'a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0'

function firstJsonArray(text) {
  return JSON.parse(/\[\s*{[\s\S]*?}\s*\]/.exec(text)[0])
}

function makeRuntime(opts) {
  const o = opts || {}
  const failModels = o.failModels || []
  const failRuns = o.failRuns || []
  const notCompleted = o.notCompleted || []
  const hardBlockerOn = o.hardBlockerOn || []
  const refutedMajorOn = o.refutedMajorOn || []
  const auditMode = o.auditMode || 'ok'
  const state = { spent: 0, calls: [], logs: [], anonToBranch: {} }

  const budget = { total: null, spent: () => state.spent, remaining: () => Infinity }
  const log = (m) => state.logs.push(m)
  const phase = () => {}
  const parallel = async (thunks) => {
    const out = []
    for (const t of thunks) {
      try {
        out.push(await t())
      } catch {
        out.push(null)
      }
    }
    return out
  }
  const pipeline = async (items, ...stages) => {
    const out = []
    for (let i = 0; i < items.length; i++) {
      let cur = items[i]
      try {
        for (const s of stages) {
          cur = await s(cur, items[i], i)
        }
        out.push(cur)
      } catch {
        out.push(null)
      }
    }
    return out
  }

  const agent = async (prompt, opts2) => {
    state.spent += 1000
    state.calls.push({ label: opts2.label, model: opts2.model, effort: opts2.effort, phase: opts2.phase, prompt })
    const label = opts2.label

    if (label.startsWith('preflight:')) {
      if (failModels.indexOf(opts2.model) >= 0) {
        return null
      }
      if (label === 'preflight:setup') {
        return { ok: true, baseCommit: FAKE_BASE }
      }
      return { ok: true }
    }

    if (label.startsWith('impl:')) {
      const key = label.replace('impl:', '')
      if (failRuns.indexOf(key) >= 0) {
        return null
      }
      return {
        branch: 'stub',
        completed: notCompleted.indexOf(key) < 0,
        commits: 4,
        testsSummary: 'stub',
        testsGreen: true,
        verificationDepth: 'editor-tests',
        screenshotOk: notCompleted.indexOf(key) < 0,
        screenshotSecondsAfterLaunch: 1.5,
        keyDecisions: 'stub',
        tokenEstimate: '~200k',
      }
    }

    if (label === 'prep:worktrees') {
      const created = []
      const missing = []
      for (const p of firstJsonArray(prompt)) {
        state.anonToBranch[p.anonId] = p.branch
        if (o.missingBranchSuffix && p.branch.endsWith(o.missingBranchSuffix)) {
          missing.push(p.anonId)
        } else {
          created.push({ anonId: p.anonId, path: '/tmp/wt/' + p.anonId, commitsAhead: 4 })
        }
      }
      return { created, missing, problems: null }
    }

    if (label.startsWith('review:')) {
      const anonId = label.split(':')[1]
      const branch = state.anonToBranch[anonId]
      const cell = branch.split('/rockets-pure-plan/')[1]
      const model = branch.split('/')[2]
      const isLow = cell.startsWith('low')
      const blockers = []
      if (refutedMajorOn.indexOf(model + '/' + cell) >= 0) {
        blockers.push({ severity: 'major', summary: 'ложный major', evidence: 'stub' })
      }
      if (hardBlockerOn.indexOf(model + '/' + cell) >= 0) {
        blockers.push({ severity: 'blocker', summary: 'реальный blocker', evidence: 'stub' })
      }
      const s = isLow ? 7 : 8
      return {
        anonId,
        scores: { architecture: s, guidance: s, tests: s, assets: s, risk: s },
        rationale: 'stub',
        metrics: {
          commits: 4,
          filesChanged: 20,
          insertions: 1500,
          deletions: 30,
          newCsFiles: 8,
          testAttributes: isLow ? 30 : 50,
          durationMinutes: isLow ? 20 : 40,
          rawGitStat: 'stub',
          missingMetaFiles: [],
          unresolvedGuids: [],
          asmdefIssues: [],
        },
        features: {
          inputKeyR: true,
          arcTrajectory: true,
          nearestTarget: true,
          collateralHits: true,
          respawnTimer: true,
          configDriven: true,
          hudCounters: true,
          ecsIntegrated: true,
          prefabWired: true,
          trailVfx: true,
          toroidalAiming: !isLow,
          leadInterception: !isLow,
        },
        claims: { claimedTestsGreen: true, discrepancies: '' },
        blockers,
      }
    }

    if (label.startsWith('verify:')) {
      return {
        anonId: label.split(':')[1],
        verdicts: firstJsonArray(prompt).map((b) => ({
          summary: b.summary,
          refuted: b.summary === 'ложный major',
          severityAdjusted: b.summary === 'ложный major' ? 'none' : 'blocker',
          reasoning: 'stub',
        })),
      }
    }

    if (label === 'audit:models') {
      if (auditMode === 'empty') {
        return { entries: [], problems: 'stub: каталог не найден' }
      }
      const pairs = firstJsonArray(prompt)
      return {
        transcriptDir: '/tmp/wf',
        agentsSeen: pairs.length * 3,
        implAgents: pairs.length,
        entries: pairs.map((p, i) => {
          // Подмена уровня на xhigh проверяет, что 'high' не совпадёт с '"xhigh"' по подстроке.
          const swap = auditMode === 'mismatch' && p.requestedEffort === 'high'
          // Прерванная попытка воспроизводится на первом прогоне: расход зачётной попытки
          // должен попасть в токены, а потерянный — в lostOutputTokens, а не в сумму.
          const retried = auditMode === 'retried' && i === 0
          return {
            branch: p.branch,
            requestedModel: p.requestedModel,
            resolvedModel: p.requestedModel === 'haiku' ? 'claude-haiku-4-5' : p.requestedModel,
            actualEffort: '"effort":"' + (swap ? 'xhigh' : p.requestedEffort) + '"',
            agentFile: 'agent-stub.jsonl',
            steps: 200 + i,
            outputTokens: auditMode === 'zeroTokens' ? 0 : 100000 + i * 1000,
            inputTokens: 500,
            cacheCreateTokens: 400000,
            cacheReadTokens: 30000000,
            attempts: retried ? 2 : 1,
            lostOutputTokens: retried ? 7777 : 0,
          }
        }),
        problems: null,
      }
    }

    if (label === 'report:pdf') {
      state.reportPrompt = prompt
      return { htmlPath: '/tmp/report.html', pdfProduced: false, problems: 'stub: PDF не собирался' }
    }
    throw new Error('неожиданный агент: ' + label)
  }

  return { state, budget, log, phase, parallel, pipeline, agent }
}

async function run(args, opts) {
  const rt = makeRuntime(opts)
  const fn = new AsyncFunction('args', 'agent', 'pipeline', 'parallel', 'log', 'phase', 'budget', 'workflow', body)
  const result = await fn(args, rt.agent, rt.pipeline, rt.parallel, rt.log, rt.phase, rt.budget, null)
  return { result, state: rt.state }
}

// ── 1. Валидация args ──
await assert.rejects(() => run({ models: ['claude-opus-5'] }), /args\.date обязателен/)
await assert.rejects(() => run({ date: '2026-07-26' }), /args\.models обязателен/)
await assert.rejects(() => run({ date: '26-07-2026', models: ['claude-opus-5'] }), /YYYY-MM-DD/)
await assert.rejects(() => run({ date: '2026-07-26', models: [{ slug: 'x' }] }), /должен быть строкой либо объектом/)
await assert.rejects(
  () => run({ date: '2026-07-26', models: ['claude-opus-4-6'], efforts: ['xhigh'] }),
  /не осталось ни одного поддерживаемого уровня/,
)
await assert.rejects(
  () => run({ date: '2026-07-26', models: ['claude-opus-5'], efforts: ['ultra'] }),
  /неизвестный уровень effort/,
)
await assert.rejects(
  () => run({ date: '2026-07-26', models: ['claude-opus-4-6'], efforts: ['high'], runs: 1 }, { failModels: ['claude-opus-5'] }),
  /модель судьи/,
)
// Судья совпадает с матричной моделью, и её зонд упал: судья должен быть признан недоступным
// по результату общего зонда, без отдельного вызова.
await assert.rejects(
  () => run({ date: '2026-07-26', models: ['claude-opus-5', 'claude-opus-4-6'], efforts: ['high'], runs: 1 }, { failModels: ['claude-opus-5'] }),
  /модель судьи/,
)
const stringArgs = await run(
  JSON.stringify({ date: '2026-07-26', models: ['claude-opus-5'], efforts: ['low'], runs: 1, outDir: '/tmp/bench-out' }),
)
assert.equal(stringArgs.result.summary.plannedRuns, 1, 'args в виде JSON-строки должны парситься')
await assert.rejects(() => run('не json'), /не парсятся как JSON/)
// База: реф разрешается setup-агентом, явный SHA из args используется как есть,
// нерешённая база — ошибка, а не молчаливый запуск от неизвестного коммита.
await assert.rejects(
  () => run({ date: '2026-07-26', models: ['claude-opus-5'], efforts: ['low'], runs: 1 }, { failModels: ['haiku'] }),
  /не удалось разрешить базовый реф/,
)
const pinnedBase = await run({
  date: '2026-07-26',
  models: ['claude-opus-5'],
  efforts: ['low'],
  runs: 1,
  base: 'b'.repeat(40),
  outDir: '/tmp/bench-out',
})
assert.equal(pinnedBase.result.summary.baseCommit, 'b'.repeat(40), 'явный SHA базы используется без разрешения')
console.log('✓ валидация args, опечатки в effort, недоступный судья, args-строка, база')

// ── 2. Матрица по нескольким моделям, включая ранние Opus ──
const multi = await run(
  {
    date: '2026-07-26',
    models: ['claude-opus-5', 'claude-opus-4-6', 'claude-opus-4-7', 'haiku'],
    efforts: ['high', 'xhigh'],
    runs: 1,
    outDir: '/tmp/bench-out',
  },
  { failModels: ['claude-opus-4-7'], auditMode: 'mismatch' },
)

const slugs = multi.result.summary.models.map((m) => m.slug)
assert.deepEqual(slugs, ['opus5', 'opus46', 'opus47', 'haiku-latest'], 'slug выводится из полного ID')
const levels = {}
for (const m of multi.result.summary.models) {
  levels[m.slug] = m.levels
}
assert.deepEqual(levels.opus5, ['high', 'xhigh'])
assert.deepEqual(levels.opus46, ['high'], 'xhigh у Opus 4.6 не поддерживается и должен быть отброшен')
assert.deepEqual(levels['haiku-latest'], ['default'], 'модель без поддержки effort даёт одно условие')
assert.deepEqual(multi.result.summary.droppedCells, ['opus46/xhigh'])
assert.ok(
  multi.state.logs.some((l) => l.includes('opus46/xhigh') && l.includes('дубликатом')),
  'отброшенная ячейка должна быть объяснена в логе',
)
assert.ok(
  multi.result.groups.some((g) => g.model === 'haiku-latest' && g.effort === 'default'),
  'условие default должно попадать в сводную таблицу групп',
)
console.log('✓ уровни по моделям: opus5 [' + levels.opus5.join(',') + '], opus46 [' + levels.opus46.join(',') + '], haiku [default], группа default в сводке')

// ── 3. Недоступная модель исключается, остальные работают ──
const implLabels = multi.state.calls.filter((c) => c.label.startsWith('impl:')).map((c) => c.label)
assert.ok(!implLabels.some((l) => l.includes('opus47')), 'условия недоступной модели не должны запускаться')
assert.equal(multi.result.summary.plannedRuns, 6)
assert.equal(multi.result.summary.runnableRuns, 4)
assert.deepEqual(multi.result.summary.unavailableModels, ['claude-opus-4-7'])
assert.ok(multi.state.logs.some((l) => l.includes('Недоступны')), 'исключение модели должно попасть в лог')
console.log('✓ недоступная модель исключена: запущено ' + multi.result.summary.runnableRuns + ' из ' + multi.result.summary.plannedRuns)

// ── 4. Модель и effort передаются в agent(), haiku — без effort ──
for (const c of multi.state.calls.filter((x) => x.label.startsWith('impl:'))) {
  const cell = c.label.replace('impl:', '')
  if (cell.startsWith('haiku-latest/')) {
    assert.equal(c.effort, undefined, 'модели без поддержки effort уровень не передаётся')
    assert.equal(c.model, 'haiku')
  } else {
    assert.equal(c.effort, cell.split('/')[1].split('#')[0])
    assert.ok(c.model.startsWith('claude-opus-'), 'должен передаваться точный ID: ' + c.model)
  }
}
const judges = multi.state.calls.filter((c) => c.label.startsWith('review:') || c.label.startsWith('verify:'))
for (const c of judges) {
  assert.equal(c.model, 'claude-opus-5', 'судья закреплён точным ID, а не алиасом')
  assert.equal(c.effort, 'high')
}
assert.ok(
  !multi.state.calls.some((c) => c.label === 'preflight:judge'),
  'ID судьи совпадает с матричной моделью — отдельный зонд был бы дублем на ~38k токенов контекста',
)
const setupCall = multi.state.calls.find((c) => c.label === 'preflight:setup')
assert.ok(setupCall, 'харнесс должен копироваться в preflight, пока checkout на исходной ветке')
assert.equal(setupCall.model, 'haiku', 'механический setup — на haiku')
assert.equal(setupCall.effort, undefined, 'haiku не поддерживает effort — опция не передаётся')
assert.equal(multi.result.summary.baseRef, 'feature/dots', 'база по умолчанию — HEAD feature/dots')
assert.equal(multi.result.summary.baseCommit, FAKE_BASE, 'реф должен быть разрешён в SHA из setup-агента')
console.log('✓ model/effort передаются корректно, судья закреплён и не зондируется дважды, setup на haiku, база разрешена в SHA')

// ── 5. Аудит ловит расхождение, не путая high и xhigh ──
const byCell = {}
for (const r of multi.result.rows) {
  byCell[r.model + '/' + r.effort] = r
}
assert.equal(byCell['opus5/high'].paramsVerified, false, 'фактический xhigh при заявленном high — расхождение')
assert.equal(byCell['opus5/xhigh'].paramsVerified, true)
assert.equal(byCell['haiku-latest/default'].paramsVerified, true, 'для default уровень не сверяется')
assert.equal(multi.result.summary.paramsMismatchedRuns, 2) // opus5/high и opus46/high
assert.ok(multi.state.reportPrompt.includes('paramsVerified'), 'отчёт должен показывать фактические параметры')
console.log('✓ аудит параметров: расхождений найдено ' + multi.result.summary.paramsMismatchedRuns)

// ── 6. Аудит без данных не выдаётся за совпадение ──
const noAudit = await run(
  { date: '2026-07-26', models: ['claude-opus-5'], efforts: ['high'], runs: 1, outDir: '/tmp/bench-out' },
  { auditMode: 'empty' },
)
assert.equal(noAudit.result.rows[0].paramsVerified, null, 'без данных аудита — null, а не true')
assert.equal(noAudit.result.summary.paramsAuditedRuns, 0)
assert.ok(noAudit.state.logs.some((l) => l.includes('Аудит не дал данных')))
// Без аудита расход неизвестен: дельта budget.spent() сюда не подставляется, иначе смесь двух
// разных величин выглядела бы как измерение.
assert.equal(noAudit.result.rows[0].outputTokens, null, 'без аудита расход — null, а не дельта бюджета')
assert.equal(noAudit.result.rows[0].budgetDelta, 1000, 'дельта бюджета сохраняется отдельным полем')
assert.equal(noAudit.result.groups[0].tokensMedian, null)
assert.equal(noAudit.result.groups[0].tokensMeasured, 0)
assert.equal(noAudit.result.summary.tokensMeasuredRuns, 0)
console.log('✓ пустой аудит помечается как непроверенный, расход не подменяется дельтой бюджета')

// ── 6a. Расход токенов берётся из транскриптов, прерванная попытка не смешивается ──
const measured = await run(
  { date: '2026-07-26', models: ['claude-opus-5'], efforts: ['high'], runs: 3, outDir: '/tmp/bench-out' },
  { auditMode: 'retried' },
)
const mRows = measured.result.rows
const mGroup = measured.result.groups[0]
assert.deepEqual(mRows.map((r) => r.outputTokens).sort((a, b) => a - b), [100000, 101000, 102000])
assert.equal(mGroup.tokensMedian, 101000, 'медиана считается по измеренным прогонам')
assert.equal(mGroup.tokensTotal, 303000)
assert.equal(mGroup.tokensMeasured, 3)
assert.equal(mGroup.inputTokensMedian, 30400500, 'входная сторона считается с кэшем')
assert.equal(measured.result.summary.tokensMeasuredRuns, 3)
assert.equal(measured.result.summary.retriedRuns, 1)
assert.equal(mRows.filter((r) => r.lostOutputTokens === 7777).length, 1, 'потерянная попытка учтена отдельным полем')
assert.ok(
  mRows.every((r) => r.outputTokens !== r.budgetDelta),
  'измеренный расход не должен совпадать с дельтой бюджета: это разные величины',
)
assert.ok(measured.state.logs.some((l) => l.includes('расход токенов измерен у 3')))
assert.ok(measured.state.logs.some((l) => l.includes('прерванной попыткой — 1')))
const auditPrompt = measured.state.calls.find((c) => c.label === 'audit:models').prompt
assert.ok(auditPrompt.includes('message.usage') || auditPrompt.includes('usage.get("output_tokens"'), 'аудит должен считать usage скриптом')
assert.ok(auditPrompt.includes('IMPL_MARKER'), 'агент-реализация опознаётся по маркеру промпта, а не по grep всего файла')
assert.ok(!auditPrompt.includes('grep -m1 -oE "bench/'), 'старая атрибуция по grep всего транскрипта должна быть убрана')
console.log('✓ расход токенов из транскриптов: медиана ' + mGroup.tokensMedian + ', потерянных попыток 1')

// ── 7. Полный прогон: годность, агрегация, отчёт ──
const { result, state } = await run(
  {
    date: '2026-07-26',
    models: ['claude-opus-5'],
    efforts: ['low', 'high'],
    runs: 3,
    outDir: '/tmp/bench-out',
  },
  {
    failRuns: ['opus5/low#03'],
    notCompleted: ['opus5/high#02'],
    hardBlockerOn: ['opus5/high_03'],
    refutedMajorOn: ['opus5/low_01'],
    missingBranchSuffix: 'low_03',
  },
)

const branches = result.rows.map((r) => r.branch)
assert.equal(branches[0], 'bench/2026-07-26/opus5/rockets-pure-plan/low_01', 'строки отчёта в порядке матрицы')
assert.equal(branches[5], 'bench/2026-07-26/opus5/rockets-pure-plan/high_03')
const anons = result.rows.map((r) => r.anonId)
assert.equal(new Set(anons).size, 6, 'обезличенные id уникальны')
assert.notDeepEqual(anons, [...anons].sort(), 'порядок id не совпадает с порядком условий')
const execOrder = state.calls.filter((c) => c.label.startsWith('impl:')).map((c) => c.label.replace('impl:opus5/', ''))
const matrixOrder = ['low#01', 'low#02', 'low#03', 'high#01', 'high#02', 'high#03']
assert.notDeepEqual(execOrder, matrixOrder, 'порядок исполнения должен быть перемешан, а не следовать матрице')
assert.deepEqual([...execOrder].sort(), [...matrixOrder].sort(), 'перемешивание не должно терять и дублировать условия')
console.log('✓ ветки, обезличивание (' + anons.join(',') + '), исполнение перемешано: ' + execOrder.join(','))

const usable = {}
for (const r of result.rows) {
  usable[r.effort + '#' + String(r.run).padStart(2, '0')] = r.usable
}
assert.deepEqual(usable, {
  'low#01': true, // major опровергнут состязательной проверкой
  'low#02': true,
  'low#03': false, // агент упал, ревью не было
  'high#01': true,
  'high#02': false, // completed=false
  'high#03': false, // выживший blocker
})
console.log('✓ годность прогонов')

const low = result.groups.find((g) => g.effort === 'low')
const high = result.groups.find((g) => g.effort === 'high')
assert.equal(low.n, 3)
assert.equal(low.reviewed, 2)
assert.equal(low.scoreMean, 35)
assert.equal(low.scoreStdev, 0)
assert.equal(low.pass1, 0.67)
assert.equal(low.passAny, 1)
assert.equal(low.passAll, 0)
assert.equal(low.blockersRefuted, 1)
assert.equal(low.blockersConfirmed, 0)
assert.equal(low.featureRate.toroidalAiming, 0)
assert.equal(low.testsMean, 30)
assert.equal(low.durationMeanMin, 20)
assert.equal(high.scoreMean, 40)
assert.equal(high.pass1, 0.33)
assert.equal(high.blockersConfirmed, 1)
assert.equal(high.featureRate.toroidalAiming, 1)
assert.equal(high.screenshotsOk, 2)
assert.equal(result.summary.plannedRuns, 6)
assert.equal(result.summary.reviewedRuns, 5)
assert.equal(result.summary.screenshotsOk, 4) // low#03 упал, high#02 не завершён
console.log('✓ агрегация: low ' + low.scoreMean + '/50 pass@1=' + low.pass1 + ', high ' + high.scoreMean + '/50 pass@1=' + high.pass1)

const prepCall = state.calls.find((c) => c.label === 'prep:worktrees')
const auditCall = state.calls.find((c) => c.label === 'audit:models')
assert.equal(prepCall.model, 'haiku', 'prep — механическая стадия, на haiku')
assert.equal(prepCall.effort, undefined)
assert.equal(auditCall.model, 'haiku', 'audit — grep по транскриптам, на haiku')
assert.equal(auditCall.effort, undefined)
assert.ok(state.reportPrompt.includes('BENCH_JSON'), 'в промпте отчёта должен быть JSON-блок')
assert.ok(state.reportPrompt.includes('/tmp/bench-out/shots/'), 'в отчёт должны попасть пути кадров')
assert.ok(state.reportPrompt.includes('галерея кадров полёта'), 'отчёт должен требовать галерею кадров')
assert.ok(state.reportPrompt.includes('report-short.html'), 'отчёт должен требовать краткий вариант')
assert.ok(state.reportPrompt.includes('tokensMedian'), 'в отчёте токены берутся из измеренного поля')
assert.ok(!state.reportPrompt.includes('медиана токенов,'), 'старая безымянная «медиана токенов» должна быть заменена')
assert.ok(state.reportPrompt.includes('шкала effort калибруется'), 'ограничение про несопоставимость effort между моделями')
assert.equal(result.report.pdfProduced, false)
assert.ok(state.logs.some((l) => l.includes('Отчёт только в HTML')), 'HTML-фолбэк должен логироваться')
console.log('✓ отчёт: HTML-фолбэк, галерея кадров, оговорка про шкалу effort')

// ── 8. Промпты: требования к реализации и слепота ревью ──
const p = state.calls.find((c) => c.label === 'impl:opus5/high#02').prompt
for (const needle of [
  'ПРАВИЛО ПЕРЕКЛЮЧЕНИЯ ВЕТОК',
  'GetActiveScene().isDirty',
  'NewSceneSetup.EmptyScene',
  'open_scene Assets/Scenes/Main.unity',
  'git checkout -b bench/2026-07-26/opus5/rockets-pure-plan/high_02 ' + FAKE_BASE,
  'НЕ читать и НЕ изменять .claude/',
  'КАДР ПОЛЁТА (обязательный артефакт)',
  'QueuePlayerLoopUpdate',
  'sips -f vertical',
  '/tmp/bench-out/shots/',
  'НЕ читать каталоги памяти Claude',
  'НЕ заглядывать в другие git-ветки',
  'НЕ упоминай ни модель, ни режим effort',
  'bench-run.json',
]) {
  assert.ok(p.includes(needle), 'в промпте реализации нет: ' + needle)
}
assert.ok(p.indexOf('NewSceneSetup.EmptyScene') < p.indexOf('Только теперь переключай ветку git'), 'пустая сцена до checkout')
assert.ok(p.indexOf('Только теперь переключай ветку git') < p.indexOf('лишь затем open_scene'), 'целевая сцена после checkout')

const rp = state.calls.find((c) => c.label.startsWith('review:')).prompt
assert.ok(!/opus|fable|sonnet|haiku|effort=|low_0|high_0/.test(rp), 'промпт ревью не должен раскрывать условие')
assert.ok(rp.includes('слепая оценка'))
console.log('✓ промпты: правило переключения веток, кадр полёта, слепота ревью')

console.log('\nВсе проверки пройдены.')
