using Android.OS;
using Android.Runtime;
using Android.Views;
using MvvmCross.Binding.Droid.BindingContext;
using MvvmCross.Droid.Shared.Attributes;
using MvvmCross.Droid.Support.V7.RecyclerView;
using MvxRecyclerViewLeakTest.ViewModels;

namespace MvxRecyclerViewLeakTest.Droid.Fragments
{
    [MvxFragment(typeof(MainViewModel), Resource.Id.content_frame)]
    [Register("mvxrecyclerviewleaktest.droid.fragments.RecyclerViewTestFragment")]
    public class RecyclerViewTestFragment : BaseFragment<RecyclerViewTestViewModel>
    {
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);

            var recyclerView = view.FindViewById<MvxRecyclerView>(Resource.Id.lvTestList);
            var oldAdapter = recyclerView.Adapter;
            recyclerView.Adapter = new MvxRecyclerAdapterNoLeak(this.BindingContext as IMvxAndroidBindingContext);

            // Just in case
            oldAdapter.ItemsSource = null;
            oldAdapter.ItemClick = null;
            oldAdapter.ItemLongClick = null;
            oldAdapter.ItemTemplateSelector = null;

            return view;
        }

        protected override int FragmentId
        {
            get
            {
                return Resource.Layout.fragment_recyclerviewtest;
            }
        }
    }
}