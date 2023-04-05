using UnityEngine;

namespace SelStrom.Asteroids
{
    public abstract class BaseView : MonoBehaviour
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

    public class BaseView<TData> : BaseView
    {
        public TData Data { get; private set; }

        public void Connect(TData data)
        {
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