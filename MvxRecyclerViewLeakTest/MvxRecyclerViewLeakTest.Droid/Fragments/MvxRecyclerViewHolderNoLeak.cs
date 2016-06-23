using System;
using System.Windows.Input;
using Android.Views;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Binding.Droid.BindingContext;
using MvvmCross.Droid.Support.V7.RecyclerView;

namespace MvxRecyclerViewLeakTest.Droid.Fragments
{
    public class MvxRecyclerViewHolderNoLeak : Android.Support.V7.Widget.RecyclerView.ViewHolder, IMvxRecyclerViewHolder, IMvxBindingContextOwner
    {
        private readonly IMvxBindingContext _bindingContext;

        private object _cachedDataContext;
        private ICommand _click, _longClick;
        private bool _clickOverloaded, _longClickOverloaded;

        public IMvxBindingContext BindingContext
        {
            get { return this._bindingContext; }
            set { throw new NotImplementedException("BindingContext is readonly in the list item"); }
        }

        public object DataContext
        {
            get { return this._bindingContext.DataContext; }
            set
            {
                this._bindingContext.DataContext = value;
                if (value != null)
                    this._cachedDataContext = null;
            }
        }

        public ICommand Click
        {
            get { return this._click; }
            set
            {
                this._click = value;
                if (this._click != null)
                    this.EnsureClickOverloaded();
            }
        }

        private void EnsureClickOverloaded()
        {
            if (this._clickOverloaded)
                return;
            this._clickOverloaded = true;
            this.ItemView.Click += this.OnItemViewOnClick;
        }

        public ICommand LongClick
        {
            get { return this._longClick; }
            set
            {
                this._longClick = value;
                if (this._longClick != null)
                    this.EnsureLongClickOverloaded();
            }
        }

        private void EnsureLongClickOverloaded()
        {
            if (this._longClickOverloaded)
                return;
            this._longClickOverloaded = true;
            this.ItemView.LongClick += this.OnItemViewOnLongClick;
        }

        protected virtual void ExecuteCommandOnItem(ICommand command)
        {
            if (command == null)
                return;

            var item = this.DataContext;
            if (item == null)
                return;

            if (!command.CanExecute(item))
                return;

            command.Execute(item);
        }

        private void OnItemViewOnClick(object sender, EventArgs args)
        {
            this.ExecuteCommandOnItem(this.Click);
        }

        private void OnItemViewOnLongClick(object sender, View.LongClickEventArgs args)
        {
            this.ExecuteCommandOnItem(this.LongClick);
        }

        public MvxRecyclerViewHolderNoLeak(View itemView, IMvxAndroidBindingContext context)
            : base(itemView)
        {
            this._bindingContext = context;
        }

        public void OnAttachedToWindow()
        {
            //Console.WriteLine($"OnAttachedToWindow VH : 0x{RuntimeHelpers.GetHashCode(this):x8}");
            if (this._cachedDataContext != null && this.DataContext == null)
                this.DataContext = this._cachedDataContext;
        }

        public void OnDetachedFromWindow()
        {
            //Console.WriteLine($"OnDetachedFromWindow VH : 0x{RuntimeHelpers.GetHashCode(this):x8}");
            this._cachedDataContext = this.DataContext;
            this.DataContext = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this._bindingContext.ClearAllBindings();
                this._cachedDataContext = null;

                if (ItemView != null)
                {
                    ItemView.Click -= this.OnItemViewOnClick;
                    ItemView.LongClick -= this.OnItemViewOnLongClick;
                }
            }

            base.Dispose(disposing);
        }

        public void OnViewRecycled()
        {
            //Console.WriteLine($"Recycled VH : 0x{RuntimeHelpers.GetHashCode(this):x8}");
            this.DataContext = null;
            this._cachedDataContext = null;
        }
    }
}