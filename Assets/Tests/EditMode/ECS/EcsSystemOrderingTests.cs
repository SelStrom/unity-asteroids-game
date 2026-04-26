using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    // Регресс: цикл UpdateAfter/UpdateBefore приводил к
    // IndexOutOfRangeException в Unity.Entities.ComponentSystemSorter
    // при инициализации World, в результате чего SimulationSystemGroup
    // не сортировался и ни одна система не выполнялась
    // (объекты замирали в центре карты).
    public class EcsSystemOrderingTests
    {
        // Все системы из SimulationSystemGroup проекта.
        private static readonly Type[] SimulationSystems =
        {
            typeof(EcsRotateSystem),
            typeof(EcsThrustSystem),
            typeof(EcsMoveSystem),
            typeof(EcsShipPositionUpdateSystem),
            typeof(EcsLifeTimeSystem),
            typeof(EcsDeadByLifeTimeSystem),
            typeof(EcsGunSystem),
            typeof(EcsLaserSystem),
            typeof(EcsMissileSystem),
            typeof(EcsHomingSystem),
            typeof(EcsShootToSystem),
            typeof(EcsMoveToSystem),
            typeof(EcsCollisionHandlerSystem),
        };

        [Test]
        public void SimulationSystems_HaveNoCircularUpdateOrder()
        {
            // Граф направленных рёбер: A → B означает «A выполняется до B».
            var graph = BuildOrderingGraph(SimulationSystems);

            var path = new List<Type>();
            foreach (var sys in SimulationSystems)
            {
                var visiting = new HashSet<Type>();
                var visited = new HashSet<Type>();
                if (HasCycle(sys, graph, visiting, visited, path))
                {
                    Assert.Fail("Цикл UpdateAfter/UpdateBefore: "
                        + string.Join(" → ", path.ConvertAll(t => t.Name)));
                }
            }
        }

        [Test]
        public void EcsHomingSystem_DoesNotForceItselfBeforeMoveSystem()
        {
            // Strong invariant: явный регресс на конкретное правило,
            // которое замыкало цикл Homing < Move < ShipPos < Gun < Laser < Missile < Homing.
            var attrs = typeof(EcsHomingSystem).GetCustomAttributes(
                typeof(UpdateBeforeAttribute), inherit: false);
            foreach (var raw in attrs)
            {
                var attr = (UpdateBeforeAttribute)raw;
                Assert.AreNotEqual(typeof(EcsMoveSystem), attr.SystemType,
                    "EcsHomingSystem не должна иметь UpdateBefore(EcsMoveSystem) — "
                    + "это создаёт цикл через Move→ShipPos→Gun→Laser→Missile→Homing.");
            }
        }

        private static Dictionary<Type, HashSet<Type>> BuildOrderingGraph(Type[] systems)
        {
            var systemSet = new HashSet<Type>(systems);
            var graph = new Dictionary<Type, HashSet<Type>>();
            foreach (var t in systems)
            {
                graph[t] = new HashSet<Type>();
            }

            foreach (var t in systems)
            {
                // [UpdateAfter(X)] на T → X должна выполниться до T → ребро X → T.
                foreach (var raw in t.GetCustomAttributes(typeof(UpdateAfterAttribute), false))
                {
                    var dep = ((UpdateAfterAttribute)raw).SystemType;
                    if (systemSet.Contains(dep))
                    {
                        graph[dep].Add(t);
                    }
                }

                // [UpdateBefore(Y)] на T → T выполнится до Y → ребро T → Y.
                foreach (var raw in t.GetCustomAttributes(typeof(UpdateBeforeAttribute), false))
                {
                    var dep = ((UpdateBeforeAttribute)raw).SystemType;
                    if (systemSet.Contains(dep))
                    {
                        graph[t].Add(dep);
                    }
                }
            }

            return graph;
        }

        private static bool HasCycle(
            Type node,
            Dictionary<Type, HashSet<Type>> graph,
            HashSet<Type> visiting,
            HashSet<Type> visited,
            List<Type> path)
        {
            if (visited.Contains(node))
            {
                return false;
            }

            if (!visiting.Add(node))
            {
                path.Add(node);
                return true;
            }

            path.Add(node);
            foreach (var next in graph[node])
            {
                if (HasCycle(next, graph, visiting, visited, path))
                {
                    return true;
                }
            }

            path.RemoveAt(path.Count - 1);
            visiting.Remove(node);
            visited.Add(node);
            return false;
        }
    }
}
