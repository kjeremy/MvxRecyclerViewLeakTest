using System;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Android.Runtime;
using Android.Views;
using MvvmCross.Binding;
using MvvmCross.Binding.Attributes;
using MvvmCross.Binding.Droid.BindingContext;
using MvvmCross.Binding.ExtensionMethods;
using MvvmCross.Droid.Support.V7.RecyclerView;
using MvvmCross.Droid.Support.V7.RecyclerView.ItemTemplates;
using MvvmCross.Platform;
using MvvmCross.Platform.Exceptions;
using MvvmCross.Platform.Platform;
using MvvmCross.Platform.WeakSubscription;

namespace MvxRecyclerViewLeakTest.Droid.Fragments
{
    public class MvxRecyclerAdapterNoLeak
        : Android.Support.V7.Widget.RecyclerView.Adapter, IMvxRecyclerAdapter
    {
        private ICommand _itemClick, _itemLongClick;
        private IEnumerable _itemsSource;
        private IDisposable _subscription;
        private IMvxTemplateSelector _itemTemplateSelector;

        protected IMvxAndroidBindingContext BindingContext { get; }

        public MvxRecyclerAdapterNoLeak() : this(MvxAndroidBindingContextHelpers.Current()) { }
        public MvxRecyclerAdapterNoLeak(IMvxAndroidBindingContext bindingContext)
        {
            this.BindingContext = bindingContext;
        }

        public MvxRecyclerAdapterNoLeak(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer) { }

        public bool ReloadOnAllItemsSourceSets { get; set; }

        public ICommand ItemClick
        {
            get { return this._itemClick; }
            set
            {
                if (ReferenceEquals(this._itemClick, value))
                {
                    return;
                }

                if (this._itemClick != null)
                {
                    MvxTrace.Warning("Changing ItemClick may cause inconsistencies where some items still call the old command.");
                }

                this._itemClick = value;
            }
        }

        public ICommand ItemLongClick
        {
            get { return this._itemLongClick; }
            set
            {
                if (ReferenceEquals(this._itemLongClick, value))
                {
                    return;
                }

                if (this._itemLongClick != null)
                {
                    MvxTrace.Warning("Changing ItemLongClick may cause inconsistencies where some items still call the old command.");
                }

                this._itemLongClick = value;
            }
        }

        [MvxSetToNullAfterBinding]
        public virtual IEnumerable ItemsSource
        {
            get { return this._itemsSource; }
            set { this.SetItemsSource(value); }
        }


        public virtual IMvxTemplateSelector ItemTemplateSelector
        {
            get { return this._itemTemplateSelector; }
            set
            {
                if (ReferenceEquals(this._itemTemplateSelector, value))
                    return;

                this._itemTemplateSelector = value;

                // since the template selector has changed then let's force the list to redisplay by firing NotifyDataSetChanged()
                if (this._itemsSource != null)
                    this.NotifyDataSetChanged();
            }
        }

        public override void OnViewAttachedToWindow(Java.Lang.Object holder)
        {
            base.OnViewAttachedToWindow(holder);

            var viewHolder = (IMvxRecyclerViewHolder)holder;
            var vh = viewHolder as MvxRecyclerViewHolderNoLeak;
            if (vh != null)
            {
                //vh.Click = ItemClick;
                //vh.LongClick = ItemLongClick;
            }

            viewHolder.OnAttachedToWindow();
        }

        public override void OnViewDetachedFromWindow(Java.Lang.Object holder)
        {
            base.OnViewDetachedFromWindow(holder);

            var viewHolder = (IMvxRecyclerViewHolder)holder;
            var vh = viewHolder as MvxRecyclerViewHolderNoLeak;
            if (vh != null)
            {
                //vh.Click = null;
                //vh.LongClick = null;
            }

            viewHolder.OnDetachedFromWindow();
        }

        public override void OnViewRecycled(Java.Lang.Object holder)
        {
            base.OnViewRecycled(holder);

            var vh = holder as MvxRecyclerViewHolderNoLeak;
            vh?.OnViewRecycled();
        }

        public override Android.Support.V7.Widget.RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemBindingContext = new MvxAndroidBindingContext(parent.Context, this.BindingContext.LayoutInflaterHolder);

            var viewHolder =
                new MvxRecyclerViewHolderNoLeak(this.InflateViewForHolder(parent, viewType, itemBindingContext), itemBindingContext)
                {
                    Click = ItemClick,
                    LongClick = ItemLongClick
                };

            //Console.WriteLine($"CREATE VH : 0x{RuntimeHelpers.GetHashCode(viewHolder):x8}");

            return viewHolder;
        }

        public override int GetItemViewType(int position)
        {
            var itemAtPosition = this.GetItem(position);
            return this.ItemTemplateSelector.GetItemViewType(itemAtPosition);
        }

        protected virtual View InflateViewForHolder(ViewGroup parent, int viewType, IMvxAndroidBindingContext bindingContext)
        {
            var layoutId = this.ItemTemplateSelector.GetItemLayoutId(viewType);
            return bindingContext.BindingInflate(layoutId, parent, false);
        }

        public override void OnBindViewHolder(Android.Support.V7.Widget.RecyclerView.ViewHolder holder, int position)
        {
            ((IMvxRecyclerViewHolder)holder).DataContext = this.GetItem(position);
        }

        public override int ItemCount => this._itemsSource.Count();

        public virtual object GetItem(int position)
        {
            return this._itemsSource.ElementAt(position);
        }

        public int ItemTemplateId { get; set; }

        protected virtual void SetItemsSource(IEnumerable value)
        {
            if (ReferenceEquals(this._itemsSource, value) && !this.ReloadOnAllItemsSourceSets)
            {
                return;
            }

            if (this._subscription != null)
            {
                this._subscription.Dispose();
                this._subscription = null;
            }

            this._itemsSource = value;

            if (this._itemsSource != null && !(this._itemsSource is IList))
            {
                MvxBindingTrace.Trace(MvxTraceLevel.Warning,
                    "Binding to IEnumerable rather than IList - this can be inefficient, especially for large lists");
            }

            var newObservable = this._itemsSource as INotifyCollectionChanged;
            if (newObservable != null)
            {
                this._subscription = newObservable.WeakSubscribe(OnItemsSourceCollectionChanged);
            }

            this.NotifyDataSetChanged();
        }

        protected virtual void OnItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.NotifyDataSetChanged(e);
        }

        public virtual void NotifyDataSetChanged(NotifyCollectionChangedEventArgs e)
        {
            try
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        this.NotifyItemRangeInserted(e.NewStartingIndex, e.NewItems.Count);
                        break;
                    case NotifyCollectionChangedAction.Move:
                        for (int i = 0; i < e.NewItems.Count; i++)
                        {
                            var oldItem = e.OldItems[i];
                            var newItem = e.NewItems[i];

                            this.NotifyItemMoved(this.ItemsSource.GetPosition(oldItem), this.ItemsSource.GetPosition(newItem));
                        }
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        this.NotifyItemRangeChanged(e.NewStartingIndex, e.NewItems.Count);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        this.NotifyItemRangeRemoved(e.OldStartingIndex, e.OldItems.Count);
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        this.NotifyDataSetChanged();
                        break;
                }
            }
            catch (Exception exception)
            {
                Mvx.Warning(
                    "Exception masked during Adapter RealNotifyDataSetChanged {0}. Are you trying to update your collection from a background task? See http://goo.gl/0nW0L6",
                    exception.ToLongString());
            }
        }
    }
}