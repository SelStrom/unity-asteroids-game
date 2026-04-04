# Phase 4: ECS Foundation - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-02
**Phase:** 04-ecs-foundation
**Areas discussed:** System API Pattern, Component Granularity, Managed Data Access, File Organization, Collision Strategy
**Mode:** --auto (all decisions auto-selected)

---

## System API Pattern

| Option | Description | Selected |
|--------|-------------|----------|
| ISystem (unmanaged) | Modern Unity DOTS API, Burst-compatible, recommended for new projects | ✓ |
| SystemBase (managed) | Simpler API, but legacy, no Burst support | |

**User's choice:** [auto] ISystem (unmanaged)
**Notes:** Requirements ECS-04/05/06 explicitly require Burst compilation for Move, Rotate, Thrust systems. ISystem is the only option that supports this.

---

## Component Granularity

| Option | Description | Selected |
|--------|-------------|----------|
| 1:1 mapping | Each existing component maps to one IComponentData | ✓ |
| Restructured | Merge/split components for better ECS data layout | |

**User's choice:** [auto] 1:1 mapping
**Notes:** Existing components are clean and well-separated. 1:1 mapping minimizes risk and keeps predictable structure for Phase 5 bridge.

---

## Managed Data Access (AI Systems)

| Option | Description | Selected |
|--------|-------------|----------|
| Singleton component | ShipPosition as singleton, accessed via SystemAPI.GetSingleton | ✓ |
| Shared component | ShipPosition as ISharedComponentData on all entities needing it | |
| Entity lookup | Store ship Entity reference, query position each frame | |

**User's choice:** [auto] Singleton component
**Notes:** Ship is unique — singleton is idiomatic DOTS for single-instance data. Simplest and most performant option.

---

## File Organization

| Option | Description | Selected |
|--------|-------------|----------|
| Separate folder (Assets/Scripts/ECS/) | New directory with own asmdef, clear separation | ✓ |
| Alongside existing (Assets/Scripts/Model/) | Co-locate with existing code | |

**User's choice:** [auto] Separate folder
**Notes:** Keeps existing code untouched, clear boundary for Phase 5 integration, own asmdef for Entities dependency isolation.

---

## Collision Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| ISystem with managed bridge | CollisionHandler as ISystem, receives Physics2D data via bridge (Phase 5) | ✓ |
| Pure ECS triggers | Use DOTS Physics trigger events | |

**User's choice:** [auto] ISystem with managed bridge
**Notes:** DOTS Physics 2D does not exist in production-ready form (per REQUIREMENTS Out of Scope). Physics2D stays on GameObjects, CollisionHandler in ECS receives data through managed bridge defined in Phase 5.

---

## Claude's Discretion

- IComponentData field types (float2 vs float, Unity.Mathematics usage)
- System ordering mechanism (UpdateBefore/UpdateAfter vs SystemGroup)
- EntityFactory API design
- Test infrastructure (World creation, helper methods)

## Deferred Ideas

- Bug fixes (wrapping formula, UFO kill, division by zero) — separate milestone
- Hardcode refactoring (bullet speed 20, MoveToComponent.Every = 3f) — not part of 1:1 migration
