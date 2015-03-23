
#region ================== Copyright (c) 2007 Pascal vd Heiden

/*
 * Copyright (c) 2007 Pascal vd Heiden, www.codeimp.com
 * This program is released under GNU General Public License
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 */

#endregion

#region ================== Namespaces

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using CodeImp.DoomBuilder.Actions;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Controls;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.Editing;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.GZBuilder.Windows;
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Plugins;
using CodeImp.DoomBuilder.Properties;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.VisualModes;

#endregion

namespace CodeImp.DoomBuilder.Windows
{
	public partial class MainForm : DelayedForm, IMainForm
	{
		#region ================== Constants
		
		// Recent files
		private const int MAX_RECENT_FILES_PIXELS = 250;

		// Dockers
		//private const int DOCKER_TAB_WIDTH = 20;
		
		// Status bar
		private const string STATUS_READY_TEXT = "Ready.";
		private const string STATUS_NO_SELECTION_TEXT = "Nothing selected."; //mxd
		private const string STATUS_LOADING_TEXT = "Loading resources...";
		private const int WARNING_FLASH_COUNT = 10;
		private const int WARNING_FLASH_INTERVAL = 100;
		private const int WARNING_RESET_DELAY = 5000;
		private const int INFO_RESET_DELAY = 5000;
		private const int ACTION_FLASH_COUNT = 3;
		private const int ACTION_FLASH_INTERVAL = 50;
		private const int ACTION_RESET_DELAY = 5000;
		
		private readonly Image[,] STATUS_IMAGES = new Image[,]
		{
			// Normal versions
			{
			  Resources.Status0, Resources.Status1,
			  Resources.Status2, Resources.Warning
			},
			
			// Flashing versions
			{
			  Resources.Status10, Resources.Status11,
			  Resources.Status12, Resources.WarningOff
			}
		};
		
		// Message pump
		public enum ThreadMessages
		{
			// Sent by the background threat to update the status
			UpdateStatus = General.WM_USER + 1,
			
			// This is sent by the background thread when images are loaded
			// but only when first loaded or when dimensions were changed
			ImageDataLoaded = General.WM_USER + 2,
			
			//mxd. This is sent by the background thread when sprites are loaded
			SpriteDataLoaded = General.WM_USER + 3,
		}
		
		#endregion

		#region ================== Delegates

		//private delegate void CallUpdateStatusIcon();
		//private delegate void CallImageDataLoaded(ImageData img);
		private delegate void CallBlink(); //mxd

		#endregion

		#region ================== mxd. Events

		public event EventHandler OnEditFormValuesChanged; //mxd

		#endregion

		#region ================== Variables

		// Position/size
		private Point lastposition;
		private Size lastsize;
		private bool displayresized = true;
		private bool windowactive;
		
		// Mouse in display
		private bool mouseinside;
		
		// Input
		private bool shift, ctrl, alt;
		private MouseButtons mousebuttons;
		private MouseInput mouseinput;
		private Rectangle originalclip;
		private bool mouseexclusive;
		private int mouseexclusivebreaklevel;
		
		// Skills
		private ToolStripItem[] skills;
		
		// Last info on panels
		private object lastinfoobject;
		
		// Recent files
		private ToolStripMenuItem[] recentitems;
		
		// View modes
		private ToolStripButton[] viewmodesbuttons;
		private ToolStripMenuItem[] viewmodesitems;
		
		// Edit modes
		private List<ToolStripItem> editmodeitems;
		
		// Toolbar
		private List<PluginToolbarButton> pluginbuttons;
		private EventHandler buttonvisiblechangedhandler;
		private bool preventupdateseperators;
		private bool updatingfilters;
		private bool toolbarContextMenuShiftPressed; //mxd
		
		// Statusbar
		private StatusInfo status;
		private int statusflashcount;
		private bool statusflashicon;
		private string selectionInfo = STATUS_NO_SELECTION_TEXT; //mxd
		
		// Properties
		private IntPtr windowptr;
		
		// Processing
		private int processingcount;
		private float lastupdatetime;

		// Updating
		private int lockupdatecount;

		//mxd. Hints
		private Docker hintsDocker;
		private HintsPanel hintsPanel;

		//mxd
		private System.Timers.Timer blinkTimer; 
		private bool editformopen;
		
		#endregion

		#region ================== Properties

		public bool ShiftState { get { return shift; } }
		public bool CtrlState { get { return ctrl; } }
		public bool AltState { get { return alt; } }
		new public MouseButtons MouseButtons { get { return mousebuttons; } }
		public bool MouseInDisplay { get { return mouseinside; } }
		public RenderTargetControl Display { get { return display; } }
		public bool SnapToGrid { get { return buttonsnaptogrid.Checked; } }
		public bool AutoMerge { get { return buttonautomerge.Checked; } }
		public bool MouseExclusive { get { return mouseexclusive; } }
		new public IntPtr Handle { get { return windowptr; } }
		public bool IsInfoPanelExpanded { get { return (panelinfo.Height == heightpanel1.Height); } }
		public string ActiveDockerTabName { get { return dockerspanel.IsCollpased ? "None" : dockerspanel.SelectedTabName; } }
		public bool IsActiveWindow { get { return windowactive; } }
		public StatusInfo Status { get { return status; } }
		
		#endregion

		#region ================== Constructor / Disposer

		// Constructor
		internal MainForm()
		{
			// Setup controls
			InitializeComponent();
			pluginbuttons = new List<PluginToolbarButton>();
			editmodeitems = new List<ToolStripItem>();
			labelcollapsedinfo.Text = "";
			display.Dock = DockStyle.Fill;
			
			// Fetch pointer
			windowptr = base.Handle;
			
			// Make array for view modes
			viewmodesbuttons = new ToolStripButton[Renderer2D.NUM_VIEW_MODES];
			viewmodesbuttons[(int)ViewMode.Normal] = buttonviewnormal;
			viewmodesbuttons[(int)ViewMode.Brightness] = buttonviewbrightness;
			viewmodesbuttons[(int)ViewMode.FloorTextures] = buttonviewfloors;
			viewmodesbuttons[(int)ViewMode.CeilingTextures] = buttonviewceilings;
			viewmodesitems = new ToolStripMenuItem[Renderer2D.NUM_VIEW_MODES];
			viewmodesitems[(int)ViewMode.Normal] = itemviewnormal;
			viewmodesitems[(int)ViewMode.Brightness] = itemviewbrightness;
			viewmodesitems[(int)ViewMode.FloorTextures] = itemviewfloors;
			viewmodesitems[(int)ViewMode.CeilingTextures] = itemviewceilings;
			
			// Visual Studio IDE doesn't let me set these in the designer :(
			buttonzoom.Font = menufile.Font;
			buttonzoom.DropDownDirection = ToolStripDropDownDirection.AboveLeft;
			buttongrid.Font = menufile.Font;
			buttongrid.DropDownDirection = ToolStripDropDownDirection.AboveLeft;

			// Event handlers
			buttonvisiblechangedhandler = ToolbarButtonVisibleChanged;
			//mxd
			display.OnKeyReleased += display_OnKeyReleased;
			toolbarContextMenu.KeyDown += toolbarContextMenu_KeyDown;
			toolbarContextMenu.KeyUp += toolbarContextMenu_KeyUp;
			
			// Apply shortcut keys
			ApplyShortcutKeys();
			
			// Make recent items list
			CreateRecentFiles();
			
			// Show splash
			ShowSplashDisplay();
			
			// Keep last position and size
			lastposition = this.Location;
			lastsize = this.Size;

			//mxd
			blinkTimer = new System.Timers.Timer {Interval = 500};
			blinkTimer.Elapsed += blinkTimer_Elapsed;

			//mxd. Debug Console
#if DEBUG
			modename.Visible = false;
#else
			console.Visible = false;
#endif

			//mxd. Hints
			hintsPanel = new HintsPanel();
			hintsDocker = new Docker("hints", "Help", hintsPanel);
		}
		
		#endregion
		
		#region ================== General

		// Editing mode changed!
		internal void EditModeChanged()
		{
			// Check appropriate button on interface
			// And show the mode name
			if(General.Editing.Mode != null)
			{
				General.MainWindow.CheckEditModeButton(General.Editing.Mode.EditModeButtonName);
				General.MainWindow.DisplayModeName(General.Editing.Mode.Attributes.DisplayName);
			}
			else
			{
				General.MainWindow.CheckEditModeButton("");
				General.MainWindow.DisplayModeName("");
			}

			// View mode only matters in classic editing modes
			for(int i = 0; i < Renderer2D.NUM_VIEW_MODES; i++)
			{
				viewmodesitems[i].Enabled = (General.Editing.Mode is ClassicMode);
				viewmodesbuttons[i].Enabled = (General.Editing.Mode is ClassicMode);
			}

			UpdateEditMenu();
			UpdatePrefabsMenu();
		}

		// This makes a beep sound
		public void MessageBeep(MessageBeepType type)
		{
			General.MessageBeep(type);
		}

		// This sets up the interface
		internal void SetupInterface()
		{
			// Setup docker
			if(General.Settings.DockersPosition != 2 && General.Map != null)
			{
				LockUpdate();
				dockerspanel.Visible = true;
				dockersspace.Visible = true;

				// We can't place the docker easily when collapsed
				dockerspanel.Expand();

				// Setup docker width
				if(General.Settings.DockersWidth < dockerspanel.GetCollapsedWidth())
					General.Settings.DockersWidth = dockerspanel.GetCollapsedWidth();

				// Determine fixed space required
				if(General.Settings.CollapseDockers)
					dockersspace.Width = dockerspanel.GetCollapsedWidth();
				else
					dockersspace.Width = General.Settings.DockersWidth;

				// Setup docker
				int targetindex = this.Controls.IndexOf(display) + 1; //mxd
				if(General.Settings.DockersPosition == 0)
				{
					modestoolbar.Dock = DockStyle.Right; //mxd
					dockersspace.Dock = DockStyle.Left;
					AdjustDockersSpace(targetindex); //mxd
					dockerspanel.Setup(false);
					dockerspanel.Location = dockersspace.Location;
					dockerspanel.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
				}
				else
				{
					modestoolbar.Dock = DockStyle.Left; //mxd
					dockersspace.Dock = DockStyle.Right;
					AdjustDockersSpace(targetindex); //mxd
					dockerspanel.Setup(true);
					dockerspanel.Location = new Point(dockersspace.Right - General.Settings.DockersWidth, dockersspace.Top);
					dockerspanel.Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
				}

				dockerspanel.Width = General.Settings.DockersWidth;
				dockerspanel.Height = dockersspace.Height;
				dockerspanel.BringToFront();

				if(General.Settings.CollapseDockers) dockerspanel.Collapse();

				UnlockUpdate();
			}
			else
			{
				dockerspanel.Visible = false;
				dockersspace.Visible = false;
				modestoolbar.Dock = DockStyle.Left; //mxd
			}
		}

		//mxd. dockersspace display index gets messed up while re-docking. This fixes it...
		private void AdjustDockersSpace(int targetindex)
		{
			while(this.Controls.IndexOf(dockersspace) != targetindex)
			{
				this.Controls.SetChildIndex(dockersspace, targetindex);
			}
		}
		
		// This updates all menus for the current status
		internal void UpdateInterface()
		{
			// Map opened?
			if(General.Map != null)
			{
				// Show map name and filename in caption
				this.Text = General.Map.FileTitle + " (" + General.Map.Options.CurrentName + ") - " + Application.ProductName;
			}
			else
			{
				// Show normal caption
#if DEBUG
				this.Text = Application.ProductName + " - DEVBUILD";
#else
				this.Text = Application.ProductName + " v" + Application.ProductVersion;
#endif
			}

			// Update the status bar
			UpdateStatusbar();
			
			// Update menus and toolbar icons
			UpdateFileMenu();
			UpdateEditMenu();
			UpdateViewMenu();
			UpdateModeMenu();
			UpdatePrefabsMenu();
			UpdateToolsMenu();
			UpdateToolbar();
			UpdateSkills();
			UpdateHelpMenu();
		}
		
		// Generic event that invokes the tagged action
		public void InvokeTaggedAction(object sender, EventArgs e)
		{
			this.Update();
			
			if(sender is ToolStripItem)
				General.Actions.InvokeAction((sender as ToolStripItem).Tag.ToString());
			else if(sender is Control)
				General.Actions.InvokeAction((sender as Control).Tag.ToString());
			else
				General.Fail("InvokeTaggedAction used on an unexpected control.");
			
			this.Update();
		}
		
		#endregion
		
		#region ================== Window
		
		// This locks the window for updating
		internal void LockUpdate()
		{
			lockupdatecount++;
			if(lockupdatecount == 1) General.LockWindowUpdate(this.Handle);
		}

		// This unlocks for updating
		internal void UnlockUpdate()
		{
			lockupdatecount--;
			if(lockupdatecount == 0) General.LockWindowUpdate(IntPtr.Zero);
			if(lockupdatecount < 0) lockupdatecount = 0;
		}

		// This unlocks for updating
		/*internal void ForceUnlockUpdate()
		{
			if(lockupdatecount > 0) General.LockWindowUpdate(IntPtr.Zero);
			lockupdatecount = 0;
		}*/
		
		// This sets the focus on the display for correct key input
		public bool FocusDisplay()
		{
			return display.Focus();
		}

		// Window is first shown
		private void MainForm_Shown(object sender, EventArgs e)
		{
			// Perform auto mapo loading action when the window is not delayed
			if(!General.DelayMainWindow) PerformAutoMapLoading();
		}

		// Auto map loading that must be done when the window is first shown after loading
		// but also before the window is shown when the -delaywindow parameter is given
		internal void PerformAutoMapLoading()
		{
			// Check if the command line arguments tell us to load something
			if(General.AutoLoadFile != null)
			{
				bool showdialog = false;
				MapOptions options = new MapOptions();
				
				// Any of the options already given?
				if(General.AutoLoadMap != null)
				{
					Configuration mapsettings;
					
					// Try to find existing options in the settings file
					string dbsfile = General.AutoLoadFile.Substring(0, General.AutoLoadFile.Length - 4) + ".dbs";
					if(File.Exists(dbsfile))
						try { mapsettings = new Configuration(dbsfile, true); }
						catch(Exception) { mapsettings = new Configuration(true); }
					else
						mapsettings = new Configuration(true);

					//mxd. Get proper configuration file
					bool longtexturenamessupported = false;
					string configfile = General.AutoLoadConfig;
					if(string.IsNullOrEmpty(configfile)) configfile = mapsettings.ReadSetting("gameconfig", "");
					if(configfile.Trim().Length == 0)
					{
						showdialog = true;
					}
					else
					{
						//TODO: test this!
						Configuration gamecfg = new Configuration(configfile);
						longtexturenamessupported = gamecfg.ReadSetting("longtexturenames", false);
					}

					// Set map name and other options
					options = new MapOptions(mapsettings, General.AutoLoadMap, longtexturenamessupported);

					// Set resource data locations
					options.CopyResources(General.AutoLoadResources);

					// Set strict patches
					options.StrictPatches = General.AutoLoadStrictPatches;
					
					// Set configuration file (constructor already does this, but we want this info from the cmd args if possible)
					options.ConfigFile = configfile;
				}
				else
				{
					// No options given
					showdialog = true;
				}

				// Show open map dialog?
				if(showdialog)
				{
					// Show open dialog
					General.OpenMapFile(General.AutoLoadFile, null);
				}
				else
				{
					// Open with options
					General.OpenMapFileWithOptions(General.AutoLoadFile, options);
				}
			}
		}

		// Window is loaded
		private void MainForm_Load(object sender, EventArgs e)
		{
			// Position window from configuration settings
			this.SuspendLayout();
			this.Size = new Size(General.Settings.ReadSetting("mainwindow.sizewidth", this.Size.Width),
								 General.Settings.ReadSetting("mainwindow.sizeheight", this.Size.Height));
			this.Location = new Point(General.Clamp(General.Settings.ReadSetting("mainwindow.positionx", this.Location.X), 
													SystemInformation.VirtualScreen.Left - this.Size.Width + 128,
													SystemInformation.VirtualScreen.Right - 128),
									  General.Clamp(General.Settings.ReadSetting("mainwindow.positiony", this.Location.Y),
													SystemInformation.VirtualScreen.Top,
													SystemInformation.VirtualScreen.Bottom - 128));
			this.WindowState = (FormWindowState)General.Settings.ReadSetting("mainwindow.windowstate", (int)FormWindowState.Maximized);
			this.ResumeLayout(true);
			
			// Normal windowstate?
			if(this.WindowState == FormWindowState.Normal)
			{
				// Keep last position and size
				lastposition = this.Location;
				lastsize = this.Size;
			}

			//mxd. Enable drag and drop
			this.AllowDrop = true;
			this.DragEnter += OnDragEnter;
			this.DragDrop += OnDragDrop;

			// Info panel state?
			bool expandedpanel = General.Settings.ReadSetting("mainwindow.expandedinfopanel", true);
			if(expandedpanel != IsInfoPanelExpanded) ToggleInfoPanel();
		}

		// Window receives focus
		private void MainForm_Activated(object sender, EventArgs e)
		{
			windowactive = true;

			//UpdateInterface();
			ResumeExclusiveMouseInput();
			ReleaseAllKeys();
			FocusDisplay();
		}
		
		// Window loses focus
		private void MainForm_Deactivate(object sender, EventArgs e)
		{
			windowactive = false;
			
			BreakExclusiveMouseInput();
			ReleaseAllKeys();
		}
		
		// Window is moved
		private void MainForm_Move(object sender, EventArgs e)
		{
			// Normal windowstate?
			if(this.WindowState == FormWindowState.Normal)
			{
				// Keep last position and size
				lastposition = this.Location;
				lastsize = this.Size;
			}
		}

		// Window resizes
		private void MainForm_Resize(object sender, EventArgs e)
		{
			// Resizing
			//this.SuspendLayout();
			//resized = true;
		}

		// Window was resized
		private void MainForm_ResizeEnd(object sender, EventArgs e)
		{
			// Normal windowstate?
			if(this.WindowState == FormWindowState.Normal)
			{
				// Keep last position and size
				lastposition = this.Location;
				lastsize = this.Size;
			}
		}

		// Window is being closed
		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if(e.CloseReason != CloseReason.ApplicationExitCall)
			{
				//mxd
				if(General.Map != null && General.Map.Launcher.GameEngineRunning) General.Map.Launcher.StopGameEngine();
				
				// Close the map
				if(General.CloseMap())
				{
					General.WriteLogLine("Closing main interface window...");
					
					// Stop timers
					statusflasher.Stop();
					statusresetter.Stop();
					blinkTimer.Stop(); //mxd

					// Stop exclusive mode, if any is active
					StopExclusiveMouseInput();
					StopProcessing();

					// Unbind methods
					General.Actions.UnbindMethods(this);

					// Determine window state to save
					int windowstate;
					if(this.WindowState != FormWindowState.Minimized)
						windowstate = (int)this.WindowState;
					else
						windowstate = (int)FormWindowState.Normal;

					// Save window settings
					General.Settings.WriteSetting("mainwindow.positionx", lastposition.X);
					General.Settings.WriteSetting("mainwindow.positiony", lastposition.Y);
					General.Settings.WriteSetting("mainwindow.sizewidth", lastsize.Width);
					General.Settings.WriteSetting("mainwindow.sizeheight", lastsize.Height);
					General.Settings.WriteSetting("mainwindow.windowstate", windowstate);
					General.Settings.WriteSetting("mainwindow.expandedinfopanel", IsInfoPanelExpanded);

					// Save recent files
					SaveRecentFiles();

					// Terminate the program
					General.Terminate(true);
				}
				else
				{
					// Cancel the close
					e.Cancel = true;
				}
			}
		}

		//mxd
		private void OnDragEnter(object sender, DragEventArgs e) 
		{
			if(e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effect = DragDropEffects.Copy;
			} 
			else 
			{
				e.Effect = DragDropEffects.None;
			}
		}

		//mxd
		private void OnDragDrop(object sender, DragEventArgs e) 
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop)) 
			{
				string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
				if (filePaths.Length != 1) 
				{
					General.Interface.DisplayStatus(StatusType.Warning, "Cannot open multiple files at once!");
					return;
				}

				if (!File.Exists(filePaths[0])) 
				{
					General.Interface.DisplayStatus(StatusType.Warning, "Cannot open '" + filePaths[0] + "': file does not exist!");
					return;
				}

				string ext = Path.GetExtension(filePaths[0]);
				if(string.IsNullOrEmpty(ext) || ext.ToLower() != ".wad") 
				{
					General.Interface.DisplayStatus(StatusType.Warning, "Cannot open '" + filePaths[0] + "': only WAD files can be loaded this way!");
					return;
				}

				this.Update(); // Update main window
				General.OpenMapFile(filePaths[0], null);
				UpdateGZDoomPanel();
			}
		}

		#endregion
		
		#region ================== Statusbar
		
		// This updates the status bar
		private void UpdateStatusbar()
		{
			// Map open?
			if(General.Map != null)
			{
				// Enable items
				xposlabel.Enabled = true;
				yposlabel.Enabled = true;
				poscommalabel.Enabled = true;
				zoomlabel.Enabled = true;
				buttonzoom.Enabled = true;
				gridlabel.Enabled = true;
				buttongrid.Enabled = true;
				configlabel.Text = General.Map.Config.Name;
			}
			else
			{
				// Disable items
				xposlabel.Text = "--";
				yposlabel.Text = "--";
				xposlabel.Enabled = false;
				yposlabel.Enabled = false;
				poscommalabel.Enabled = false;
				zoomlabel.Enabled = false;
				buttonzoom.Enabled = false;
				gridlabel.Enabled = false;
				buttongrid.Enabled = false;
				configlabel.Text = "";
			}
			
			UpdateStatusIcon();
		}
		
		// This flashes the status icon
		private void statusflasher_Tick(object sender, EventArgs e)
		{
			statusflashicon = !statusflashicon;
			UpdateStatusIcon();
			statusflashcount--;
			if(statusflashcount == 0) statusflasher.Stop();
		}
		
		// This resets the status to ready
		private void statusresetter_Tick(object sender, EventArgs e)
		{
			DisplayReady();
		}
		
		// This changes status text
		public void DisplayStatus(StatusType type, string message) { DisplayStatus(new StatusInfo(type, message)); }
		public void DisplayStatus(StatusInfo newstatus)
		{
			//mxd. New message is the same as the one being displayed?
			if(newstatus.type != StatusType.Ready && status.displayed && newstatus.type == status.type && newstatus.message == status.message) return;
			
			// Stop timers
			if(newstatus.type != StatusType.Selection && !newstatus.displayed) //mxd
			{
				statusresetter.Stop();
				statusflasher.Stop();
				statusflashicon = false;
			}
			
			// Determine what to do specifically for this status type
			switch(newstatus.type)
			{
				// When no particular information is to be displayed.
				// The messages displayed depends on running background processes.
				case StatusType.Ready:
					if ((General.Map != null) && (General.Map.Data != null)) 
					{
						newstatus.message = General.Map.Data.IsLoading ? STATUS_LOADING_TEXT : selectionInfo;
					} 
					else 
					{
						newstatus.message = STATUS_READY_TEXT;
					}
					break;

				case StatusType.Selection: //mxd
					if (statusresetter.Enabled) //don't change the message right now if info or warning is displayed
					{ 
						selectionInfo = (string.IsNullOrEmpty(newstatus.message) ? STATUS_NO_SELECTION_TEXT : newstatus.message);
						return;
					}
					if(string.IsNullOrEmpty(newstatus.message))
						newstatus.message = STATUS_NO_SELECTION_TEXT;
					selectionInfo = newstatus.message;
					break;

				// Shows information without flashing the icon.
				case StatusType.Info:
					if(!newstatus.displayed)
					{
						statusresetter.Interval = INFO_RESET_DELAY;
						statusresetter.Start();
					}
					break;
					
				// Shows action information and flashes up the status icon once.	
				case StatusType.Action:
					if(!newstatus.displayed)
					{
						statusflashicon = true;
						statusflasher.Interval = ACTION_FLASH_INTERVAL;
						statusflashcount = ACTION_FLASH_COUNT;
						statusflasher.Start();
						statusresetter.Interval = ACTION_RESET_DELAY;
						statusresetter.Start();
					}
					break;
					
				// Shows a warning, makes a warning sound and flashes a warning icon.
				case StatusType.Warning:
					if(!newstatus.displayed)
					{
						MessageBeep(MessageBeepType.Warning);
						statusflasher.Interval = WARNING_FLASH_INTERVAL;
						statusflashcount = WARNING_FLASH_COUNT;
						statusflasher.Start();
						statusresetter.Interval = WARNING_RESET_DELAY;
						statusresetter.Start();
					}
					break;
			}
			
			// Update status description
			status = newstatus;
			status.displayed = true;
			if(statuslabel.Text != status.message)
				statuslabel.Text = status.message;
			
			// Update icon as well
			UpdateStatusIcon();
			
			// Refresh
			statusbar.Invalidate();
			this.Update();
		}
		
		// This changes status text to Ready
		public void DisplayReady()
		{
			DisplayStatus(StatusType.Ready, null);
		}
		
		// This updates the status icon
		internal void UpdateStatusIcon()
		{
			int statusicon = 0;
			int statusflashindex = statusflashicon ? 1 : 0;
			
			// Loading icon?
			if((General.Map != null) && (General.Map.Data != null) && General.Map.Data.IsLoading)
				statusicon = 1;
			
			// Status type
			switch(status.type)
			{
				case StatusType.Ready:
				case StatusType.Info:
				case StatusType.Action:
				case StatusType.Selection: //mxd
					statuslabel.Image = STATUS_IMAGES[statusflashindex, statusicon];
					break;
				
				case StatusType.Busy:
					statuslabel.Image = STATUS_IMAGES[statusflashindex, 2];
					break;
					
				case StatusType.Warning:
					statuslabel.Image = STATUS_IMAGES[statusflashindex, 3];
					break;
			}
		}
		
		// This changes coordinates display
		public void UpdateCoordinates(Vector2D coords)
		{
			// X position
			if(float.IsNaN(coords.x))
				xposlabel.Text = "--";
			else
				xposlabel.Text = coords.x.ToString("####0");

			// Y position
			if(float.IsNaN(coords.y))
				yposlabel.Text = "--";
			else
				yposlabel.Text = coords.y.ToString("####0");
			
			// Update status bar
			//statusbar.Update();
		}

		// This changes zoom display
		internal void UpdateZoom(float scale)
		{
			// Update scale label
			if(float.IsNaN(scale))
				zoomlabel.Text = "--";
			else
			{
				scale *= 100;
				zoomlabel.Text = scale.ToString("##0") + "%";
			}

			// Update status bar
			//statusbar.Update();
		}

		// Zoom to a specified level
		private void itemzoomto_Click(object sender, EventArgs e)
		{
			int zoom;

			if(General.Map == null) return;

			// In classic mode?
			if(General.Editing.Mode is ClassicMode)
			{
				// Requested from menu?
				if(sender is ToolStripMenuItem)
				{
					// Get integral zoom level
					zoom = int.Parse((sender as ToolStripMenuItem).Tag.ToString(), CultureInfo.InvariantCulture);

					// Zoom now
					(General.Editing.Mode as ClassicMode).SetZoom(zoom / 100f);
				}
			}
		}

		// Zoom to fit in screen
		private void itemzoomfittoscreen_Click(object sender, EventArgs e)
		{
			if(General.Map == null) return;
			
			// In classic mode?
			if(General.Editing.Mode is ClassicMode)
				(General.Editing.Mode as ClassicMode).CenterInScreen();
		}

		// This changes grid display
		internal void UpdateGrid(int gridsize)
		{
			// Update grid label
			if(gridsize == 0)
				gridlabel.Text = "--";
			else
				gridlabel.Text = gridsize.ToString("###0") + " mp";

			// Update status bar
			//statusbar.Update();
		}

		// Set grid to a specified size
		private void itemgridsize_Click(object sender, EventArgs e)
		{
			int size;

			if(General.Map == null) return;

			// In classic mode?
			if(General.Editing.Mode is ClassicMode)
			{
				// Requested from menu?
				if(sender is ToolStripMenuItem)
				{
					// Get integral zoom level
					size = int.Parse((sender as ToolStripMenuItem).Tag.ToString(), CultureInfo.InvariantCulture);

					// Change grid size
					General.Map.Grid.SetGridSize(size);
					
					// Redraw display
					RedrawDisplay();
				}
			}
		}

		// Show grid setup
		private void itemgridcustom_Click(object sender, EventArgs e)
		{
			if(General.Map == null) return;

			GridSetup.ShowGridSetup();
		}
		
		#endregion

		#region ================== Display

		// This shows the splash screen on display
		internal void ShowSplashDisplay()
		{
			// Change display to show splash logo
			display.SetSplashLogoDisplay();
			display.Cursor = Cursors.Default;
			this.Update();
		}
		
		// This clears the display
		internal void ClearDisplay()
		{
			// Clear the display
			display.SetManualRendering();
			this.Update();
		}

		// This sets the display cursor
		public void SetCursor(Cursor cursor)
		{
			// Only when a map is open
			if(General.Map != null) display.Cursor = cursor;
		}

		// This redraws the display on the next paint event
		public void RedrawDisplay()
		{
			if((General.Map != null) && (General.Editing.Mode != null))
			{
				General.Plugins.OnEditRedrawDisplayBegin();
				General.Editing.Mode.OnRedrawDisplay();
				General.Plugins.OnEditRedrawDisplayEnd();
				statistics.UpdateStatistics(); //mxd
			}
			else
			{
				display.Invalidate();
			}
		}

		// This event is called when a repaint is needed
		private void display_Paint(object sender, PaintEventArgs e)
		{
			if(General.Map != null)
			{
				if(General.Editing.Mode != null)
				{
					if(!displayresized) General.Editing.Mode.OnPresentDisplay();
				}
				else
				{
					if(General.Colors != null)
						e.Graphics.Clear(Color.FromArgb(General.Colors.Background.ToInt()));
					else
						e.Graphics.Clear(SystemColors.AppWorkspace);
				}
			}
		}
		
		// Redraw requested
		private void redrawtimer_Tick(object sender, EventArgs e)
		{
			// Disable timer (only redraw once)
			redrawtimer.Enabled = false;

			// Don't do anything when minimized (mxd)
			if(this.WindowState == FormWindowState.Minimized) return;

			// Resume control layouts
			//if(displayresized) General.LockWindowUpdate(IntPtr.Zero);

			// Map opened?
			if(General.Map != null)
			{
				// Display was resized?
				if(displayresized)
				{
					// Reset graphics to match changes
					General.Map.Graphics.Reset();
				}

				// This is a dirty trick to give the display a new mousemove event with correct arguments
				if(mouseinside)
				{
					Point mousepos = Cursor.Position;
					Cursor.Position = new Point(mousepos.X + 1, mousepos.Y + 1);
					Cursor.Position = mousepos;
				}
				
				// Redraw now
				RedrawDisplay();
			}

			// Display resize is done
			displayresized = false;
		}

		// Display size changes
		private void display_Resize(object sender, EventArgs e)
		{
			// Resizing
			//if(!displayresized) General.LockWindowUpdate(display.Handle);
			displayresized = true;
			
			// Request redraw
			if(!redrawtimer.Enabled) redrawtimer.Enabled = true;
		}
		
		// This requests a delayed redraw
		public void DelayedRedraw()
		{
			// Request redraw
			if(!redrawtimer.Enabled) redrawtimer.Enabled = true;
		}
		
		// Mouse click
		private void display_MouseClick(object sender, MouseEventArgs e)
		{
			if((General.Map != null) && (General.Editing.Mode != null))
			{
				General.Plugins.OnEditMouseClick(e);
				General.Editing.Mode.OnMouseClick(e);
			}
		}

		// Mouse doubleclick
		private void display_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if((General.Map != null) && (General.Editing.Mode != null))
			{
				General.Plugins.OnEditMouseDoubleClick(e);
				General.Editing.Mode.OnMouseDoubleClick(e);
			}
		}

		// Mouse down
		private void display_MouseDown(object sender, MouseEventArgs e)
		{
			int key = 0;
			
			LoseFocus(this, EventArgs.Empty);
			
			int mod = 0;
			if(alt) mod |= (int)Keys.Alt;
			if(shift) mod |= (int)Keys.Shift;
			if(ctrl) mod |= (int)Keys.Control;
			
			// Apply button
			mousebuttons |= e.Button;
			
			// Create key
			switch(e.Button)
			{
				case MouseButtons.Left: key = (int)Keys.LButton; break;
				case MouseButtons.Middle: key = (int)Keys.MButton; break;
				case MouseButtons.Right: key = (int)Keys.RButton; break;
				case MouseButtons.XButton1: key = (int)Keys.XButton1; break;
				case MouseButtons.XButton2: key = (int)Keys.XButton2; break;
			}
			
			// Invoke any actions associated with this key
			General.Actions.KeyPressed(key | mod);
			
			// Invoke on editing mode
			if((General.Map != null) && (General.Editing.Mode != null))
			{
				General.Plugins.OnEditMouseDown(e);
				General.Editing.Mode.OnMouseDown(e);
			}
		}

		// Mouse enters
		private void display_MouseEnter(object sender, EventArgs e)
		{
			mouseinside = true;
			if((General.Map != null) && (mouseinput == null) && (General.Editing.Mode != null))
			{
				General.Plugins.OnEditMouseEnter(e);
				General.Editing.Mode.OnMouseEnter(e);
				if(Application.OpenForms.Count == 1 || editformopen) display.Focus(); //mxd
			}
		}

		// Mouse leaves
		private void display_MouseLeave(object sender, EventArgs e)
		{
			mouseinside = false;
			if((General.Map != null) && (mouseinput == null) && (General.Editing.Mode != null))
			{
				General.Plugins.OnEditMouseLeave(e);
				General.Editing.Mode.OnMouseLeave(e);
			}
		}

		// Mouse moves
		private void display_MouseMove(object sender, MouseEventArgs e)
		{
			if((General.Map != null) && (mouseinput == null) && (General.Editing.Mode != null))
			{
				General.Plugins.OnEditMouseMove(e);
				General.Editing.Mode.OnMouseMove(e);
			}
		}

		// Mouse up
		private void display_MouseUp(object sender, MouseEventArgs e)
		{
			int key = 0;
			
			int mod = 0;
			if(alt) mod |= (int)Keys.Alt;
			if(shift) mod |= (int)Keys.Shift;
			if(ctrl) mod |= (int)Keys.Control;
			
			// Apply button
			mousebuttons &= ~e.Button;
			
			// Create key
			switch(e.Button)
			{
				case MouseButtons.Left: key = (int)Keys.LButton; break;
				case MouseButtons.Middle: key = (int)Keys.MButton; break;
				case MouseButtons.Right: key = (int)Keys.RButton; break;
				case MouseButtons.XButton1: key = (int)Keys.XButton1; break;
				case MouseButtons.XButton2: key = (int)Keys.XButton2; break;
			}
			
			// Invoke any actions associated with this key
			General.Actions.KeyReleased(key | mod);

			// Invoke on editing mode
			if((General.Map != null) && (General.Editing.Mode != null))
			{
				General.Plugins.OnEditMouseUp(e);
				General.Editing.Mode.OnMouseUp(e);
			}
		}
		
		#endregion

		#region ================== Input
		
		// This is a tool to lock the mouse in exclusive mode
		private void StartMouseExclusive()
		{
			// Not already locked?
			if(mouseinput == null)
			{
				// Start special input device
				mouseinput = new MouseInput(this);

				// Lock and hide the mouse in window
				originalclip = Cursor.Clip;
				Cursor.Clip = display.RectangleToScreen(display.ClientRectangle);
				Cursor.Hide();
			}
		}

		// This is a tool to unlock the mouse
		private void StopMouseExclusive()
		{
			// Locked?
			if(mouseinput != null)
			{
				// Stop special input device
				mouseinput.Dispose();
				mouseinput = null;

				// Release and show the mouse
				Cursor.Clip = originalclip;
				Cursor.Position = display.PointToScreen(new Point(display.ClientSize.Width / 2, display.ClientSize.Height / 2));
				Cursor.Show();
			}
		}
		
		// This requests exclusive mouse input
		public void StartExclusiveMouseInput()
		{
			// Only when not already in exclusive mode
			if(!mouseexclusive)
			{
				General.WriteLogLine("Starting exclusive mouse input mode...");
				
				// Start special input device
				StartMouseExclusive();
				mouseexclusive = true;
				mouseexclusivebreaklevel = 0;
			}
		}
		
		// This stops exclusive mouse input
		public void StopExclusiveMouseInput()
		{
			// Only when in exclusive mode
			if(mouseexclusive)
			{
				General.WriteLogLine("Stopping exclusive mouse input mode...");

				// Stop special input device
				StopMouseExclusive();
				mouseexclusive = false;
				mouseexclusivebreaklevel = 0;
			}
		}

		// This temporarely breaks exclusive mode and counts the break level
		public void BreakExclusiveMouseInput()
		{
			// Only when in exclusive mode
			if(mouseexclusive)
			{
				// Stop special input device
				StopMouseExclusive();
				
				// Count the break level
				mouseexclusivebreaklevel++;
			}
		}

		// This resumes exclusive mode from a break when all breaks have been called to resume
		public void ResumeExclusiveMouseInput()
		{
			// Only when in exclusive mode
			if(mouseexclusive && (mouseexclusivebreaklevel > 0))
			{
				// Decrease break level
				mouseexclusivebreaklevel--;

				// All break levels resumed? Then lock the mouse again.
				if(mouseexclusivebreaklevel == 0)
					StartMouseExclusive();
			}
		}

		// This releases all keys
		internal void ReleaseAllKeys()
		{
			General.Actions.ReleaseAllKeys();
			mousebuttons = MouseButtons.None;
			shift = false;
			ctrl = false;
			alt = false;
		}
		
		// When the mouse wheel is changed
		protected override void OnMouseWheel(MouseEventArgs e)
		{
			int mod = 0;
			if(alt) mod |= (int)Keys.Alt;
			if(shift) mod |= (int)Keys.Shift;
			if(ctrl) mod |= (int)Keys.Control;
			
			// Scrollwheel up?
			if(e.Delta > 0)
			{
				// Invoke actions for scrollwheel
				//for(int i = 0; i < e.Delta; i += 120)
				General.Actions.KeyPressed((int)SpecialKeys.MScrollUp | mod);
				General.Actions.KeyReleased((int)SpecialKeys.MScrollUp | mod);
			}
			// Scrollwheel down?
			else if(e.Delta < 0)
			{
				// Invoke actions for scrollwheel
				//for(int i = 0; i > e.Delta; i -= 120)
				General.Actions.KeyPressed((int)SpecialKeys.MScrollDown | mod);
				General.Actions.KeyReleased((int)SpecialKeys.MScrollDown | mod);
			}
			
			// Let the base know
			base.OnMouseWheel(e);
		}
		
		// When a key is pressed
		private void MainForm_KeyDown(object sender, KeyEventArgs e)
		{
			int mod = 0;
			
			// Keep key modifiers
			alt = e.Alt;
			shift = e.Shift;
			ctrl = e.Control;
			if(alt) mod |= (int)Keys.Alt;
			if(shift) mod |= (int)Keys.Shift;
			if(ctrl) mod |= (int)Keys.Control;
			
			// Don't process any keys when they are meant for other input controls
			if((ActiveControl == null) || (ActiveControl == display))
			{
				// Invoke any actions associated with this key
				General.Actions.UpdateModifiers(mod);
				e.Handled = General.Actions.KeyPressed((int)e.KeyData);
				
				// Invoke on editing mode
				if((General.Map != null) && (General.Editing.Mode != null))
				{
					General.Plugins.OnEditKeyDown(e);
					General.Editing.Mode.OnKeyDown(e);
				}

				// Handled
				if(e.Handled)
					e.SuppressKeyPress = true;
			}
			
			// F1 pressed?
			if((e.KeyCode == Keys.F1) && (e.Modifiers == Keys.None))
			{
				// No action bound to F1?
				Actions.Action[] f1actions = General.Actions.GetActionsByKey((int)e.KeyData);
				if(f1actions.Length == 0)
				{
					// If we don't have any map open, show the Main Window help
					// otherwise, give the help request to the editing mode so it
					// can open the appropriate help file.
					if((General.Map == null) || (General.Editing.Mode == null))
					{
						General.ShowHelp("introduction.html");
					}
					else
					{
						General.Editing.Mode.OnHelp();
					}
				}
			}
		}

		// When a key is released
		private void MainForm_KeyUp(object sender, KeyEventArgs e)
		{
			int mod = 0;
			
			// Keep key modifiers
			alt = e.Alt;
			shift = e.Shift;
			ctrl = e.Control;
			if(alt) mod |= (int)Keys.Alt;
			if(shift) mod |= (int)Keys.Shift;
			if(ctrl) mod |= (int)Keys.Control;
			
			// Don't process any keys when they are meant for other input controls
			if((ActiveControl == null) || (ActiveControl == display))
			{
				// Invoke any actions associated with this key
				General.Actions.UpdateModifiers(mod);
				e.Handled = General.Actions.KeyReleased((int)e.KeyData);
				
				// Invoke on editing mode
				if((General.Map != null) && (General.Editing.Mode != null))
				{
					General.Plugins.OnEditKeyUp(e);
					General.Editing.Mode.OnKeyUp(e);
				}
				
				// Handled
				if(e.Handled)
					e.SuppressKeyPress = true;
			}
		}

		//mxd. Sometimes it's handeled by RenderTargetControl, not by MainForm leading to keys being "stuck"
		private void display_OnKeyReleased(object sender, KeyEventArgs e)
		{
			MainForm_KeyUp(sender, e);
		}
		
		// These prevent focus changes by way of TAB or Arrow keys
		protected override bool IsInputChar(char charCode) { return false; }
		protected override bool IsInputKey(Keys keyData) { return false; }
		protected override bool ProcessKeyPreview(ref Message m) { return false; }
		protected override bool ProcessDialogKey(Keys keyData) { return false; }
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData) { return false; }
		
		// This fixes some odd input behaviour
		private void display_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if((ActiveControl == null) || (ActiveControl == display))
			{
				LoseFocus(this, EventArgs.Empty);
				KeyEventArgs ea = new KeyEventArgs(e.KeyData);
				MainForm_KeyDown(sender, ea);
			}
		}
		
		#endregion

		#region ================== Toolbar
		
		// This updates the skills list
		private void UpdateSkills()
		{
			// Clear list
			buttontest.DropDownItems.Clear();
			
			// Map loaded?
			if(General.Map != null)
			{
				// Make the new skills list
				skills = new ToolStripItem[(General.Map.Config.Skills.Count * 2) + 3]; //mxd

				//mxd. Add engine selector
				ToolStripMenuItem menuitem = new ToolStripMenuItem("Engine:", Resources.Marine);
				for (int i = 0; i < General.Map.ConfigSettings.TestEngines.Count; i++) 
				{
					ToolStripMenuItem engineItem = new ToolStripMenuItem(General.Map.ConfigSettings.TestEngines[i].TestProgramName);
					engineItem.Tag = i;
					engineItem.Checked = (i == General.Map.ConfigSettings.CurrentEngineIndex);
					engineItem.Click += engineItem_Click;
					menuitem.DropDownItems.Add(engineItem);
				}
				skills[0] = menuitem;
				
				//mxd. Add seperator
				skills[1] = new ToolStripSeparator();
				skills[1].Padding = new Padding(0, 3, 0, 3);
				int addindex = 2;
				
				// Positive skills are with monsters
				for(int i = 0; i < General.Map.Config.Skills.Count; i++)
				{
					menuitem = new ToolStripMenuItem(General.Map.Config.Skills[i].ToString());
					menuitem.Image = Resources.Monster2;
					menuitem.Click += TestSkill_Click;
					menuitem.Tag = General.Map.Config.Skills[i].Index;
					menuitem.Checked = (General.Settings.TestMonsters && (General.Map.ConfigSettings.TestSkill == General.Map.Config.Skills[i].Index));
					skills[addindex++] = menuitem;
				}

				// Add seperator
				skills[addindex] = new ToolStripSeparator();
				skills[addindex].Padding = new Padding(0, 3, 0, 3);
				addindex++;

				// Negative skills are without monsters
				for(int i = 0; i < General.Map.Config.Skills.Count; i++)
				{
					menuitem = new ToolStripMenuItem(General.Map.Config.Skills[i].ToString());
					menuitem.Image = Resources.Monster3;
					menuitem.Click += TestSkill_Click;
					menuitem.Tag = -General.Map.Config.Skills[i].Index;
					menuitem.Checked = (!General.Settings.TestMonsters && (General.Map.ConfigSettings.TestSkill == General.Map.Config.Skills[i].Index));
					skills[addindex++] = menuitem;
				}
				
				// Add to list
				buttontest.DropDownItems.AddRange(skills);
			}
		}

		//mxd
		private void engineItem_Click(object sender, EventArgs e)
		{
			General.Map.ConfigSettings.CurrentEngineIndex = (int)(((ToolStripMenuItem)sender).Tag);
			General.Map.ConfigSettings.Changed = true;
			UpdateSkills();
		}
		
		// Event handler for testing at a specific skill
		private void TestSkill_Click(object sender, EventArgs e)
		{
			int skill = (int)((sender as ToolStripMenuItem).Tag);
			General.Settings.TestMonsters = (skill > 0);
			General.Map.ConfigSettings.TestSkill = Math.Abs(skill);
			General.Map.Launcher.TestAtSkill(Math.Abs(skill));
			UpdateSkills();
		}
		
		// This loses focus
		private void LoseFocus(object sender, EventArgs e)
		{
			// Lose focus!
			try { display.Focus(); } catch(Exception) { }
			this.ActiveControl = null;
		}

		// Things filter selected
		private void thingfilters_SelectedIndexChanged(object sender, EventArgs e)
		{
			// Only possible when a map is open
			if((General.Map != null) && !updatingfilters)
			{
				updatingfilters = true;
				
				// Change filter
				General.Map.ChangeThingFilter(thingfilters.SelectedItem as ThingsFilter);

				updatingfilters = false;
			}
			
			// Lose focus
			if(!thingfilters.DroppedDown) LoseFocus(sender, e);
		}
		
		// This updates the things filter on the toolbar
		internal void UpdateThingsFilters()
		{
			// Only possible to list filters when a map is open
			if(General.Map != null)
			{
				ThingsFilter oldfilter = null;
				if(thingfilters.SelectedIndex > -1)
					oldfilter = thingfilters.SelectedItem as ThingsFilter;
				
				updatingfilters = true;

				// Clear the list
				thingfilters.Items.Clear();

				// Add null filter
				if(General.Map.ThingsFilter is NullThingsFilter)
					thingfilters.Items.Add(General.Map.ThingsFilter);
				else
					thingfilters.Items.Add(new NullThingsFilter());

				// Add all filters
				foreach(ThingsFilter f in General.Map.ConfigSettings.ThingsFilters)
					thingfilters.Items.Add(f);

				// Select current filter
				foreach(ThingsFilter f in thingfilters.Items)
					if(f == General.Map.ThingsFilter) thingfilters.SelectedItem = f;

				updatingfilters = false;
				
				// No filter selected?
				if(thingfilters.SelectedIndex == -1)
				{
					// Select the first and update
					thingfilters.SelectedIndex = 0;
				}
				// Another filter got selected?
				else if(oldfilter != (thingfilters.SelectedItem as ThingsFilter))
				{
					// Update!
					thingfilters_SelectedIndexChanged(this, EventArgs.Empty);
				}
			}
			else
			{
				// Clear the list
				thingfilters.Items.Clear();
			}
		}

		// This selects the things filter based on the filter set on the map manager
		internal void ReflectThingsFilter()
		{
			if(!updatingfilters)
			{
				updatingfilters = true;
				
				// Select current filter
				bool selecteditemfound = false;
				foreach(ThingsFilter f in thingfilters.Items)
				{
					if(f == General.Map.ThingsFilter)
					{
						thingfilters.SelectedItem = f;
						selecteditemfound = true;
					}
				}

				// Not in the list?
				if(!selecteditemfound)
				{
					// Select nothing
					thingfilters.SelectedIndex = -1;
				}

				updatingfilters = false;
			}
		}
		
		// This adds a button to the toolbar
		public void AddButton(ToolStripItem button) { AddButton(button, ToolbarSection.Custom, General.Plugins.FindPluginByAssembly(Assembly.GetCallingAssembly())); }
		public void AddButton(ToolStripItem button, ToolbarSection section) { AddButton(button, section, General.Plugins.FindPluginByAssembly(Assembly.GetCallingAssembly())); }
		private void AddButton(ToolStripItem button, ToolbarSection section, Plugin plugin)
		{
			// Fix tags to full action names
			ToolStripItemCollection items = new ToolStripItemCollection(toolbar, new ToolStripItem[0]);
			items.Add(button);
			RenameTagsToFullActions(items, plugin);

			// Add to the list so we can update it as needed
			PluginToolbarButton buttoninfo = new PluginToolbarButton();
			buttoninfo.button = button;
			buttoninfo.section = section;
			pluginbuttons.Add(buttoninfo);
			
			// Bind visible changed event
			if(!(button is ToolStripSeparator)) button.VisibleChanged += buttonvisiblechangedhandler;
			
			// Insert the button in the right section
			switch(section)
			{
				case ToolbarSection.File: toolbar.Items.Insert(toolbar.Items.IndexOf(seperatorfile), button); break;
				case ToolbarSection.Script: toolbar.Items.Insert(toolbar.Items.IndexOf(seperatorscript), button); break;
				case ToolbarSection.UndoRedo: toolbar.Items.Insert(toolbar.Items.IndexOf(seperatorundo), button); break;
				case ToolbarSection.CopyPaste: toolbar.Items.Insert(toolbar.Items.IndexOf(seperatorcopypaste), button); break;
				case ToolbarSection.Prefabs: toolbar.Items.Insert(toolbar.Items.IndexOf(seperatorprefabs), button); break;
				case ToolbarSection.Things: toolbar.Items.Insert(toolbar.Items.IndexOf(buttonviewnormal), button); break;
				case ToolbarSection.Views: toolbar.Items.Insert(toolbar.Items.IndexOf(seperatorviews), button); break;
				case ToolbarSection.Geometry: toolbar.Items.Insert(toolbar.Items.IndexOf(seperatorgeometry), button); break;
				case ToolbarSection.Testing: toolbar.Items.Insert(toolbar.Items.IndexOf(seperatortesting), button); break;
				case ToolbarSection.Modes: modestoolbar.Items.Add(button); break; //mxd
				case ToolbarSection.Custom: modecontrolsloolbar.Items.Add(button); modecontrolsloolbar.Visible = true; break; //mxd
			}
			
			UpdateToolbar();
		}

		//mxd
		public void AddModesButton(ToolStripItem button, string group) 
		{
			// Set proper styling
			button.Padding = new Padding(0, 1, 0, 1);
			button.Margin = new Padding();
			
			// Fix tags to full action names
			ToolStripItemCollection items = new ToolStripItemCollection(toolbar, new ToolStripItem[0]);
			items.Add(button);
			RenameTagsToFullActions(items, General.Plugins.FindPluginByAssembly(Assembly.GetCallingAssembly()));

			// Add to the list so we can update it as needed
			PluginToolbarButton buttoninfo = new PluginToolbarButton();
			buttoninfo.button = button;
			buttoninfo.section = ToolbarSection.Modes;
			pluginbuttons.Add(buttoninfo);

			button.VisibleChanged += buttonvisiblechangedhandler;

			//find the separator we need
			for(int i = 0; i < modestoolbar.Items.Count; i++) 
			{
				if(modestoolbar.Items[i] is ToolStripSeparator && modestoolbar.Items[i].Text == group) 
				{
					modestoolbar.Items.Insert(i + 1, button);
					break;
				}
			}

			UpdateToolbar();
		}

		// Removes a button
		public void RemoveButton(ToolStripItem button)
		{
			// Find in the list and remove it
			PluginToolbarButton buttoninfo = new PluginToolbarButton();
			for(int i = 0; i < pluginbuttons.Count; i++)
			{
				if(pluginbuttons[i].button == button)
				{
					buttoninfo = pluginbuttons[i];
					pluginbuttons.RemoveAt(i);
					break;
				}
			}

			if(buttoninfo.button != null)
			{
				// Unbind visible changed event
				if(!(button is ToolStripSeparator)) button.VisibleChanged -= buttonvisiblechangedhandler;

				//mxd. Remove button from toolbars
				switch (buttoninfo.section) 
				{
					case ToolbarSection.Modes:
						modestoolbar.Items.Remove(button);
						break;
					case ToolbarSection.Custom:
						modecontrolsloolbar.Items.Remove(button);
						modecontrolsloolbar.Visible = (modecontrolsloolbar.Items.Count > 0);
						break;
					default:
						toolbar.Items.Remove(button);
						break;
				}
				
				UpdateSeparators();
			}
		}

		// This handle visibility changes in the toolbar buttons
		private void ToolbarButtonVisibleChanged(object sender, EventArgs e)
		{
			if(!preventupdateseperators)
			{
				// Update the seeprators
				UpdateSeparators();
			}
		}

		// This hides redundant separators
		internal void UpdateSeparators()
		{
			UpdateToolStripSeparators(toolbar.Items, false);
			UpdateToolStripSeparators(menumode.DropDownItems, true);

			//mxd
			UpdateToolStripSeparators(modestoolbar.Items, true);
			UpdateToolStripSeparators(modecontrolsloolbar.Items, true);
		}
		
		// This hides redundant separators
		private static void UpdateToolStripSeparators(ToolStripItemCollection items, bool defaultvisible)
		{
			ToolStripItem pvi = null;
			foreach(ToolStripItem i in items) 
			{
				bool separatorvisible = false;

				// This is a seperator?
				if(i is ToolStripSeparator) 
				{
					// Make visible when previous item was not a seperator
					separatorvisible = !(pvi is ToolStripSeparator) && (pvi != null);
					i.Visible = separatorvisible;
				}

				// Keep as previous visible item
				if(i.Visible || separatorvisible || (defaultvisible && !(i is ToolStripSeparator))) pvi = i;
			}

			// Hide last item if it is a seperator
			if(pvi is ToolStripSeparator) pvi.Visible = false;
		}
		
		// This enables or disables all editing mode items and toolbar buttons
		private void UpdateToolbar()
		{
			preventupdateseperators = true;
			
			// Show/hide items based on preferences
			bool maploaded = General.Map != null; //mxd
			buttonnewmap.Visible = General.Settings.ToolbarFile;
			buttonopenmap.Visible = General.Settings.ToolbarFile;
			buttonsavemap.Visible = General.Settings.ToolbarFile;
			buttonscripteditor.Visible = General.Settings.ToolbarScript && maploaded;
			buttonundo.Visible = General.Settings.ToolbarUndo && maploaded;
			buttonredo.Visible = General.Settings.ToolbarUndo && maploaded;
			buttoncut.Visible = General.Settings.ToolbarCopy && maploaded;
			buttoncopy.Visible = General.Settings.ToolbarCopy && maploaded;
			buttonpaste.Visible = General.Settings.ToolbarCopy && maploaded;
			buttoninsertprefabfile.Visible = General.Settings.ToolbarPrefabs && maploaded;
			buttoninsertpreviousprefab.Visible = General.Settings.ToolbarPrefabs && maploaded;
			buttonthingsfilter.Visible = General.Settings.ToolbarFilter && maploaded;
			thingfilters.Visible = General.Settings.ToolbarFilter && maploaded;
			separatorfilters.Visible = General.Settings.ToolbarViewModes && maploaded; //mxd
			buttonfullbrightness.Visible = General.Settings.ToolbarViewModes && maploaded; //mxd
			separatorfullbrightness.Visible = General.Settings.ToolbarViewModes && maploaded; //mxd
			buttonviewbrightness.Visible = General.Settings.ToolbarViewModes && maploaded;
			buttonviewceilings.Visible = General.Settings.ToolbarViewModes && maploaded;
			buttonviewfloors.Visible = General.Settings.ToolbarViewModes && maploaded;
			buttonviewnormal.Visible = General.Settings.ToolbarViewModes && maploaded;
			buttonsnaptogrid.Visible = General.Settings.ToolbarGeometry && maploaded;
			buttonautomerge.Visible = General.Settings.ToolbarGeometry && maploaded;
			buttonautoclearsidetextures.Visible = General.Settings.ToolbarGeometry && maploaded; //mxd
			buttontest.Visible = General.Settings.ToolbarTesting && maploaded;

			//mxd
			modelrendermode.Visible = General.Settings.GZToolbarGZDoom && maploaded;
			dynamiclightmode.Visible = General.Settings.GZToolbarGZDoom && maploaded;
			buttontogglefx.Visible = General.Settings.GZToolbarGZDoom && maploaded;
			buttontogglefog.Visible = General.Settings.GZToolbarGZDoom && maploaded;
			buttontoggleeventlines.Visible = General.Settings.GZToolbarGZDoom && maploaded;
			buttontogglevisualvertices.Visible = General.Settings.GZToolbarGZDoom && maploaded;
			separatorgzmodes.Visible = General.Settings.GZToolbarGZDoom && maploaded;

			// Enable/disable all edit mode items
			//foreach(ToolStripItem i in editmodeitems) i.Enabled = (General.Map != null);

			//mxd. Show/hide additional panels
			modestoolbar.Visible = (General.Map != null);
			panelinfo.Visible = (General.Map != null);
			modecontrolsloolbar.Visible = (General.Map != null && modecontrolsloolbar.Items.Count > 0);
			
			//mxd. modestoolbar index in Controls gets messed up when it's invisible. This fixes it. TODO: find why this happens in the first place
			if(modestoolbar.Visible) 
			{
				int toolbarpos = this.Controls.IndexOf(toolbar);
				if(this.Controls.IndexOf(modestoolbar) > toolbarpos) 
				{
					this.Controls.SetChildIndex(modestoolbar, toolbarpos);
				}
			}

			// Update plugin buttons
			foreach(PluginToolbarButton p in pluginbuttons)
			{
				switch(p.section)
				{
					case ToolbarSection.File: p.button.Visible = General.Settings.ToolbarFile; break;
					case ToolbarSection.Script: p.button.Visible = General.Settings.ToolbarScript; break;
					case ToolbarSection.UndoRedo: p.button.Visible = General.Settings.ToolbarUndo; break;
					case ToolbarSection.CopyPaste: p.button.Visible = General.Settings.ToolbarCopy; break;
					case ToolbarSection.Prefabs: p.button.Visible = General.Settings.ToolbarPrefabs; break;
					case ToolbarSection.Things: p.button.Visible = General.Settings.ToolbarFilter; break;
					case ToolbarSection.Views: p.button.Visible = General.Settings.ToolbarViewModes; break;
					case ToolbarSection.Geometry: p.button.Visible = General.Settings.ToolbarGeometry; break;
					case ToolbarSection.Testing: p.button.Visible = General.Settings.ToolbarTesting; break;
				}
			}

			preventupdateseperators = false;

			UpdateSeparators();
		}

		// This checks one of the edit mode items (and unchecks all others)
		internal void CheckEditModeButton(string modeclassname)
		{
			// Go for all items
			foreach(ToolStripItem i in editmodeitems)
			{
				// Check what type it is
				if(i is ToolStripMenuItem)
				{
					// Check if mode type matches with given name
					(i as ToolStripMenuItem).Checked = ((i.Tag as EditModeInfo).Type.Name == modeclassname);
				}
				else if(i is ToolStripButton)
				{
					// Check if mode type matches with given name
					(i as ToolStripButton).Checked = ((i.Tag as EditModeInfo).Type.Name == modeclassname);
				}
			}
		}
		
		// This removes the config-specific editing mode buttons
		internal void RemoveEditModeButtons()
		{
			// Go for all items
			foreach(ToolStripItem i in editmodeitems)
			{
				// Remove it and restart
				menumode.DropDownItems.Remove(i);
				i.Dispose();
			}
			
			// Done
			modestoolbar.Items.Clear(); //mxd
			editmodeitems.Clear();
			UpdateSeparators();
		}
		
		// This adds an editing mode seperator on the toolbar and menu
		internal void AddEditModeSeperator(string group)
		{
			// Create a button
			ToolStripSeparator item = new ToolStripSeparator();
			item.Text = group; //mxd
			item.Margin = new Padding(0, 3, 0, 3); //mxd
			modestoolbar.Items.Add(item); //mxd
			editmodeitems.Add(item);
			
			// Create menu item
			int index = menumode.DropDownItems.Count;
			item = new ToolStripSeparator();
			item.Text = group; //mxd
			item.Margin = new Padding(0, 3, 0, 3);
			menumode.DropDownItems.Insert(index, item);
			editmodeitems.Add(item);
			
			UpdateSeparators();
		}
		
		// This adds an editing mode button to the toolbar and edit menu
		internal void AddEditModeButton(EditModeInfo modeinfo)
		{
			string controlname = modeinfo.ButtonDesc.Replace("&", "&&");
			
			// Create a button
			ToolStripItem item = new ToolStripButton(modeinfo.ButtonDesc, modeinfo.ButtonImage, EditModeButtonHandler);
			item.DisplayStyle = ToolStripItemDisplayStyle.Image;
			item.Padding = new Padding(0, 2, 0, 2);
			item.Margin = new Padding();
			item.Tag = modeinfo;
			modestoolbar.Items.Add(item); //mxd
			editmodeitems.Add(item);
			
			// Create menu item
			int index = menumode.DropDownItems.Count;
			item = new ToolStripMenuItem(controlname, modeinfo.ButtonImage, EditModeButtonHandler);
			item.Tag = modeinfo;
			menumode.DropDownItems.Insert(index, item);
			editmodeitems.Add(item);
			item.Visible = true;
			
			ApplyShortcutKeys(menumode.DropDownItems);
			UpdateSeparators();
		}

		// This handles edit mode button clicks
		private void EditModeButtonHandler(object sender, EventArgs e)
		{
			this.Update();
			EditModeInfo modeinfo = (EditModeInfo)((sender as ToolStripItem).Tag);
			General.Actions.InvokeAction(modeinfo.SwitchAction.GetFullActionName(modeinfo.Plugin.Assembly));
			this.Update();
		}

		//mxd
		public void UpdateGZDoomPanel() 
		{
			if (General.Map != null) 
			{
				modelrendermode.Enabled = true;
				dynamiclightmode.Enabled = true;
				buttontogglefog.Enabled = true;
				buttontogglefx.Enabled = true;
				buttontoggleeventlines.Enabled = true;
				buttontogglevisualvertices.Enabled = General.Map.UDMF;

				if (General.Settings.GZToolbarGZDoom) 
				{
					foreach(ToolStripMenuItem item in modelrendermode.DropDownItems)
					{
						item.Checked = ((ModelRenderMode)item.Tag == General.Settings.GZDrawModelsMode);
						if (item.Checked) modelrendermode.Image = item.Image;
					}
					foreach(ToolStripMenuItem item in dynamiclightmode.DropDownItems)
					{
						item.Checked = ((LightRenderMode)item.Tag == General.Settings.GZDrawLightsMode);
						if(item.Checked) dynamiclightmode.Image = item.Image;
					}
					
					buttontogglefog.Checked = General.Settings.GZDrawFog;
					buttontoggleeventlines.Checked = General.Settings.GZShowEventLines;
					buttontogglevisualvertices.Checked = General.Settings.GZShowVisualVertices;
				}
			} 
			else
			{
				modelrendermode.Enabled = false;
				dynamiclightmode.Enabled = false;
				buttontogglefog.Enabled = false;
				buttontogglefx.Enabled = false;
				buttontoggleeventlines.Enabled = false;
				buttontogglevisualvertices.Enabled = false;
			}
		}

		#endregion

		#region ================== Toolbar context menu (mxd)

		private void toolbarContextMenu_Opening(object sender, CancelEventArgs e)
		{
			if(General.Map == null)
			{
				e.Cancel = true;
				return;
			}

			toggleFile.Image = General.Settings.ToolbarFile ? Resources.Check : null;
			toggleScript.Image = General.Settings.ToolbarScript ? Resources.Check : null;
			toggleUndo.Image = General.Settings.ToolbarUndo ? Resources.Check : null;
			toggleCopy.Image = General.Settings.ToolbarCopy ? Resources.Check : null;
			togglePrefabs.Image = General.Settings.ToolbarPrefabs ? Resources.Check : null;
			toggleFilter.Image = General.Settings.ToolbarFilter ? Resources.Check : null;
			toggleViewModes.Image = General.Settings.ToolbarViewModes ? Resources.Check : null;
			toggleGeometry.Image = General.Settings.ToolbarGeometry ? Resources.Check : null;
			toggleTesting.Image = General.Settings.ToolbarTesting ? Resources.Check : null;
			toggleRendering.Image = General.Settings.GZToolbarGZDoom ? Resources.Check : null;
		}

		private void toolbarContextMenu_Closing(object sender, ToolStripDropDownClosingEventArgs e) 
		{
			e.Cancel = (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked && toolbarContextMenuShiftPressed);
		}

		private void toolbarContextMenu_KeyDown(object sender, KeyEventArgs e) 
		{
			toolbarContextMenuShiftPressed = (e.KeyCode == Keys.ShiftKey);
		}

		private void toolbarContextMenu_KeyUp(object sender, KeyEventArgs e) 
		{
			toolbarContextMenuShiftPressed = (e.KeyCode != Keys.ShiftKey);
		}

		private void toggleFile_Click(object sender, EventArgs e) 
		{
			General.Settings.ToolbarFile = !General.Settings.ToolbarFile;
			UpdateToolbar();

			if(toolbarContextMenuShiftPressed) 
				toggleFile.Image = General.Settings.ToolbarFile ? Resources.Check : null;
		}

		private void toggleScript_Click(object sender, EventArgs e) 
		{
			General.Settings.ToolbarScript = !General.Settings.ToolbarScript;
			UpdateToolbar();

			if(toolbarContextMenuShiftPressed) 
				toggleScript.Image = General.Settings.ToolbarScript ? Resources.Check : null;
		}

		private void toggleUndo_Click(object sender, EventArgs e) 
		{
			General.Settings.ToolbarUndo = !General.Settings.ToolbarUndo;
			UpdateToolbar();

			if(toolbarContextMenuShiftPressed) 
				toggleUndo.Image = General.Settings.ToolbarUndo ? Resources.Check : null;
		}

		private void toggleCopy_Click(object sender, EventArgs e) 
		{
			General.Settings.ToolbarCopy = !General.Settings.ToolbarCopy;
			UpdateToolbar();

			if(toolbarContextMenuShiftPressed) 
				toggleCopy.Image = General.Settings.ToolbarCopy ? Resources.Check : null;
		}

		private void togglePrefabs_Click(object sender, EventArgs e) 
		{
			General.Settings.ToolbarPrefabs = !General.Settings.ToolbarPrefabs;
			UpdateToolbar();

			if(toolbarContextMenuShiftPressed) 
				togglePrefabs.Image = General.Settings.ToolbarPrefabs ? Resources.Check : null;
		}

		private void toggleFilter_Click(object sender, EventArgs e) 
		{
			General.Settings.ToolbarFilter = !General.Settings.ToolbarFilter;
			UpdateToolbar();

			if(toolbarContextMenuShiftPressed) 
				toggleFilter.Image = General.Settings.ToolbarFilter ? Resources.Check : null;
		}

		private void toggleViewModes_Click(object sender, EventArgs e) 
		{
			General.Settings.ToolbarViewModes = !General.Settings.ToolbarViewModes;
			UpdateToolbar();

			if(toolbarContextMenuShiftPressed) 
				toggleViewModes.Image = General.Settings.ToolbarViewModes ? Resources.Check : null;
		}

		private void toggleGeometry_Click(object sender, EventArgs e) 
		{
			General.Settings.ToolbarGeometry = !General.Settings.ToolbarGeometry;
			UpdateToolbar();

			if(toolbarContextMenuShiftPressed) 
				toggleGeometry.Image = General.Settings.ToolbarGeometry ? Resources.Check : null;
		}

		private void toggleTesting_Click(object sender, EventArgs e) 
		{
			General.Settings.ToolbarTesting = !General.Settings.ToolbarTesting;
			UpdateToolbar();

			if(toolbarContextMenuShiftPressed) 
				toggleTesting.Image = General.Settings.ToolbarTesting ? Resources.Check : null;
		}

		private void toggleRendering_Click(object sender, EventArgs e) 
		{
			General.Settings.GZToolbarGZDoom = !General.Settings.GZToolbarGZDoom;
			UpdateToolbar();

			if(toolbarContextMenuShiftPressed) 
				toggleRendering.Image = General.Settings.GZToolbarGZDoom ? Resources.Check : null;
		}

		#endregion

		#region ================== Menus

		// This adds a menu to the menus bar
		public void AddMenu(ToolStripMenuItem menu) { AddMenu(menu, MenuSection.Top, General.Plugins.FindPluginByAssembly(Assembly.GetCallingAssembly())); }
		public void AddMenu(ToolStripMenuItem menu, MenuSection section) { AddMenu(menu, section, General.Plugins.FindPluginByAssembly(Assembly.GetCallingAssembly())); }
		private void AddMenu(ToolStripMenuItem menu, MenuSection section, Plugin plugin)
		{
			// Fix tags to full action names
			ToolStripItemCollection items = new ToolStripItemCollection(this.menumain, new ToolStripItem[0]);
			items.Add(menu);
			RenameTagsToFullActions(items, plugin);
			
			// Insert the menu in the right location
			switch(section)
			{
				case MenuSection.FileNewOpenClose: menufile.DropDownItems.Insert(menufile.DropDownItems.IndexOf(seperatorfileopen), menu); break;
				case MenuSection.FileSave: menufile.DropDownItems.Insert(menufile.DropDownItems.IndexOf(seperatorfilesave), menu); break;
				case MenuSection.FileImport: itemimport.DropDownItems.Add(menu); break; //mxd
				case MenuSection.FileExport: itemexport.DropDownItems.Add(menu); break; //mxd
				case MenuSection.FileRecent: menufile.DropDownItems.Insert(menufile.DropDownItems.IndexOf(seperatorfilerecent), menu); break;
				case MenuSection.FileExit: menufile.DropDownItems.Insert(menufile.DropDownItems.IndexOf(itemexit), menu); break;
				case MenuSection.EditUndoRedo: menuedit.DropDownItems.Insert(menuedit.DropDownItems.IndexOf(seperatoreditundo), menu); break;
				case MenuSection.EditCopyPaste: menuedit.DropDownItems.Insert(menuedit.DropDownItems.IndexOf(seperatoreditcopypaste), menu); break;
				case MenuSection.EditGeometry: menuedit.DropDownItems.Insert(menuedit.DropDownItems.IndexOf(seperatoreditgeometry), menu); break;
				case MenuSection.EditGrid: menuedit.DropDownItems.Insert(menuedit.DropDownItems.IndexOf(seperatoreditgrid), menu); break;
				case MenuSection.EditMapOptions: menuedit.DropDownItems.Add(menu); break;
				case MenuSection.ViewThings: menuview.DropDownItems.Insert(menuview.DropDownItems.IndexOf(seperatorviewthings), menu); break;
				case MenuSection.ViewViews: menuview.DropDownItems.Insert(menuview.DropDownItems.IndexOf(seperatorviewviews), menu); break;
				case MenuSection.ViewZoom: menuview.DropDownItems.Insert(menuview.DropDownItems.IndexOf(seperatorviewzoom), menu); break;
				case MenuSection.ViewScriptEdit: menuview.DropDownItems.Add(menu); break;
				case MenuSection.PrefabsInsert: menuprefabs.DropDownItems.Insert(menuprefabs.DropDownItems.IndexOf(seperatorprefabsinsert), menu); break;
				case MenuSection.PrefabsCreate: menuprefabs.DropDownItems.Add(menu); break;
				case MenuSection.ToolsResources: menutools.DropDownItems.Insert(menutools.DropDownItems.IndexOf(seperatortoolsresources), menu); break;
				case MenuSection.ToolsConfiguration: menutools.DropDownItems.Insert(menutools.DropDownItems.IndexOf(seperatortoolsconfig), menu); break;
				case MenuSection.ToolsTesting: menutools.DropDownItems.Add(menu); break;
				case MenuSection.HelpManual: menuhelp.DropDownItems.Insert(menuhelp.DropDownItems.IndexOf(seperatorhelpmanual), menu); break;
				case MenuSection.HelpAbout: menuhelp.DropDownItems.Add(menu); break;
				case MenuSection.Top: menumain.Items.Insert(menumain.Items.IndexOf(menutools), menu); break;
			}
			
			ApplyShortcutKeys(items);
		}

		//mxd
		public void AddModesMenu(ToolStripMenuItem menu, string group) 
		{
			// Fix tags to full action names
			ToolStripItemCollection items = new ToolStripItemCollection(this.menumain, new ToolStripItem[0]);
			items.Add(menu);
			RenameTagsToFullActions(items, General.Plugins.FindPluginByAssembly(Assembly.GetCallingAssembly()));
			
			//find the separator we need
			for(int i = 0; i < menumode.DropDownItems.Count; i++) 
			{
				if(menumode.DropDownItems[i] is ToolStripSeparator && menumode.DropDownItems[i].Text == group) 
				{
					menumode.DropDownItems.Insert(i + 1, menu);
					break;
				}
			}

			ApplyShortcutKeys(items);
		}
		
		// Removes a menu
		public void RemoveMenu(ToolStripMenuItem menu)
		{
			// We actually have no idea in which menu this item is,
			// so try removing from all menus and the top strip
			menufile.DropDownItems.Remove(menu);
			menuedit.DropDownItems.Remove(menu);
			menumode.DropDownItems.Remove(menu); //mxd
			menuview.DropDownItems.Remove(menu);
			menuprefabs.DropDownItems.Remove(menu);
			menutools.DropDownItems.Remove(menu);
			menuhelp.DropDownItems.Remove(menu);
			menumain.Items.Remove(menu);
		}
		
		// Public method to apply shortcut keys
		internal void ApplyShortcutKeys()
		{
			// Apply shortcut keys to menus
			ApplyShortcutKeys(menumain.Items);
		}
		
		// This sets the shortcut keys on menu items
		private static void ApplyShortcutKeys(ToolStripItemCollection items)
		{
			// Go for all controls to find menu items
			foreach(ToolStripItem item in items)
			{
				// This is a menu item?
				if(item is ToolStripMenuItem)
				{
					// Get the item in proper type
					ToolStripMenuItem menuitem = (item as ToolStripMenuItem);

					// Tag set for this item?
					if(menuitem.Tag is string)
					{
						// Action with this name available?
						string actionname = menuitem.Tag.ToString();
						if(General.Actions.Exists(actionname))
						{
							// Put the action shortcut key on the menu item
							menuitem.ShortcutKeyDisplayString = Actions.Action.GetShortcutKeyDesc(General.Actions[actionname].ShortcutKey);
						}
					}
					// Edit mode info set for this item?
					else if(menuitem.Tag is EditModeInfo)
					{
						// Action with this name available?
						EditModeInfo modeinfo = (menuitem.Tag as EditModeInfo);
						string actionname = modeinfo.SwitchAction.GetFullActionName(modeinfo.Plugin.Assembly);
						if(General.Actions.Exists(actionname))
						{
							// Put the action shortcut key on the menu item
							menuitem.ShortcutKeyDisplayString = Actions.Action.GetShortcutKeyDesc(General.Actions[actionname].ShortcutKey);
						}
					}

					// Recursively apply shortcut keys to child menu items as well
					ApplyShortcutKeys(menuitem.DropDownItems);
				}
			}
		}

		// This fixes short action names to fully qualified
		// action names on menu item tags
		private static void RenameTagsToFullActions(ToolStripItemCollection items, Plugin plugin)
		{
			// Go for all controls to find menu items
			foreach(ToolStripItem item in items)
			{
				// Tag set for this item?
				if(item.Tag is string)
				{
					// Check if the tag doe not already begin with the assembly name
					if(!(item.Tag as string).StartsWith(plugin.Name + "_", StringComparison.InvariantCultureIgnoreCase))
					{
						// Change the tag to a fully qualified action name
						item.Tag = plugin.Name.ToLowerInvariant() + "_" + (item.Tag as string);
					}
				}

				// This is a menu item?
				if(item is ToolStripMenuItem)
				{
					// Get the item in proper type
					ToolStripMenuItem menuitem = (item as ToolStripMenuItem);
					
					// Recursively perform operation on child menu items
					RenameTagsToFullActions(menuitem.DropDownItems, plugin);
				}
			}
		}
		
		#endregion

		#region ================== File Menu

		// This sets up the file menu
		private void UpdateFileMenu()
		{
			// Enable/disable items
			itemclosemap.Enabled = (General.Map != null);
			itemsavemap.Enabled = (General.Map != null);
			itemsavemapas.Enabled = (General.Map != null);
			itemsavemapinto.Enabled = (General.Map != null);
			itemtestmap.Enabled = (General.Map != null);
			itemopenmapincurwad.Enabled = (General.Map != null); //mxd
			itemimport.Enabled = (General.Map != null); //mxd
			itemexport.Enabled = (General.Map != null); //mxd

			// Toolbar icons
			buttonnewmap.Enabled = itemnewmap.Enabled;
			buttonopenmap.Enabled = itemopenmap.Enabled;
			buttonsavemap.Enabled = itemsavemap.Enabled;
		}

		// This sets the recent files from configuration
		private void CreateRecentFiles()
		{
			bool anyitems = false;
			string filename;
			
			// Where to insert
			int insertindex = menufile.DropDownItems.IndexOf(itemnorecent);
			
			// Create all items
			recentitems = new ToolStripMenuItem[General.Settings.MaxRecentFiles];
			for(int i = 0; i < General.Settings.MaxRecentFiles; i++)
			{
				// Create item
				recentitems[i] = new ToolStripMenuItem("");
				recentitems[i].Tag = "";
				recentitems[i].Click += recentitem_Click;
				menufile.DropDownItems.Insert(insertindex + i, recentitems[i]);

				// Get configuration setting
				filename = General.Settings.ReadSetting("recentfiles.file" + i, "");
				if(filename != "" && File.Exists(filename))
				{
					// Set up item
					int number = i + 1;
					recentitems[i].Text = "&" + number + "  " + GetDisplayFilename(filename);
					recentitems[i].Tag = filename;
					recentitems[i].Visible = true;
					anyitems = true;
				}
				else
				{
					// Hide item
					recentitems[i].Visible = false;
				}
			}

			// Hide the no recent item when there are items
			itemnorecent.Visible = !anyitems;
		}
		
		// This saves the recent files list
		private void SaveRecentFiles()
		{
			// Go for all items
			for(int i = 0; i < recentitems.Length; i++)
			{
				// Recent file set?
				if(recentitems[i].Text != "")
				{
					// Save to configuration
					General.Settings.WriteSetting("recentfiles.file" + i, recentitems[i].Tag.ToString());
				}
			}
		}
		
		// This adds a recent file to the list
		internal void AddRecentFile(string filename)
		{
			//mxd. Recreate recent files list
			if (recentitems.Length != General.Settings.MaxRecentFiles) 
			{
				foreach(ToolStripMenuItem item in recentitems)
					menufile.DropDownItems.Remove(item);

				SaveRecentFiles();
				CreateRecentFiles();
			}

			int movedownto = General.Settings.MaxRecentFiles - 1;
			
			// Check if this file is already in the list
			for(int i = 0; i < General.Settings.MaxRecentFiles; i++)
			{
				// File same as this item?
				if(string.Compare(filename, recentitems[i].Tag.ToString(), true) == 0)
				{
					// Move down to here so that this item will disappear
					movedownto = i;
					break;
				}
			}
			
			// Go for all items, except the last one, backwards
			for(int i = movedownto - 1; i >= 0; i--)
			{
				// Move recent file down the list
				int number = i + 2;
				recentitems[i + 1].Text = "&" + number + "  " + GetDisplayFilename(recentitems[i].Tag.ToString());
				recentitems[i + 1].Tag = recentitems[i].Tag.ToString();
				recentitems[i + 1].Visible = (recentitems[i].Tag.ToString() != "");
			}

			// Add new file at the top
			recentitems[0].Text = "&1  " + GetDisplayFilename(filename);
			recentitems[0].Tag = filename;
			recentitems[0].Visible = true;

			// Hide the no recent item
			itemnorecent.Visible = false;
		}

		// This returns the trimmed file/path string
		private string GetDisplayFilename(string filename)
		{
			string newname;
			
			// String doesnt fit?
			if(GetStringWidth(filename) > MAX_RECENT_FILES_PIXELS)
			{
				// Start chopping off characters
				for(int i = filename.Length - 6; i >= 0; i--)
				{
					// Does it fit now?
					newname = filename.Substring(0, 3) + "..." + filename.Substring(filename.Length - i, i);
					if(GetStringWidth(newname) <= MAX_RECENT_FILES_PIXELS) return newname;
				}

				// Cant find anything that fits (most unlikely!)
				return "wtf?!";
			}
			else
			{
				// The whole string fits
				return filename;
			}
		}
		
		// This returns the width of a string
		private float GetStringWidth(string str)
		{
			Graphics g = Graphics.FromHwndInternal(this.Handle);
			SizeF strsize = g.MeasureString(str, this.Font);
			return strsize.Width;
		}
		
		// Exit clicked
		private void itemexit_Click(object sender, EventArgs e) { this.Close(); }

		// Recent item clicked
		private void recentitem_Click(object sender, EventArgs e)
		{
			// Get the item that was clicked
			ToolStripItem item = (sender as ToolStripItem);

			// Open this file
			General.OpenMapFile(item.Tag.ToString(), null);
		}
		
		#endregion

		#region ================== Edit Menu

		// This sets up the edit menu
		private void UpdateEditMenu()
		{
			// No edit menu when no map open
			menuedit.Visible = (General.Map != null);
			
			// Enable/disable items
			itemundo.Enabled = (General.Map != null) && (General.Map.UndoRedo.NextUndo != null);
			itemredo.Enabled = (General.Map != null) && (General.Map.UndoRedo.NextRedo != null);
			itemcut.Enabled = (General.Map != null) && (General.Editing.Mode != null) && General.Editing.Mode.Attributes.AllowCopyPaste;
			itemcopy.Enabled = (General.Map != null) && (General.Editing.Mode != null) && General.Editing.Mode.Attributes.AllowCopyPaste;
			itempaste.Enabled = (General.Map != null) && (General.Editing.Mode != null) && General.Editing.Mode.Attributes.AllowCopyPaste;
			itempastespecial.Enabled = (General.Map != null) && (General.Editing.Mode != null) && General.Editing.Mode.Attributes.AllowCopyPaste;
			itemautoclearsidetextures.Checked = General.Settings.AutoClearSidedefTextures; //mxd

			// Determine undo description
			if(itemundo.Enabled)
				itemundo.Text = "Undo " + General.Map.UndoRedo.NextUndo.Description;
			else
				itemundo.Text = "Undo";

			// Determine redo description
			if(itemredo.Enabled)
				itemredo.Text = "Redo " + General.Map.UndoRedo.NextRedo.Description;
			else
				itemredo.Text = "Redo";
			
			// Toolbar icons
			buttonundo.Enabled = itemundo.Enabled;
			buttonredo.Enabled = itemredo.Enabled;
			buttonundo.ToolTipText = itemundo.Text;
			buttonredo.ToolTipText = itemredo.Text;
			buttonautoclearsidetextures.Checked = itemautoclearsidetextures.Checked; //mxd
			buttoncut.Enabled = itemcut.Enabled;
			buttoncopy.Enabled = itemcopy.Enabled;
			buttonpaste.Enabled = itempaste.Enabled;
		}

		//mxd
		private void menuedit_DropDownOpening(object sender, EventArgs e) 
		{
			if (General.Map == null) 
			{
				selectGroup.Enabled = false;
				clearGroup.Enabled = false;
				addToGroup.Enabled = false;
				return;
			}

			//get data
			ToolStripItem item;
			GroupInfo[] infos = new GroupInfo[10];
			for(int i = 0; i < infos.Length; i++) infos[i] = General.Map.Map.GetGroupInfo(i);

			//update "Add to group" menu
			addToGroup.Enabled = true;
			addToGroup.DropDownItems.Clear();
			foreach (GroupInfo gi in infos) 
			{
				item = addToGroup.DropDownItems.Add(gi.ToString());
				item.Tag = "builder_assigngroup" + gi.Index;
				item.Click += InvokeTaggedAction;
			}

			//update "Select group" menu
			selectGroup.DropDownItems.Clear();
			foreach (GroupInfo gi in infos) 
			{
				if(gi.Empty) continue;
				item = selectGroup.DropDownItems.Add(gi.ToString());
				item.Tag = "builder_selectgroup" + gi.Index;
				item.Click += InvokeTaggedAction;
			}

			//update "Clear group" menu
			clearGroup.DropDownItems.Clear();
			foreach(GroupInfo gi in infos) 
			{
				if(gi.Empty) continue;
				item = clearGroup.DropDownItems.Add(gi.ToString());
				item.Tag = "builder_cleargroup" + gi.Index;
				item.Click += InvokeTaggedAction;
			}

			selectGroup.Enabled = selectGroup.DropDownItems.Count > 0;
			clearGroup.Enabled = clearGroup.DropDownItems.Count > 0;
		}

		// Action to toggle snap to grid
		[BeginAction("togglesnap")]
		internal void ToggleSnapToGrid()
		{
			buttonsnaptogrid.Checked = !buttonsnaptogrid.Checked;
			itemsnaptogrid.Checked = buttonsnaptogrid.Checked;
			DisplayStatus(StatusType.Action, "Snap to grid is " + (buttonsnaptogrid.Checked ? "ENABLED" : "DISABLED"));
		}

		// Action to toggle auto merge
		[BeginAction("toggleautomerge")]
		internal void ToggleAutoMerge()
		{
			buttonautomerge.Checked = !buttonautomerge.Checked;
			itemautomerge.Checked = buttonautomerge.Checked;
			DisplayStatus(StatusType.Action, "Snap to geometry is " + (buttonautomerge.Checked ? "ENABLED" : "DISABLED"));
		}

		//mxd
		[BeginAction("togglebrightness")]
		internal void ToggleBrightness() 
		{
			Renderer.FullBrightness = !Renderer.FullBrightness;
			buttonfullbrightness.Checked = Renderer.FullBrightness;
			menufullbrightness.Checked = Renderer.FullBrightness;
			General.Interface.DisplayStatus(StatusType.Action, "Full Brightness is now " + (Renderer.FullBrightness ? "ON" : "OFF"));

			// Redraw display to show changes
			General.Interface.RedrawDisplay();
		}

		//mxd
		[BeginAction("toggleautoclearsidetextures")]
		internal void ToggleAutoClearSideTextures() 
		{
			buttonautoclearsidetextures.Checked = !buttonautoclearsidetextures.Checked;
			itemautoclearsidetextures.Checked = buttonautoclearsidetextures.Checked;
			General.Settings.AutoClearSidedefTextures = buttonautoclearsidetextures.Checked;
			DisplayStatus(StatusType.Action, "Auto removal of unused sidedef textures is " + (buttonautoclearsidetextures.Checked ? "ENABLED" : "DISABLED"));
		}

		//mxd
		[BeginAction("viewusedtags")]
		internal void ViewUsedTags() 
		{
			TagStatisticsForm f = new TagStatisticsForm();
			f.ShowDialog(this);
		}

		//mxd
		[BeginAction("viewthingtypes")]
		internal void ViewThingTypes()
		{
			ThingStatisticsForm f = new ThingStatisticsForm();
			f.ShowDialog(this);
		}
		
		#endregion

		#region ================== View Menu

		// This sets up the modes menu
		private void UpdateViewMenu()
		{
			// Menu items
			itemthingsfilter.Enabled = (General.Map != null);
			itemscripteditor.Enabled = (General.Map != null);
			itemfittoscreen.Enabled = (General.Map != null);
			menuzoom.Enabled = (General.Map != null);
			menugotocoords.Enabled = (General.Map != null); //mxd
			menufullbrightness.Enabled = (General.Map != null); //mxd
			itemtoggleinfo.Enabled = (General.Map != null); //mxd
			itemtoggleinfo.Checked = IsInfoPanelExpanded;
			
			// View mode items
			for(int i = 0; i < Renderer2D.NUM_VIEW_MODES; i++)
			{
				// NOTE: We only disable them when no map is loaded, because they may
				// need to be disabled for non-classic modes
				if(General.Map == null)
				{
					viewmodesbuttons[i].Enabled = false;
					viewmodesbuttons[i].Checked = false;
					viewmodesitems[i].Enabled = false;
					viewmodesitems[i].Checked = false;
				}
				else
				{
					// Check the correct item
					viewmodesbuttons[i].Checked = (i == (int)General.Map.CRenderer2D.ViewMode);
					viewmodesitems[i].Checked = (i == (int)General.Map.CRenderer2D.ViewMode);
				}
			}
		}

		#endregion

		#region ================== Mode Menu

		// This sets up the modes menu
		private void UpdateModeMenu()
		{
			menumode.Visible = (General.Map != null);
		}
		
		#endregion

		#region ================== Help Menu
		
		// This sets up the help menu
		private void UpdateHelpMenu()
		{
			itemhelpeditmode.Enabled = ((General.Map != null) && (General.Editing.Mode != null));
		}
		
		// About clicked
		private void itemhelpabout_Click(object sender, EventArgs e)
		{
			// Show about dialog
			AboutForm aboutform = new AboutForm();
			aboutform.ShowDialog(this);
		}

		// Reference Manual clicked
		private void itemhelprefmanual_Click(object sender, EventArgs e)
		{
			General.ShowHelp("introduction.html");
		}

		// About this Editing Mode
		private void itemhelpeditmode_Click(object sender, EventArgs e)
		{
			if((General.Map != null) && (General.Editing.Mode != null))
				General.Editing.Mode.OnHelp();
		}

		//mxd
		private void itemShortcutReference_Click(object sender, EventArgs e) 
		{
			const string columnLabels = "<tr><td width=\"240px;\"><strong>Action</strong></td><td width=\"120px;\"><div align=\"center\"><strong>Shortcut</strong></div></td><td width=\"120px;\"><div align=\"center\"><strong>Modifiers</strong></div></td><td><strong>Description</strong></td></tr>";
			const string categoryPadding = "<tr><td colspan=\"4\"></td></tr>";
			const string categoryStart = "<tr><td colspan=\"4\" bgcolor=\"#333333\"><strong style=\"color:#FFFFFF\">";
			const string categoryEnd = "</strong><div style=\"text-align:right; float:right\"><a style=\"color:#FFFFFF\" href=\"#top\">[to top]</a></div></td></tr>";
			const string fileName = "GZDB Actions Reference.html";

			Actions.Action[] actions = General.Actions.GetAllActions();
			Dictionary<string, List<Actions.Action>> sortedActions = new Dictionary<string, List<Actions.Action>>(StringComparer.Ordinal);

			foreach(Actions.Action action in actions) 
			{
				if(!sortedActions.ContainsKey(action.Category))
					sortedActions.Add(action.Category, new List<Actions.Action>());
				sortedActions[action.Category].Add(action);
			}

			System.Text.StringBuilder html = new System.Text.StringBuilder();

			//head
			html.AppendLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">" + Environment.NewLine +
								"<html xmlns=\"http://www.w3.org/1999/xhtml\">" + Environment.NewLine +
								"<head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" /><title>GZDoom Builder Actions Reference</title></head>" + Environment.NewLine +
								"<body bgcolor=\"#666666\">" + Environment.NewLine +
									"<div style=\"padding-left:60px; padding-right:60px; padding-top:20px; padding-bottom:20px;\">" + Environment.NewLine);

			//table header
			html.AppendLine("<table bgcolor=\"#FFFFFF\" width=\"100%\" border=\"0\" cellspacing=\"6\" cellpadding=\"6\" style=\"font-family: 'Trebuchet MS',georgia,Verdana,Sans-serif;\">" + Environment.NewLine +
							"<tr><td colspan=\"4\" bgcolor=\"#333333\"><span style=\"font-size: 24px\"><a name=\"top\" id=\"top\"></a><strong style=\"color:#FFFFFF\">GZDoom Builder Actions Reference</strong></span></td></tr>");

			//categories navigator
			List<string> catnames = new List<string>(sortedActions.Count);
			int counter = 0;
			int numActions = 0;
			foreach(KeyValuePair<string, List<Actions.Action>> category in sortedActions) 
			{
				catnames.Add("<a href=\"#cat" + (counter++) + "\">" + General.Actions.Categories[category.Key] + "</a>");
				numActions += category.Value.Count;
			}

			html.AppendLine("<tr><td colspan=\"4\"><strong>Total number of actions:</strong> " + numActions + "<br/><strong>Jump to:</strong> ");
			html.AppendLine(string.Join(" | ", catnames.ToArray()));
			html.AppendLine("</td></tr>" + Environment.NewLine);

			//add descriptions
			counter = 0;
			foreach(KeyValuePair<string, List<Actions.Action>> category in sortedActions) 
			{
				//add category title
				html.AppendLine(categoryPadding);
				html.AppendLine(categoryStart + "<a name=\"cat" + counter + "\" id=\"cat" + counter + "\"></a>" + General.Actions.Categories[category.Key] + categoryEnd);
				html.AppendLine(columnLabels);
				counter++;

				Dictionary<string, Actions.Action> actionsByTitle = new Dictionary<string, Actions.Action>(StringComparer.Ordinal);
				List<string> actionTitles = new List<string>();

				foreach(Actions.Action action in category.Value) 
				{
					actionsByTitle.Add(action.Title, action);
					actionTitles.Add(action.Title);
				}

				actionTitles.Sort();

				Actions.Action a;
				foreach(string title in actionTitles) 
				{
					a = actionsByTitle[title];
					List<string> modifiers = new List<string>();

					html.AppendLine("<tr>");
					html.AppendLine("<td>" + title + "</td>");
					html.AppendLine("<td><div align=\"center\">" + Actions.Action.GetShortcutKeyDesc(a.ShortcutKey) + "</div></td>");

					if(a.DisregardControl) modifiers.Add("Ctrl");
					if(a.DisregardAlt) modifiers.Add("Alt");
					if(a.DisregardShift) modifiers.Add("Shift");

					html.AppendLine("<td><div align=\"center\">" + string.Join(", ", modifiers.ToArray()) + "</div></td>");
					html.AppendLine("<td>" + a.Description + "</td>");
					html.AppendLine("</tr>");
				}
			}

			//add bottom
			html.AppendLine("</table></div></body></html>");

			//write
			string path;
			try 
			{
				path = Path.Combine(General.AppPath, fileName);
				using(StreamWriter writer = File.CreateText(path)) 
				{
					writer.Write(html.ToString());
				}
			} 
			catch (Exception) 
			{
				//Configurtions path SHOULD be accessible and not read-only, right?
				path = Path.Combine(General.SettingsPath, fileName);
				using(StreamWriter writer = File.CreateText(path)) 
				{
					writer.Write(html.ToString());
				}
			}

			//open file
			DisplayStatus(StatusType.Info, "Shortcut reference saved to '" + path + "'");
			System.Diagnostics.Process.Start(path);
		}
		
		#endregion

		#region ================== Prefabs Menu

		// This sets up the prefabs menu
		private void UpdatePrefabsMenu()
		{
			// Enable/disable items
			itemcreateprefab.Enabled = (General.Map != null) && (General.Editing.Mode != null) && General.Editing.Mode.Attributes.AllowCopyPaste;
			iteminsertprefabfile.Enabled = (General.Map != null) && (General.Editing.Mode != null) && General.Editing.Mode.Attributes.AllowCopyPaste;
			iteminsertpreviousprefab.Enabled = (General.Map != null) && (General.Editing.Mode != null) && General.Map.CopyPaste.IsPreviousPrefabAvailable && General.Editing.Mode.Attributes.AllowCopyPaste;
			
			// Toolbar icons
			buttoninsertprefabfile.Enabled = iteminsertprefabfile.Enabled;
			buttoninsertpreviousprefab.Enabled = iteminsertpreviousprefab.Enabled;
		}
		
		#endregion
		
		#region ================== Tools Menu

		// This sets up the tools menu
		private void UpdateToolsMenu()
		{
			// Enable/disable items
			bool enabled = (General.Map != null);
			itemreloadresources.Enabled = enabled;
			
			//mxd
			itemReloadGldefs.Enabled = enabled;
			itemReloadMapinfo.Enabled = enabled;
			itemReloadModedef.Enabled = enabled;
		}
		
		// Errors and Warnings
		[BeginAction("showerrors")]
		internal void ShowErrors()
		{
			ErrorsForm errform = new ErrorsForm();
			errform.ShowDialog(this);
			errform.Dispose();
			//mxd
			SetWarningsCount(General.ErrorLogger.ErrorsCount, false);
		}
		
		// Game Configuration action
		[BeginAction("configuration")]
		internal void ShowConfiguration()
		{
			// Show configuration dialog
			ShowConfigurationPage(-1);
		}

		// This shows the configuration on a specific page
		internal void ShowConfigurationPage(int pageindex)
		{
			// Show configuration dialog
			ConfigForm cfgform = new ConfigForm();
			if(pageindex > -1) cfgform.ShowTab(pageindex);
			if(cfgform.ShowDialog(this) == DialogResult.OK)
			{
				// Update stuff
				SetupInterface();
				UpdateInterface();
				General.Editing.UpdateCurrentEditModes();
				General.Plugins.ProgramReconfigure();
				
				// Reload resources if a map is open
				if((General.Map != null) && cfgform.ReloadResources) General.Actions.InvokeAction("builder_reloadresources");
				
				// Redraw display
				RedrawDisplay();
			}

			// Done
			cfgform.Dispose();
		}

		// Preferences action
		[BeginAction("preferences")]
		internal void ShowPreferences()
		{
			// Show preferences dialog
			PreferencesForm prefform = new PreferencesForm();
			if(prefform.ShowDialog(this) == DialogResult.OK)
			{
				// Update stuff
				SetupInterface();
				UpdateInterface();
				ApplyShortcutKeys();
				General.Colors.CreateCorrectionTable();
				General.Plugins.ProgramReconfigure();
				
				// Map opened?
				if(General.Map != null)
				{
					// Reload resources!
					if(General.Map.ScriptEditor != null) General.Map.ScriptEditor.Editor.RefreshSettings();
					General.Map.Graphics.SetupSettings();
					General.Map.UpdateConfiguration();
					if(prefform.ReloadResources) General.Actions.InvokeAction("builder_reloadresources");
				}
				
				// Redraw display
				RedrawDisplay();
			}

			// Done
			prefform.Dispose();
		}

		//mxd
		internal void SaveScreenshot(bool activeControlOnly) 
		{
			//pick a valid folder
			string folder = General.Settings.ScreenshotsPath;
			if (!Directory.Exists(folder)) 
			{
				if (folder != General.DefaultScreenshotsPath 
					&& General.ShowErrorMessage("Screenshots save path '" + folder 
					+ "' does not exist!\nPress OK to save to the default folder ('" 
					+ General.DefaultScreenshotsPath 
					+ "').\nPress Cancel to abort.", MessageBoxButtons.OKCancel) == DialogResult.Cancel) return;


				folder = General.DefaultScreenshotsPath;
				if(!Directory.Exists(folder)) Directory.CreateDirectory(folder);
			}

			// Create name and bounds
			string name;
			Rectangle bounds;
			bool displayextrainfo = false;
			string mapname = (General.Map != null ? Path.GetFileNameWithoutExtension(General.Map.FileTitle) : General.ThisAssembly.GetName().Name);

			if(activeControlOnly)
			{
				if (Form.ActiveForm != null && Form.ActiveForm != this)
				{
					name = mapname + " (" + Form.ActiveForm.Text + ") at ";
					bounds = (Form.ActiveForm.WindowState == FormWindowState.Maximized ? 
						Screen.GetWorkingArea(Form.ActiveForm) : 
						Form.ActiveForm.Bounds);
				}
				else
				{
					name = mapname + " (edit area) at ";
					bounds = this.display.Bounds;
					bounds.Offset(this.PointToScreen(new Point()));
					displayextrainfo = true;
				}
			} 
			else
			{
				name = mapname + " at ";
				bounds = (this.WindowState == FormWindowState.Maximized ? Screen.GetWorkingArea(this) : this.Bounds);
			}

			Point cursorLocation = Point.Empty;
			//dont want to render the cursor in VisualMode
			if(General.Editing.Mode == null || !(General.Editing.Mode is VisualMode))
				cursorLocation = Cursor.Position - new Size(bounds.Location);

			//create path
			string date = DateTime.Now.ToString("yyyy.MM.dd HH-mm-ss.fff");
			string revision = (General.DebugBuild ? "DEVBUILD" : "R" + General.ThisAssembly.GetName().Version.MinorRevision);
			string path = Path.Combine(folder, name + date + " [" + revision + "].jpg");

			//save image
			using(Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height)) 
			{
				using(Graphics g = Graphics.FromImage(bitmap)) 
				{
					g.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);

					//draw the cursor
					if(!cursorLocation.IsEmpty) g.DrawImage(Resources.Cursor, cursorLocation);

					//gather some info
					string info;
					if(displayextrainfo && General.Editing.Mode != null) 
					{
						info = General.Map.FileTitle + " | " + General.Map.Options.CurrentName + " | ";

						//get map coordinates
						if (General.Editing.Mode is ClassicMode) 
						{
							Vector2D pos = ((ClassicMode) General.Editing.Mode).MouseMapPos;

							//mouse inside the view?
							if (pos.IsFinite()) 
							{
								info += "X:" + Math.Round(pos.x) + " Y:" + Math.Round(pos.y);
							} 
							else 
							{
								info += "X:" + Math.Round(General.Map.Renderer2D.TranslateX) + " Y:" + Math.Round(General.Map.Renderer2D.TranslateY);
							}
						} 
						else 
						{ //should be visual mode
							info += "X:" + Math.Round(General.Map.VisualCamera.Position.x) + " Y:" + Math.Round(General.Map.VisualCamera.Position.y) + " Z:" + Math.Round(General.Map.VisualCamera.Position.z);
						}

						//add the revision number
						info += " | " + revision;
					} 
					else 
					{
						//just use the revision number
						info = revision;
					}

					//draw info
					Font font = new Font("Tahoma", 10);
					SolidBrush brush = new SolidBrush(Color.White);
					SizeF rect = g.MeasureString(info, font);
					float px = bounds.Width - rect.Width - 4;
					float py = 4;

					g.FillRectangle(Brushes.Black, px, py, rect.Width, rect.Height + 3);
					g.DrawString(info, font, brush, px + 2, py + 2);
				}

				try 
				{
					ImageCodecInfo jpegCodec = null;
					ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
					foreach(ImageCodecInfo codec in codecs) 
					{
						if(codec.FormatID == ImageFormat.Jpeg.Guid) 
						{
							jpegCodec = codec;
							break;
						}
					}

					EncoderParameter qualityParam = new EncoderParameter(Encoder.Quality, 90L);
					EncoderParameters encoderParams = new EncoderParameters(1);
					encoderParams.Param[0] = qualityParam;

					bitmap.Save(path, jpegCodec, encoderParams);
					DisplayStatus(StatusType.Info, "Screenshot saved to '" + path + "'");
				} 
				catch(ExternalException e) 
				{
					DisplayStatus(StatusType.Warning, "Failed to save screenshot...");
					General.ErrorLogger.Add(ErrorType.Error, "Failed to save screenshot: " + e.Message);
				}
			}
		}
		
		#endregion

		#region ================== Models and Lights mode (mxd)

		private void ChangeModelRenderingMode(object sender, EventArgs e)
		{
			General.Settings.GZDrawModelsMode = (ModelRenderMode)((ToolStripMenuItem)sender).Tag;

			switch(General.Settings.GZDrawModelsMode) 
			{
				case ModelRenderMode.NONE:
					General.MainWindow.DisplayStatus(StatusType.Action, "Models rendering mode: NONE");
					break;

				case ModelRenderMode.SELECTION:
					General.MainWindow.DisplayStatus(StatusType.Action, "Models rendering mode: SELECTION ONLY");
					break;

				case ModelRenderMode.ALL:
					General.MainWindow.DisplayStatus(StatusType.Action, "Models rendering mode: ALL");
					break;
			}
			
			UpdateGZDoomPanel();
			RedrawDisplay();
		}

		private void ChangeLightRenderingMode(object sender, EventArgs e) 
		{
			General.Settings.GZDrawLightsMode = (LightRenderMode)((ToolStripMenuItem)sender).Tag;

			switch(General.Settings.GZDrawLightsMode) 
			{
				case LightRenderMode.NONE:
					General.MainWindow.DisplayStatus(StatusType.Action, "Dynamic lights rendering mode: NONE");
					break;

				case LightRenderMode.ALL:
					General.MainWindow.DisplayStatus(StatusType.Action, "Models rendering mode: ALL");
					break;

				case LightRenderMode.ALL_ANIMATED:
					General.MainWindow.DisplayStatus(StatusType.Action, "Models rendering mode: ANIMATED");
					break;
			}
			
			UpdateGZDoomPanel();
			RedrawDisplay();
		}


		#endregion

		#region ================== Info Panels

		// This toggles the panel expanded / collapsed
		[BeginAction("toggleinfopanel")]
		internal void ToggleInfoPanel()
		{
			if(IsInfoPanelExpanded)
			{
				panelinfo.Height = buttontoggleinfo.Height + buttontoggleinfo.Top;
				buttontoggleinfo.Image = Resources.InfoPanelExpand; //mxd
				if(linedefinfo.Visible) linedefinfo.Hide();
				if(vertexinfo.Visible) vertexinfo.Hide();
				if(sectorinfo.Visible) sectorinfo.Hide();
				if(thinginfo.Visible) thinginfo.Hide();
				modename.Visible = false;
#if DEBUG
				console.Visible = false; //mxd
#endif
				statistics.Visible = false; //mxd
				labelcollapsedinfo.Visible = true;
				itemtoggleinfo.Checked = false;
			}
			else
			{
				panelinfo.Height = heightpanel1.Height;
				buttontoggleinfo.Image = Resources.InfoPanelCollapse; //mxd
				labelcollapsedinfo.Visible = false;
				itemtoggleinfo.Checked = true;
				if(lastinfoobject is Vertex) ShowVertexInfo(lastinfoobject as Vertex);
				else if(lastinfoobject is Linedef) ShowLinedefInfo(lastinfoobject as Linedef);
				else if(lastinfoobject is Sector) ShowSectorInfo(lastinfoobject as Sector);
				else if(lastinfoobject is Thing) ShowThingInfo(lastinfoobject as Thing);
				else HideInfo();
			}

			dockerspanel.Height = dockersspace.Height; //mxd
			FocusDisplay();
		}

		// Mouse released on info panel toggle button
		private void buttontoggleinfo_MouseUp(object sender, MouseEventArgs e)
		{
			dockerspanel.Height = dockersspace.Height; //mxd
			FocusDisplay();
		}
		
		// This displays the current mode name
		internal void DisplayModeName(string name)
		{
			if(lastinfoobject == null) 
			{
				labelcollapsedinfo.Text = name;
				labelcollapsedinfo.Refresh();
			}
			modename.Text = name;
			modename.Refresh();
		}
		
		// This hides all info panels
		public void HideInfo()
		{
			// Hide them all
			bool showModeName = ((General.Map != null) && IsInfoPanelExpanded); //mxd
			lastinfoobject = null;
			if(linedefinfo.Visible) linedefinfo.Hide();
			if(vertexinfo.Visible) vertexinfo.Hide();
			if(sectorinfo.Visible) sectorinfo.Hide();
			if(thinginfo.Visible) thinginfo.Hide();
			labelcollapsedinfo.Text = modename.Text;
			labelcollapsedinfo.Refresh();
#if DEBUG
			console.Visible = true;
#else
			modename.Visible = showModeName;
#endif
			modename.Refresh();
			statistics.Visible = showModeName; //mxd

			//mxd. let the plugins know
			General.Plugins.OnHighlightLost();
		}
		
		// This refreshes info
		public void RefreshInfo()
		{
			if(lastinfoobject is Vertex) ShowVertexInfo(lastinfoobject as Vertex);
			else if(lastinfoobject is Linedef) ShowLinedefInfo(lastinfoobject as Linedef);
			else if(lastinfoobject is Sector) ShowSectorInfo(lastinfoobject as Sector);
			else if(lastinfoobject is Thing) ShowThingInfo(lastinfoobject as Thing);

			//mxd. let the plugins know
			General.Plugins.OnHighlightRefreshed(lastinfoobject);
		}

		//mxd
		public void ShowHints(string hintsText) 
		{
			if (!string.IsNullOrEmpty(hintsText)) 
			{
				hintsPanel.SetHints(hintsText);
			} 
			else 
			{
				ClearHints();
			}
		}

		//mxd
		public void ClearHints() 
		{
			hintsPanel.ClearHints();
		}

		//mxd
		internal void AddHintsDocker() 
		{
			if(!dockerspanel.Contains(hintsDocker)) dockerspanel.Add(hintsDocker);
		}

		//mxd
		internal void RemoveHintsDocker() 
		{
			dockerspanel.Remove(hintsDocker);
		}

		//mxd. Show linedef info
		public void ShowLinedefInfo(Linedef l) 
		{
			ShowLinedefInfo(l, null);
		}
		
		//mxd. Show linedef info and highlight given sidedef
		public void ShowLinedefInfo(Linedef l, Sidedef highlightside)
		{
			if(l.IsDisposed)
			{
				HideInfo();
				return;
			}
			
			lastinfoobject = l;
			modename.Visible = false;
#if DEBUG
			console.Visible = console.AlwaysOnTop; //mxd
#endif
			statistics.Visible = false; //mxd
			if(vertexinfo.Visible) vertexinfo.Hide();
			if(sectorinfo.Visible) sectorinfo.Hide();
			if(thinginfo.Visible) thinginfo.Hide();
			if(IsInfoPanelExpanded) linedefinfo.ShowInfo(l, highlightside);

			// Show info on collapsed label
			if(General.Map.Config.LinedefActions.ContainsKey(l.Action)) 
			{
				LinedefActionInfo act = General.Map.Config.LinedefActions[l.Action];
				labelcollapsedinfo.Text = act.ToString();
			} 
			else if (l.Action == 0)
			{
				labelcollapsedinfo.Text = l.Action + " - None";
			}
			else
			{
				labelcollapsedinfo.Text = l.Action + " - Unknown";
			}

			labelcollapsedinfo.Refresh();

			//mxd. let the plugins know
			General.Plugins.OnHighlightLinedef(l);
		}

		// Show vertex info
		public void ShowVertexInfo(Vertex v) 
		{
			if (v.IsDisposed) 
			{
				HideInfo();
				return;
			}

			lastinfoobject = v;
			modename.Visible = false;
#if DEBUG
			console.Visible = console.AlwaysOnTop; //mxd
#endif
			statistics.Visible = false; //mxd
			if (linedefinfo.Visible) linedefinfo.Hide();
			if (sectorinfo.Visible) sectorinfo.Hide();
			if (thinginfo.Visible) thinginfo.Hide();
			if (IsInfoPanelExpanded) vertexinfo.ShowInfo(v);

			// Show info on collapsed label
			labelcollapsedinfo.Text = v.Position.x.ToString("0.##") + ", " + v.Position.y.ToString("0.##");
			labelcollapsedinfo.Refresh();

			//mxd. let the plugins know
			General.Plugins.OnHighlightVertex(v);
		}

		//mxd. Show sector info
		public void ShowSectorInfo(Sector s) 
		{
			ShowSectorInfo(s, false, false);
		}

		// Show sector info
		public void ShowSectorInfo(Sector s, bool highlightceiling, bool highlightfloor) 
		{
			if (s.IsDisposed) 
			{
				HideInfo();
				return;
			}

			lastinfoobject = s;
			modename.Visible = false;
#if DEBUG
			console.Visible = console.AlwaysOnTop; //mxd
#endif
			statistics.Visible = false; //mxd
			if (linedefinfo.Visible) linedefinfo.Hide();
			if (vertexinfo.Visible) vertexinfo.Hide();
			if (thinginfo.Visible) thinginfo.Hide();
			if(IsInfoPanelExpanded) sectorinfo.ShowInfo(s, highlightceiling, highlightfloor); //mxd

			// Show info on collapsed label
			if(General.Map.Config.SectorEffects.ContainsKey(s.Effect))
				labelcollapsedinfo.Text = General.Map.Config.SectorEffects[s.Effect].ToString();
			else if(s.Effect == 0)
				labelcollapsedinfo.Text = s.Effect + " - Normal";
			else
				labelcollapsedinfo.Text = s.Effect + " - Unknown";

			labelcollapsedinfo.Refresh();

			//mxd. let the plugins know
			General.Plugins.OnHighlightSector(s);
		}

		// Show thing info
		public void ShowThingInfo(Thing t)
		{
			if(t.IsDisposed)
			{
				HideInfo();
				return;
			}

			lastinfoobject = t;
			modename.Visible = false;
#if DEBUG
			console.Visible = console.AlwaysOnTop; //mxd
#endif
			statistics.Visible = false; //mxd
			if(linedefinfo.Visible) linedefinfo.Hide();
			if(vertexinfo.Visible) vertexinfo.Hide();
			if(sectorinfo.Visible) sectorinfo.Hide();
			if(IsInfoPanelExpanded) thinginfo.ShowInfo(t);

			// Show info on collapsed label
			ThingTypeInfo ti = General.Map.Data.GetThingInfo(t.Type);
			labelcollapsedinfo.Text = t.Type + " - " + ti.Title;
			labelcollapsedinfo.Refresh();

			//mxd. let the plugins know
			General.Plugins.OnHighlightThing(t);
		}

		#endregion

		#region ================== Dialogs

		// This browses for a texture
		// Returns the new texture name or the same texture name when cancelled
		public string BrowseTexture(IWin32Window owner, string initialvalue)
		{
			return TextureBrowserForm.Browse(owner, initialvalue, false);//mxd
		}

		// This browses for a flat
		// Returns the new flat name or the same flat name when cancelled
		public string BrowseFlat(IWin32Window owner, string initialvalue)
		{
			return TextureBrowserForm.Browse(owner, initialvalue, true); //mxd. was FlatBrowserForm
		}
		
		// This browses the lindef types
		// Returns the new action or the same action when cancelled
		public int BrowseLinedefActions(IWin32Window owner, int initialvalue)
		{
			return ActionBrowserForm.BrowseAction(owner, initialvalue);
		}

		// This browses sector effects
		// Returns the new effect or the same effect when cancelled
		public int BrowseSectorEffect(IWin32Window owner, int initialvalue)
		{
			return EffectBrowserForm.BrowseEffect(owner, initialvalue);
		}

		// This browses thing types
		// Returns the new thing type or the same thing type when cancelled
		public int BrowseThingType(IWin32Window owner, int initialvalue)
		{
			return ThingBrowserForm.BrowseThing(owner, initialvalue);
		}

		//mxd
		public DialogResult ShowEditVertices(ICollection<Vertex> vertices) 
		{
			return ShowEditVertices(vertices, true);
		}

		//mxd. This shows the dialog to edit vertices
		public DialogResult ShowEditVertices(ICollection<Vertex> vertices, bool allowPositionChange)
		{
			// Show sector edit dialog
			VertexEditForm f = new VertexEditForm();
			DisableProcessing(); //mxd
			f.Setup(vertices, allowPositionChange);
			EnableProcessing(); //mxd
			f.OnValuesChanged += EditForm_OnValuesChanged;
			editformopen = true; //mxd
			DialogResult result = f.ShowDialog(this);
			editformopen = false; //mxd
			f.Dispose();

			return result;
		}
		
		// This shows the dialog to edit lines
		public DialogResult ShowEditLinedefs(ICollection<Linedef> lines)
		{
			DialogResult result;
			
			// Show line edit dialog
			if(General.Map.UDMF) //mxd
			{
				LinedefEditFormUDMF f = new LinedefEditFormUDMF();
				DisableProcessing(); //mxd
				f.Setup(lines);
				EnableProcessing(); //mxd
				f.OnValuesChanged += EditForm_OnValuesChanged;
				editformopen = true; //mxd
				result = f.ShowDialog(this);
				editformopen = false; //mxd
				f.Dispose();
			}
			else
			{
				LinedefEditForm f = new LinedefEditForm();
				DisableProcessing(); //mxd
				f.Setup(lines);
				EnableProcessing(); //mxd
				f.OnValuesChanged += EditForm_OnValuesChanged;
				editformopen = true; //mxd
				result = f.ShowDialog(this);
				editformopen = false; //mxd
				f.Dispose();
			}

			return result;
		}

		// This shows the dialog to edit sectors
		public DialogResult ShowEditSectors(ICollection<Sector> sectors)
		{
			DialogResult result;

			// Show sector edit dialog
			if(General.Map.UDMF) //mxd
			{ 
				SectorEditFormUDMF f = new SectorEditFormUDMF();
				DisableProcessing(); //mxd
				f.Setup(sectors);
				EnableProcessing(); //mxd
				f.OnValuesChanged += EditForm_OnValuesChanged;
				editformopen = true; //mxd
				result = f.ShowDialog(this);
				editformopen = false; //mxd
				f.Dispose();
			}
			else
			{
				SectorEditForm f = new SectorEditForm();
				DisableProcessing(); //mxd
				f.Setup(sectors);
				EnableProcessing(); //mxd
				f.OnValuesChanged += EditForm_OnValuesChanged;
				editformopen = true; //mxd
				result = f.ShowDialog(this);
				editformopen = false; //mxd
				f.Dispose();
			}

			return result;
		}

		// This shows the dialog to edit things
		public DialogResult ShowEditThings(ICollection<Thing> things) 
		{
			DialogResult result;

			// Show thing edit dialog
			if(General.Map.UDMF) 
			{
				ThingEditFormUDMF f = new ThingEditFormUDMF();
				DisableProcessing(); //mxd
				f.Setup(things);
				EnableProcessing(); //mxd
				f.OnValuesChanged += EditForm_OnValuesChanged;
				editformopen = true; //mxd
				result = f.ShowDialog(this);
				editformopen = false; //mxd
				f.Dispose();
			} 
			else 
			{
				ThingEditForm f = new ThingEditForm();
				DisableProcessing(); //mxd
				f.Setup(things);
				EnableProcessing(); //mxd
				f.OnValuesChanged += EditForm_OnValuesChanged;
				editformopen = true; //mxd
				result = f.ShowDialog(this);
				editformopen = false; //mxd
				f.Dispose();
			}

			return result;
		}

		//mxd
		private void EditForm_OnValuesChanged(object sender, EventArgs e) 
		{
			if (OnEditFormValuesChanged != null) 
			{
				OnEditFormValuesChanged(sender, e);
			} 
			else 
			{
				//If current mode doesn't handle this event, let's at least update the map and redraw display.
				General.Map.Map.Update();
				RedrawDisplay();
			}
		}

		#endregion

		#region ================== Message Pump
		
		// This handles messages
		protected override void WndProc(ref Message m)
		{
			// Notify message?
			switch(m.Msg)
			{
				case (int)ThreadMessages.UpdateStatus:
					DisplayStatus(status);
					break;
					
				case (int)ThreadMessages.ImageDataLoaded:
					string imagename = Marshal.PtrToStringAuto(m.WParam);
					Marshal.FreeCoTaskMem(m.WParam);
					if((General.Map != null) && (General.Map.Data != null))
					{
						ImageData img = General.Map.Data.GetFlatImage(imagename);
						ImageDataLoaded(img);
					}
					break;

				case (int)ThreadMessages.SpriteDataLoaded: //mxd
					string spritename = Marshal.PtrToStringAuto(m.WParam);
					Marshal.FreeCoTaskMem(m.WParam);
					if ((General.Map != null) && (General.Map.Data != null))
					{
						ImageData img = General.Map.Data.GetSpriteImage(spritename);
						if (img != null && img.UsedInMap && !img.IsDisposed)
						{
							DelayedRedraw();
						}
					}
					break;

				case General.WM_SYSCOMMAND:
					// We don't want to open a menu when ALT is pressed
					if(m.WParam.ToInt32() != General.SC_KEYMENU)
					{
						base.WndProc(ref m);
					}
					break;
					
				default:
					// Let the base handle the message
					base.WndProc(ref m);
					break;
			}
		}

		//mxd. Warnings panel
		internal void SetWarningsCount(int count, bool blink) 
		{
			if(count > 0) 
			{
				if (warnsLabel.Image != Resources.Warning) warnsLabel.Image = Resources.Warning;
			} 
			else 
			{
				warnsLabel.Image = Resources.WarningOff;
				warnsLabel.BackColor = SystemColors.Control;
			}

			warnsLabel.Text = count.ToString();
			
			//start annoying blinking!
			if (blink) 
			{
				if(!blinkTimer.Enabled) blinkTimer.Start();
			} 
			else 
			{
				blinkTimer.Stop();
				warnsLabel.BackColor = SystemColors.Control;
			}
		}

		//mxd. Bliks warnings indicator
		private void Blink() 
		{
			warnsLabel.BackColor = (warnsLabel.BackColor == Color.Red ? SystemColors.Control : Color.Red);
		}

		//mxd
		private void warnsLabel_Click(object sender, EventArgs e) 
		{
			ShowErrors();
		}

		//mxd
		private void blinkTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) 
		{
			try 
			{
				this.Invoke(new CallBlink(Blink));
			} catch(ObjectDisposedException) { } //la-la-la. We don't care.
		}
		
		#endregion
		
		#region ================== Processing
		
		// This is called from the background thread when images are loaded
		// but only when first loaded or when dimensions were changed
		internal void ImageDataLoaded(ImageData img)
		{
			// Image is used in the map?
			if((img != null) && img.UsedInMap && !img.IsDisposed)
			{
				// Go for all setors
				bool updated = false;
				foreach(Sector s in General.Map.Map.Sectors)
				{
					// Update floor buffer if needed
					if(s.LongFloorTexture == img.LongName)
					{
						s.UpdateFloorSurface();
						updated = true;
					}
					
					// Update ceiling buffer if needed
					if(s.LongCeilTexture == img.LongName)
					{
						s.UpdateCeilingSurface();
						updated = true;
					}
				}
				
				// If we made updates, redraw the screen
				if(updated) DelayedRedraw();
			}
		}

		public void EnableProcessing()
		{
			// Increase count
			processingcount++;

			// If not already enabled, enable processing now
			if(!processor.Enabled)
			{
				processor.Enabled = true;
				lastupdatetime = Clock.CurrentTime;
			}
		}

		public void DisableProcessing()
		{
			// Increase count
			processingcount--;
			if(processingcount < 0) processingcount = 0;
			
			// Turn off
			if(processor.Enabled && (processingcount == 0))
				processor.Enabled = false;
		}

		internal void StopProcessing()
		{
			// Turn off
			processingcount = 0;
			processor.Enabled = false;
		}
		
		// Processor event
		private void processor_Tick(object sender, EventArgs e)
		{
			float curtime = Clock.CurrentTime;
			float deltatime = curtime - lastupdatetime;
			lastupdatetime = curtime;
			
			// In exclusive mouse mode?
			if(mouseinput != null)
			{
				// Process mouse input
				Vector2D deltamouse = mouseinput.Process();
				if((General.Map != null) && (General.Editing.Mode != null))
				{
					General.Plugins.OnEditMouseInput(deltamouse);
					General.Editing.Mode.OnMouseInput(deltamouse);
				}
			}
			
			// Process signal
			if((General.Map != null) && (General.Editing.Mode != null))
				General.Editing.Mode.OnProcess(deltatime);
		}

		#endregion

		#region ================== Dockers
		// This adds a docker
		public void AddDocker(Docker d)
		{
			if(dockerspanel.Contains(d)) return; //mxd
			
			// Make sure the full name is set with the plugin name as prefix
			Plugin plugin = General.Plugins.FindPluginByAssembly(Assembly.GetCallingAssembly());
			d.MakeFullName(plugin.Name.ToLowerInvariant());
			
			dockerspanel.Add(d);
		}
		
		// This removes a docker
		public bool RemoveDocker(Docker d)
		{
			// Make sure the full name is set with the plugin name as prefix
			Plugin plugin = General.Plugins.FindPluginByAssembly(Assembly.GetCallingAssembly());
			d.MakeFullName(plugin.Name.ToLowerInvariant());
			
			// We must release all keys because the focus may be stolen when
			// this was the selected docker (the previous docker is automatically selected)
			ReleaseAllKeys();
			
			return dockerspanel.Remove(d);
		}
		
		// This selects a docker
		public bool SelectDocker(Docker d)
		{
			// Make sure the full name is set with the plugin name as prefix
			Plugin plugin = General.Plugins.FindPluginByAssembly(Assembly.GetCallingAssembly());
			d.MakeFullName(plugin.Name.ToLowerInvariant());
			
			// We must release all keys because the focus will be stolen
			ReleaseAllKeys();
			
			return dockerspanel.SelectDocker(d);
		}
		
		// This selects the previous selected docker
		public void SelectPreviousDocker()
		{
			// We must release all keys because the focus will be stolen
			ReleaseAllKeys();
			
			dockerspanel.SelectPrevious();
		}
		
		// Mouse enters dockers window
		private void dockerspanel_MouseContainerEnter(object sender, EventArgs e)
		{
			if(General.Settings.CollapseDockers)
				dockerscollapser.Start();
			
			dockerspanel.Expand();
		}
		
		// Automatic collapsing
		private void dockerscollapser_Tick(object sender, EventArgs e)
		{
			if(General.Settings.CollapseDockers)
			{
				if(!dockerspanel.IsFocused)
				{
					Point p = this.PointToClient(Cursor.Position);
					Rectangle r = new Rectangle(dockerspanel.Location, dockerspanel.Size);
					if(!r.IntersectsWith(new Rectangle(p, Size.Empty)))
					{
						dockerspanel.Collapse();
						dockerscollapser.Stop();
					}
				}
			}
			else
			{
				dockerscollapser.Stop();
			}
		}
		
		// User resizes the docker
		private void dockerspanel_UserResize(object sender, EventArgs e)
		{
			General.Settings.DockersWidth = dockerspanel.Width;

			if(!General.Settings.CollapseDockers)
			{
				dockersspace.Width = dockerspanel.Width;
				dockerspanel.Left = dockersspace.Left;
			}
		}
		
		#endregion

	}
}