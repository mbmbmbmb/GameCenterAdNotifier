using System.Threading.Tasks;
using System.Windows.Forms;

namespace GameCenterAdNotifier.Common
{
    public interface IAdModule
    {
        string Title { get; }

        Task Initialize();

        Task AdStarted(Screen screen);

        Task AdEnded();
    }
}