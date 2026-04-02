# Phase 5: Bridge Layer + Integration - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-03
**Phase:** 05-bridge-layer-integration
**Areas discussed:** GameObject↔Entity binding, Sync direction, Collision bridge mechanism, Observable bridge pattern, Lifecycle orchestration
**Mode:** auto (all areas auto-selected, recommended defaults chosen)

---

## GameObject↔Entity Binding

| Option | Description | Selected |
|--------|-------------|----------|
| ICleanupComponentData `GameObjectRef` | Managed component storing Transform reference, standard Unity DOTS hybrid pattern | ✓ |
| Dictionary-only lookup | No ECS component, external dictionary maps Entity↔GameObject | |
| Companion GameObject pattern | Unity's built-in companion pattern via authoring | |

**User's choice:** [auto] ICleanupComponentData `GameObjectRef` — standard hybrid pattern, allows SystemAPI queries
**Notes:** Reverse mapping Dictionary<GameObject, Entity> also maintained for O(1) collision lookup

---

## ECS→Transform Sync

| Option | Description | Selected |
|--------|-------------|----------|
| Every frame, all entities | Simple sync each frame for all entities with GameObjectRef | ✓ |
| Change filter sync | Only sync entities whose MoveData/RotateData changed | |
| Event-driven sync | Sync only on explicit dirty flag | |

**User's choice:** [auto] Every frame sync — entity count low (~20-50), simplicity over optimization
**Notes:** No change filter overhead needed at this scale

---

## Collision Bridge Mechanism

| Option | Description | Selected |
|--------|-------------|----------|
| Existing OnCollisionEnter2D → Bridge | Visuals keep OnCollisionEnter2D, resolve Entity via reverse map, write to DynamicBuffer | ✓ |
| Physics2D.ContactEvent (new API) | Use Unity 6.3 Physics2D.ContactEvent for batch collision processing | |
| Custom overlap checks in ECS | Replace Physics2D with manual overlap in ECS system | |

**User's choice:** [auto] Existing OnCollisionEnter2D → Bridge — minimal refactoring, reuses established pattern
**Notes:** CollisionEventData DynamicBuffer already implemented in Phase 4

---

## Observable Bridge Pattern

| Option | Description | Selected |
|--------|-------------|----------|
| Dedicated ObservableBridgeSystem | Single managed ISystem reads ECS data, pushes to ReactiveValue each frame | ✓ |
| Per-entity bridge components | Each entity stores managed ref to its ViewModel | |
| Event-based bridge | ECS fires events, managed listener updates MVVM | |

**User's choice:** [auto] Dedicated ObservableBridgeSystem — clean separation, single responsibility
**Notes:** Replaces current EventBindingContext source from Model-components to ECS data

---

## Lifecycle Orchestration

| Option | Description | Selected |
|--------|-------------|----------|
| EntitiesCatalog remains orchestrator | Creates both Entity and GameObject, maintains bidirectional mapping | ✓ |
| ECS-first creation | Entity created first, GameObject spawned by sync system | |
| Split ownership | ECS owns entities, EntitiesCatalog owns only GameObjects | |

**User's choice:** [auto] EntitiesCatalog remains orchestrator — minimal refactoring of Game.cs
**Notes:** DeadTag triggers cleanup through bridge, EntitiesCatalog.Release handles both sides

---

## Claude's Discretion

- Reverse mapping implementation details (static dictionary, singleton, or part of EntitiesCatalog)
- ECS World initialization order relative to ApplicationEntry.Awake
- Migration flag mechanism (bool in Game.cs, ScriptableObject, or define)
- PlayMode test scenarios for TST-12

## Deferred Ideas

- Full DOTS Physics migration — package not production-ready
- Entities Graphics for rendering — no SpriteRenderer/WebGL support
- Removal of old Model layer — disabled by flag, full removal in future milestone
- Change filter optimization — unnecessary at current entity scale
