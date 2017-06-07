﻿/*
    소스 참조 : https://github.com/xamarin/Xamarin.Forms/blob/master/Xamarin.Forms.Platform.iOS/ContextActionCell.cs
*/
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Foundation;
using CustomCell;
using CustomCell.iOS.Resources;
using Xamarin.Forms;
using PointF = CoreGraphics.CGPoint;
using RectangleF = CoreGraphics.CGRect;
using SizeF = CoreGraphics.CGSize;
using CGPoint = CoreGraphics.CGPoint;
using UIKit;
using System.ComponentModel;
using Xamarin.Forms.Platform.iOS;

namespace CustomCell.iOS
{
    public class SwipeiOSCell : UITableViewCell, INativeElementView
    {
        public const string Key = "SwipeiOSCell";
		
        static readonly UIImage DestructiveBackground;
        static readonly UIImage NormalBackground;
        readonly List<UIButton> _leftButtons = new List<UIButton>();
		readonly List<ActionMenuItem> _leftMenuItems = new List<ActionMenuItem>();
		readonly List<UIButton> _rightButtons = new List<UIButton>();
		readonly List<ActionMenuItem> _rightMenuItems = new List<ActionMenuItem>();

        public SwipeCell SwipeCell { get; private set; }

		SwipeCell _cell;
		UIButton _rightMoreButton;
        UIButton _leftMoreButton;
		UIScrollView _scroller;
		UITableView _tableView;

		static SwipeiOSCell()
		{
            var rect = new RectangleF(0, 0, 1, 1);
			var size = rect.Size;

			UIGraphics.BeginImageContext(size);
			var context = UIGraphics.GetCurrentContext();
			context.SetFillColor(1, 0, 0, 1);
			context.FillRect(rect);
			DestructiveBackground = UIGraphics.GetImageFromCurrentImageContext();

			context.SetFillColor(UIColor.LightGray.ToColor().ToCGColor());
			context.FillRect(rect);

			NormalBackground = UIGraphics.GetImageFromCurrentImageContext();

			context.Dispose();
		}

        public SwipeiOSCell(SwipeCell swipeCell) : base(UITableViewCellStyle.Default, Key)
        {
            this.SwipeCell = swipeCell;
		}

        public SwipeiOSCell(string templateId, SwipeCell swipeCell) : base(UITableViewCellStyle.Default, Key + templateId)
        {
            this.SwipeCell = swipeCell;
        }

		Element INativeElementView.Element
		{
			get
			{
				var boxedCell = ContentCell as INativeElementView;
				if (boxedCell == null)
				{
					throw new InvalidOperationException($"Implement {nameof(INativeElementView)} on cell renderer: {ContentCell.GetType().AssemblyQualifiedName}");
				}

				return boxedCell.Element;
			}
		}

		public void Close()
		{
			_scroller.ContentOffset = new PointF(0, 0);
		}

		public override void LayoutSubviews()
		{
			base.LayoutSubviews();

			// Leave room for 1px of play because the border is 1 or .5px and must be accounted for.
			if (_scroller == null || (_scroller.Frame.Width == ContentView.Bounds.Width && Math.Abs(_scroller.Frame.Height - ContentView.Bounds.Height) < 1))
				return;

            Update(_tableView, _cell, ContentCell);

			if (ContentCell.Subviews.Length > 0 && Math.Abs(ContentCell.Subviews[0].Frame.Height - Bounds.Height) > 1)
			{
				// Something goes weird inside iOS where LayoutSubviews wont get called when updating the bounds if the user
				// forces us to flip flop between a ContextActionCell and a normal cell in the middle of actually displaying the cell
				// so here we are going to hack it a forced update. Leave room for 1px of play because the border is 1 or .5px and must
				// be accounted for.
				//
				// Fixes https://bugzilla.xamarin.com/show_bug.cgi?id=39450
				ContentCell.LayoutSubviews();
			}
		}

        public UITableViewCell ContentCell { get; private set; }

		ContextScrollViewDelegate ScrollDelegate
		{
			get { return (ContextScrollViewDelegate)_scroller.Delegate; }
		}

		// Either the LeftButton or RightButton is 'open'
		public bool IsOpen
		{
			get
			{
				return IsLeftOpen || IsRightOpen;
			}
		}


		// Use the ContentOffset of the ScrollView to determine whether or not the LeftButton is 'open'
		public bool IsLeftOpen
		{
			get
			{
				return _scroller.ContentOffset != CGPoint.Empty && _scroller.ContentOffset.X < 0f;
			}
		}


		// Use the ContentOffset of the ScrollView to determine whether or not the RightButton is 'open'
		public bool IsRightOpen
		{
			get
			{
				return _scroller.ContentOffset != CGPoint.Empty && _scroller.ContentOffset.X > 0f;
			}
		}

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();
		}

		public override SizeF SizeThatFits(SizeF size)
		{
			return ContentCell.SizeThatFits(size);
		}

        public void Update(UITableView tableView, SwipeCell cell, UITableViewCell nativeCell)
        {
            var recycling = tableView.DequeueReusableCell(ReuseIdentifier) != null ? true : false;

            if(_cell != cell && recycling)    
			{
                if (_cell != null)
                {
                    ((INotifyCollectionChanged)_cell.LeftContextActions).CollectionChanged -= OnContextItemsChanged;
                    ((INotifyCollectionChanged)_cell.RightContextActions).CollectionChanged -= OnContextItemsChanged;
                }

				((INotifyCollectionChanged)cell.LeftContextActions).CollectionChanged += OnContextItemsChanged;
                ((INotifyCollectionChanged)cell.RightContextActions).CollectionChanged += OnContextItemsChanged;
			}

			var height = Frame.Height;
			var width = ContentView.Frame.Width;

			nativeCell.Frame = new RectangleF(0, 0, width, height);
			nativeCell.SetNeedsLayout();


			var handler = new PropertyChangedEventHandler(OnMenuItemPropertyChanged);

			_tableView = tableView;
			SetupSelection(tableView);

			if (_cell != null)
			{
				if (!recycling)
					_cell.PropertyChanged -= OnCellPropertyChanged;
                
                if (_leftMenuItems.Count > 0)
				{
					if (!recycling)
                        ((INotifyCollectionChanged)_cell.LeftContextActions).CollectionChanged -= OnContextItemsChanged;

					foreach (var item in _leftMenuItems)
						item.PropertyChanged -= handler;
				}

                _leftMenuItems.Clear();

				if (_rightMenuItems.Count > 0)
				{
					if (!recycling)
                        ((INotifyCollectionChanged)_cell.RightContextActions).CollectionChanged -= OnContextItemsChanged;

					foreach (var item in _rightMenuItems)
						item.PropertyChanged -= handler;
				}

				_rightMenuItems.Clear();
			}

            _leftMenuItems.AddRange(cell.LeftContextActions);
            _rightMenuItems.AddRange(cell.RightContextActions);

			_cell = cell;

			if (!recycling)
			{
				cell.PropertyChanged += OnCellPropertyChanged;
				((INotifyCollectionChanged)_cell.LeftContextActions).CollectionChanged += OnContextItemsChanged;
                ((INotifyCollectionChanged)_cell.RightContextActions).CollectionChanged += OnContextItemsChanged;
			}

			if (_scroller == null)
			{
				_scroller = new UIScrollView(new RectangleF(0, 0, width, height));
				_scroller.ScrollsToTop = false;
				_scroller.ShowsHorizontalScrollIndicator = false;
				_scroller.PreservesSuperviewLayoutMargins = true;

				ContentView.AddSubview(_scroller);
			}
			else
			{
				_scroller.Frame = new RectangleF(0, 0, width, height);

				for (var i = 0; i < _rightButtons.Count; i++)
				{
					var b = _rightButtons[i];
					b.RemoveFromSuperview();
					b.Dispose();
				}

				_rightButtons.Clear();

                for (var i = 0; i < _leftButtons.Count; i++)
				{
					var b = _leftButtons[i];
					b.RemoveFromSuperview();
					b.Dispose();
				}

				_leftButtons.Clear();

				ScrollDelegate.Unhook(_scroller);
				ScrollDelegate.Dispose();
			}

			if (ContentCell != nativeCell)
			{
				if (ContentCell != null)
				{
					ContentCell.RemoveFromSuperview();
					ContentCell = null;
				}

				ContentCell = nativeCell;

				_scroller.AddSubview(nativeCell);
			}

            SetupButtons(width, height);

			UIView container = null;

			var totalWidth = width;

            nfloat leftTotalWidth = .0f;
			for (var i = _leftButtons.Count - 1; i >= 0; i--)
			{
				var b = _leftButtons[i];
				leftTotalWidth += b.Frame.Width;
				totalWidth += b.Frame.Width;

				if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
					_scroller.AddSubview(b);
				else
				{
					if (container == null)
					{
						container = new iOS7ButtonContainer(b.Frame.Width);
						_scroller.InsertSubview(container, 0);
					}

					container.AddSubview(b);
				}
			}

            nfloat rightTotalWidth = .0f;
			for (var i = _rightButtons.Count - 1; i >= 0; i--)
			{
				var b = _rightButtons[i];
				totalWidth += b.Frame.Width;
                rightTotalWidth += b.Frame.Width;

				if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
					_scroller.AddSubview(b);
				else
				{
					if (container == null)
					{
						container = new iOS7ButtonContainer(b.Frame.Width);
						_scroller.InsertSubview(container, 0);
					}

					container.AddSubview(b);
				}
			}

            _scroller.Delegate = new ContextScrollViewDelegate(container, _rightButtons, _leftButtons, IsRightOpen, IsLeftOpen);
            //slider area reset
            _scroller.ContentInset = new UIEdgeInsets(0.0f, leftTotalWidth, 0.0f, rightTotalWidth);
            _scroller.ContentSize =  new SizeF(totalWidth, height);

            //TODO : if 문 조건이 정확한지 확인해야함.
            if (IsRightOpen && recycling)
                _scroller.SetContentOffset(new PointF(ScrollDelegate.RightButtonsWidth , 0), false);
            else if (IsLeftOpen && recycling)
                _scroller.SetContentOffset(new PointF(-ScrollDelegate.LeftButtonsWidth, 0), false);
			else
                _scroller.SetContentOffset(CGPoint.Empty, false);
        }

		void OnCellPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "HasLeftContextActions" || e.PropertyName == "HasRightContextActions")
			{
				var reusecCell = _tableView.DequeueReusableCell(ReuseIdentifier);
				if (reusecCell != null)
					ReloadRow();
			}
		}

		void OnContextItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			var reusecCell = _tableView.DequeueReusableCell(ReuseIdentifier);
            if (reusecCell != null)
                Update(_tableView, _cell, ContentCell);
			else
				ReloadRow();
			// TODO: Perhaps make this nicer if it's open while adding
		}

		void OnMenuItemPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var reusecCell = _tableView.DequeueReusableCell(ReuseIdentifier);
			if (reusecCell != null)
                Update(_tableView, _cell, ContentCell);
			else
				ReloadRow();
		}

		void ReloadRow()
		{
            if (_scroller.ContentOffset.X > 0 || _scroller.ContentOffset.X < 0)
			{
				((ContextScrollViewDelegate)_scroller.Delegate).ClosedCallback = () =>
				{
					ReloadRowCore();
					((ContextScrollViewDelegate)_scroller.Delegate).ClosedCallback = null;
				};

				_scroller.SetContentOffset(CGPoint.Empty, true);
			}
			else
				ReloadRowCore();
		}

		void ReloadRowCore()
		{
            if (_cell.Parent == null)
				return;

			var path = _tableView.IndexPathForCell(this); //_cell.GetIndexPath();

			var selected = path.Equals(_tableView.IndexPathForSelectedRow);

			_tableView.ReloadRows(new[] { path }, UITableViewRowAnimation.None);

			if (selected)
			{
				_tableView.SelectRow(path, false, UITableViewScrollPosition.None);
				_tableView.Source.RowSelected(_tableView, path);
			}
		}

        #region LeftButton/RightButton Setting
        UIView SetupButtons(nfloat width, nfloat height)
		{
			ActionMenuItem destructive = null;
			nfloat largestWidth = 0, acceptableSize = width * 0.80f;

            #region RightButton Setting 

            if (_cell.RightContextActions.Count > 0)
            {
                for (var i = 0; i < _cell.RightContextActions.Count; i++)
                {
                    var item = _cell.RightContextActions[i];

                    //버튼이 3개 이상인 경우 More 버튼 처리를 위한 로직 
                    if (_rightButtons.Count == 3)
                    {
                        if (destructive != null)
                            break;
                        if (!item.IsDestructive)
                            continue;

                        _rightButtons.RemoveAt(_rightButtons.Count - 1);
                    }

                    if (item.IsDestructive)
                        destructive = item;

                    var button = GetButton(item);

                    button.Tag = i;
                    var buttonWidth = button.TitleLabel.SizeThatFits(new SizeF(width, height)).Width + 30;
                    if (buttonWidth > largestWidth)
                        largestWidth = buttonWidth;

                    if (destructive == item)
                        _rightButtons.Insert(0, button);
                    else
                        _rightButtons.Add(button);
                }

                var needMoreRight = _cell.RightContextActions.Count > _rightButtons.Count;

                if (_cell.RightContextActions.Count > 2)
                    CullRightButtons(acceptableSize, ref needMoreRight, ref largestWidth);

                var resize = false;
                //MORE 버튼
                if (needMoreRight)
                {
                    if (largestWidth * 2 > acceptableSize)
                    {
                        largestWidth = acceptableSize / 2;
                        resize = true;
                    }

                    var button = new UIButton(new RectangleF(0, 0, largestWidth, height));
                    button.SetBackgroundImage(NormalBackground, UIControlState.Normal);
                    button.TitleEdgeInsets = new UIEdgeInsets(0, 15, 0, 15);
                    button.SetTitle(StringResources.More, UIControlState.Normal);

                    var moreWidth = button.TitleLabel.SizeThatFits(new SizeF(width, height)).Width + 30;
                    if (moreWidth > largestWidth)
                    {
                        largestWidth = moreWidth;
                        CullRightButtons(acceptableSize, ref needMoreRight, ref largestWidth);

                        if (largestWidth * 2 > acceptableSize)
                        {
                            largestWidth = acceptableSize / 2;
                            resize = true;
                        }
                    }

                    button.Tag = -1;
                    button.TouchUpInside += OnRightButtonActivated;
                    if (resize)
                        button.TitleLabel.AdjustsFontSizeToFitWidth = true;

                    _rightMoreButton = button;
                    _rightButtons.Add(button);
                }

                var handler = new PropertyChangedEventHandler(OnMenuItemPropertyChanged);
                var totalWidth = _rightButtons.Count * largestWidth;
                for (var n = 0; n < _rightButtons.Count; n++)
                {
                    var b = _rightButtons[n];

                    if (b.Tag >= 0)
                    {
                        var item = _cell.RightContextActions[(int)b.Tag];
                        item.PropertyChanged += handler;
                    }

                    var offset = (n + 1) * largestWidth;

                    var x = width - offset;
                    if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
                        x += totalWidth;

                    b.Frame = new RectangleF(x, 0, largestWidth, height);
                    if (resize)
                        b.TitleLabel.AdjustsFontSizeToFitWidth = true;

                    b.SetNeedsLayout();

                    if (b != _rightMoreButton)
                        b.TouchUpInside += OnRightButtonActivated;
                }
            }
			#endregion  //RightButton Setting End

			#region LeftButton Setting 
			if (_cell.LeftContextActions.Count > 0)
			{
				for (var i = 0; i < _cell.LeftContextActions.Count; i++)
				{
					var item = _cell.LeftContextActions[i];

					//버튼이 3개 이상인 경우 More 버튼 처리를 위한 로직 
					if (_leftButtons.Count == 3)
					{
						if (destructive != null)
							break;
						if (!item.IsDestructive)
							continue;

                        _leftButtons.RemoveAt(_leftButtons.Count - 1);
					}

					if (item.IsDestructive)
						destructive = item;

					var button = GetButton(item);

					button.Tag = i;
					var buttonWidth = button.TitleLabel.SizeThatFits(new SizeF(width, height)).Width + 30;
					if (buttonWidth > largestWidth)
						largestWidth = buttonWidth;

					if (destructive == item)
                        _leftButtons.Insert(0, button);
					else
                        _leftButtons.Add(button);
				}

				var needMoreLeft = _cell.LeftContextActions.Count > _leftButtons.Count;

				if (_cell.LeftContextActions.Count > 2)
                    CullLeftButtons(acceptableSize, ref needMoreLeft, ref largestWidth);

				var resize = false;
                //MORE 버튼
                if (needMoreLeft)
				{
					if (largestWidth * 2 > acceptableSize)
					{
						largestWidth = acceptableSize / 2;
						resize = true;
					}

					var button = new UIButton(new RectangleF(0, 0, largestWidth, height));
					button.SetBackgroundImage(NormalBackground, UIControlState.Normal);
					button.TitleEdgeInsets = new UIEdgeInsets(0, 15, 0, 15);
					button.SetTitle(StringResources.More, UIControlState.Normal);

					var moreWidth = button.TitleLabel.SizeThatFits(new SizeF(width, height)).Width + 30;
					if (moreWidth > largestWidth)
					{
						largestWidth = moreWidth;
                        CullLeftButtons(acceptableSize, ref needMoreLeft, ref largestWidth);

						if (largestWidth * 2 > acceptableSize)
						{
							largestWidth = acceptableSize / 2;
							resize = true;
						}
					}

					button.Tag = -1;
					button.TouchUpInside += OnLeftButtonActivated;
					if (resize)
						button.TitleLabel.AdjustsFontSizeToFitWidth = true;

                    _leftMoreButton = button;
                    _leftButtons.Add(button);
				}

				var handler = new PropertyChangedEventHandler(OnMenuItemPropertyChanged);
				var totalWidth = _leftButtons.Count * largestWidth;
				for (var n = 0; n < _leftButtons.Count; n++)
				{
					var b = _leftButtons[n];

					if (b.Tag >= 0)
					{
						var item = _cell.LeftContextActions[(int)b.Tag];
						item.PropertyChanged += handler;
					}

                    var offset = largestWidth;

                    var x = - offset * ( _leftButtons.Count - n) ;
					//if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
					//	x -= totalWidth;

					b.Frame = new RectangleF(x, 0, largestWidth, height);
					if (resize)
						b.TitleLabel.AdjustsFontSizeToFitWidth = true;

					b.SetNeedsLayout();

					if (b != _leftMoreButton)
						b.TouchUpInside += OnLeftButtonActivated;
				}
			}
            #endregion //LeftButton Setting End
            return null;
		}
        #endregion

        #region RightButton Etc Setting 
        void OnRightButtonActivated(object sender, EventArgs e)
		{
			var button = (UIButton)sender;
            if (button.Tag == -1)
            {
				//More 버튼 클릭시 스크롤 포지션 동작참조 : GrobalCloseContextGestureRecognizer.cs => OnShouldReceiveTouch
				//_scroller.SetContentOffset(CGPoint.Empty, true);
				ActivateMoreRight();
            }
			else
			{
				_scroller.SetContentOffset(CGPoint.Empty, true);
				_cell.RightContextActions[(int)button.Tag].Activate();
			}
		}

		void ActivateMoreRight()
		{
			var displayed = new HashSet<nint>();
			for (var i = 0; i < _rightButtons.Count; i++)
			{
				var tag = _rightButtons[i].Tag;
				if (tag >= 0)
					displayed.Add(tag);
			}

			var frame = _rightMoreButton.Frame;
            var x = frame.X - _scroller.ContentOffset.X;

            //TODO : 원본 소스와 달리 IndexPathForCell 을 호출 할 경우 값이 반환되지 않음.
			//var path = _tableView.IndexPathForCell(this);
            var path = _tableView.IndexPathForRowAtPoint(this.Center);
			var rowPosition = _tableView.RectForRowAtIndexPath(path);
			var sourceRect = new RectangleF(x, rowPosition.Y, rowPosition.Width, rowPosition.Height);

			if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
			{
				var actionSheet = new MoreActionSheetController();
				for (var i = 0; i < _cell.RightContextActions.Count; i++)
				{
					if (displayed.Contains(i))
						continue;

					var item = _cell.RightContextActions[i];
					var weakItem = new WeakReference<ActionMenuItem>(item);
					var action = UIAlertAction.Create(item.Text, UIAlertActionStyle.Default, a =>
					{
						_scroller.SetContentOffset(CGPoint.Empty, true);
						ActionMenuItem mi;
						if (weakItem.TryGetTarget(out mi))
							mi.Activate();
					});
					actionSheet.AddAction(action);
				}

				var controller = GetController();
				if (controller == null)
					throw new InvalidOperationException("No UIViewController found to present.");

				if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
				{
					var cancel = UIAlertAction.Create(StringResources.Cancel, UIAlertActionStyle.Cancel, null);
					actionSheet.AddAction(cancel);
				}
				else
				{
					actionSheet.PopoverPresentationController.SourceView = _tableView;
					actionSheet.PopoverPresentationController.SourceRect = sourceRect;
				}

				controller.PresentViewController(actionSheet, true, null);
			}
			else
			{
				var d = new MoreActionSheetDelegate { Scroller = _scroller, Items = new List<ActionMenuItem>() };

				var actionSheet = new UIActionSheet(null, (IUIActionSheetDelegate)d);

				for (var i = 0; i < _cell.RightContextActions.Count; i++)
				{
					if (displayed.Contains(i))
						continue;

					var item = _cell.RightContextActions[i];
					d.Items.Add(item);
					actionSheet.AddButton(item.Text);
				}

				if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
				{
					var index = actionSheet.AddButton(StringResources.Cancel);
					actionSheet.CancelButtonIndex = index;
				}

				actionSheet.ShowFrom(sourceRect, _tableView, true);
			}
		}

        void CullRightButtons(nfloat acceptableTotalWidth, ref bool needMoreButton, ref nfloat largestButtonWidth)
		{
            while (largestButtonWidth * (_rightButtons.Count + (needMoreButton ? 1 : 0)) > acceptableTotalWidth && _rightButtons.Count > 1)
			{
				needMoreButton = true;

				var button = _rightButtons[_rightButtons.Count - 1];
				_rightButtons.RemoveAt(_rightButtons.Count - 1);

				if (largestButtonWidth == button.Frame.Width)
					largestButtonWidth = GetRightLargestWidth();
			}

			if (needMoreButton && _cell.RightContextActions.Count - _rightButtons.Count == 1)
				_rightButtons.RemoveAt(_rightButtons.Count - 1);
		}

		nfloat GetRightLargestWidth()
		{
			nfloat largestWidth = 0;
			for (var i = 0; i < _rightButtons.Count; i++)
			{
				var frame = _rightButtons[i].Frame;
				if (frame.Width > largestWidth)
					largestWidth = frame.Width;
			}

			return largestWidth;
		}
        #endregion //RightButton Etc Setting

        #region LeftButton Etc Setting
        void OnLeftButtonActivated(object sender, EventArgs e)
		{
			var button = (UIButton)sender;
            if (button.Tag == -1)
            {
				//_scroller.SetContentOffset(CGPoint.Empty, true);
				ActivateMoreLeft();
            }
			else
			{
				_scroller.SetContentOffset(CGPoint.Empty, true);
				_cell.LeftContextActions[(int)button.Tag].Activate();
			}
		}

		void ActivateMoreLeft()
		{
			var displayed = new HashSet<nint>();
			for (var i = 0; i < _leftButtons.Count; i++)
			{
				var tag = _leftButtons[i].Tag;
				if (tag >= 0)
					displayed.Add(tag);
			}

			var frame = _leftMoreButton.Frame;

			var x = frame.X - _scroller.ContentOffset.X;

			//var path = _tableView.IndexPathForCell(this);
            var path = _tableView.IndexPathForRowAtPoint(this.Center);
			var rowPosition = _tableView.RectForRowAtIndexPath(path);
			var sourceRect = new RectangleF(x, rowPosition.Y, rowPosition.Width, rowPosition.Height);

			if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
			{
				var actionSheet = new MoreActionSheetController();

				for (var i = 0; i < _cell.LeftContextActions.Count; i++)
				{
					if (displayed.Contains(i))
						continue;

					var item = _cell.LeftContextActions[i];
					var weakItem = new WeakReference<ActionMenuItem>(item);
					var action = UIAlertAction.Create(item.Text, UIAlertActionStyle.Default, a =>
					{
						_scroller.SetContentOffset(CGPoint.Empty, true);
						ActionMenuItem mi;
						if (weakItem.TryGetTarget(out mi))
							mi.Activate();
					});
					actionSheet.AddAction(action);
				}

				var controller = GetController();
				if (controller == null)
					throw new InvalidOperationException("No UIViewController found to present.");

				if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
				{
					var cancel = UIAlertAction.Create(StringResources.Cancel, UIAlertActionStyle.Cancel, null);
					actionSheet.AddAction(cancel);
				}
				else
				{
					actionSheet.PopoverPresentationController.SourceView = _tableView;
					actionSheet.PopoverPresentationController.SourceRect = sourceRect;
				}

				controller.PresentViewController(actionSheet, true, null);
			}
			else
			{
				var d = new MoreActionSheetDelegate { Scroller = _scroller, Items = new List<ActionMenuItem>() };

				var actionSheet = new UIActionSheet(null, (IUIActionSheetDelegate)d);

				for (var i = 0; i < _cell.LeftContextActions.Count; i++)
				{
					if (displayed.Contains(i))
						continue;

					var item = _cell.LeftContextActions[i];
					d.Items.Add(item);
					actionSheet.AddButton(item.Text);
				}

				if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
				{
					var index = actionSheet.AddButton(StringResources.Cancel);
					actionSheet.CancelButtonIndex = index;
				}

				actionSheet.ShowFrom(sourceRect, _tableView, true);
			}
		}

        void CullLeftButtons(nfloat acceptableTotalWidth, ref bool needMoreButton, ref nfloat largestButtonWidth)
		{
			while (largestButtonWidth * (_leftButtons.Count + (needMoreButton ? 1 : 0)) > acceptableTotalWidth && _leftButtons.Count > 1)
			{
				needMoreButton = true;

				var button = _leftButtons[_leftButtons.Count - 1];
                _leftButtons.RemoveAt(_leftButtons.Count - 1);

				if (largestButtonWidth == button.Frame.Width)
					largestButtonWidth = GetLeftLargestWidth();
			}

			if (needMoreButton && _cell.LeftContextActions.Count - _leftButtons.Count == 1)
                _leftButtons.RemoveAt(_leftButtons.Count - 1);
		}

        nfloat GetLeftLargestWidth()
		{
			nfloat largestWidth = 0;
			for (var i = 0; i < _leftButtons.Count; i++)
			{
				var frame = _leftButtons[i].Frame;
				if (frame.Width > largestWidth)
					largestWidth = frame.Width;
			}

			return largestWidth;
		}
		#endregion //LeftButton Etc Setting

		UIButton GetButton(ActionMenuItem item)
		{
			var button = new UIButton(new RectangleF(0, 0, 1, 1));
			button.SetTitle(item.Text, UIControlState.Normal);
			if (!item.IsDestructive)
			{
				button.BackgroundColor = item.BackgroundColor.ToUIColor();
			}
			else
			{
				button.SetBackgroundImage(DestructiveBackground, UIControlState.Normal);
			}
			button.TitleEdgeInsets = new UIEdgeInsets(0, 15, 0, 15);

			button.Enabled = true;

			return button;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_scroller != null)
				{
					_scroller.Dispose();
					_scroller = null;
				}

				_tableView = null;

                if (_leftMoreButton != null)
				{
					_leftMoreButton.Dispose();
					_leftMoreButton = null;
				}

                if(_rightMoreButton != null)
                {
                    _rightMoreButton.Dispose();
                    _rightMoreButton = null;
                }

				for (var i = 0; i < _leftButtons.Count; i++)
					_leftButtons[i].Dispose();

				_leftButtons.Clear();
                _leftMenuItems.Clear();

                for (var i = 0; i < _rightButtons.Count; i++)
                    _rightButtons[i].Dispose();

				_rightButtons.Clear();
				_rightMenuItems.Clear();

				if (_cell != null)
				{
					if (_cell.HasContextActions)
						((INotifyCollectionChanged)_cell.ContextActions).CollectionChanged -= OnContextItemsChanged;

                    if(_cell.HasLeftContextActions)
                        ((INotifyCollectionChanged)_cell.LeftContextActions).CollectionChanged -= OnContextItemsChanged;

                    if (_cell.HasRightContextActions)
                        ((INotifyCollectionChanged)_cell.RightContextActions).CollectionChanged -= OnContextItemsChanged;
                    
					_cell = null;
				}
			}

			base.Dispose(disposing);
		}


		UIViewController GetController()
		{
            //TODO : e.RealParent 확인해 보기
			Element e = _cell;
            while (e.Parent != null)
			{
                var renderer = Platform.GetRenderer((VisualElement)e.Parent);
				if (renderer.ViewController != null)
					return renderer.ViewController;

                e = e.Parent;
			}

			return null;
		}

        void SetupSelection(UITableView table)
		{
			for (var i = 0; i < table.GestureRecognizers.Length; i++)
			{
				var r = table.GestureRecognizers[i] as SelectGestureRecognizer;
				if (r != null)
					return;
			}

			_tableView.AddGestureRecognizer(new SelectGestureRecognizer());
		}

		class SelectGestureRecognizer : UITapGestureRecognizer
		{
			NSIndexPath _lastPath;

			public SelectGestureRecognizer() : base(Tapped)
			{
				ShouldReceiveTouch = (recognizer, touch) =>
				{
					var table = (UITableView)View;
					var pos = touch.LocationInView(table);

					_lastPath = table.IndexPathForRowAtPoint(pos);
					if (_lastPath == null)
						return false;

                    var cell = table.CellAt(_lastPath) ;//as SwipeiOSCell;

					return cell != null;
				};
			}

			static void Tapped(UIGestureRecognizer recognizer)
			{
				var selector = (SelectGestureRecognizer)recognizer;

				if (selector._lastPath == null)
					return;

				var table = (UITableView)recognizer.View;
				if (!selector._lastPath.Equals(table.IndexPathForSelectedRow))
					table.SelectRow(selector._lastPath, false, UITableViewScrollPosition.None);
				table.Source.RowSelected(table, selector._lastPath);

				//cell 선택시 backgroundcolor 변경
				var cell = table.VisibleCells[selector._lastPath.Row] as SwipeiOSCell;
				if (cell != null)
					cell.ContentCell.BackgroundColor = UIColor.Clear;
			}
		}

		class MoreActionSheetController : UIAlertController
		{
			public override UIAlertControllerStyle PreferredStyle
			{
				get { return UIAlertControllerStyle.ActionSheet; }
			}

			public override void WillRotate(UIInterfaceOrientation toInterfaceOrientation, double duration)
			{
				DismissViewController(false, null);
			}
		}

		class MoreActionSheetDelegate : UIActionSheetDelegate
		{
			public List<ActionMenuItem> Items;
			public UIScrollView Scroller;

			public override void Clicked(UIActionSheet actionSheet, nint buttonIndex)
			{
				if (buttonIndex == Items.Count)
					return; // Cancel button

				Scroller.SetContentOffset(CGPoint.Empty, true);

				// do not activate a -1 index when dismissing by clicking outside the popover
				if (buttonIndex >= 0)
					Items[(int)buttonIndex].Activate();
			}
		}
	}
}
