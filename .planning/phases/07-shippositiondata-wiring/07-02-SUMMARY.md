---
phase: 07-shippositiondata-wiring
plan: 02
status: complete
started: 2026-04-04T12:00:00Z
completed: 2026-04-04T12:30:00Z
---

## Summary

Ручная верификация геймплея 1:1 после добавления ShipPositionData singleton.

## Result

**UAT: APPROVED с замечанием**

Пользователь подтвердил:
- UFO стреляют в сторону корабля с упреждением (EcsShootToSystem работает)
- Малые UFO преследуют корабль (EcsMoveToSystem работает)
- Корабль двигается, стреляет, лазер работает
- Астероиды дробятся при попадании
- HUD очков отображается
- Game Over и Restart работают

### Обнаруженный issue (вне скоупа Phase 7)

**UFO не коллайдятся с астероидами.** В `EcsCollisionHandlerSystem.ProcessCollision` обрабатываются только пары: PlayerBullet+Enemy, EnemyBullet+Ship, Ship+Enemy. Пара UFO+Asteroid не обрабатывается. В классическом Asteroids UFO и астероиды уничтожают друг друга при столкновении.

Зафиксировано как отдельный backlog item.

## Self-Check: PASSED

- [x] UFO AI работает (стрельба с упреждением, преследование)
- [x] Геймплей 1:1 подтверждён пользователем
- [x] LC-07 verified (с замечанием о UFO-asteroid collision — отдельный scope)

## key-files

### created
(none — checkpoint plan, no code changes)

### modified
(none)
