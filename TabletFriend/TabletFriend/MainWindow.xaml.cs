﻿using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using TabletFriend.Docking;
using WpfAppBar;

namespace TabletFriend
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		private LayoutManager _layout;
		private LayoutListManager _layoutList;
		private TrayManager _tray;
		private FileManager _file;

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(string property)
		{
			// i hate bizdev i hate bizdev i hate bizdev i hate bizdev i hate bizdev i hate bizdev i hate bizdev i hate bizdev i hate bizdev i hate bizdev i hate bizdev i hate bizdev i hate bizdev i hate bizdev
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
		}

		public MainWindow()
		{
			SystemEvents.DisplaySettingsChanged += OnSizeChanged;

			Directory.SetCurrentDirectory(AppState.CurrentDirectory);

			Topmost = true;
			InitializeComponent();
			MouseDown += OnMouseDown;

			_file = new FileManager();

			ToggleManager.Init();

			_layout = new LayoutManager(this);
			Settings.Load();

			Installer.TryInstall();

			_ = UpdateChecker.Check();


			_layoutList = new LayoutListManager();
			ContextMenu = new System.Windows.Controls.ContextMenu();

			OnUpdateLayoutList();


			_tray = new TrayManager(_layoutList);

			

			if (AppState.Settings.AddToAutostart)
			{
				AutostartManager.SetAutostart();
			}
			else
			{
				AutostartManager.ResetAutostart();
			}

			EventBeacon.Subscribe("toggle_minimize", OnToggleMinimize);
			EventBeacon.Subscribe("update_layout_list", OnUpdateLayoutList);
			EventBeacon.Subscribe("change_layout", OnUpdateLayoutList);
			EventBeacon.Subscribe("docking_changed", OnDockingChanged);
		}


		private void OnSizeChanged(object sender, EventArgs eventArgs)
		{
			UiFactory.CreateUi(AppState.CurrentLayout, this);
		}


		private double _maxOpacity;
		public double MaxOpacity
		{
			get => _maxOpacity;
			set
			{
				_maxOpacity = value;
				OnPropertyChanged(nameof(MaxOpacity));
			}
		}


		private double _minOpacity;
		public double MinOpacity
		{
			get => _minOpacity;
			set
			{
				_minOpacity = value;
				OnPropertyChanged(nameof(MinOpacity));
			}
		}


		private void OnUpdateLayoutList(object[] obj = null)
		{
			Application.Current.Dispatcher.Invoke(
				() =>
				{
					ContextMenu.Items.Clear();
					DockingMenuFactory.CreateDockingMenu(ContextMenu);
					var items = _layoutList.CloneMenu();
					foreach (var item in items)
					{
						ContextMenu.Items.Add(item);
					}
				}
			);
		}

		private void OnMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (
				e.ChangedButton == MouseButton.Left
				&& AppState.Settings.DockingMode == DockingMode.None
			)
			{
				DragMove();
			}
		}

		private void OnDockingChanged(params object[] args)
		{
			var side = (DockingMode)args[0];

			if (side != DockingMode.None && side != AppState.Settings.DockingMode)
			{
				AppBarFunctions.SetAppBar(this, DockingMode.None);
			}
			
			AppState.Settings.DockingMode = side;

			UiFactory.CreateUi(AppState.CurrentLayout, this);

			AppBarFunctions.SetAppBar(this, side);

			if (side != DockingMode.None)
			{
				MinOpacity = AppState.CurrentLayout.Theme.MaxOpacity;
				MaxOpacity = AppState.CurrentLayout.Theme.MaxOpacity;
				BeginAnimation(OpacityProperty, null);
				Opacity = AppState.CurrentLayout.Theme.MaxOpacity;
			}
			else
			{
				MinOpacity = AppState.CurrentLayout.Theme.MinOpacity;
				MaxOpacity = AppState.CurrentLayout.Theme.MaxOpacity;
				BeginAnimation(OpacityProperty, null);
				Opacity = AppState.CurrentLayout.Theme.MaxOpacity;
				BeginAnimation(OpacityProperty, FadeOut);
			}


			EventBeacon.SendEvent("update_settings");
		}


		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			// Makes it so the window doesn't hold focus.
			var helper = new WindowInteropHelper(this);
			SetWindowLong(
				helper.Handle,
				GWL_EXSTYLE,
				GetWindowLong(helper.Handle, GWL_EXSTYLE) | WS_EX_NOACTIVATE
			);

			EventBeacon.SendEvent("docking_changed", AppState.Settings.DockingMode);
		}


		private void OnToggleMinimize(object[] obj)
		{
			// Regular minimize doesn't work without the taaskbar icon.
			// The window just derps out and stays at the bottom left corner.
			// There are workarounds, btu they make an icon flash in the taskbar
			// for a split second. This is the best solution I found.
			if (Visibility == Visibility.Collapsed)
			{
				Visibility = Visibility.Visible;
				AppBarFunctions.SetAppBar(this, AppState.Settings.DockingMode);
			}
			else
			{
				AppBarFunctions.SetAppBar(this, DockingMode.None);
				Visibility = Visibility.Collapsed;
			}
		}


		private const int GWL_EXSTYLE = -20;
		private const int WS_EX_NOACTIVATE = 0x08000000;

		[DllImport("user32.dll")]
		public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		[DllImport("user32.dll")]
		public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
			EventBeacon.SendEvent("update_settings");
			AppBarFunctions.SetAppBar(this, DockingMode.None);
			Environment.Exit(0);
		}
	}
}
