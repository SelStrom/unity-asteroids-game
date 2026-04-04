---
status: partial
phase: 03-urp-migration
source: [03-VERIFICATION.md]
started: 2026-04-02T20:30:00Z
updated: 2026-04-02T20:30:00Z
---

## Current Test

[awaiting human testing]

## Tests

### 1. YAML asset deserialization
expected: Проект открывается в Unity 6.3 без Missing Script errors в Console
result: [pending]

### 2. EditMode тесты (12 штук)
expected: Все 12 тестов (UrpSetupTests, UrpMaterialTests, UrpPostProcessingTests) зелёные в Test Runner
result: [pending]

### 3. Рендеринг спрайтов
expected: Белые контуры корабля, астероидов, пуль, НЛО на чёрном фоне — без розовых материалов
result: [pending]

### 4. Лазер LineRenderer
expected: Белый луч лазера при нажатии Q — без розовых материалов
result: [pending]

### 5. Эффект взрыва частиц
expected: Эффект взрыва отображается при уничтожении объектов и корректно исчезает
result: [pending]

### 6. UI элементы
expected: HUD (координаты, скорость, заряды), Score, Leaderboard — текст читаем, без артефактов
result: [pending]

### 7. Post-Processing
expected: Bloom glow видно на ярких контурах + Vignette затемнение по краям экрана
result: [pending]

### 8. Геймплей 1:1
expected: Полный прогон: управление кораблём, стрельба, астероиды дробятся, НЛО стреляют, лидерборд работает
result: [pending]

## Summary

total: 8
passed: 0
issues: 0
pending: 8
skipped: 0
blocked: 0

## Gaps
