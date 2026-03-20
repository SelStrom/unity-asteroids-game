using Shtl.Mvvm;
using UnityEngine;

namespace Model.Components
{
    public class RotateComponent : IModelComponent
    {
        public const int DegreePerSecond = 90;

        public float TargetDirection { get; set; }
        public readonly ObservableValue<Vector2> Rotation = new(Vector2.right);
    }
}