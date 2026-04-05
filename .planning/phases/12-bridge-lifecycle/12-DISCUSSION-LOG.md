# Phase 12: Bridge & Lifecycle - Discussion Log (Assumptions Mode)

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions captured in CONTEXT.md — this log preserves the analysis.

**Date:** 2026-04-05
**Phase:** 12-Bridge & Lifecycle
**Mode:** assumptions (auto)
**Areas analyzed:** Синхронизация позиции и вращения, Создание визуала ракеты, Триггер спавна ракеты, Спрайт и префаб ракеты

## Assumptions Presented

### Синхронизация позиции и вращения
| Assumption | Confidence | Evidence |
|------------|-----------|----------|
| Добавить третью ветку в GameObjectSyncSystem для RocketTag: MoveData.Direction -> Transform.rotation через atan2 | Likely | GameObjectSyncSystem.cs имеет 2 ветки (с RotateData и без), ракета не имеет RotateData |
| Не добавлять RotateData на rocket entity -- конфликт с EcsRotateSystem | Likely | EcsRotateSystem обрабатывает RotateData по TargetDirection от ввода игрока |

### Создание визуала ракеты
| Assumption | Confidence | Evidence |
|------------|-----------|----------|
| Новый метод EntitiesCatalog.CreateRocket() по аналогии с CreateBullet() | Confident | EntitiesCatalog.cs: все entity с визуалом создаются через CreateX() |
| Расширение enum EntityType значением Rocket | Confident | EntityType enum не содержит Rocket |
| GameObjectRef обязателен для DeadEntityCleanupSystem | Confident | DeadEntityCleanupSystem.cs использует GameObjectRef для cleanup |

### Триггер спавна ракеты
| Assumption | Confidence | Evidence |
|------------|-----------|----------|
| Новый RocketShootEvent как DynamicBuffer<IBufferElementData> | Likely | GunShootEvent, LaserShootEvent -- установленный паттерн событий стрельбы |
| Обработка в ShootEventProcessorSystem | Likely | ShootEventProcessorSystem.cs обрабатывает все события стрельбы |

### Спрайт и префаб ракеты
| Assumption | Confidence | Evidence |
|------------|-----------|----------|
| Минимальный RocketVisual (аналог BulletVisual) с Collider2D | Likely | BulletVisual.cs -- минимальный Visual, точный аналог |
| Отдельный префаб с уменьшенным спрайтом корабля | Likely | VIS-01 requirement, ShipData.MainSprite |
| RocketData в GameData | Likely | GameData.cs содержит данные для каждого типа entity |

## Corrections Made

No corrections — all assumptions confirmed (auto mode).

## Auto-Resolved

- Синхронизация: auto-selected "третья ветка в GameObjectSyncSystem" (recommended)
- Триггер спавна: auto-selected "RocketShootEvent в DynamicBuffer" (recommended)
- Спрайт и префаб: auto-selected "минимальный RocketVisual + отдельный префаб" (recommended)
