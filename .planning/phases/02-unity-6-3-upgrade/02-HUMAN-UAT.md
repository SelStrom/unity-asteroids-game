---
status: partial
phase: 02-unity-6-3-upgrade
source: [02-VERIFICATION.md]
started: 2026-04-02T20:00:00.000Z
updated: 2026-04-03T12:00:00Z
---

## Current Test

[testing complete]

## Tests

### 1. Компиляция в Unity Editor
expected: Проект открывается в Unity 6.3 без ошибок компиляции (0 errors в Console)
result: issue
reported: "UrpPostProcessingTests.cs и UrpSetupTests.cs — CS0234: UnityEngine.Rendering.Universal не найден. MoveSystemTests.cs, ThrustSystemTests.cs, RotateSystemTests.cs — CS0246: TimeData не найден. EcsEditModeTests.asmdef — duplicate references: Asteroids,Shtl.Mvvm. com.unity.ide.vscode deprecated."
severity: blocker

### 2. Импорт TMP Essential Resources
expected: Window > TextMeshPro > Import TMP Essential Resources. Шрифты и UI-текст отображаются корректно в сцене Main
result: pass

### 3. Запуск тестов (EditMode + PlayMode)
expected: Test Runner > Run All -- все тесты зелёные (UpgradeValidationTests, TmpIntegrationTests, PackageCompatibilityTests, GameplaySmokeTests)
result: blocked
blocked_by: prior-phase
reason: "Из-за ошибок компиляции из теста 1 невозможно запустить тесты"

### 4. Геймплей 1:1
expected: Играть 2-3 минуты. Проверить: корабль двигается (WASD), стреляет пулями (Space), лазером (Q), астероиды дробятся, НЛО появляются и стреляют, лидерборд работает, тороидальный экран
result: blocked
blocked_by: prior-phase
reason: "Из-за ошибок компиляции из теста 1 невозможно запустить игру"

## Summary

total: 4
passed: 1
issues: 1
pending: 0
skipped: 0
blocked: 2

## Gaps

- truth: "Проект открывается в Unity 6.3 без ошибок компиляции (0 errors в Console)"
  status: failed
  reason: "User reported: UrpPostProcessingTests.cs и UrpSetupTests.cs — CS0234: UnityEngine.Rendering.Universal не найден. MoveSystemTests.cs, ThrustSystemTests.cs, RotateSystemTests.cs — CS0246: TimeData не найден. EcsEditModeTests.asmdef — duplicate references. com.unity.ide.vscode deprecated."
  severity: blocker
  test: 1
  root_cause: "3 независимых бага: (1) EditModeTests.asmdef не ссылается на com.unity.render-pipelines.universal — URP установлен но тесты не видят namespace; (2) MoveSystemTests/ThrustSystemTests/RotateSystemTests используют bare TimeData вместо Unity.Core.TimeData (MoveToSystemTests делает правильно); (3) EcsEditModeTests.asmdef содержит дублированные ссылки Asteroids и Shtl.Mvvm"
  artifacts:
    - path: "Assets/Tests/EditMode/EditModeTests.asmdef"
      issue: "Отсутствует ссылка на URP assembly"
    - path: "Assets/Tests/EditMode/ECS/MoveSystemTests.cs"
      issue: "TimeData без квалификации Unity.Core"
    - path: "Assets/Tests/EditMode/ECS/ThrustSystemTests.cs"
      issue: "TimeData без квалификации Unity.Core"
    - path: "Assets/Tests/EditMode/ECS/RotateSystemTests.cs"
      issue: "TimeData без квалификации Unity.Core"
    - path: "Assets/Tests/EditMode/ECS/EcsEditModeTests.asmdef"
      issue: "Дублированные ссылки Asteroids и Shtl.Mvvm"
  missing:
    - "Добавить ссылку на URP в EditModeTests.asmdef"
    - "Добавить using Unity.Core или квалифицировать TimeData как Unity.Core.TimeData в 3 тест-файлах"
    - "Удалить дублирующиеся записи из EcsEditModeTests.asmdef"
  debug_session: ""
