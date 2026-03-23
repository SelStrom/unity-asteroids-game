using System.Collections;

namespace SelStrom.Asteroids
{
    public interface IAuthProxy
    {
        bool IsSignedIn { get; }
        string PlayerId { get; }
        IEnumerator Initialize(CoroutineResult result);
        IEnumerator SignInAnonymously(CoroutineResult result);
    }
}
