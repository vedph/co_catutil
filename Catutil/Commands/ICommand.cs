using System.Threading.Tasks;

namespace Catutil.Commands
{
    public interface ICommand
    {
        Task Run();
    }
}
