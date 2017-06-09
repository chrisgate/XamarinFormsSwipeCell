using System;
using Xamarin.Forms;

namespace CustomCell
{
    public class SwipeListView : ListView
    {
        public SwipeListView(): this(ListViewCachingStrategy.RetainElement)
        {
            
        }
        public SwipeListView(ListViewCachingStrategy strategy) : base(strategy)
        {
        }
    }
}
