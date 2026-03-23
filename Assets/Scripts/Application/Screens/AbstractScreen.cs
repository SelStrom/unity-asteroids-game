using Shtl.Mvvm;

namespace SelStrom.Asteroids {
    public abstract class AbstractScreen 
    {
        private readonly EventBindingContext _context = new();
        protected EventBindingContext Bind => _context;

        protected void CleanUp()
        {
            _context.CleanUp();
        }
    }
}