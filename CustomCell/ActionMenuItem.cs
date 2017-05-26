using System;
using System.ComponentModel;
using System.Windows.Input;
using Xamarin.Forms;

namespace CustomCell
{
	public class ActionMenuItem : BaseMenuItem, IMenuItemController
    {
        public static readonly BindableProperty TextProperty = BindableProperty.Create("Text", typeof(string), typeof(ActionMenuItem), null);

        public static readonly BindableProperty CommandProperty = BindableProperty.Create("Command", typeof(ICommand), typeof(ActionMenuItem), default(ICommand),
			propertyChanging: (bindable, oldvalue, newvalue) =>
			{
                var actionMenuItem = (ActionMenuItem)bindable;
				var oldcommand = (ICommand)oldvalue;
				if (oldcommand != null)
					oldcommand.CanExecuteChanged -= actionMenuItem.OnCommandCanExecuteChanged;
			}, propertyChanged: (bindable, oldvalue, newvalue) =>
			{
				var actionMenuItem = (ActionMenuItem)bindable;
				var newcommand = (ICommand)newvalue;
				if (newcommand != null)
				{
					actionMenuItem.IsEnabled = newcommand.CanExecute(actionMenuItem.CommandParameter);
					newcommand.CanExecuteChanged += actionMenuItem.OnCommandCanExecuteChanged;
				}
			});
        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create("CommandParameter", typeof(object), typeof(ActionMenuItem), default(object),
			propertyChanged: (bindable, oldvalue, newvalue) =>
			{
				var actionMenuItem = (ActionMenuItem)bindable;
				if (actionMenuItem.Command != null)
				{
					actionMenuItem.IsEnabled = actionMenuItem.Command.CanExecute(newvalue);
				}
			});
		public static readonly BindableProperty IsDestructiveProperty = BindableProperty.Create("IsDestructive", typeof(bool), typeof(ActionMenuItem), false);

		public static readonly BindableProperty IconProperty = BindableProperty.Create("Icon", typeof(FileImageSource), typeof(ActionMenuItem), default(FileImageSource));

		public static readonly BindableProperty IsEnabledProperty = BindableProperty.Create("IsEnabled", typeof(bool), typeof(ActionMenuItem), true);

        public static readonly BindableProperty BackgroundColorProperty = BindableProperty.Create("BackgroundColor", typeof(Color), typeof(ActionMenuItem), Color.Default);

		public string IsEnabledPropertyName
		{
			get
			{
				return IsEnabledProperty.PropertyName;
			}
		}

		public ICommand Command
		{
			get { return (ICommand)GetValue(CommandProperty); }
			set { SetValue(CommandProperty, value); }
		}

		public object CommandParameter
		{
			get { return GetValue(CommandParameterProperty); }
			set { SetValue(CommandParameterProperty, value); }
		}

		public FileImageSource Icon
		{
			get { return (FileImageSource)GetValue(IconProperty); }
			set { SetValue(IconProperty, value); }
		}

		public bool IsDestructive
		{
			get { return (bool)GetValue(IsDestructiveProperty); }
			set { SetValue(IsDestructiveProperty, value); }
		}

		public string Text
		{
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}

        [EditorBrowsable(EditorBrowsableState.Never)]
		public bool IsEnabled
		{
			get { return (bool)GetValue(IsEnabledProperty); }
			set { SetValue(IsEnabledProperty, value); }
		}

		public Color BackgroundColor
		{
			get { return (Color)GetValue(BackgroundColorProperty); }
			set { SetValue(BackgroundColorProperty, value); }
		}

		public event EventHandler Clicked;

		protected virtual void OnClicked()
		{
			EventHandler handler = Clicked;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}

        [EditorBrowsable(EditorBrowsableState.Never)]
		public void Activate()
		{
			if (Command != null)
			{
				if (IsEnabled)
					Command.Execute(CommandParameter);
			}

			OnClicked();
		}

		void OnCommandCanExecuteChanged(object sender, EventArgs eventArgs)
		{
			IsEnabled = Command.CanExecute(CommandParameter);
		}
	}
}
