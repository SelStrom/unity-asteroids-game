using Shtl.Mvvm;
using UnityEngine;

namespace SelStrom.Asteroids.Bindings
{
    public static class BindingToExtensions
    {
        public static void To(this BindFrom<ReactiveValue<Vector2>> from, Transform target) =>
            from.Source.Connect(value =>
            {
                var position = target.position;
                position.x = value.x;
                position.y = value.y;
                target.position = position;
            });
    }
}
