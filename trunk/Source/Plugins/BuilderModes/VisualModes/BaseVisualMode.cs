
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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using CodeImp.DoomBuilder.Windows;
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Editing;
using CodeImp.DoomBuilder.Actions;
using CodeImp.DoomBuilder.VisualModes;

#endregion

namespace CodeImp.DoomBuilder.BuilderModes
{
	[EditMode(DisplayName = "Visual Mode",
			  SwitchAction = "visualmode",		// Action name used to switch to this mode
			  ButtonImage = "VisualMode.png",	// Image resource name for the button
			  ButtonOrder = 0,					// Position of the button (lower is more to the left)
			  ButtonGroup = "001_visual",
			  UseByDefault = true)]

	public class BaseVisualMode : VisualMode
	{
		#region ================== Constants
		
		// Object picking
		private const double PICK_INTERVAL = 80.0d;
		private const float PICK_RANGE = 0.98f;

		// Gravity
		private const float GRAVITY = -0.06f;
		private const float CAMERA_FLOOR_OFFSET = 41f;		// same as in doom
		private const float CAMERA_CEILING_OFFSET = 10f;
		
		#endregion
		
		#region ================== Variables

		// Gravity vector
		private Vector3D gravity;
		
		// Object picking
		private VisualPickResult target;
		private double lastpicktime;
		private bool locktarget;
		
		// This is true when a selection was made because the action is performed
		// on an object that was not selected. In this case the previous selection
		// is cleared and the targeted object is temporarely selected to perform
		// the action on. After the action is completed, the object is deselected.
		private bool singleselection;
		
		// We keep these to determine if we need to make a new undo level
		private bool selectionchanged;
		private Actions.Action lastaction;
		private VisualActionResult actionresult;
		private bool undocreated;

		// List of selected objects when an action is performed
		private List<IVisualEventReceiver> selectedobjects;
		
		#endregion
		
		#region ================== Properties

		public IRenderer3D Renderer { get { return renderer; } }
		
		public bool IsSingleSelection { get { return singleselection; } }
		public bool SelectionChanged { get { return selectionchanged; } set { selectionchanged |= value; } }

		#endregion
		
		#region ================== Constructor / Disposer

		// Constructor
		public BaseVisualMode()
		{
			// Initialize
			this.gravity = new Vector3D(0.0f, 0.0f, 0.0f);
			
			// We have no destructor
			GC.SuppressFinalize(this);
		}

		// Disposer
		public override void Dispose()
		{
			// Not already disposed?
			if(!isdisposed)
			{
				// Clean up
				
				// Done
				base.Dispose();
			}
		}

		#endregion
		
		#region ================== Methods
		
		// This is called before an action is performed
		private void PreAction(bool groupmultiselectionundo)
		{
			actionresult = new VisualActionResult();
			
			PickTargetUnlocked();
			
			// If the action is not performed on a selected object, clear the
			// current selection and make a temporary selection for the target.
			if((target.picked != null) && !target.picked.Selected)
			{
				// Single object, no selection
				singleselection = true;
				ClearSelection();
				target.picked.Selected = true;
				undocreated = false;
			}
			else
			{
				singleselection = false;
				
				// Check if we should make a new undo level
				// We don't want to do this if this is the same action with the same
				// selection and the action wants to group the undo levels
				if((lastaction != General.Actions.Current) || selectionchanged || !groupmultiselectionundo)
				{
					// We want to create a new undo level, but not just yet
					undocreated = false;
				}
				else
				{
					// We don't want to make a new undo level (changes will be combined)
					undocreated = true;
				}
			}
			
			MakeSelectedObjectsList();
		}

		// Called before an action is performed. This does not make an undo level or change selection.
		private void PreActionNoChange()
		{
			actionresult = new VisualActionResult();
			singleselection = false;
			undocreated = false;
			MakeSelectedObjectsList();
		}
		
		// This is called after an action is performed
		private void PostAction()
		{
			if(!string.IsNullOrEmpty(actionresult.displaystatus))
				General.Interface.DisplayStatus(StatusType.Action, actionresult.displaystatus);
			
			lastaction = General.Actions.Current;
			selectionchanged = false;
			
			if(singleselection)
				ClearSelection();
			
			UpdateChangedObjects();
			ShowTargetInfo();
		}
		
		// This sets the result for an action
		public void SetActionResult(VisualActionResult result)
		{
			actionresult = result;
		}

		// This sets the result for an action
		public void SetActionResult(string displaystatus)
		{
			actionresult = new VisualActionResult();
			actionresult.displaystatus = displaystatus;
		}
		
		// This creates an undo, when only a single selection is made
		// When a multi-selection is made, the undo is created by the PreAction function
		public int CreateUndo(string description, UndoGroup group, int grouptag)
		{
			if(!undocreated)
			{
				undocreated = true;

				if(singleselection)
					return General.Map.UndoRedo.CreateUndo(description, group, grouptag);
				else
					return General.Map.UndoRedo.CreateUndo(description, UndoGroup.None, 0);
			}
			else
			{
				return 0;
			}
		}

		// This creates an undo, when only a single selection is made
		// When a multi-selection is made, the undo is created by the PreAction function
		public int CreateUndo(string description)
		{
			return CreateUndo(description, UndoGroup.None, 0);
		}

		// This makes a list of the selected object
		private void MakeSelectedObjectsList()
		{
			// Make list of selected objects
			selectedobjects = new List<IVisualEventReceiver>();
			foreach(KeyValuePair<Sector, VisualSector> vs in allsectors)
			{
				BaseVisualSector bvs = (BaseVisualSector)vs.Value;
				if((bvs.Floor != null) && bvs.Floor.Selected) selectedobjects.Add(bvs.Floor);
				if((bvs.Ceiling != null) && bvs.Ceiling.Selected) selectedobjects.Add(bvs.Ceiling);
				foreach(Sidedef sd in vs.Key.Sidedefs)
				{
					List<VisualGeometry> sidedefgeos = bvs.GetSidedefGeometry(sd);
					foreach(VisualGeometry sdg in sidedefgeos)
					{
						if(sdg.Selected) selectedobjects.Add((sdg as IVisualEventReceiver));
					}
				}
			}

			foreach(KeyValuePair<Thing, VisualThing> vt in allthings)
			{
				BaseVisualThing bvt = (BaseVisualThing)vt.Value;
				if(bvt.Selected) selectedobjects.Add(bvt);
			}
		}

		// This creates a visual sector
		protected override VisualSector CreateVisualSector(Sector s)
		{
			BaseVisualSector vs = new BaseVisualSector(this, s);
			return vs;
		}
		
		// This creates a visual thing
		protected override VisualThing CreateVisualThing(Thing t)
		{
			BaseVisualThing vt = new BaseVisualThing(this, t);
			return vt.Setup() ? vt : null;
		}

		// This locks the target so that it isn't changed until unlocked
		public void LockTarget()
		{
			locktarget = true;
		}
		
		// This unlocks the target so that is changes to the aimed geometry again
		public void UnlockTarget()
		{
			locktarget = false;
		}
		
		// This picks a new target, if not locked
		private void PickTargetUnlocked()
		{
			if(!locktarget) PickTarget();
		}
		
		// This picks a new target
		private void PickTarget()
		{
			// Find the object we are aiming at
			Vector3D start = General.Map.VisualCamera.Position;
			Vector3D delta = General.Map.VisualCamera.Target - General.Map.VisualCamera.Position;
			delta = delta.GetFixedLength(General.Settings.ViewDistance * PICK_RANGE);
			VisualPickResult newtarget = PickObject(start, start + delta);
			
			// Should we update the info on panels?
			bool updateinfo = (newtarget.picked != target.picked);
			
			// Apply new target
			target = newtarget;

			// Show target info
			if(updateinfo) ShowTargetInfo();
		}

		// This shows the picked target information
		public void ShowTargetInfo()
		{
			// Any result?
			if(target.picked != null)
			{
				// Geometry picked?
				if(target.picked is VisualGeometry)
				{
					VisualGeometry pickedgeo = (target.picked as VisualGeometry);

					if(pickedgeo.Sidedef != null)
						General.Interface.ShowLinedefInfo(pickedgeo.Sidedef.Line);
					else if(pickedgeo.Sidedef == null)
						General.Interface.ShowSectorInfo(pickedgeo.Sector.Sector);
					else
						General.Interface.HideInfo();
				}
				// Thing picked?
				if(target.picked is VisualThing)
				{
					VisualThing pickedthing = (target.picked as VisualThing);
					General.Interface.ShowThingInfo(pickedthing.Thing);
				}
			}
			else
			{
				General.Interface.HideInfo();
			}
		}
		
		// This updates the VisualSectors and VisualThings that have their Changed property set
		private void UpdateChangedObjects()
		{
			foreach(KeyValuePair<Sector, VisualSector> vs in allsectors)
			{
				BaseVisualSector bvs = (BaseVisualSector)vs.Value;
				if(bvs.Changed) bvs.Rebuild();
			}

			foreach(KeyValuePair<Thing, VisualThing> vt in allthings)
			{
				BaseVisualThing bvt = (BaseVisualThing)vt.Value;
				if(bvt.Changed) bvt.Setup();
			}
		}
		
		#endregion
		
		#region ================== Events
		
		// Help!
		public override void OnHelp()
		{
			General.ShowHelp("e_visual.html");
		}
		
		// Processing
		public override void OnProcess(double deltatime)
		{
			// Process things?
			base.ProcessThings = (BuilderPlug.Me.ShowVisualThings != 0);
			
			// Setup the move multiplier depending on gravity
			Vector3D movemultiplier = new Vector3D(1.0f, 1.0f, 1.0f);
			if(BuilderPlug.Me.UseGravity) movemultiplier.z = 0.0f;
			General.Map.VisualCamera.MoveMultiplier = movemultiplier;
			
			// Apply gravity?
			if(BuilderPlug.Me.UseGravity && (General.Map.VisualCamera.Sector != null))
			{
				// Camera below floor level?
				if(General.Map.VisualCamera.Position.z <= (General.Map.VisualCamera.Sector.FloorHeight + CAMERA_FLOOR_OFFSET + 0.1f))
				{
					// Stay above floor
					gravity = new Vector3D(0.0f, 0.0f, 0.0f);
					General.Map.VisualCamera.Position = new Vector3D(General.Map.VisualCamera.Position.x,
																	 General.Map.VisualCamera.Position.y,
																	 General.Map.VisualCamera.Sector.FloorHeight + CAMERA_FLOOR_OFFSET);
				}
				else
				{
					// Fall down
					gravity += new Vector3D(0.0f, 0.0f, (float)(GRAVITY * deltatime));
					General.Map.VisualCamera.Position += gravity;
				}
				
				// Camera above ceiling level?
				if(General.Map.VisualCamera.Position.z >= (General.Map.VisualCamera.Sector.CeilHeight - CAMERA_CEILING_OFFSET - 0.1f))
				{
					// Stay below ceiling
					General.Map.VisualCamera.Position = new Vector3D(General.Map.VisualCamera.Position.x,
																	 General.Map.VisualCamera.Position.y,
																	 General.Map.VisualCamera.Sector.CeilHeight - CAMERA_CEILING_OFFSET);
				}
			}
			else
			{
				gravity = new Vector3D(0.0f, 0.0f, 0.0f);
			}
			
			// Do processing
			base.OnProcess(deltatime);

			// Process visible geometry
			foreach(IVisualEventReceiver g in visiblegeometry)
			{
				g.OnProcess(deltatime);
			}
			
			// Time to pick a new target?
			if(General.Clock.CurrentTime > (lastpicktime + PICK_INTERVAL))
			{
				PickTargetUnlocked();
				lastpicktime = General.Clock.CurrentTime;
			}
			
			// The mouse is always in motion
			MouseEventArgs args = new MouseEventArgs(General.Interface.MouseButtons, 0, 0, 0, 0);
			OnMouseMove(args);
		}
		
		// This draws a frame
		public override void OnRedrawDisplay()
		{
			// Start drawing
			if(renderer.Start())
			{
				// Use fog!
				renderer.SetFogMode(true);

				// Set target for highlighting
				renderer.SetHighlightedObject(target.picked);
				
				// Begin with geometry
				renderer.StartGeometry();

				// Render all visible sectors
				foreach(VisualGeometry g in visiblegeometry)
					renderer.AddSectorGeometry(g);

				if(BuilderPlug.Me.ShowVisualThings != 0)
				{
					// Render things in cages?
					renderer.DrawThingCages = ((BuilderPlug.Me.ShowVisualThings & 2) != 0);
					
					// Render all visible things
					foreach(VisualThing t in visiblethings)
						renderer.AddThingGeometry(t);
				}
				
				// Done rendering geometry
				renderer.FinishGeometry();
				
				// Render crosshair
				renderer.RenderCrosshair();
				
				// Present!
				renderer.Finish();
			}
		}
		
		// After resources were reloaded
		protected override void ResourcesReloaded()
		{
			base.ResourcesReloaded();
			PickTarget();
		}
		
		// Mouse moves
		public override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if(target.picked != null) (target.picked as IVisualEventReceiver).OnMouseMove(e);
		}
		
		#endregion

		#region ================== Action Assist

		// Because some actions can only be called on a single (the targeted) object because
		// they show a dialog window or something, these functions help applying the result
		// to all compatible selected objects.
		
		// Apply texture offsets
		public void ApplyTextureOffsetChange(int dx, int dy)
		{
			foreach(IVisualEventReceiver i in selectedobjects)
			{
				i.OnChangeTextureOffset(dx, dy);
			}
		}

		// Apply upper unpegged flag
		public void ApplyUpperUnpegged(bool set)
		{
			foreach(IVisualEventReceiver i in selectedobjects)
			{
				i.ApplyUpperUnpegged(set);
			}
		}

		// Apply lower unpegged flag
		public void ApplyLowerUnpegged(bool set)
		{
			foreach(IVisualEventReceiver i in selectedobjects)
			{
				i.ApplyLowerUnpegged(set);
			}
		}

		// Apply texture change
		public void ApplySelectTexture(string texture, bool flat)
		{
			if(General.Map.Config.MixTexturesFlats)
			{
				// Apply on all compatible types
				foreach(IVisualEventReceiver i in selectedobjects)
				{
					i.ApplyTexture(texture);
				}
			}
			else
			{
				// We don't want to mix textures and flats, so apply only on the same type
				foreach(IVisualEventReceiver i in selectedobjects)
				{
					if(((i is BaseVisualGeometrySector) && flat) ||
					   ((i is BaseVisualGeometrySidedef) && !flat))
					{
						i.ApplyTexture(texture);
					}
				}
			}
		}

		// This returns all selected sectors
		public List<Sector> GetSelectedSectors()
		{
			List<Sector> sectors = new List<Sector>();
			foreach(IVisualEventReceiver i in selectedobjects)
			{
				if(i is BaseVisualGeometrySector) sectors.Add((i as BaseVisualGeometrySector).Sector.Sector);
			}
			return sectors;
		}

		// This returns all selected linedefs
		public List<Linedef> GetSelectedLinedefs()
		{
			List<Linedef> linedefs = new List<Linedef>();
			foreach(IVisualEventReceiver i in selectedobjects)
			{
				if(i is BaseVisualGeometrySidedef) linedefs.Add((i as BaseVisualGeometrySidedef).Sidedef.Line);
			}
			return linedefs;
		}

		// This returns all selected things
		public List<Thing> GetSelectedThings()
		{
			List<Thing> things = new List<Thing>();
			foreach(IVisualEventReceiver i in selectedobjects)
			{
				if(i is BaseVisualThing) things.Add((i as BaseVisualThing).Thing);
			}
			return things;
		}

		#endregion

		#region ================== Actions

		[BeginAction("clearselection", BaseAction = true)]
		public void ClearSelection()
		{
			selectedobjects = new List<IVisualEventReceiver>();
			
			foreach(KeyValuePair<Sector, VisualSector> vs in allsectors)
			{
				BaseVisualSector bvs = (BaseVisualSector)vs.Value;
				if(bvs.Floor != null) bvs.Floor.Selected = false;
				if(bvs.Ceiling != null) bvs.Ceiling.Selected = false;
				foreach(Sidedef sd in vs.Key.Sidedefs)
				{
					List<VisualGeometry> sidedefgeos = bvs.GetSidedefGeometry(sd);
					foreach(VisualGeometry sdg in sidedefgeos)
					{
						sdg.Selected = false;
					}
				}
			}

			foreach(KeyValuePair<Thing, VisualThing> vt in allthings)
			{
				BaseVisualThing bvt = (BaseVisualThing)vt.Value;
				bvt.Selected = false;
			}
		}

		[BeginAction("visualselect", BaseAction = true)]
		public void BeginSelect()
		{
			PreActionNoChange();
			PickTargetUnlocked();
			if(target.picked != null) (target.picked as IVisualEventReceiver).OnSelectBegin();
			PostAction();
		}

		[EndAction("visualselect", BaseAction = true)]
		public void EndSelect()
		{
			PreActionNoChange();
			if(target.picked != null) (target.picked as IVisualEventReceiver).OnSelectEnd();
			PostAction();
		}

		[BeginAction("visualedit", BaseAction = true)]
		public void BeginEdit()
		{
			PreAction(false);
			if(target.picked != null) (target.picked as IVisualEventReceiver).OnEditBegin();
			PostAction();
		}

		[EndAction("visualedit", BaseAction = true)]
		public void EndEdit()
		{
			PreAction(false);
			if(target.picked != null) (target.picked as IVisualEventReceiver).OnEditEnd();
			PostAction();
		}

		[BeginAction("raisesector8")]
		public void RaiseSector8()
		{
			PreAction(true);
			foreach(IVisualEventReceiver i in selectedobjects) i.OnChangeTargetHeight(8);
			PostAction();
		}

		[BeginAction("lowersector8")]
		public void LowerSector8()
		{
			PreAction(true);
			foreach(IVisualEventReceiver i in selectedobjects) i.OnChangeTargetHeight(-8);
			PostAction();
		}

		[BeginAction("raisesector1")]
		public void RaiseSector1()
		{
			PreAction(true);
			foreach(IVisualEventReceiver i in selectedobjects) i.OnChangeTargetHeight(1);
			PostAction();
		}
		
		[BeginAction("lowersector1")]
		public void LowerSector1()
		{
			PreAction(true);
			foreach(IVisualEventReceiver i in selectedobjects) i.OnChangeTargetHeight(-1);
			PostAction();
		}

		[BeginAction("showvisualthings")]
		public void ShowVisualThings()
		{
			BuilderPlug.Me.ShowVisualThings++;
			if(BuilderPlug.Me.ShowVisualThings > 2) BuilderPlug.Me.ShowVisualThings = 0;
		}

		[BeginAction("raisebrightness8")]
		public void RaiseBrightness8()
		{
			PreAction(true);
			foreach(IVisualEventReceiver i in selectedobjects) i.OnChangeTargetBrightness(true);
			PostAction();
		}

		[BeginAction("lowerbrightness8")]
		public void LowerBrightness8()
		{
			PreAction(true);
			foreach(IVisualEventReceiver i in selectedobjects) i.OnChangeTargetBrightness(false);
			PostAction();
		}

		[BeginAction("movetextureleft")]
		public void MoveTextureLeft1()
		{
			PreAction(true);
			foreach(IVisualEventReceiver i in selectedobjects) i.OnChangeTextureOffset(-1, 0);
			PostAction();
		}

		[BeginAction("movetextureright")]
		public void MoveTextureRight1()
		{
			PreAction(true);
			foreach(IVisualEventReceiver i in selectedobjects) i.OnChangeTextureOffset(1, 0);
			PostAction();
		}

		[BeginAction("movetextureup")]
		public void MoveTextureUp1()
		{
			PreAction(true);
			foreach(IVisualEventReceiver i in selectedobjects) i.OnChangeTextureOffset(0, -1);
			PostAction();
		}

		[BeginAction("movetexturedown")]
		public void MoveTextureDown1()
		{
			PreAction(true);
			foreach(IVisualEventReceiver i in selectedobjects) i.OnChangeTextureOffset(0, 1);
			PostAction();
		}

		[BeginAction("movetextureleft8")]
		public void MoveTextureLeft8()
		{
			PreAction(true);
			foreach(IVisualEventReceiver i in selectedobjects) i.OnChangeTextureOffset(-8, 0);
			PostAction();
		}

		[BeginAction("movetextureright8")]
		public void MoveTextureRight8()
		{
			PreAction(true);
			foreach(IVisualEventReceiver i in selectedobjects) i.OnChangeTextureOffset(8, 0);
			PostAction();
		}

		[BeginAction("movetextureup8")]
		public void MoveTextureUp8()
		{
			PreAction(true);
			foreach(IVisualEventReceiver i in selectedobjects) i.OnChangeTextureOffset(0, -8);
			PostAction();
		}

		[BeginAction("movetexturedown8")]
		public void MoveTextureDown8()
		{
			PreAction(true);
			foreach(IVisualEventReceiver i in selectedobjects) i.OnChangeTextureOffset(0, 8);
			PostAction();
		}

		[BeginAction("textureselect")]
		public void TextureSelect()
		{
			PreAction(false);
			renderer.SetCrosshairBusy(true);
			General.Interface.RedrawDisplay();
			if(target.picked != null) (target.picked as IVisualEventReceiver).OnSelectTexture();
			UpdateChangedObjects();
			renderer.SetCrosshairBusy(false);
			PostAction();
		}

		[BeginAction("texturecopy")]
		public void TextureCopy()
		{
			PreActionNoChange();
			if(target.picked != null) (target.picked as IVisualEventReceiver).OnCopyTexture();
			PostAction();
		}

		[BeginAction("texturepaste")]
		public void TexturePaste()
		{
			PreAction(false);
			foreach(IVisualEventReceiver i in selectedobjects) i.OnPasteTexture();
			PostAction();
		}

		[BeginAction("visualautoalignx")]
		public void TextureAutoAlignX()
		{
			PreAction(false);
			renderer.SetCrosshairBusy(true);
			General.Interface.RedrawDisplay();
			if(target.picked != null) (target.picked as IVisualEventReceiver).OnTextureAlign(true, false);
			UpdateChangedObjects();
			renderer.SetCrosshairBusy(false);
			PostAction();
		}

		[BeginAction("visualautoaligny")]
		public void TextureAutoAlignY()
		{
			PreAction(false);
			renderer.SetCrosshairBusy(true);
			General.Interface.RedrawDisplay();
			if(target.picked != null) (target.picked as IVisualEventReceiver).OnTextureAlign(false, true);
			UpdateChangedObjects();
			renderer.SetCrosshairBusy(false);
			PostAction();
		}

		[BeginAction("toggleupperunpegged")]
		public void ToggleUpperUnpegged()
		{
			PreAction(false);
			if(target.picked != null) (target.picked as IVisualEventReceiver).OnToggleUpperUnpegged();
			PostAction();
		}

		[BeginAction("togglelowerunpegged")]
		public void ToggleLowerUnpegged()
		{
			PreAction(false);
			if(target.picked != null) (target.picked as IVisualEventReceiver).OnToggleLowerUnpegged();
			PostAction();
		}

		[BeginAction("togglegravity")]
		public void ToggleGravity()
		{
			BuilderPlug.Me.UseGravity = !BuilderPlug.Me.UseGravity;
			string onoff = BuilderPlug.Me.UseGravity ? "ON" : "OFF";
			General.Interface.DisplayStatus(StatusType.Action, "Gravity is now " + onoff + ".");
		}

		[BeginAction("togglebrightness")]
		public void ToggleBrightness()
		{
			renderer.FullBrightness = !renderer.FullBrightness;
			string onoff = renderer.FullBrightness ? "ON" : "OFF";
			General.Interface.DisplayStatus(StatusType.Action, "Full Brightness is now " + onoff + ".");
		}

		[BeginAction("resettexture")]
		public void ResetTexture()
		{
			PreAction(true);
			foreach(IVisualEventReceiver i in selectedobjects) i.OnResetTextureOffset();
			PostAction();
		}

		[BeginAction("floodfilltextures")]
		public void FloodfillTextures()
		{
			PreAction(false);
			if(target.picked != null) (target.picked as IVisualEventReceiver).OnTextureFloodfill();
			PostAction();
		}

		[BeginAction("texturecopyoffsets")]
		public void TextureCopyOffsets()
		{
			PreActionNoChange();
			if(target.picked != null) (target.picked as IVisualEventReceiver).OnCopyTextureOffsets();
			PostAction();
		}

		[BeginAction("texturepasteoffsets")]
		public void TexturePasteOffsets()
		{
			PreAction(false);
			foreach(IVisualEventReceiver i in selectedobjects) i.OnPasteTextureOffsets();
			PostAction();
		}

		[BeginAction("copyproperties")]
		public void CopyProperties()
		{
			PreActionNoChange();
			if(target.picked != null) (target.picked as IVisualEventReceiver).OnCopyProperties();
			PostAction();
		}

		[BeginAction("pasteproperties")]
		public void PasteProperties()
		{
			PreAction(false);
			foreach(IVisualEventReceiver i in selectedobjects) i.OnPasteProperties();
			PostAction();
		}
		
		[BeginAction("insertitem", BaseAction = true)]
		public void Insert()
		{
			PreAction(false);
			foreach(IVisualEventReceiver i in selectedobjects) i.OnInsert();
			PostAction();
		}

		[BeginAction("deleteitem", BaseAction = true)]
		public void Delete()
		{
			PreAction(false);
			foreach(IVisualEventReceiver i in selectedobjects) i.OnDelete();
			PostAction();
		}
		
		#endregion
	}
}
