﻿/*
    Original Source : https://github.com/xamarin/Xamarin.Forms/blob/master/Xamarin.Forms.Platform.iOS/ContextScrollViewDelegate.cs
*/
using System;
using System.Collections.Generic;
using CoreGraphics;
using UIKit;
using NSAction = System.Action;
using PointF = CoreGraphics.CGPoint;
using RectangleF = CoreGraphics.CGRect;

namespace CustomCell.iOS
{
	internal class iOS7ButtonContainer : UIView
	{
		readonly nfloat _buttonWidth;

		public iOS7ButtonContainer(nfloat buttonWidth) : base(new RectangleF(0, 0, 0, 0))
		{
			_buttonWidth = buttonWidth;
			ClipsToBounds = true;
		}

		public override void LayoutSubviews()
		{
			var width = Frame.Width;
			nfloat takenSpace = 0;

			for (var i = 0; i < Subviews.Length; i++)
			{
				var view = Subviews[i];

				var pos = Subviews.Length - i;
				var x = width - _buttonWidth * pos;
				view.Frame = new RectangleF(x, 0, view.Frame.Width, view.Frame.Height);

				takenSpace += view.Frame.Width;
			}
		}
	}

	internal class ContextScrollViewDelegate : UIScrollViewDelegate
	{
		readonly nfloat _finalRightButtonSize;
        readonly nfloat _finalLeftButtonSize;
		UIView _backgroundView;
		List<UIButton> _rightButtons;
        List<UIButton> _leftButtons;
		UITapGestureRecognizer _closer;
		UIView _container;
		GlobalCloseContextGestureRecognizer _globalCloser;

		bool _isDisposed;
		static WeakReference<UIScrollView> s_scrollViewBeingScrolled;
		UITableView _table;

        public ContextScrollViewDelegate(UIView container, List<UIButton> rightButtons, List<UIButton> leftButtons, bool isRightOpen, bool isLeftOpen)
		{
			IsRightOpen = isRightOpen;
            IsLeftOpen = isLeftOpen;
			_container = container;
            _rightButtons = rightButtons;
            _leftButtons = leftButtons;

            for (var i = 0; i < rightButtons.Count; i++)
			{
				var b = rightButtons[i];
				b.Hidden = !isRightOpen;

                RightButtonsWidth += b.Frame.Width;
                _finalRightButtonSize = b.Frame.Width;
			}

            for (var i = 0; i < leftButtons.Count; i++)
			{
				var b = leftButtons[i];
                b.Hidden = !isLeftOpen;

                LeftButtonsWidth += b.Frame.Width;
				_finalLeftButtonSize = b.Frame.Width;
			}
		}

		public nfloat RightButtonsWidth { get; }

        public nfloat LeftButtonsWidth { get; }

		public Action ClosedCallback { get; set; }

        public bool IsRightOpen { get; private set; }

        public bool IsLeftOpen { get; private set; }

        public bool IsOpen 
        {
            get 
            {
                return IsRightOpen || IsLeftOpen;
            }
        }

		public override void DraggingStarted(UIScrollView scrollView)
		{
			if (ShouldIgnoreScrolling(scrollView))
				return;

			s_scrollViewBeingScrolled = new WeakReference<UIScrollView>(scrollView);

			//if (!IsOpen)
				//SetButtonsShowing(true);

			var cell = GetContextCell(scrollView);
			if (!cell.Selected)
				return;

			if (!IsOpen)
				RemoveHighlight(scrollView);
		}

		public void PrepareForDeselect(UIScrollView scrollView)
		{
			RestoreHighlight(scrollView);
		}

		public override void Scrolled(UIScrollView scrollView)
		{
			if (ShouldIgnoreScrolling(scrollView))
				return;

            var rightWidth = _finalRightButtonSize;
            var rightCount = _rightButtons.Count;
            var leftWidth = _finalLeftButtonSize;
            var leftCount = _leftButtons.Count;

            if (!UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
            {
                //TODO : iOS 8.0 이전 버전에서 확인 해야함.
                _container.Frame = new RectangleF(scrollView.Frame.Width, 0, scrollView.ContentOffset.X, scrollView.Frame.Height);
            }
			else
			{
                if (scrollView.ContentOffset.X > 0.0f && rightCount > 0)
                {
                    SetRightButtonsShowing(true);
                    var irightoffset = scrollView.ContentOffset.X / (float)rightCount;

                    if (irightoffset > rightWidth)
                        rightWidth = irightoffset + 1;

                    for (var i = rightCount - 1; i >= 0; i--)
                    {
                        var b = _rightButtons[i];
                        var rect = b.Frame;
                        b.Frame = new RectangleF(scrollView.Frame.Width + (rightCount - (i + 1)) * irightoffset, 0, rightWidth, rect.Height);
                    }
                }

                if (scrollView.ContentOffset.X < 0.0f && leftCount > 0)
                {
                    SetLeftButtonsShowing(true);
                    var ileftoffset = -scrollView.ContentOffset.X / (float)leftCount;

                    if (ileftoffset > leftWidth)
						leftWidth = ileftoffset + 1;

                    //스크롤시 버튼 사이즈 조절 <- 약간의 에니메이션 효과
                    //아래 코드 주석 처리시 버튼 사이즈 변경 없이 스크롤
                    for (var i = 0; i < leftCount ; i++)
                    {
                        var b = _leftButtons[i];
                        var rect = b.Frame;
                        var x = -(leftCount - (i + 1)) * ileftoffset;
                        b.Frame = new RectangleF(x - leftWidth, 0, leftWidth, rect.Height);
                        //b.Frame = new RectangleF(x, 0, -ileftoffset, rect.Height);
                    }
                }
			}

			if (scrollView.ContentOffset.X == 0)
			{
				//IsOpen = false;
                SetLeftButtonsShowing(false);
                SetRightButtonsShowing(false);
				RestoreHighlight(scrollView);

				s_scrollViewBeingScrolled = null;
				ClearCloserRecognizer(scrollView);
				ClosedCallback?.Invoke();
			}
		}

		public void Unhook(UIScrollView scrollView)
		{
			RestoreHighlight(scrollView);
			ClearCloserRecognizer(scrollView);
		}

		public override void WillEndDragging(UIScrollView scrollView, PointF velocity, ref PointF targetContentOffset)
		{
			if (ShouldIgnoreScrolling(scrollView))
				return;

			var rightwidth = RightButtonsWidth;
            var leftwidth = LeftButtonsWidth;
            var x = targetContentOffset.X;
			var parentThreshold = scrollView.Frame.Width * .4f;
			var contentRightThreshold = rightwidth * .8f;
            var contentLeftThreshold = leftwidth * .8f;

            if ((x >= parentThreshold || x >= contentRightThreshold) || (x <= -parentThreshold || x <= -contentLeftThreshold))
			{
                if (x >= parentThreshold || x >= contentRightThreshold)
                {
                    IsRightOpen = true;
                    IsLeftOpen = false;
                    targetContentOffset = new PointF(rightwidth, 0);

                }
                else if (x <= -parentThreshold || x <= -contentLeftThreshold)
                {
                    IsRightOpen = false;
					IsLeftOpen = true;
                    targetContentOffset = new PointF(-leftwidth, 0);
                }

                RemoveHighlight(scrollView);

				if (_globalCloser == null)
				{
					UIView view = scrollView;
					while (view.Superview != null)
					{
						view = view.Superview;

						NSAction close = () =>
						{
							if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
								RestoreHighlight(scrollView);

                            IsRightOpen = false;
                            IsLeftOpen = false;

							scrollView.SetContentOffset(new PointF(0, 0), true);

							ClearCloserRecognizer(scrollView);
						};

						var table = view as UITableView;
						if (table != null)
						{
							_table = table;
							_globalCloser = new GlobalCloseContextGestureRecognizer(scrollView, close);
							_globalCloser.ShouldRecognizeSimultaneously = (recognizer, r) => r == _table.PanGestureRecognizer;
							table.AddGestureRecognizer(_globalCloser);

							_closer = new UITapGestureRecognizer(close);
							var cell = GetContextCell(scrollView);
							cell.AddGestureRecognizer(_closer);
						}
					}
				}
			}
			else
			{
				ClearCloserRecognizer(scrollView);

                IsRightOpen = false;
                IsLeftOpen = false;
				targetContentOffset = new PointF(0, 0);

				if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
					RestoreHighlight(scrollView);
			}
		}

		static bool ShouldIgnoreScrolling(UIScrollView scrollView)
		{
			if (s_scrollViewBeingScrolled == null)
				return false;

			UIScrollView scrollViewBeingScrolled;
			if (!s_scrollViewBeingScrolled.TryGetTarget(out scrollViewBeingScrolled) || ReferenceEquals(scrollViewBeingScrolled, scrollView) || !ReferenceEquals(((ContextScrollViewDelegate)scrollViewBeingScrolled.Delegate)._table, ((ContextScrollViewDelegate)scrollView.Delegate)._table))
				return false;

            scrollView.SetContentOffset(CGPoint.Empty, false);
			return true;
		}

		protected override void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			_isDisposed = true;

			if (disposing)
			{
				ClosedCallback = null;

				_table = null;
				_backgroundView = null;
				_container = null;

                _rightButtons = null;
                _leftButtons = null;
			}

			base.Dispose(disposing);
		}

		void ClearCloserRecognizer(UIScrollView scrollView)
		{
			if (_globalCloser == null || _globalCloser.State == UIGestureRecognizerState.Cancelled)
				return;

			var cell = GetContextCell(scrollView);
			cell.RemoveGestureRecognizer(_closer);
			_closer.Dispose();
			_closer = null;

			_table.RemoveGestureRecognizer(_globalCloser);
			_table = null;
			_globalCloser.Dispose();
			_globalCloser = null;
		}

		SwipeiOSCell GetContextCell(UIScrollView scrollView)
		{
			var view = scrollView.Superview.Superview;
			var cell = view as SwipeiOSCell;
			while (view.Superview != null)
			{
				cell = view as SwipeiOSCell;
				if (cell != null)
					break;

				view = view.Superview;
			}

			return cell;
		}

		void RemoveHighlight(UIScrollView scrollView)
		{
            var subviews = scrollView.Superview.Superview.Subviews;

			var count = 0;
			for (var i = 0; i < subviews.Length; i++)
			{
				var s = subviews[i];
				if (s.Frame.Height > 1)
					count++;
			}

			if (count <= 1)
				return;

			_backgroundView = subviews[0];
			_backgroundView.RemoveFromSuperview();

			var cell = GetContextCell(scrollView);
            cell.SelectionStyle = UITableViewCellSelectionStyle.None;
		}

		void RestoreHighlight(UIScrollView scrollView)
		{
			if (_backgroundView == null)
				return;

			var cell = GetContextCell(scrollView);
			cell.SelectionStyle = UITableViewCellSelectionStyle.Default;
            //cell.SelectionStyle = UITableViewCellSelectionStyle.Gray;
			cell.SetSelected(true, false);

			scrollView.Superview.Superview.InsertSubview(_backgroundView, 0);
			_backgroundView = null;
		}

        void SetLeftButtonsShowing(bool show)
		{
            for (var i = 0; i < _leftButtons.Count; i++)
				_leftButtons[i].Hidden = !show;
		}

		void SetRightButtonsShowing(bool show)
		{
			for (var i = 0; i < _rightButtons.Count; i++)
				_rightButtons[i].Hidden = !show;
		}
	}
}
