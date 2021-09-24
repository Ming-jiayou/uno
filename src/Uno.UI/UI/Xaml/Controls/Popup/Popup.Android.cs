﻿using Android.App;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Text;
using static Android.Views.View;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
	public partial class Popup
	{
		private PopupWindow _popupWindow;

		internal FlyoutPlacementMode Placement { get; set; }

		internal Popup(bool useNativePopup) : this()
		{
			_useNativePopup = useNativePopup;
			Initialize();
		}

		partial void InitializeNativePartial()
		{
			_popupWindow = new PopupWindow(this, WindowManagerLayoutParams.MatchParent, WindowManagerLayoutParams.MatchParent, true);

			_popupWindow.Width = WindowManagerLayoutParams.MatchParent;
			_popupWindow.Height = WindowManagerLayoutParams.MatchParent;
			_popupWindow.Focusable = true;
			_popupWindow.Touchable = true;

			OnIsLightDismissEnabledChanged(false, true);

			_popupWindow.DismissEvent += OnPopupDismissed;
		}

		partial void OnPopupPanelChangedPartialNative(PopupPanel previousPanel, PopupPanel newPanel)
		{
			previousPanel?.Children.Clear();

			if (PopupPanel != null)
			{
				if (Child != null)
				{
					PopupPanel.Children.Add(Child);
				}
			}

			newPanel.IsVisualTreeRoot = true;
			_popupWindow.ContentView = newPanel;

			UpdatePopupPanelDismissibleBackground(IsLightDismissEnabled);
		}

		private void OnPopupDismissed(object sender, EventArgs e)
		{
			IsOpen = false;
		}

		partial void OnIsOpenChangedNative(bool oldIsOpen, bool newIsOpen)
		{
			if (newIsOpen)
			{
				PopupPanel.Visibility = Visibility.Visible;
				_popupWindow.ShowAtLocation(Anchor ?? this, GravityFlags.Left | GravityFlags.Top, 0, 0);
			}
			else
			{
				if(_popupWindow.IsShowing)
				{
					_popupWindow.Dismiss();
				}
				PopupPanel.Visibility = Visibility.Collapsed;
			}
		}



		partial void OnIsLightDismissEnabledChangedNative(bool oldIsLightDismissEnabled, bool newIsLightDismissEnabled)
		{
			if (newIsLightDismissEnabled)
			{
				_popupWindow.OutsideTouchable = true;

				_popupWindow.SetBackgroundDrawable(new ColorDrawable(Colors.Transparent));
			}
			else
			{
				_popupWindow.OutsideTouchable = false;

				_popupWindow.SetBackgroundDrawable(null);
			}

			UpdatePopupPanelDismissibleBackground(newIsLightDismissEnabled);
		}

		private void UpdatePopupPanelDismissibleBackground(bool isLightDismiss)
		{
			var popupPanel = PopupPanel;
			if (popupPanel == null)
			{
				return; // nothing to do
			}

			PopupPanel.Background = GetPanelBackground();
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			if (_useNativePopup)
			{

				// Ensure Popup doesn't take any space.
				this.SetMeasuredDimension(0, 0);
			}
			else
			{
				base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
			}

		}

		/// <summary>
		/// Prevent the popup from stealing focus from views in the main window.
		/// </summary>
		internal void DisableFocus()
		{
			if (_popupWindow != null)
			{
				_popupWindow.Focusable = false;
				_popupWindow.InputMethodMode = InputMethod.Needed;
			}
		}
	}
}
