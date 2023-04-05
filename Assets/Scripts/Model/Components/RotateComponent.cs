using SelStrom.Asteroids;
using UnityEngine;

namespace Model.Components
{
    public class RotateComponent : IModelComponent
    {
        public const int DegreePerSecond = 90;

        public float TargetDirection { get; set; }
        public readonly ObservableField<Vector2> Rotation = new(Vector2.right);
    }
}