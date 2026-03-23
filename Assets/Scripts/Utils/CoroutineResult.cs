using System;

namespace SelStrom.Asteroids
{
    public class CoroutineResult
    {
        public Exception Error { get; set; }
        public bool IsSuccess => Error == null;
    }

    public class CoroutineResult<T> : CoroutineResult
    {
        public T Value { get; set; }
    }
}
