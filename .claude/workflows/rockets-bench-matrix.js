export const meta = {
  name: 'rockets-bench-matrix',
  description: 'Бенчмарк «самонаводящиеся ракеты»: матрица model × effort × N прогонов, слепое ревью и PDF-отчёт',
  whenToUse: 'Сравнить качество реализации одной и той же фичи разными моделями и режимами effort с повторными прогонами, обезличенной оценкой и итоговым PDF',
  phases: [
    { title: 'Preflight', detail: 'проверка, что каждая заявленная модель реально доступна' },
    { title: 'Implement', detail: 'model × effort × runs, строго последовательно — Unity Editor один на весь проект' },
    { title: 'Prep', detail: 'обезличенные detached-worktree, по одному на ветку' },
    { title: 'Review', detail: 'оценка по рубрике + состязательная проверка блокеров' },
    { title: 'Audit', detail: 'сверка фактических модели и effort каждого агента по транскриптам' },
    { title: 'Report', detail: 'агрегация в JS, затем HTML и PDF (weasyprint / Chrome / pandoc)' },
  ],
}

// ─── Конфигурация ───────────────────────────────────────────────────────────
// Полный перечень effort живёт здесь; модели передаются в args.
const DEFAULT_EFFORTS = ['low', 'medium', 'high', 'xhigh', 'max']
const HARNESS_VERSION = 'rockets-bench-matrix/6'

// Поддержка effort по моделям (docs.claude.com, model-config → Adjust effort level).
// Неподдержанный уровень НЕ ошибка: Claude Code молча опускает его до ближайшего доступного,
// поэтому xhigh на Opus 4.6 исполнился бы как high и дал бы в матрице два одинаковых условия.
// Такие ячейки отбрасываются явно, с записью в лог.
const FIVE_LEVELS = ['low', 'medium', 'high', 'xhigh', 'max']
const FOUR_LEVELS = ['low', 'medium', 'high', 'max']
const KNOWN_EFFORTS = {
  'claude-opus-5': FIVE_LEVELS,
  'claude-opus-4-8': FIVE_LEVELS,
  'claude-opus-4-7': FIVE_LEVELS,
  'claude-opus-4-6': FOUR_LEVELS,
  'claude-sonnet-5': FIVE_LEVELS,
  'claude-sonnet-4-6': FOUR_LEVELS,
  'claude-fable-5': FIVE_LEVELS,
  // Алиасы разрешаются в новейшую доступную версию семейства.
  opus: FIVE_LEVELS,
  sonnet: FIVE_LEVELS,
  fable: FIVE_LEVELS,
  haiku: [], // effort не поддерживается вовсе
}
const ALIASES = ['opus', 'sonnet', 'haiku', 'fable', 'default', 'best', 'opusplan']

// args может приехать JSON-строкой, а не объектом — зависит от того, как вызвали Workflow.
let cfg = args || {}
if (typeof cfg === 'string') {
  try {
    cfg = JSON.parse(cfg)
  } catch (e) {
    throw new Error('args пришли строкой и не парсятся как JSON: ' + e.message)
  }
}
const REPO = cfg.repo || '/Users/selstrom/work/projects/asteroids'
// База веток — HEAD feature/dots (или cfg.base: реф либо SHA). Реф разрешается в конкретный
// SHA один раз в Preflight: за многочасовой прогон ветка может сдвинуться, а все условия
// обязаны стартовать с одного коммита, и он фиксируется в отчёте.
const BASE_REF = cfg.base || 'feature/dots'
let BASE = /^[0-9a-f]{40}$/.test(BASE_REF) ? BASE_REF : null
const EFFORTS = cfg.efforts || DEFAULT_EFFORTS
for (const e of EFFORTS) {
  if (FIVE_LEVELS.indexOf(e) < 0) {
    // Опечатка в уровне не должна тихо выпасть через droppedCells — это сузило бы матрицу незаметно.
    throw new Error('неизвестный уровень effort "' + e + '" в args.efforts; допустимы: ' + FIVE_LEVELS.join(', '))
  }
}
const RUNS = cfg.runs || 3
const REVIEWERS = cfg.reviewersPerBranch || 2
// Модель судьи закреплена явно, причём точным ID, а не алиасом: иначе ревьюер
// унаследовал бы модель сессии (или дрейфующий алиас) и оценки разных запусков
// бенчмарка стали бы несопоставимы.
const JUDGE_MODEL = cfg.judgeModel || 'claude-opus-5'
const JUDGE_EFFORT = cfg.judgeEffort || 'high'
const UNITY = cfg.unity || '/Applications/Unity/Hub/Editor/6000.3.13f1/Unity.app/Contents/MacOS/Unity'
const SCENE = cfg.scene || 'Assets/Scenes/Main.unity'
const HARNESS_SCRIPT = cfg.harnessScript || (REPO + '/.claude/workflows/rockets-bench-matrix.js')
const MIN_TOKENS_PER_RUN = 120000

// Date/Math.random в workflow-скриптах недоступны (сломали бы resume) — дату передаёт вызывающий.
const DATE = cfg.date
if (!/^\d{4}-\d{2}-\d{2}$/.test(String(DATE || ''))) {
  throw new Error('args.date обязателен в формате YYYY-MM-DD (Date в workflow-скриптах недоступен, дату передаёт вызывающий)')
}

const rawModels = cfg.models
if (!Array.isArray(rawModels) || rawModels.length === 0) {
  throw new Error('args.models обязателен: ["claude-opus-5","claude-opus-4-6"] или [{"id":"claude-opus-4-6","slug":"opus46","efforts":["low","high"]}]')
}

// 'claude-opus-4-6' → 'opus46', 'claude-sonnet-4-5-20250929' → 'sonnet45'; дата в ID игнорируется.
function slugFor(id) {
  if (ALIASES.indexOf(id) >= 0) {
    return id + '-latest'
  }
  const parts = id.replace(/^claude-/, '').split('-').filter((p) => !/^\d{8}$/.test(p))
  const family = parts.shift()
  if (!family) {
    throw new Error('не удалось вывести slug из идентификатора модели "' + id + '"; передай slug явно')
  }
  return family + parts.join('')
}

const MODELS = rawModels.map((m) => {
  const spec = typeof m === 'string' ? { id: m } : m
  if (!spec || !spec.id) {
    throw new Error('элемент args.models должен быть строкой либо объектом {id, slug?, efforts?}')
  }
  if (spec.efforts && !Array.isArray(spec.efforts)) {
    throw new Error('models[].efforts должен быть массивом уровней либо отсутствовать')
  }
  const known = KNOWN_EFFORTS[spec.id]
  return {
    id: spec.id,
    slug: spec.slug || slugFor(spec.id),
    // null означает «поддержка неизвестна» — просим уровни как есть, не отбрасывая.
    supported: spec.efforts || (known === undefined ? null : known),
  }
})
for (const m of MODELS) {
  if (ALIASES.indexOf(m.id) >= 0) {
    log('Внимание: "' + m.id + '" — алиас, он разрешается в новейшую доступную версию семейства и со временем поменяется. Для воспроизводимого бенчмарка передавай точный ID, например claude-opus-4-6.')
  }
  if (m.supported === null) {
    log('Поддержка effort для "' + m.id + '" неизвестна харнессу: уровни запрашиваются как есть. Фактические значения проверит аудит после прогона.')
  }
}

const OUT = cfg.outDir || ('/Users/selstrom/work/bench-reports/rockets-' + DATE)

function branchFor(model, effort, run) {
  return 'bench/' + DATE + '/' + model.slug + '/rockets-pure-plan/' + effort + '_' + String(run).padStart(2, '0')
}

const CONDITIONS = []
const droppedCells = []
for (const model of MODELS) {
  let levels = []
  if (model.supported === null) {
    levels = EFFORTS.slice()
  } else if (model.supported.length === 0) {
    // Модель без поддержки effort: одно условие на модель, уровень не передаётся.
    levels = ['default']
    log('"' + model.id + '" не поддерживает effort — для неё будет одно условие ' + model.slug + '/default.')
  } else {
    for (const e of EFFORTS) {
      if (model.supported.indexOf(e) >= 0) {
        levels.push(e)
      } else {
        droppedCells.push(model.slug + '/' + e)
      }
    }
  }
  if (levels.length === 0) {
    throw new Error('для модели "' + model.id + '" не осталось ни одного поддерживаемого уровня effort из ' + EFFORTS.join(', '))
  }
  model.levels = levels
  for (const effort of levels) {
    for (let run = 1; run <= RUNS; run++) {
      CONDITIONS.push({ model: model, effort: effort, run: run, branch: branchFor(model, effort, run) })
    }
  }
}
if (droppedCells.length) {
  log('Отброшены неподдерживаемые ячейки: ' + droppedCells.join(', ') + '. Причина: Claude Code понизил бы уровень до ближайшего доступного, и условие стало бы дубликатом соседнего.')
}

// Обезличенные id раздаются в порядке хеша, а не в порядке условий: иначе S01 выдавал бы первое условие матрицы.
function hash(s) {
  let h = 5381
  for (let i = 0; i < s.length; i++) {
    h = ((h * 33) ^ s.charCodeAt(i)) >>> 0
  }
  return h
}
const ANON = CONDITIONS.slice()
  .sort((a, b) => hash(a.branch) - hash(b.branch))
  .map((c, i) => ({ branch: c.branch, anonId: 'S' + String(i + 1).padStart(2, '0') }))
const ANON_BY_BRANCH = {}
for (const a of ANON) {
  ANON_BY_BRANCH[a.branch] = a.anonId
}

// ─── Схемы ──────────────────────────────────────────────────────────────────
const RUN_SCHEMA = {
  type: 'object',
  properties: {
    branch: { type: 'string' },
    completed: { type: 'boolean', description: 'фича реализована, всё закоммичено, git status чистый' },
    commits: { type: 'integer' },
    testsSummary: { type: 'string', description: 'сколько тестов написано, сколько прошло/упало, EditMode/PlayMode' },
    testsGreen: { type: 'boolean', description: 'прогон тестов реально состоялся и был зелёным (не «должно работать»)' },
    verificationDepth: {
      type: 'string',
      enum: ['none', 'static', 'editor-tests', 'batchmode-tests', 'visual'],
      description: 'максимальная реально достигнутая глубина проверки',
    },
    unityWedged: { type: 'boolean', description: 'редактор завис на модалке (ping жив, остальное таймаутит)' },
    screenshotOk: { type: 'boolean', description: 'кадр полёта ракеты со развернувшимся следом снят и проверен глазами' },
    screenshotPath: { type: 'string' },
    screenshotSecondsAfterLaunch: { type: 'number', description: 'сколько игрового времени прошло от пуска до кадра' },
    screenshotNotes: { type: 'string', description: 'что видно на кадре; если снять не удалось — почему' },
    keyDecisions: { type: 'string' },
    unverified: { type: 'string', description: 'что не удалось проверить автоматически' },
    tokenEstimate: { type: 'string', description: 'собственная оценка потраченных токенов' },
    problems: { type: 'string' },
  },
  required: ['branch', 'completed', 'testsSummary', 'testsGreen', 'verificationDepth', 'screenshotOk', 'keyDecisions'],
}

const PREP_SCHEMA = {
  type: 'object',
  properties: {
    created: {
      type: 'array',
      items: {
        type: 'object',
        properties: {
          anonId: { type: 'string' },
          path: { type: 'string' },
          commitsAhead: { type: 'integer', description: 'коммитов в ветке поверх базового' },
        },
        required: ['anonId', 'path'],
      },
    },
    missing: { type: 'array', items: { type: 'string' }, description: 'anonId ветвей, которых нет в репозитории' },
    problems: { type: 'string' },
  },
  required: ['created', 'missing'],
}

const SCORE = { type: 'integer', minimum: 1, maximum: 10 }
const FLAG = { type: 'boolean' }
const REVIEW_SCHEMA = {
  type: 'object',
  properties: {
    anonId: { type: 'string' },
    scores: {
      type: 'object',
      properties: {
        architecture: SCORE,
        guidance: SCORE,
        tests: SCORE,
        assets: SCORE,
        risk: SCORE,
      },
      required: ['architecture', 'guidance', 'tests', 'assets', 'risk'],
    },
    rationale: { type: 'string', description: 'по 1–2 фразы на измерение, с опорой на конкретные файлы' },
    metrics: {
      type: 'object',
      properties: {
        commits: { type: 'integer' },
        filesChanged: { type: 'integer' },
        insertions: { type: 'integer' },
        deletions: { type: 'integer' },
        newCsFiles: { type: 'integer' },
        testAttributes: { type: 'integer', description: 'число [Test]/[UnityTest]/[TestCase] в добавленных тестах' },
        durationMinutes: { type: 'number', description: 'из bench-run.json; -1 если файла нет' },
        rawGitStat: { type: 'string', description: 'итоговая строка git diff --stat, дословно' },
        missingMetaFiles: { type: 'array', items: { type: 'string' } },
        unresolvedGuids: { type: 'array', items: { type: 'string' }, description: 'GUID-ссылки в префабах/сценах, не находящие .meta' },
        asmdefIssues: { type: 'array', items: { type: 'string' }, description: 'тесты используют сборки, не указанные в их .asmdef, и т.п.' },
      },
      required: ['commits', 'filesChanged', 'insertions', 'deletions', 'testAttributes', 'missingMetaFiles', 'unresolvedGuids', 'asmdefIssues'],
    },
    features: {
      type: 'object',
      description: 'соответствие ТЗ по факту кода, не по заявлениям',
      properties: {
        inputKeyR: FLAG,
        arcTrajectory: FLAG,
        nearestTarget: FLAG,
        collateralHits: FLAG,
        respawnTimer: FLAG,
        configDriven: FLAG,
        hudCounters: FLAG,
        ecsIntegrated: FLAG,
        prefabWired: FLAG,
        trailVfx: FLAG,
        toroidalAiming: FLAG,
        leadInterception: FLAG,
      },
      required: ['inputKeyR', 'arcTrajectory', 'nearestTarget', 'collateralHits', 'respawnTimer', 'configDriven', 'hudCounters', 'ecsIntegrated', 'prefabWired', 'trailVfx'],
    },
    claims: {
      type: 'object',
      properties: {
        claimedTestsGreen: { type: 'boolean' },
        claimedTestsSummary: { type: 'string' },
        claimedVerificationDepth: { type: 'string' },
        discrepancies: { type: 'string', description: 'расхождения между заявленным и наблюдаемым; пусто, если нет' },
      },
      required: ['claimedTestsGreen', 'discrepancies'],
    },
    blockers: {
      type: 'array',
      items: {
        type: 'object',
        properties: {
          severity: { type: 'string', enum: ['blocker', 'major', 'minor'] },
          summary: { type: 'string' },
          evidence: { type: 'string', description: 'файл, строка, вывод команды' },
          file: { type: 'string' },
        },
        required: ['severity', 'summary', 'evidence'],
      },
    },
  },
  required: ['anonId', 'scores', 'metrics', 'features', 'claims', 'blockers'],
}

const VERDICT_SCHEMA = {
  type: 'object',
  properties: {
    anonId: { type: 'string' },
    verdicts: {
      type: 'array',
      items: {
        type: 'object',
        properties: {
          summary: { type: 'string', description: 'проверяемый блокер, дословно из входных данных' },
          refuted: { type: 'boolean', description: 'true — блокер опровергнут, проблемы нет' },
          severityAdjusted: { type: 'string', enum: ['blocker', 'major', 'minor', 'none'] },
          reasoning: { type: 'string', description: 'чем именно подтверждён или опровергнут' },
        },
        required: ['summary', 'refuted', 'severityAdjusted', 'reasoning'],
      },
    },
  },
  required: ['anonId', 'verdicts'],
}

const PREFLIGHT_SCHEMA = {
  type: 'object',
  properties: {
    ok: { type: 'boolean' },
  },
  required: ['ok'],
}

const SETUP_SCHEMA = {
  type: 'object',
  properties: {
    ok: { type: 'boolean', description: 'харнесс скопирован' },
    baseCommit: { type: 'string', description: 'полный 40-символьный SHA из git rev-parse' },
    problems: { type: 'string' },
  },
  required: ['ok', 'baseCommit'],
}

const AUDIT_SCHEMA = {
  type: 'object',
  properties: {
    transcriptDir: { type: 'string' },
    entries: {
      type: 'array',
      items: {
        type: 'object',
        properties: {
          branch: { type: 'string', description: 'ветка, найденная в транскрипте агента' },
          requestedModel: { type: 'string', description: 'поле model из agent-*.meta.json' },
          resolvedModel: { type: 'string', description: 'первое "model":"claude-..." в транскрипте' },
          actualEffort: { type: 'string', description: 'первое "effort":"..." в транскрипте' },
          agentFile: { type: 'string' },
        },
        required: ['branch', 'resolvedModel', 'actualEffort'],
      },
    },
    problems: { type: 'string' },
  },
  required: ['entries'],
}

const REPORT_SCHEMA = {
  type: 'object',
  properties: {
    htmlPath: { type: 'string', description: 'обязательный результат' },
    pdfProduced: { type: 'boolean' },
    pdfPath: { type: 'string', description: 'если PDF собрался' },
    renderer: { type: 'string', description: 'чем собран PDF: weasyprint / chrome / pandoc; пусто, если PDF не собран' },
    pages: { type: 'integer' },
    screenshotsEmbedded: { type: 'integer', description: 'сколько кадров полёта попало в отчёт' },
    worktreesRemoved: { type: 'integer' },
    problems: { type: 'string' },
  },
  required: ['htmlPath', 'pdfProduced'],
}

// ─── Промпты ────────────────────────────────────────────────────────────────
const FEATURE_SPEC = '**Самонаводящиеся ракеты.** Нужно сделать новую фичу. Добавим самонаводящиеся ракеты. У игрока есть одна ракета, которую можно запустить, нажав кнопку R. Ракета летит по дуге в ближайшую цель. Если ракета по пути врежется не в выбранную цель, это тоже считается. После запуска ракет включается счётчик на респавн ракет. Количество ракет у игрока и время респавна должно быть задано в конфигах. Ракета коллайдится с астероидами и UFO. В качестве визуала ракеты можно использовать уменьшенный спрайт корабля. Инверсионный след можно сделать из спрайта частиц. Ракета должна быть создана и вписана в текущую архитектуру ECS + визуал. Количество доступных ракет и время респавна должно выводиться в HUD-е игрока. Фича должна быть разработана в парадигме TDD, весь функционал покрыт тестами. Проверка функционала при помощи юнит, интеграционных тестов и MCP. Human validation в исключительных случаях, когда текущих инструментов недостаточно и невозможно написать новый функционал для MCP.'

// Порядок «сохранить → пустая сцена → checkout → целевая сцена» обязателен: checkout, перезаписывающий
// открытую сцену, поднимает модалку EditorSceneManager и вешает главный поток Unity до вмешательства человека.
const BRANCH_SWITCH_RULE = 'ПРАВИЛО ПЕРЕКЛЮЧЕНИЯ ВЕТОК (соблюдать КАЖДЫЙ раз, когда меняешь ветку)\n' +
  'a. Убедись, что в открытой сцене нет несохранённых изменений: run_csharp с UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty. Если true — save_scene и проверь повторно, что isDirty стал false.\n' +
  'b. Открой пустую сцену, чтобы целевая перестала быть открытой: run_csharp с UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single). Убедись, что GetActiveScene().path теперь пустой.\n' +
  'c. Только теперь переключай ветку git.\n' +
  'd. После переключения: refresh_assets, дождись окончания компиляции (status), и лишь затем open_scene ' + SCENE + '.\n' +
  'Пропуск шагов a–b означает модальный диалог «scene changed on disk»: ping будет отвечать, а status/run_tests таймаутить, и снять диалог сможет только человек.'

function buildImplPrompt(cond) {
  const branch = cond.branch
  const anonId = ANON_BY_BRANCH[branch]
  return 'Ты — автономный инженер, работаешь в одиночку, пользователь недоступен. Проект: классическая аркада Asteroids на Unity 6.3, репозиторий ' + REPO + '. Unity Editor уже открыт с этим проектом. Его MCP-инструменты (сервер unity-asteroids: status, ping, recompile, refresh_assets, run_tests, get_logs, screenshot, read_asset, write_asset, create_prefab, open_prefab, open_scene, save_scene, run_csharp и др.) подключай через ToolSearch (например, запрос "select:mcp__unity-asteroids__run_tests"). Если MCP-инструмент недоступен (connection refused / timeout) — прочитай /Users/selstrom/.unity-mcp/registry.json: там recovery-блок с шагами и командой перезапуска.\n\n' +
    BRANCH_SWITCH_RULE + '\n\n' +
    'ПОДГОТОВКА\n' +
    '1. Отметь время старта: date -u +%FT%TZ — оно понадобится в конце.\n' +
    '2. Проверь чистоту рабочей копии: git status --porcelain. Если она НЕ чистая — закоммить всё в ТЕКУЩУЮ ветку коммитом "chore: leftover from previous bench run" и только потом продолжай.\n' +
    '3. Убедись, что ветки ' + branch + ' ещё нет: git rev-parse --verify ' + branch + ' должен ЗАВЕРШИТЬСЯ ОШИБКОЙ. Если ветка существует — это данные другого запуска; НЕ удаляй и НЕ переиспользуй её, прекрати работу и верни completed=false с объяснением в problems.\n' +
    '4. По правилу переключения веток (шаги a–b) освободи сцену, затем создай ветку и переключись: git checkout -b ' + branch + ' ' + BASE + '\n' +
    '5. Заверши переключение по шагу d: refresh_assets, status, open_scene ' + SCENE + '.\n\n' +
    'ЗАДАЧА\n' + FEATURE_SPEC + '\n\n' +
    '0. Составь план\n' +
    '1. Выполни задачу по плану\n' +
    '2. Все решения, оценку по токенам и этот промпт кратко запиши в DECISIONS.md (в корне репозитория)\n\n' +
    'ЖЁСТКИЕ ОГРАНИЧЕНИЯ\n' +
    '- НЕ читать каталоги памяти Claude (~/.claude/**/memory/, файлы MEMORY.md) — эксперимент требует чистого решения.\n' +
    '- НЕ заглядывать в другие git-ветки, reflog, stash (никаких git log/show/diff по чужим веткам, никаких --all): в репозитории есть другие реализации этой же фичи — подсматривать их нельзя. Работай только с кодом своей ветки.\n' +
    '- НЕ читать и НЕ изменять .claude/** в рабочей копии: там харнесс бенчмарка с рубрикой, по которой тебя будут оценивать, — его содержимое не должно влиять на решение.\n' +
    '- НЕ использовать skills superpowers:* и gsd-* (требование "GSD Workflow Enforcement" из CLAUDE.md для этого запуска отменено пользователем — правь файлы напрямую).\n' +
    '- Web-рисёч разрешён (WebSearch/WebFetch) — для исследования качественных решений (наведение ракет, дуговые траектории и т.п.).\n' +
    '- Human validation недоступна: всё проверяется юнит-/интеграционными тестами (EditMode/PlayMode) и через Unity MCP. Что проверить невозможно — зафиксируй в DECISIONS.md в разделе "Непроверенное".\n' +
    '- НЕ пушить в remote. Коммиты только локальные и только в свою ветку. НЕ создавай git worktree — работай в основной рабочей копии.\n\n' +
    'ОБЕЗЛИЧИВАНИЕ (важно для честности эксперимента)\n' +
    'Твою работу будет оценивать независимый ревьюер, который не должен знать, какая модель и какой режим её выполнял. Поэтому в DECISIONS.md, bench-run.json, коммит-сообщениях и комментариях НЕ упоминай ни модель, ни режим effort, ни имя ветки, ни номер прогона.\n\n' +
    'КАДР ПОЛЁТА (обязательный артефакт)\n' +
    'Кроме тестов нужен один кадр Game View: корабль уже выпустил ракету, ракета отлетела от корабля, инверсионный след развернулся на полную длину, но столкновения ещё не произошло.\n' +
    'Учитывай две ловушки редактора: кадры между MCP-вызовами сами не тикают, если окно не в фокусе, а InputSystem без фокуса Game View глотает события устройств — эмулировать нажатие R бесполезно.\n' +
    '1. Войди в Play Mode (set_play_mode) и дождись инициализации игры.\n' +
    '2. Пусти ракету программно через run_csharp — по тому же пути, каким это делает обработка ввода (вызов метода или публикация события пуска), а не имитацией клавиши.\n' +
    '3. Прокручивай кадры детерминированно: EditorApplication.QueuePlayerLoopUpdate() в цикле, либо PlayMode-тест с yield return null.\n' +
    '4. Момент кадра посчитай по своему же конфигу: след живёт ограниченное время (lifetime частиц или шлейфа), ракета живёт своё. Целься в t, примерно равное времени жизни следа после пуска, но заведомо до попадания. Если ракета попадает раньше, чем след развернётся, отодвинь цель или сдвинь момент.\n' +
    '5. Сними 3–4 кадра на разных t и выбери тот, где след длиннее всего, а ракета ещё в полёте. Посмотри выбранный кадр глазами (прочитай файл как изображение) и убедись, что видно корабль, ракету и непрерывный след.\n' +
    '6. Скриншот Game View через MCP приходит перевёрнутым по вертикали — известный артефакт. Переверни обратно: sips -f vertical <файл>.\n' +
    '7. Сохрани итоговый кадр в ' + OUT + '/shots/' + anonId + '.png (каталог создай). Это обезличенный идентификатор для имён файлов: ни модель, ни режим в имени и метаданных не упоминай.\n' +
    '8. Выйди из Play Mode.\n' +
    'Если кадр снять не удалось — screenshotOk=false и честная причина в screenshotNotes. Кадр без ракеты или без следа не подставляй.\n\n' +
    'ЕСЛИ UNITY ВСЁ-ТАКИ ЗАВИС\n' +
    'Признак: ping отвечает, а status/run_tests таймаутят — редактор стоит на модальном диалоге, снять его программно нельзя.\n' +
    'Тогда сделай APFS-клон и прогоняй тесты во втором редакторе в batchmode:\n' +
    '  cp -Rc ' + REPO + ' /Users/selstrom/work/.bench-clones/run-tmp\n' +
    '  "' + UNITY + '" -batchmode -projectPath /Users/selstrom/work/.bench-clones/run-tmp -runTests -testPlatform EditMode -testResults /tmp/bench-editmode.xml -logFile -\n' +
    'Ассеты, которые нужно генерировать кодом, собирай через -executeMethod и копируй результат назад со сверкой md5. Клон удали после себя.\n' +
    'Ни при каких обстоятельствах не заявляй зелёные тесты, если прогон не состоялся: это портит данные эксперимента сильнее, чем честное "не проверено".\n\n' +
    'ЗАВЕРШЕНИЕ (обязательно)\n' +
    '- Запиши bench-run.json в корне репозитория: {"startedAt": "<время из шага 1>", "finishedAt": "<date -u +%FT%TZ сейчас>", "verificationDepth": "none|static|editor-tests|batchmode-tests|visual", "testsGreen": true|false, "testsTotal": <число>, "testsFailed": <число>, "unityWedged": true|false, "selfTokenEstimate": "<оценка>"}. Без упоминаний модели и режима.\n' +
    '- Кадр полёта лежит в ' + OUT + '/shots/' + anonId + '.png, либо screenshotOk=false с причиной.\n' +
    '- Все изменения (код, тесты, ассеты, .meta, DECISIONS.md, bench-run.json) закоммичены в ветку ' + branch + '; git status --porcelain пустой. Кадр в репозиторий не коммить — он лежит вне него.\n' +
    '- Unity: Play Mode выключен, открытые сцены и префабы сохранены, тесты прогнаны, результат зафиксирован.\n' +
    '- Ветку не переключай — оставь checkout на своей ветке.'
}

function buildPreflightPrompt(model) {
  return 'Проверка доступности модели. Не вызывай инструменты, ничего не читай и не пиши. Просто верни ok=true. Смысл вызова в том, что он либо состоится на запрошенной модели, либо упадёт, и оркестратор узнает об этом до многочасовой фазы реализации. Модель: ' + model.id + '.'
}

function buildAuditPrompt(pairs) {
  return 'Сверь, на какой модели и с каким уровнем effort реально работал каждый агент этого прогона. Это проверка честности харнесса: заявленные параметры не принимаем на веру, читаем записанные транскрипты.\n\n' +
    'Заявлено (JSON, ветка → что просил оркестратор):\n' + JSON.stringify(pairs, null, 2) + '\n\n' +
    'КАК ИСКАТЬ\n' +
    '1. Найди каталог транскриптов текущего прогона: ls -dt ~/.claude/projects/*/subagents/workflows/wf_*/ | head -5. Нужен тот, где есть journal.jsonl и файлы agent-*.meta.json, а в транскриптах встречаются имена ветвей из списка выше (grep -l).\n' +
    '2. Для каждого файла agent-*.jsonl:\n' +
    '   ветка — найди в транскрипте имя одной из ветвей выше: grep -m1 -oE "bench/[^ \\"]+" <файл>;\n' +
    '   requestedModel — поле model из парного agent-*.meta.json;\n' +
    '   resolvedModel — фактическая модель запросов: grep -m1 -oE \'"model":"claude-[a-z0-9-]+"\' <файл>;\n' +
    '   actualEffort — фактический уровень: grep -m1 -oE \'"effort":"[a-z]+"\' <файл>.\n' +
    '   Файлы большие (единицы мегабайт) — работай через grep с -m1, не читай их целиком.\n' +
    '3. Агенты ревью и вспомогательные в список не включай. Файл, в котором встречаются СРАЗУ НЕСКОЛЬКО веток из списка, — это вспомогательный агент (подготовка worktree, сам аудит), а не реализация: пропусти его. У агента-реализации в транскрипте ровно одна ветка из списка.\n\n' +
    'Если каталог найти не удалось или в транскриптах нет нужных полей — верни пустой entries и объясни в problems. Не выдумывай значения: этот аудит существует ровно для того, чтобы поймать расхождение между заявленным и фактическим.'
}

function buildPrepPrompt(pairs) {
  return 'Подготовь обезличенные рабочие копии для слепого код-ревью. Репозиторий: ' + REPO + '.\n\n' +
    'Вход (JSON, ветка → обезличенный id):\n' + JSON.stringify(pairs, null, 2) + '\n\n' +
    'Для каждой пары:\n' +
    '1. Проверь, что ветка существует: git -C ' + REPO + ' rev-parse --verify <branch>. Если нет — добавь её anonId в missing и переходи к следующей.\n' +
    '2. Посчитай коммиты поверх базового: git -C ' + REPO + ' rev-list --count ' + BASE + '..<branch>\n' +
    '3. Создай detached-worktree по пути ' + OUT + '/wt/<anonId>: mkdir -p ' + OUT + '/wt && git -C ' + REPO + ' worktree add --detach ' + OUT + '/wt/<anonId> <branch>. Detached — чтобы в рабочей копии не светилось имя ветки.\n' +
    '4. Проверь, не осталось ли в копии следов условия эксперимента: grep -nIE "effort|opus|fable|sonnet|haiku|bench/" <path>/DECISIONS.md <path>/bench-run.json 2>/dev/null. Находки перечисли в problems, файлы НЕ правь.\n\n' +
    'Ничего не собирай, Unity не запускай, текущий checkout основного репозитория не меняй. Верни результат строго по схеме.'
}

function buildReviewPrompt(item) {
  return 'Ты — независимый код-ревьюер. Оцени одну реализацию фичи по коду. Рабочая копия: ' + item.path + ' (обезличенный идентификатор ' + item.anonId + ').\n\n' +
    'Ты НЕ знаешь, какая модель и какой режим её писал, и знать не должен: это слепая оценка. Не выполняй git-команды, раскрывающие имена ветвей (git branch, git log --all, git reflog, git worktree list). Если в файлах всё же попадётся упоминание модели или режима — игнорируй его и не давай ему влиять на оценку.\n\n' +
    'ИСХОДНОЕ ТЗ, по которому работал автор:\n' + FEATURE_SPEC + '\n\n' +
    'База для diff: коммит ' + BASE + ' (доступен в этой копии). Изменения смотри относительно него: git -C ' + item.path + ' diff --stat ' + BASE + ' HEAD\n\n' +
    'ЧТО СДЕЛАТЬ\n' +
    '1. Прочитай реализацию: ECS-компоненты и системы, интеграцию в мост и визуал, конфиги, ввод, HUD, тесты.\n' +
    '2. Проверь соответствие ТЗ по фактическому коду, а не по заявлениям автора: пуск по R, дуга (а не прямая), выбор ближайшей цели, зачёт попадания в постороннюю цель, таймер респавна, значения из конфигов, счётчики в HUD, вписанность в ECS, собранный префаб, шлейф из частиц. Отдельно отметь, учитывается ли тороидальность экрана при выборе цели (toroidalAiming) и есть ли упреждение — аналитический перехват движущейся цели (leadInterception).\n' +
    '3. Ассеты и сборка, статически: для каждого нового файла в Assets/ есть ли парный .meta (без .meta Unity ломается на чистом клоне); резолвятся ли GUID-ссылки из префабов и сцены в существующие .meta; не использует ли тестовая сборка типы из сборок, не перечисленных в её .asmdef.\n' +
    '4. Посчитай метрики: коммиты, файлы, вставки и удаления, новые .cs, число тестовых атрибутов ([Test], [UnityTest], [TestCase]). Длительность возьми из bench-run.json (startedAt/finishedAt) в минутах, при отсутствии файла верни -1. В rawGitStat положи дословно итоговую строку git diff --stat.\n' +
    '5. Сверь заявления автора из DECISIONS.md и bench-run.json с наблюдаемым: заявлены ли зелёные тесты и подтверждает ли это код. Например, если тест ссылается на тип, недоступный его сборке, прогон не мог быть зелёным. Расхождения опиши в claims.discrepancies.\n' +
    '6. Выстави оценки 1–10 по пяти измерениям:\n' +
    '   architecture — вписанность в существующую архитектуру, чистота слоёв, отсутствие дублирования;\n' +
    '   guidance — корректность наведения и полёта: дуга, выбор цели, поведение на границах, устойчивость математики;\n' +
    '   tests — осмысленность покрытия, наличие подтверждённого RED-гейта, отсутствие тавтологических и заглушённых тестов;\n' +
    '   assets — префаб, материалы, .meta, значения конфигов, строки HUD в сцене: заработает ли на чистом клоне;\n' +
    '   risk — регрессии и опасные места: на что наступит следующий разработчик.\n' +
    '7. Блокеры перечисли отдельно, с доказательством (файл, строка, вывод команды). severity=blocker — то, из-за чего фича не работает или проект не собирается на чистом клоне.\n\n' +
    'Файлы DECISIONS.md и bench-run.json — артефакты харнесса: используй их как заявления автора, но не учитывай в оценке качества кода. Unity не запускай, тесты не гоняй: оценка статическая. Верни результат строго по схеме, anonId = ' + item.anonId + '.'
}

function buildVerifyPrompt(item, blockers) {
  return 'Ты — состязательный проверяющий. Другой ревьюер заявил проблемы в реализации, лежащей в ' + item.path + ' (идентификатор ' + item.anonId + '). Твоя задача — попытаться ОПРОВЕРГНУТЬ каждую, а не подтвердить.\n\n' +
    'Заявленные проблемы (JSON):\n' + JSON.stringify(blockers, null, 2) + '\n\n' +
    'Для каждой проверь доказательство сам: прочитай указанные файлы, поищи механизм, который проблему снимает (файл существует под другим именем, ссылка резолвится, поведение покрыто иначе). Если проблема реальна — подтверди и при необходимости уточни severity; если доказательство не выдерживает проверки — refuted=true.\n\n' +
    'Опровергай только при наличии конкретного опровергающего свидетельства и указывай его в reasoning; догадка «возможно, всё в порядке» опровержением не является. Не выполняй git-команды, раскрывающие имена ветвей. Unity не запускай.\n\n' +
    'Поле summary копируй дословно из входных данных, иначе результаты не сопоставятся. anonId = ' + item.anonId + '.'
}

function buildReportPrompt(payload) {
  return 'Собери итоговый отчёт по бенчмарку и отрендери его в PDF. Каталог вывода: ' + OUT + ' (создай, если нет).\n\n' +
    'ДАННЫЕ. Ниже JSON между маркерами. Он уже агрегирован — НЕ пересчитывай, НЕ правь, НЕ придумывай чисел, которых в нём нет. Чего в JSON нет, помечай в отчёте «н/д».\n' +
    '<<<BENCH_JSON\n' + JSON.stringify(payload, null, 2) + '\nBENCH_JSON>>>\n\n' +
    'ШАГИ\n' +
    '1. mkdir -p ' + OUT + '/harness и запиши JSON дословно в ' + OUT + '/data.json.\n' +
    '2. Харнесс должен уже лежать в ' + OUT + '/harness/ — его копирует фаза Preflight, пока рабочая копия ещё на исходной ветке. Проверь наличие; если файла нет — восстанови из git: git -C ' + REPO + ' show "$(git -C ' + REPO + ' log --all -1 --format=%H -- .claude/workflows/rockets-bench-matrix.js)":.claude/workflows/rockets-bench-matrix.js > ' + OUT + '/harness/rockets-bench-matrix.js. В харнессе дословный промпт агентов, это часть воспроизводимости отчёта.\n' +
    '3. Напиши ' + OUT + '/conclusions.md — краткая проза по-русски, без воды: сначала главный вывод одним абзацем (что победило и победило ли вообще, с оговоркой про размер выборки), затем что реально различалось между условиями, затем что оказалось общим у всех реализаций, затем подтверждённые блокеры, затем чего этот эксперимент НЕ измеряет. Полными предложениями, не обрубками. Числа только из data.json.\n' +
    '4. Напиши ' + OUT + '/render.py — скрипт, который читает data.json и conclusions.md и генерирует ' + OUT + '/report.html. Все таблицы строятся кодом из JSON, руками числа не вписывай. Модуль markdown в системе есть — им конвертируй conclusions.md. HTML полностью самодостаточный: инлайновый CSS, никаких внешних шрифтов и картинок (иначе weasyprint полезет в сеть), печатная вёрстка @page A4 с полями 14 мм, компактные таблицы с полосатыми строками, моноширинный шрифт для идентификаторов и путей, page-break-inside: avoid для строк таблиц.\n' +
    '   Состав отчёта:\n' +
    '   титул — название, дата, базовый коммит, версия харнесса, матрица (модели × режимы × число прогонов), сколько прогонов реально состоялось;\n' +
    '   «Итоги» — текст из conclusions.md;\n' +
    '   таблица по условиям, одна строка = модель × режим: n прогонов, средний балл из 50 и разброс min–max, стандартное отклонение, Pass@1, Pass@' + RUNS + ', All@' + RUNS + ', медиана токенов, среднее число тестов, среднее время, число подтверждённых блокеров;\n' +
    '   таблица по измерениям рубрики (architecture / guidance / tests / assets / risk) — средние по каждому условию, чтобы видеть, где именно расходятся;\n' +
    '   таблица прогонов, все строки включая упавшие: условие, номер прогона, обезличенный id, балл, годен ли прогон, глубина верификации, «заявлено зелёным» против «подтверждено», число блокеров, а также фактические модель и effort из аудита (поля actualModel, actualEffort, paramsVerified) — расхождения выдели, paramsVerified=null означает «аудит не дал данных», а не «совпало»;\n' +
    '   матрица соответствия ТЗ: фичи по строкам, условия по столбцам, доля прогонов, где фича реализована;\n' +
    '   таблица блокеров: подтверждённые и опровергнутые, с severity, доказательством и id прогона;\n' +
    '   галерея кадров полёта — по одному кадру на прогон, сгруппировано по условиям, сетка по 2–3 в ряд; подпись: условие, номер прогона, обезличенный id, момент t после пуска. Путь кадра в каждой строке rows (screenshotPath). Перед встраиванием сделай копию и уменьши до ширины не больше 900 px (sips -Z 900), затем встрой как data-URI в base64 — так HTML остаётся самодостаточным и переносимым. Если файла нет или screenshotOk=false — вместо картинки плашка с причиной из screenshotNotes;\n' +
    '   «Методология» — как считались Pass@1 (средняя доля годных прогонов), Pass@' + RUNS + ' (годен хотя бы один), All@' + RUNS + ' (годны все); что значит годный прогон (завершён и без выживших блокеров severity=blocker); как проходило слепое ревью и состязательная проверка; какой моделью и на каком effort работал судья (meta.judgeModel, meta.judgeEffort) — это часть условий эксперимента, и её надо указать в отчёте прямым текстом;\n' +
    '   «Ограничения» — обязательно: если в матрице больше одной модели, скажи прямо, что шкала effort калибруется под каждую модель и одинаковый уровень у разных моделей не означает равный объём рассуждений (meta.effortScaleNote), поэтому сравнение по столбцу effort между моделями некорректно, сравнивать честно только модели целиком; если meta.droppedCells не пуст — перечисли отброшенные ячейки и причину; выборка ' + RUNS + ' прогонов на условие, поэтому разницу в пару баллов нельзя считать превосходством; проверка статическая, тесты в рамках ревью не запускались, поэтому зелёные прогоны идут как заявленные, а не подтверждённые; ревью слепое, но остаточная утечка условия через тексты возможна; Unity Editor и его Library общие для всех прогонов; порядок исполнения перемешан детерминированно (по хешу ветки), не случайно.\n' +
    '5. Проверь HTML: открой его и убедись, что таблицы не пустые, числа совпадают с data.json, картинки отображаются (проверить можно, срендерив страницу в PNG или прочитав размер data-URI). HTML — обязательный результат отчёта.\n' +
    '6. PDF — по возможности, не любой ценой. Попробуй по порядку до первого успеха: weasyprint ' + OUT + '/report.html ' + OUT + '/report.pdf; иначе "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" --headless --disable-gpu --no-pdf-header-footer --print-to-pdf=' + OUT + '/report.pdf ' + OUT + '/report.html; иначе pandoc. Если рендер падает или тянется долго — не занимайся починкой вёрстки под PDF: верни pdfProduced=false, оставь HTML как итог и напиши причину в problems. Если PDF собрался — проверь размер больше 20 КБ и число страниц через mdls -name kMDItemNumberOfPages, и верни в renderer то, что сработало.\n' +
    '7. Убери рабочие копии ревьюеров: для каждого пути из worktrees в JSON — git -C ' + REPO + ' worktree remove --force <path>, затем git -C ' + REPO + ' worktree prune. Посчитай удалённые.\n\n' +
    'Ветки эксперимента НЕ удаляй, текущий checkout основного репозитория не меняй. Верни результат строго по схеме.'
}

// ─── Фаза 0: доступность моделей ────────────────────────────────────────────
// Дешёвый вызов на каждой модели: если ID недействителен или модель недоступна аккаунту,
// это выяснится за секунды, а не после нескольких часов реализаций.
phase('Preflight')
log('Матрица: ' + MODELS.map((m) => m.slug + ' [' + m.levels.join(', ') + ']').join(' | ') + ' × ' + RUNS + ' прогонов = ' + CONDITIONS.length + ' реализаций. Последовательно (Unity Editor один), порядок перемешан детерминированно.')

const probeThunks = MODELS.map((m) => {
  return () => {
    return agent(buildPreflightPrompt(m), {
      label: 'preflight:' + m.slug,
      phase: 'Preflight',
      model: m.id,
      effort: m.supported && m.supported.length === 0 ? undefined : 'low',
      schema: PREFLIGHT_SCHEMA,
    }).then((r) => ({ ok: !!(r && r.ok) }))
  }
})
// Судья проверяется вместе с матрицей: его недоступность обнаружилась бы только после
// самой дорогой фазы, когда все реализации уже готовы, а ревью молча вернуло бы нули.
// Отдельный зонд нужен только когда ID судьи не совпадает ни с одной матричной моделью:
// каждый субагент платит ~38k токенов фиксированного контекста, дубль зонда — чистая трата.
const JUDGE_NEEDS_PROBE = !MODELS.some((m) => m.id === JUDGE_MODEL)
if (JUDGE_NEEDS_PROBE) {
  probeThunks.push(() => {
    return agent(buildPreflightPrompt({ id: JUDGE_MODEL }), {
      label: 'preflight:judge',
      phase: 'Preflight',
      model: JUDGE_MODEL,
      effort: 'low',
      schema: PREFLIGHT_SCHEMA,
    }).then((r) => ({ ok: !!(r && r.ok) }))
  })
}
// Копия харнесса в каталог отчёта делается сейчас, пока рабочая копия на исходной ветке:
// к фазе Report checkout будет стоять на ветке эксперимента, где этих файлов нет.
probeThunks.push(() => {
  // Механическая работа — haiku; опция effort не передаётся, haiku её не поддерживает.
  return agent('Две подготовительные операции, больше ничего не делай.\n' +
    '1. Разреши базовый реф в коммит: git -C ' + REPO + ' rev-parse "' + BASE_REF + '^{commit}" — полный 40-символьный SHA верни в baseCommit.\n' +
    '2. Скопируй файлы харнесса в каталог отчёта: mkdir -p ' + OUT + '/harness && cp ' + HARNESS_SCRIPT + ' ' + OUT + '/harness/. Рядом с харнессом может лежать одноимённый *.dryrun.mjs — скопируй и его, если есть. ok=true, если харнесс скопирован.', {
    label: 'preflight:setup',
    phase: 'Preflight',
    model: 'haiku',
    schema: SETUP_SCHEMA,
  }).then((r) => ({ ok: !!(r && r.ok), baseCommit: r ? r.baseCommit : null }))
})
const probes = await parallel(probeThunks)
if (JUDGE_NEEDS_PROBE) {
  const judgeProbe = probes[MODELS.length]
  if (!judgeProbe || !judgeProbe.ok) {
    throw new Error('модель судьи "' + JUDGE_MODEL + '" недоступна — без неё ревью сорвётся после многочасовой фазы реализации; передай другой judgeModel в args')
  }
}
const setupProbe = probes[probes.length - 1]
if (!setupProbe || !setupProbe.ok) {
  log('Не удалось скопировать харнесс в ' + OUT + '/harness — отчёт попробует восстановить его из git.')
}
if (!BASE) {
  const resolved = setupProbe ? setupProbe.baseCommit : null
  if (!resolved || !/^[0-9a-f]{40}$/.test(String(resolved))) {
    throw new Error('не удалось разрешить базовый реф "' + BASE_REF + '" в коммит — без зафиксированной базы прогон не воспроизводим')
  }
  BASE = String(resolved)
}
log('База веток: ' + BASE_REF + ' → ' + BASE)
const okModels = []
const unavailable = []
for (let i = 0; i < MODELS.length; i++) {
  if (probes[i] && probes[i].ok) {
    okModels.push(MODELS[i])
  } else {
    unavailable.push(MODELS[i].id)
  }
}
if (unavailable.length) {
  log('Недоступны, их условия исключены из матрицы: ' + unavailable.join(', '))
}
if (!JUDGE_NEEDS_PROBE && unavailable.indexOf(JUDGE_MODEL) >= 0) {
  throw new Error('модель судьи "' + JUDGE_MODEL + '" недоступна — без неё ревью сорвётся после многочасовой фазы реализации; передай другой judgeModel в args')
}
const RUNNABLE = CONDITIONS.filter((c) => okModels.indexOf(c.model) >= 0)
if (RUNNABLE.length === 0) {
  throw new Error('ни одна из заявленных моделей не доступна: ' + MODELS.map((m) => m.id).join(', '))
}
// Порядок исполнения перемешан детерминированно (по хешу ветки): подряд идущие условия
// делили бы общий дрейф окружения (Library, кеши, время суток), и он читался бы как
// разница между ячейками матрицы. Хеш, а не Math.random — чтобы не сломать resume.
const ORDERED = RUNNABLE.slice().sort((a, b) => hash(a.branch) - hash(b.branch))

// ─── Фаза 1: реализация ─────────────────────────────────────────────────────
const runResults = []
let skipped = 0
for (const cond of ORDERED) {
  phase('Implement')
  if (budget.total && budget.remaining() < MIN_TOKENS_PER_RUN) {
    skipped = ORDERED.length - runResults.length
    log('Бюджет исчерпан: пропущено прогонов — ' + skipped + '. Отчёт будет собран по выполненным.')
    break
  }
  const tag = cond.model.slug + '/' + cond.effort + '#' + String(cond.run).padStart(2, '0')
  log('Стартует ' + tag)
  const before = budget.spent()
  const report = await agent(buildImplPrompt(cond), {
    label: 'impl:' + tag,
    phase: 'Implement',
    model: cond.model.id,
    effort: cond.effort === 'default' ? undefined : cond.effort,
    schema: RUN_SCHEMA,
  })
  const outputTokens = budget.spent() - before
  runResults.push({
    model: cond.model.slug,
    modelId: cond.model.id,
    effort: cond.effort,
    run: cond.run,
    branch: cond.branch,
    anonId: ANON_BY_BRANCH[cond.branch],
    outputTokens: outputTokens,
    report: report || null,
    error: report ? null : 'agent failed or was skipped',
  })
  if (report) {
    log(tag + ': completed=' + report.completed + ', testsGreen=' + report.testsGreen + ', ~' + Math.round(outputTokens / 1000) + 'k output-токенов')
  } else {
    log(tag + ': АГЕНТ УПАЛ, ~' + Math.round(outputTokens / 1000) + 'k output-токенов')
  }
}

// ─── Фаза 2: обезличенные рабочие копии ─────────────────────────────────────
phase('Prep')
const pairs = runResults.map((r) => ({ branch: r.branch, anonId: r.anonId }))
const prep = await agent(buildPrepPrompt(pairs), { label: 'prep:worktrees', phase: 'Prep', model: 'haiku', schema: PREP_SCHEMA })
if (!prep || !prep.created || prep.created.length === 0) {
  log('Обезличенные копии создать не удалось — ревью и отчёт пропущены.')
  return { date: DATE, base: BASE, harness: HARNESS_VERSION, runs: runResults, prep: prep, review: null, report: null }
}
if (prep.missing && prep.missing.length) {
  log('Готово копий для ревью: ' + prep.created.length + ', отсутствуют ветки: ' + prep.missing.length)
} else {
  log('Готово копий для ревью: ' + prep.created.length)
}

// ─── Фаза 3: слепое ревью ───────────────────────────────────────────────────
phase('Review')
const reviewed = await pipeline(
  prep.created,
  (item) => {
    return agent(buildReviewPrompt(item), {
      label: 'review:' + item.anonId,
      phase: 'Review',
      model: JUDGE_MODEL,
      effort: JUDGE_EFFORT,
      schema: REVIEW_SCHEMA,
    })
  },
  (review, item) => {
    if (!review) {
      return null
    }
    const worth = (review.blockers || []).filter((b) => b.severity === 'blocker' || b.severity === 'major')
    if (REVIEWERS < 2 || worth.length === 0) {
      return { review: review, verdicts: null }
    }
    return agent(buildVerifyPrompt(item, worth), {
      label: 'verify:' + item.anonId,
      phase: 'Review',
      model: JUDGE_MODEL,
      effort: JUDGE_EFFORT,
      schema: VERDICT_SCHEMA,
    }).then((v) => ({ review: review, verdicts: v ? v.verdicts : null }))
  },
)

// ─── Фаза 3.5: сверка фактических параметров агентов ────────────────────────
phase('Audit')
const auditPairs = runResults.map((r) => ({ branch: r.branch, requestedModel: r.modelId, requestedEffort: r.effort }))
const audit = await agent(buildAuditPrompt(auditPairs), { label: 'audit:models', phase: 'Audit', model: 'haiku', schema: AUDIT_SCHEMA })
const auditByBranch = {}
if (audit && audit.entries) {
  for (const e of audit.entries) {
    auditByBranch[e.branch] = e
  }
}
if (audit && audit.entries && audit.entries.length) {
  log('Аудит параметров: сверено агентов — ' + audit.entries.length + ' из ' + runResults.length)
} else {
  log('Аудит параметров не дал данных' + (audit && audit.problems ? ': ' + audit.problems : '') + '. Фактические модель и effort в отчёте будут помечены как непроверенные.')
}

// Алиас разрешается в новейшую версию семейства, поэтому его совпадение не проверяем;
// точный ID должен присутствовать в фактической модели, а уровень effort сверяется в кавычках,
// иначе "high" совпало бы с "xhigh" по подстроке.
function paramsMatch(modelId, effort, entry) {
  if (!entry) {
    return null
  }
  const modelOk = ALIASES.indexOf(modelId) >= 0 ? true : String(entry.resolvedModel || '').indexOf(modelId) >= 0
  const effortOk = effort === 'default'
    ? true
    : (entry.actualEffort === effort || String(entry.actualEffort || '').indexOf('"' + effort + '"') >= 0)
  return modelOk && effortOk
}

// ─── Фаза 4: агрегация в коде и отчёт ───────────────────────────────────────
phase('Report')

const byAnon = {}
for (const r of reviewed) {
  if (r && r.review) {
    byAnon[r.review.anonId] = r
  }
}

function survivingBlockers(entry) {
  const blockers = entry.review.blockers || []
  if (!entry.verdicts) {
    return blockers
  }
  const refuted = {}
  for (const v of entry.verdicts) {
    if (v.refuted) {
      refuted[v.summary] = true
    }
  }
  return blockers.filter((b) => !refuted[b.summary])
}

const rows = runResults.map((r) => {
  const entry = byAnon[r.anonId] || null
  const auditEntry = auditByBranch[r.branch] || null
  const rep = r.report
  const scores = entry ? entry.review.scores : null
  const total = scores ? scores.architecture + scores.guidance + scores.tests + scores.assets + scores.risk : null
  const surviving = entry ? survivingBlockers(entry) : []
  const hardBlockers = surviving.filter((b) => b.severity === 'blocker')
  return {
    model: r.model,
    modelId: r.modelId,
    effort: r.effort,
    run: r.run,
    anonId: r.anonId,
    branch: r.branch,
    outputTokens: r.outputTokens,
    actualModel: auditEntry ? auditEntry.resolvedModel : null,
    actualEffort: auditEntry ? auditEntry.actualEffort : null,
    paramsVerified: paramsMatch(r.modelId, r.effort, auditEntry),
    agentCompleted: rep ? rep.completed : false,
    claimedTestsGreen: rep ? rep.testsGreen : (entry ? entry.review.claims.claimedTestsGreen : false),
    verificationDepth: rep ? rep.verificationDepth : (entry ? entry.review.claims.claimedVerificationDepth : 'none'),
    unityWedged: rep ? !!rep.unityWedged : false,
    selfTokenEstimate: rep ? rep.tokenEstimate : null,
    screenshotOk: rep ? !!rep.screenshotOk : false,
    screenshotPath: rep && rep.screenshotPath ? rep.screenshotPath : (OUT + '/shots/' + r.anonId + '.png'),
    screenshotSecondsAfterLaunch: rep && rep.screenshotSecondsAfterLaunch !== undefined ? rep.screenshotSecondsAfterLaunch : null,
    screenshotNotes: rep ? rep.screenshotNotes || null : null,
    reviewed: !!entry,
    scores: scores,
    scoreTotal: total,
    features: entry ? entry.review.features : null,
    metrics: entry ? entry.review.metrics : null,
    discrepancies: entry ? entry.review.claims.discrepancies : null,
    rationale: entry ? entry.review.rationale : null,
    blockers: surviving,
    refutedBlockers: entry && entry.verdicts ? entry.verdicts.filter((v) => v.refuted) : [],
    adversariallyVerified: !!(entry && entry.verdicts),
    // Годный прогон: агент довёл до конца, ревью состоялось, блокеров severity=blocker не выжило.
    usable: !!(rep && rep.completed && entry && hardBlockers.length === 0),
  }
})
// В отчёте строки идут в порядке матрицы, а не в перемешанном порядке исполнения.
const CONDITION_INDEX = {}
CONDITIONS.forEach((c, i) => {
  CONDITION_INDEX[c.branch] = i
})
rows.sort((a, b) => CONDITION_INDEX[a.branch] - CONDITION_INDEX[b.branch])

function mean(xs) {
  if (!xs.length) {
    return null
  }
  return xs.reduce((a, b) => a + b, 0) / xs.length
}
function stdev(xs) {
  if (xs.length < 2) {
    return null
  }
  const m = mean(xs)
  return Math.sqrt(xs.reduce((a, x) => a + (x - m) * (x - m), 0) / (xs.length - 1))
}
function median(xs) {
  if (!xs.length) {
    return null
  }
  const s = xs.slice().sort((a, b) => a - b)
  const mid = Math.floor(s.length / 2)
  if (s.length % 2) {
    return s[mid]
  }
  return (s[mid - 1] + s[mid]) / 2
}
function round(x, d) {
  if (x === null || x === undefined) {
    return null
  }
  const k = Math.pow(10, d)
  return Math.round(x * k) / k
}

const FEATURE_KEYS = ['inputKeyR', 'arcTrajectory', 'nearestTarget', 'collateralHits', 'respawnTimer', 'configDriven', 'hudCounters', 'ecsIntegrated', 'prefabWired', 'trailVfx', 'toroidalAiming', 'leadInterception']
const DIMS = ['architecture', 'guidance', 'tests', 'assets', 'risk']

const groups = []
for (const model of MODELS) {
  for (const effort of model.levels) {
    const g = rows.filter((r) => r.model === model.slug && r.effort === effort)
    if (g.length === 0) {
      continue
    }
    const totals = g.filter((r) => r.scoreTotal !== null).map((r) => r.scoreTotal)
    const usableCount = g.filter((r) => r.usable).length
    const dimMeans = {}
    for (const d of DIMS) {
      dimMeans[d] = round(mean(g.filter((r) => r.scores).map((r) => r.scores[d])), 2)
    }
    const featureRate = {}
    for (const k of FEATURE_KEYS) {
      const seen = g.filter((r) => r.features && r.features[k] !== undefined && r.features[k] !== null)
      featureRate[k] = seen.length ? round(seen.filter((r) => r.features[k]).length / seen.length, 2) : null
    }
    groups.push({
      model: model.slug,
      effort: effort,
      n: g.length,
      reviewed: g.filter((r) => r.reviewed).length,
      scoreMean: round(mean(totals), 2),
      scoreMin: totals.length ? Math.min.apply(null, totals) : null,
      scoreMax: totals.length ? Math.max.apply(null, totals) : null,
      scoreStdev: round(stdev(totals), 2),
      dimMeans: dimMeans,
      featureRate: featureRate,
      pass1: round(usableCount / g.length, 2),
      passAny: usableCount > 0 ? 1 : 0,
      passAll: usableCount === g.length ? 1 : 0,
      tokensMedian: median(g.map((r) => r.outputTokens)),
      tokensTotal: g.reduce((a, r) => a + r.outputTokens, 0),
      testsMean: round(mean(g.filter((r) => r.metrics).map((r) => r.metrics.testAttributes)), 1),
      durationMeanMin: round(mean(g.filter((r) => r.metrics && r.metrics.durationMinutes > 0).map((r) => r.metrics.durationMinutes)), 1),
      claimedGreen: g.filter((r) => r.claimedTestsGreen).length,
      screenshotsOk: g.filter((r) => r.screenshotOk).length,
      blockersConfirmed: g.reduce((a, r) => a + r.blockers.filter((b) => b.severity === 'blocker').length, 0),
      majorsConfirmed: g.reduce((a, r) => a + r.blockers.filter((b) => b.severity === 'major').length, 0),
      blockersRefuted: g.reduce((a, r) => a + r.refutedBlockers.length, 0),
    })
  }
}

const payload = {
  meta: {
    title: 'Бенчмарк: самонаводящиеся ракеты',
    date: DATE,
    baseRef: BASE_REF,
    baseCommit: BASE,
    repo: REPO,
    harness: HARNESS_VERSION,
    branchTemplate: 'bench/{date}/{model_slug}/rockets-pure-plan/{effort}_NN',
    models: MODELS,
    efforts: EFFORTS,
    droppedCells: droppedCells,
    unavailableModels: unavailable,
    runnableRuns: RUNNABLE.length,
    effortScaleNote: 'шкала effort калибруется отдельно для каждой модели: одинаковый уровень у разных моделей не означает одинаковый объём рассуждений',
    paramsAuditedRuns: rows.filter((r) => r.paramsVerified === true).length,
    paramsMismatchedRuns: rows.filter((r) => r.paramsVerified === false).length,
    runsPerCondition: RUNS,
    reviewersPerBranch: REVIEWERS,
    judgeModel: JUDGE_MODEL,
    judgeEffort: JUDGE_EFFORT,
    plannedRuns: CONDITIONS.length,
    executedRuns: runResults.length,
    skippedForBudget: skipped,
    reviewedRuns: rows.filter((r) => r.reviewed).length,
    screenshotsOk: rows.filter((r) => r.screenshotOk).length,
    unityVerification: 'ревью статическое: тесты в рамках ревью не запускались',
    visualArtifact: 'по одному кадру полёта ракеты на прогон, снят автором реализации в Play Mode',
    dimensions: DIMS,
    featureKeys: FEATURE_KEYS,
  },
  groups: groups,
  rows: rows,
  audit: audit || null,
  worktrees: prep.created.map((c) => c.path),
  missingBranches: prep.missing || [],
  prepProblems: prep.problems || null,
}

const reportOut = await agent(buildReportPrompt(payload), { label: 'report:pdf', phase: 'Report', schema: REPORT_SCHEMA })
if (reportOut && reportOut.pdfProduced) {
  log('Отчёт: ' + reportOut.pdfPath + ' (' + reportOut.renderer + ', страниц: ' + (reportOut.pages || '?') + '), HTML: ' + reportOut.htmlPath)
} else if (reportOut) {
  log('Отчёт только в HTML: ' + reportOut.htmlPath + (reportOut.problems ? ' — PDF не собран: ' + reportOut.problems : ''))
} else {
  log('Отчёт собрать не удалось; агрегированные данные возвращены в результате workflow.')
}

return { summary: payload.meta, groups: groups, rows: rows, report: reportOut, outDir: OUT }
