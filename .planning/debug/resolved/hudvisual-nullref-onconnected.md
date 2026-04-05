---
status: resolved
trigger: "NullReferenceException в HudVisual.OnConnected() line 40 при вызове GameScreen.ActivateHud()"
created: 2026-04-06T12:00:00Z
updated: 2026-04-06T12:00:00Z
---

## Current Focus

hypothesis: _rocketReloadTime SerializeField = null в runtime, потому что Unity не десериализовала ручные YAML-изменения сцены Main.unity корректно. Строка 40 обращается к _rocketReloadTime.gameObject без null-check.
test: Проверить формат YAML stripped references — идентичен рабочим (laser)
expecting: Если формат идентичен, проблема в кэше Unity или необходимости пересохранения сцены
next_action: Ожидание подтверждения от пользователя — пересохранить сцену и проверить PlayMode

## Symptoms

expected: Игра запускается, HUD показывает информацию о ракетах (Rockets: N, Reload rocket: N sec)
actual: NullReferenceException в HudVisual.OnConnected() at line 40 при старте игры
errors: |
  NullReferenceException: Object reference not set to an instance of an object
  SelStrom.Asteroids.HudVisual.OnConnected () (at Assets/Scripts/View/HudVisual.cs:40)
  Shtl.Mvvm.AbstractWidgetView`1[TViewModel].Connect (TViewModel vm)
  SelStrom.Asteroids.GameScreen.ActivateHud () (at Assets/Scripts/Application/Screens/GameScreen.cs:59)
reproduction: Запустить PlayMode, нажать Play на Title Screen
started: После выполнения Phase 15 (HUD rocket) — добавлены новые SerializeField в HudVisual

## Eliminated

- hypothesis: Неправильный формат YAML stripped references для PrefabInstance
  evidence: Формат 2010010003/2010020003 идентичен рабочим laser references (1554481866). Те же поля, тот же prefab GUID, тот же m_CorrespondingSourceObject.
  timestamp: 2026-04-06T12:00:00Z

- hypothesis: Неправильный parent или отсутствие в m_Children
  evidence: RectTransform 2010010002 и 2010020002 есть в m_Children родителя 1687355232 (тот же что и для laser/other HUD items).
  timestamp: 2026-04-06T12:00:00Z

- hypothesis: ViewModel или Bind = null
  evidence: Строки 32-37 выполняются успешно (laser bindings работают), ViewModel назначается в Connect() перед OnConnected().
  timestamp: 2026-04-06T12:00:00Z

## Evidence

- timestamp: 2026-04-06T12:00:00Z
  checked: HudVisual.cs строка 40
  found: _rocketReloadTime.gameObject — доступ к .gameObject на потенциально null SerializeField. Строка 39 (.To(_rocketReloadTime)) не падает т.к. лямбда-захват (view=null, но лямбда не исполняется сразу).
  implication: Если _rocketReloadTime null, NullRef на строке 40 неизбежен.

- timestamp: 2026-04-06T12:00:00Z
  checked: Main.unity YAML — ссылки _rocketAmmoCount и _rocketReloadTime
  found: Ссылки fileID 2010010003 и 2010020003 присутствуют, stripped objects существуют, формат идентичен рабочим (laser fileID 1554481866).
  implication: YAML корректен. Проблема вероятно в том, что Unity Editor не загрузил/не десериализовал ручные YAML-изменения.

- timestamp: 2026-04-06T12:00:00Z
  checked: Коммит a0a358a (15-02)
  found: PrefabInstance для rocket_ammo_count и rocket_reload_time добавлены вручную через YAML-редактирование
  implication: Ручное YAML-редактирование потенциально не подхвачено кэшем Unity Editor.

## Resolution

root_cause: Строка 40 в HudVisual.OnConnected() обращается к _rocketReloadTime.gameObject без null-проверки. SerializeField _rocketReloadTime и _rocketAmmoCount не десериализованы в runtime (null) — вероятно из-за того что PrefabInstance были добавлены вручную через YAML и Unity Editor не подхватил изменения (требуется пересохранение сцены).
fix: 1) Null-guard в HudVisual.OnConnected() с Debug.LogWarning для rocket SerializeField. 2) Регрессионный тест HudSerializeFieldTests — проверяет что все SerializeField HudVisual привязаны в сцене (fileID != 0) и ссылки существуют. 3) Пользователь должен пересохранить сцену через Unity Editor (File > Save Scene) чтобы Unity подхватил ручные YAML-изменения.
verification: Пользователь подтвердил — NullRef устранён, игра запускается корректно
files_changed: [Assets/Scripts/View/HudVisual.cs, Assets/Tests/EditMode/Upgrade/HudSerializeFieldTests.cs]
