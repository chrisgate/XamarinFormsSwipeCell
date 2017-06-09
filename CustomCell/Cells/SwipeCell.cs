using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Xamarin.Forms;

namespace CustomCell
{
    public class SwipeCell : ViewCell
    {
        #region Left/Right Action Menu fields
        ObservableCollection<ActionMenuItem> _contextLeftActions;
        ObservableCollection<ActionMenuItem> _contextRightActions;
        #endregion


        #region Property Section
        public IList<ActionMenuItem> LeftContextActions
        {
            get
            {
                if (_contextLeftActions == null)
                {
                    _contextLeftActions = new ObservableCollection<ActionMenuItem>();
                    _contextLeftActions.CollectionChanged += OnLeftContextActionsChanged;
                }

                return _contextLeftActions;
            }
        }

        public bool HasLeftContextActions
        {
            get { return _contextLeftActions?.Count > 0 && IsEnabled; }
        }

        public IList<ActionMenuItem> RightContextActions
        {
            get
            {
                if (_contextRightActions == null)
                {
                    _contextRightActions = new ObservableCollection<ActionMenuItem>();
                    _contextRightActions.CollectionChanged += OnRightContextActionsChanged;
                }

                return _contextRightActions;
            }
        }

        public bool HasRightContextActions
		{
			get { return _contextRightActions?.Count > 0  && IsEnabled; }
		}
        #endregion

        #region Private Method Section
        void OnLeftContextActionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            for (var i = 0; i < _contextLeftActions.Count; i++)
                SetInheritedBindingContext(_contextLeftActions[i], BindingContext);

            OnPropertyChanged("HasLeftContextActions");
        }

        void OnRightContextActionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            for (var i = 0; i < _contextRightActions.Count; i++)
                SetInheritedBindingContext(_contextRightActions[i], BindingContext);

            OnPropertyChanged("HasRightContextActions");
        }
        #endregion

        #region Override Methoid Section
        protected override void OnPropertyChanged(string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            if (propertyName == IsEnabledProperty.PropertyName)
            {
                base.OnPropertyChanged("HasLeftContextActions");
                base.OnPropertyChanged("HasRightContextActions");
            }
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            if (HasLeftContextActions)
            {
				for (var i = 0; i < _contextLeftActions.Count; i++)
					SetInheritedBindingContext(_contextLeftActions[i], BindingContext);
            }

			if (HasRightContextActions)
			{
				for (var i = 0; i < _contextRightActions.Count; i++)
					SetInheritedBindingContext(_contextRightActions[i], BindingContext);
			}
        }
        #endregion
    }
}
