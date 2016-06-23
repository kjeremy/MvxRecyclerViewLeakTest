using System.Collections.ObjectModel;
using System.Windows.Input;
using MvvmCross.Core.ViewModels;
using MvvmCross.Platform.Platform;
using MvxRecyclerViewLeakTest.Models;

namespace MvxRecyclerViewLeakTest.ViewModels
{
    public class RecyclerViewTestViewModel : BaseViewModel
    {
        private ObservableCollection<ListItem> m_testList;

        public ObservableCollection<ListItem> TestList
        {
            get { return m_testList; }
            private set
            {
                m_testList = value;
                RaisePropertyChanged(() => TestList);
            }
        }

        public RecyclerViewTestViewModel()
        {
            TestList = new ObservableCollection<ListItem>();

        }

        public ICommand ClickCommand { get; } = new MvxCommand<ListItem>(item =>
        {
            MvxTrace.Trace($"{item.Name} clicked");
        });

        public virtual ICommand ButtonClick
        {
            get
            {
                return new MvxCommand(() =>
                {
                    TestList.Clear();
                    for (int i = 0; i < 500; i++)
                    {
                        TestList.Add(new ListItem("Test" + i));
                    }
                });
            }
        }
    }
}
