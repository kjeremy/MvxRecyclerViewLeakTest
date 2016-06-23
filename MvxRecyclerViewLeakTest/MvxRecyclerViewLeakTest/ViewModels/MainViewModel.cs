using MvvmCross.Core.ViewModels;

namespace MvxRecyclerViewLeakTest.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public MainViewModel()
        {
            PerformGC = new MvxCommand(() =>
            {
                System.GC.Collect(System.GC.MaxGeneration);
                System.GC.WaitForPendingFinalizers();
            });
        }

        public IMvxCommand ShowInitScreen
        {
            get
            {
                return new MvxCommand(() =>
                {
                    ShowViewModel<RecyclerViewTestViewModel>();
                });
            }
        }

        public IMvxCommand PerformGC { get; }
    }
}
