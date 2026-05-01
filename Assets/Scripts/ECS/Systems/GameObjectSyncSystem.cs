using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SelStrom.Asteroids.ECS
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class GameObjectSyncSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // Entities с MoveData + RotateData + GameObjectRef (корабль, UFO)
            foreach (var (move, rotate, goRef) in
                     SystemAPI.Query<RefRO<MoveData>, RefRO<RotateData>, GameObjectRef>())
            {
                var pos = move.ValueRO.Position;
                goRef.Transform.position = new Vector3(pos.x, pos.y, goRef.Transform.position.z);

                var rot = rotate.ValueRO.Rotation;
                var angle = math.atan2(rot.y, rot.x) * Mathf.Rad2Deg;
                goRef.Transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            // Ракеты — поворот по MoveData.Direction
            foreach (var (move, goRef) in
                     SystemAPI.Query<RefRO<MoveData>, GameObjectRef>()
                         .WithAll<RocketTag>()
                         .WithNone<RotateData>())
            {
                var pos = move.ValueRO.Position;
                goRef.Transform.position = new Vector3(pos.x, pos.y, goRef.Transform.position.z);

                var dir = move.ValueRO.Direction;
                if (math.lengthsq(dir) > 1e-8f)
                {
                    var angle = math.atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    goRef.Transform.rotation = Quaternion.Euler(0f, 0f, angle);
                }
            }

            // Entities с MoveData + GameObjectRef, без RotateData и без RocketTag (астероиды, пули)
            foreach (var (move, goRef) in
                     SystemAPI.Query<RefRO<MoveData>, GameObjectRef>()
                         .WithNone<RotateData, RocketTag>())
            {
                var pos = move.ValueRO.Position;
                goRef.Transform.position = new Vector3(pos.x, pos.y, goRef.Transform.position.z);
            }
        }
    }
}
