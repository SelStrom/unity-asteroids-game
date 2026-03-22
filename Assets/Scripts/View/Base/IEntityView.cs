using UnityEngine;

namespace SelStrom.Asteroids
{
    public interface IEntityView
    {
        void Dispose();
        GameObject gameObject { get; }
    }
}
