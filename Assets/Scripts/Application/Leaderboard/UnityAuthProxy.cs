using System.Collections;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class UnityAuthProxy : IAuthProxy
    {
        public bool IsSignedIn => AuthenticationService.Instance.IsSignedIn;
        public string PlayerId => AuthenticationService.Instance.PlayerId;

        public IEnumerator Initialize(CoroutineResult result)
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                var task = UnityServices.InitializeAsync();
                yield return new WaitUntil(() => task.IsCompleted);
                if (task.IsFaulted)
                {
                    result.Error = task.Exception;
                }
            }
        }

        public IEnumerator SignInAnonymously(CoroutineResult result)
        {
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                var task = AuthenticationService.Instance.SignInAnonymouslyAsync();
                yield return new WaitUntil(() => task.IsCompleted);
                if (task.IsFaulted)
                {
                    result.Error = task.Exception;
                }
            }
        }
    }
}
