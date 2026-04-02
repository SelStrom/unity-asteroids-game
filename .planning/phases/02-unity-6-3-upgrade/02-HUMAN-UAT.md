---
status: partial
phase: 02-unity-6-3-upgrade
source: [02-VERIFICATION.md]
started: 2026-04-02T20:00:00.000Z
updated: 2026-04-02T20:00:00.000Z
---

## Current Test

[awaiting human testing]

## Tests

### 1. Компиляция в Unity Editor
expected: Проект открывается в Unity 6.3 без ошибок компиляции (0 errors в Console)
result: [pending]

### 2. Импорт TMP Essential Resources
expected: Window > TextMeshPro > Import TMP Essential Resources. Шрифты и UI-текст отображаются корректно в сцене Main
result: [pending]

### 3. Запуск тестов (EditMode + PlayMode)
expected: Test Runner > Run All -- все тесты зелёные (UpgradeValidationTests, TmpIntegrationTests, PackageCompatibilityTests, GameplaySmokeTests)
result: [pending]

### 4. Геймплей 1:1
expected: Играть 2-3 минуты. Проверить: корабль двигается (WASD), стреляет пулями (Space), лазером (Q), астероиды дробятся, НЛО появляются и стреляют, лидерборд работает, тороидальный экран
result: [pending]

## Summary

total: 4
passed: 0
issues: 0
pending: 4
skipped: 0
blocked: 0

## Gaps
