export const meta = {
  name: 'rockets-opus5-effort-matrix',
  description: 'Самонаводящиеся ракеты: 5 независимых реализаций Opus 5 (low..max), последовательно, каждая в своей ветке',
  phases: [
    { title: 'low', detail: 'Opus 5 effort=low → feature/rockets-pure-opus5-low-plan', model: 'opus' },
    { title: 'medium', detail: 'Opus 5 effort=medium → feature/rockets-pure-opus5-medium-plan', model: 'opus' },
    { title: 'high', detail: 'Opus 5 effort=high → feature/rockets-pure-opus5-high-plan', model: 'opus' },
    { title: 'xhigh', detail: 'Opus 5 effort=xhigh → feature/rockets-pure-opus5-xhigh-plan', model: 'opus' },
    { title: 'max', detail: 'Opus 5 effort=max → feature/rockets-pure-opus5-max-plan', model: 'opus' },
  ],
}

const EFFORTS = ['low', 'medium', 'high', 'xhigh', 'max']
const BASE = '264ba77a47f4d03ab4beb10cf83cc1dcaf54a34d'

const REPORT_SCHEMA = {
  type: 'object',
  properties: {
    branch: { type: 'string' },
    completed: { type: 'boolean', description: 'фича реализована, всё закоммичено, git status чистый' },
    commits: { type: 'integer', description: 'сколько коммитов сделано в ветке' },
    testsSummary: { type: 'string', description: 'сколько тестов написано, сколько прошло/упало, EditMode/PlayMode' },
    keyDecisions: { type: 'string', description: 'ключевые архитектурные решения, кратко' },
    unverified: { type: 'string', description: 'что не удалось проверить автоматически' },
    tokenEstimate: { type: 'string', description: 'собственная оценка потраченных токенов (как в DECISIONS.md)' },
    problems: { type: 'string', description: 'проблемы по ходу работы (MCP, компиляция, тесты)' },
  },
  required: ['branch', 'completed', 'testsSummary', 'keyDecisions'],
}

function buildPrompt(effort) {
  const branch = 'feature/rockets-pure-opus5-' + effort + '-plan'
  return `Ты — автономный инженер, работаешь в одиночку, пользователь недоступен. Проект: классическая аркада Asteroids на Unity, репозиторий /Users/selstrom/work/projects/asteroids. Unity Editor уже открыт с этим проектом. Его MCP-инструменты (сервер unity-asteroids: status, recompile, refresh_assets, run_tests, get_logs, screenshot, read_asset, write_asset, create_prefab, open_prefab, run_csharp и др.) подключай через ToolSearch (например, запрос "select:mcp__unity-asteroids__run_tests"). Если MCP-инструмент недоступен (connection refused / timeout) — прочитай /Users/selstrom/.unity-mcp/registry.json: там recovery-блок с шагами и командой перезапуска; инструмент ping отвечает даже когда главный поток Unity занят (компиляция/модальное окно).

ПОДГОТОВКА
1. Проверь чистоту рабочей копии: git status --porcelain. Если она НЕ чистая — закоммить всё, что есть, в ТЕКУЩУЮ ветку коммитом "chore: leftover from previous run" и только потом продолжай.
2. Создай ветку и переключись: git checkout -b ${branch} ${BASE}
3. Дождись перекомпиляции Unity (refresh_assets, затем status), прежде чем работать с редактором.

ЗАДАЧА
**Самонаводящиеся ракеты.** Нужно сделать новую фичу. Добавим самонаводящиеся ракеты. У игрока есть одна ракета, которую можно запустить, нажав кнопку R. Ракета летит по дуге в ближайшую цель. Если ракета по пути врежется не в выбранную цель, это тоже считается. После запуска ракет включается счётчик на респавн ракет. Количество ракет у игрока и время респавна должно быть задано в конфигах. Ракета коллайдится с астероидами и UFO. В качестве визуала ракеты можно использовать уменьшенный спрайт корабля. Инверсионный след можно сделать из спрайта частиц. Ракета должна быть создана и вписана в текущую архитектуру ECS + визуал. Количество доступных ракет и время респавна должно выводиться в HUD-е игрока. Фича должна быть разработана в парадигме TDD, весь функционал покрыт тестами. Проверка функционала при помощи юнит, интеграционных тестов и MCP. Human validation в исключительных случаях, когда текущих инструментов недостаточно и невозможно написать новый функционал для MCP.

0. Составь план
1. Выполни задачу по плану
2. Все решения, оценку по токенам и этот промпт кратко запиши в DECISIONS.md (в корне репозитория)

ЖЁСТКИЕ ОГРАНИЧЕНИЯ
- НЕ читать каталоги памяти Claude (~/.claude/**/memory/, файлы MEMORY.md) — эксперимент требует чистого решения.
- НЕ заглядывать в другие git-ветки, reflog, stash (никаких git log/show/diff по чужим веткам): в репозитории могут существовать другие реализации этой же фичи — подсматривать их нельзя. Работай только с кодом своей ветки.
- НЕ использовать skills superpowers:* и gsd-* (требование "GSD Workflow Enforcement" из CLAUDE.md для этого запуска отменено пользователем — правь файлы напрямую).
- Web-рисёч разрешён (WebSearch/WebFetch) — для исследования качественных решений (наведение ракет, дуговые траектории и т.п.).
- Human validation недоступна: всё проверяется юнит-/интеграционными тестами (EditMode/PlayMode) и через Unity MCP. Что проверить невозможно — зафиксируй в DECISIONS.md в разделе "Непроверенное".
- НЕ пушить в remote. Коммиты только локальные и только в свою ветку.

ЗАВЕРШЕНИЕ (обязательно)
- Все изменения (код, тесты, ассеты, .meta, DECISIONS.md) закоммичены в ветку ${branch}; git status --porcelain пустой.
- Unity: Play Mode выключен, открытые сцены/префабы сохранены, тесты прогнаны, результат зафиксирован, ошибок нет, .meta файлы закомичены.
- Ветку не переключай — оставь checkout на своей ветке.`
}

const results = []
for (const effort of EFFORTS) {
  phase(effort)
  log('Стартует агент Opus 5, effort=' + effort)
  const before = budget.spent()
  const report = await agent(buildPrompt(effort), {
    label: 'opus5-' + effort,
    phase: effort,
    model: 'opus',
    effort: effort,
    schema: REPORT_SCHEMA,
  })
  const outputTokens = budget.spent() - before
  if (!report) {
    log(effort + ': агент упал или пропущен (~' + Math.round(outputTokens / 1000) + 'k output-токенов)')
    results.push({ effort, outputTokens, report: null, error: 'agent failed or was skipped' })
    continue
  }
  log(effort + ': завершено, completed=' + report.completed + ', ~' + Math.round(outputTokens / 1000) + 'k output-токенов')
  results.push({ effort, outputTokens, report })
}
return { baseCommit: BASE, results }