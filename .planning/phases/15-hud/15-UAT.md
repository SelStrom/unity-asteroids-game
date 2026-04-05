---
status: complete
phase: 15-hud
source: [15-01-SUMMARY.md, 15-02-SUMMARY.md]
started: 2026-04-06T12:00:00Z
updated: 2026-04-06T12:30:00Z
---

## Current Test

[testing complete]

## Tests

### 1. HUD отображает количество ракет
expected: Запустить PlayMode. В левом верхнем углу HUD видна строка "Rockets: 3" (или текущее значение MaxAmmo). Текст расположен ниже строки "Reload laser".
result: pass

### 2. HUD отображает таймер перезарядки
expected: При неполном боезапасе ракет (после запуска ракеты кнопкой R) появляется строка "Reload rocket: N sec" с обратным отсчётом. Таймер обновляется каждый кадр.
result: pass

### 3. Счётчик уменьшается при запуске ракеты
expected: Нажать R для запуска ракеты. Число в "Rockets: N" уменьшается на 1 сразу после запуска.
result: pass

### 4. Таймер скрывается при полном боезапасе
expected: Когда все ракеты перезарядились (боезапас = MaxAmmo), строка "Reload rocket: N sec" исчезает. Видна только строка "Rockets: N".
result: pass

### 5. Счётчик увеличивается при перезарядке
expected: После запуска ракеты дождаться окончания перезарядки. Число в "Rockets: N" увеличивается на 1 когда таймер достигает нуля.
result: pass

## Summary

total: 5
passed: 5
issues: 0
pending: 0
skipped: 0
blocked: 0

## Gaps

[none]
