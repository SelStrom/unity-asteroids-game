using System.Threading.Tasks;

namespace SelStrom.Asteroids
{
    public interface IAuthProxy
    {
        bool IsSignedIn { get; }
        string PlayerId { get; }
        Task InitializeAsync();
        Task SignInAnonymouslyAsync();
    }
}
