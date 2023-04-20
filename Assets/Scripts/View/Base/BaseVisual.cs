using JetBrains.Annotations;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public abstract class BaseVisual : MonoBehaviour
    {
        protected virtual void OnConnected()
        {
            //empty
        }

        protected virtual void OnDisposed()
        {
            //empty
        }

        public void Dispose()
        {
            OnDisposed();
        }
    }

    public abstract class BaseVisual<TData> : BaseVisual
    {
        [PublicAPI]
        public TData Data { get; private set; }

        public void Connect(in TData data)
        {
            // TODO @a.shatalov: except redundant copy for value types
            Data = data;
            OnConnected();
        }

        protected override void OnDisposed()
        {
            base.OnDisposed();
            Data = default(TData);
        }
    }
}