---
status: partial
phase: 02-unity-6-3-upgrade
source: [02-VERIFICATION.md]
started: 2026-04-02T20:00:00.000Z
updated: 2026-04-03T23:30:00Z
---

## Current Test

[awaiting re-verification after gap closure 02-03]

## Tests

### 1. Компиляция в Unity Editor
expected: Проект открывается в Unity 6.3 без ошибок компиляции (0 errors в Console)
result: pending
note: "Gap closure 02-03 исправил все 3 бага (URP asmdef, TimeData квалификация, дубликаты). Требуется повторная проверка в Unity Editor."

### 2. Импорт TMP Essential Resources
expected: Window > TextMeshPro > Import TMP Essential Resources. Шрифты и UI-текст отображаются корректно в сцене Main
result: pass

### 3. Запуск тестов (EditMode + PlayMode)
expected: Test Runner > Run All -- все тесты зелёные (UpgradeValidationTests, TmpIntegrationTests, PackageCompatibilityTests, GameplaySmokeTests)
result: pending
note: "Разблокировано после gap closure 02-03. Требуется проверка в Unity Editor."

### 4. Геймплей 1:1
expected: Играть 2-3 минуты. Проверить: корабль двигается (WASD), стреляет пулями (Space), лазером (Q), астероиды дробятся, НЛО появляются и стреляют, лидерборд работает, тороидальный экран
result: pending
note: "Разблокировано после gap closure 02-03. Требуется проверка в Unity Editor."

## Summary

total: 4
passed: 1
issues: 0
pending: 3
skipped: 0
blocked: 0

## Gaps

- truth: "Проект открывается в Unity 6.3 без ошибок компиляции (0 errors в Console)"
  status: resolved
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
