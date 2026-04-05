---
status: resolved
trigger: "У ракеты нет инверсионного следа (ParticleSystem trail). VIS-02 был реализован в Phase 14."
created: 2026-04-06T00:00:00Z
updated: 2026-04-06T00:00:00Z
---

## Current Focus

hypothesis: CONFIRMED — ParticleSystemRenderer использовал Default-Particle материал (Built-in RP), который невидим в URP
test: Заменён материал на Particle-URP (guid: 4be2522842094e02ab4a0c9dd5e68203)
expecting: Частицы trail теперь видны в PlayMode
next_action: Ожидание ручной верификации в PlayMode

## Symptoms

expected: Ракета при полёте должна оставлять инверсионный след (ParticleSystem). Требование VIS-02.
actual: Ракета летит без видимого следа. ParticleSystem trail не виден.
errors: Нет ошибок в консоли (RocketVisual имеет null-check на _trailEffect)
reproduction: Запустить PlayMode, нажать R для запуска ракеты, наблюдать — нет trail эффекта
started: После реализации Phase 14 (Config & Visual Polish)

## Eliminated

## Evidence

- timestamp: 2026-04-06T00:01:00Z
  checked: RocketVisual.cs — код _trailEffect
  found: Поле [SerializeField] private ParticleSystem _trailEffect с null-check в OnConnected() и OnDisable(). Если null — молча пропускает Play/Stop.
  implication: Если _trailEffect не назначен в prefab, trail не будет работать без ошибок.

- timestamp: 2026-04-06T00:02:00Z
  checked: rocket.prefab YAML — MonoBehaviour RocketVisual (fileID 210864306994376054)
  found: Сериализованные поля содержат только _collider. Поле _trailEffect ОТСУТСТВУЕТ в YAML.
  implication: _trailEffect == null в runtime, Play() никогда не вызывается.

- timestamp: 2026-04-06T00:02:30Z
  checked: rocket.prefab YAML — дочерний объект Trail (fileID 7048776353523148144)
  found: Дочерний GameObject "Trail" с ParticleSystem (fileID 1549792664386277330) существует на prefab.
  implication: ParticleSystem создан, но не привязан к SerializeField.

- timestamp: 2026-04-06T01:00:00Z
  checked: ParticleSystemRenderer материал в rocket.prefab (строка 4957)
  found: m_Materials использует {fileID: 10301, guid: 0000000000000000f000000000000000, type: 0} — это встроенный Default-Particle (Built-in RP шейдер). Проект использует URP (UniversalRenderPipelineGlobalSettings.asset существует).
  implication: Default-Particle материал невидим в URP — частицы рендерятся, но с pink/invisible шейдером.

- timestamp: 2026-04-06T01:01:00Z
  checked: vfx_blow.prefab — рабочий ParticleSystem для сравнения
  found: vfx_blow использует материал Particle-URP (fileID: 2100000, guid: 4be2522842094e02ab4a0c9dd5e68203, type: 2). Этот эффект визуально работает.
  implication: Particle-URP — правильный материал для частиц в этом проекте.

- timestamp: 2026-04-06T01:02:00Z
  checked: Настройки ParticleSystem Trail: startSize, emission, lifetime, color
  found: startSize=0.08, startLifetime=0.4, emission rateOverTime=40, startColor alpha=0.8, startSpeed=0. Сравнение: vfx_blow startSize=1.
  implication: Размер мал, но при правильном материале должен быть виден. Если после фикса материала trail слишком мелкий — потребуется увеличить startSize.

- timestamp: 2026-04-06T01:03:00Z
  checked: Применён фикс — заменён материал в rocket.prefab
  found: Строка m_Materials заменена с Default-Particle на Particle-URP (идентично vfx_blow.prefab).
  implication: Частицы теперь должны рендериться URP-совместимым шейдером.

## Resolution

root_cause: Два дефекта: (1) _trailEffect SerializeField не был назначен в prefab (исправлено ранее). (2) ParticleSystemRenderer использовал встроенный материал Default-Particle (fileID: 10301, Built-in RP), который не рендерится в URP-проекте. Эффект vfx_blow корректно использует Particle-URP материал, а rocket trail — нет.
fix: (1) Ранее: привязана ссылка _trailEffect к ParticleSystem дочернего Trail. (2) Сейчас: заменён материал ParticleSystemRenderer с Default-Particle на Particle-URP (guid: 4be2522842094e02ab4a0c9dd5e68203, fileID: 2100000). Параметры PS: startSize=0.08, lifetime=0.4, emission rate=40, startSpeed=0 — корректны для trail-эффекта.
verification: Подтверждено пользователем в PlayMode — trail ракеты виден.
files_changed: [Assets/Media/prefabs/rocket.prefab, Assets/Tests/EditMode/Upgrade/RocketPrefabSerializeFieldTests.cs, Assets/Tests/EditMode/Upgrade/UrpMaterialTests.cs]
