
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
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.VisualModes;
using CodeImp.DoomBuilder.Windows;

#endregion

namespace CodeImp.DoomBuilder.BuilderModes
{
	internal abstract class BaseVisualGeometrySector : VisualGeometry, IVisualEventReceiver
	{
		#region ================== Constants

		private const float DRAG_ANGLE_TOLERANCE = 0.06f;

		#endregion

		#region ================== Variables

		protected readonly BaseVisualMode mode;
		protected long setuponloadedtexture;

		// This is only used to see if this object has already received a change
		// in a multiselection. The Changed property on the BaseVisualSector is
		// used to indicate a rebuild is needed.
		protected bool changed;

		protected SectorLevel level;
		protected Effect3DFloor extrafloor;
		
		// Undo/redo
		private int undoticket;
		
		// UV dragging
		private float dragstartanglexy;
		private float dragstartanglez;
		private Vector3D dragorigin;
		private int startoffsetx;
		private int startoffsety;
		protected bool uvdragging;
		private int prevoffsetx;		// We have to provide delta offsets, but I don't
		private int prevoffsety;		// want to calculate with delta offsets to prevent
										// inaccuracy in the dragging.

		private static List<BaseVisualSector> updatelist; //mxd
		protected bool performautoselection; //mxd

		#endregion

		#region ================== Properties
		
		new public BaseVisualSector Sector { get { return (BaseVisualSector)base.Sector; } }
		public bool Changed { get { return changed; } set { changed = value; } }
		public SectorLevel Level { get { return level; } }
		public Effect3DFloor ExtraFloor { get { return extrafloor; } }

		#endregion

		#region ================== Constructor / Destructor

		// Constructor
		protected BaseVisualGeometrySector(BaseVisualMode mode, VisualSector vs) : base(vs)
		{
			this.mode = mode;
		}

		#endregion

		#region ================== Methods

		// This changes the height
		protected abstract void ChangeHeight(int amount);
		protected abstract void ChangeTextureScale(int incrementX, int incrementY); //mxd
		public virtual void SelectNeighbours(bool select, bool withSameTexture, bool withSameHeight) { } //mxd

		//mxd
		override protected void PerformAutoSelection()
		{
			if(!performautoselection) return;
			if(Triangles > 0)
			{
				this.selected = true;
				mode.AddSelectedObject(this);
			}

			performautoselection = false;
		}

		// This swaps triangles so that the plane faces the other way
		protected static void SwapTriangleVertices(WorldVertex[] verts)
		{
			// Swap some vertices to flip all triangles
			for(int i = 0; i < verts.Length; i += 3)
			{
				// Swap
				WorldVertex v = verts[i];
				verts[i] = verts[i + 1];
				verts[i + 1] = v;
			}
		}

		// This is called to update UV dragging
		protected virtual void UpdateDragUV()
		{
			float u_ray = 1.0f;

			// Calculate intersection position
			this.Level.plane.GetIntersection(General.Map.VisualCamera.Position, General.Map.VisualCamera.Target, ref u_ray);
			Vector3D intersect = General.Map.VisualCamera.Position + (General.Map.VisualCamera.Target - General.Map.VisualCamera.Position) * u_ray;

			// Calculate offsets
			Vector3D dragdelta = intersect - dragorigin;
			float offsetx = dragdelta.x;
			float offsety = dragdelta.y;

			bool lockX = General.Interface.CtrlState && !General.Interface.ShiftState;
			bool lockY = !General.Interface.CtrlState && General.Interface.ShiftState;

			if(lockX || lockY) 
			{
				float camAngle = Angle2D.RadToDeg(General.Map.VisualCamera.AngleXY);
				
				if(camAngle > 315 || camAngle < 46) 
				{
					if(lockX) offsetx = 0;
					if(lockY) offsety = 0;
				} 
				else if(camAngle > 225) 
				{
					if(lockX) offsety = 0;
					if(lockY) offsetx = 0;
				} 
				else if(camAngle > 135) 
				{
					if(lockX) offsetx = 0;
					if(lockY) offsety = 0;
				} 
				else 
				{
					if(lockX) offsety = 0;
					if(lockY) offsetx = 0;
				}
			}

			//mxd. Modify offsets based on surface and camera angles
			float angle;

			if(GeometryType == VisualGeometryType.CEILING)
				angle = Angle2D.DegToRad(level.sector.Fields.GetValue("rotationceiling", 0f));
			else
				angle = Angle2D.DegToRad(level.sector.Fields.GetValue("rotationfloor", 0f));

			Vector2D v = new Vector2D(offsetx, offsety).GetRotated(angle);

			offsetx = (int)Math.Round(v.x);
			offsety = (int)Math.Round(v.y);

			// Calculate deltas
			int deltax, deltay;
			if(General.Interface.CtrlState && General.Interface.ShiftState) 
			{ 
				//mxd. Clamp to grid size?
				int newoffsetx = startoffsetx - (int)Math.Round(offsetx);
				int newoffsety = startoffsety + (int)Math.Round(offsety);
				deltax = prevoffsetx - newoffsetx;
				deltay = prevoffsety - newoffsety;

				if(Math.Abs(deltax) >= General.Map.Grid.GridSize) 
				{
					deltax = General.Map.Grid.GridSize * Math.Sign(deltax);
					prevoffsetx = newoffsetx;
				} 
				else 
				{
					deltax = 0;
				}

				if(Math.Abs(deltay) >= General.Map.Grid.GridSize) 
				{
					deltay = General.Map.Grid.GridSize * Math.Sign(deltay);
					prevoffsety = newoffsety;
				} 
				else 
				{
					deltay = 0;
				}
			} 
			else 
			{
				int newoffsetx = startoffsetx - (int)Math.Round(offsetx);
				int newoffsety = startoffsety + (int)Math.Round(offsety);

				deltax = prevoffsetx - newoffsetx;
				deltay = prevoffsety - newoffsety;

				prevoffsetx = newoffsetx;
				prevoffsety = newoffsety;
			}

			//mxd. Apply offset?
			if(deltax != 0 || deltay != 0) mode.ApplyFlatOffsetChange(deltax, deltay);
			mode.ShowTargetInfo();
		}

		//mxd
		public override Sector GetControlSector() { return level.sector; }

		//mxd
		protected void AlignTextureToClosestLine(bool alignx, bool aligny) 
		{
			if(!(mode.HighlightedObject is BaseVisualSector)) return;
			
			// Do we need to align this? (and also grab texture scale while we are at it)
			float scaleX, scaleY;
			bool isFloor = (geometrytype == VisualGeometryType.FLOOR);

			if(mode.HighlightedTarget is VisualFloor) 
			{
				VisualFloor target = (VisualFloor)mode.HighlightedTarget;

				// Check texture
				if(target.Sector.Sector.FloorTexture != (isFloor ? Sector.Sector.FloorTexture : Sector.Sector.CeilTexture))	return;

				scaleX = target.Sector.Sector.Fields.GetValue("xscalefloor", 1.0f);
				scaleY = target.Sector.Sector.Fields.GetValue("yscalefloor", 1.0f);
			} 
			else 
			{
				VisualCeiling target = (VisualCeiling)mode.HighlightedTarget;

				// Check texture
				if(target.Sector.Sector.CeilTexture != (isFloor ? Sector.Sector.FloorTexture : Sector.Sector.CeilTexture)) return;

				scaleX = target.Sector.Sector.Fields.GetValue("xscaleceiling", 1.0f);
				scaleY = target.Sector.Sector.Fields.GetValue("yscaleceiling", 1.0f);
			}

			//find a linedef to align to
			Vector2D hitpos = mode.GetHitPosition();
			if(!hitpos.IsFinite()) return;

			//align to line of highlighted sector, which is closest to hitpos
			Sector highlightedSector = ((BaseVisualSector)mode.HighlightedObject).Sector;
			List<Linedef> lines = new List<Linedef>();
			foreach(Sidedef side in highlightedSector.Sidedefs)	lines.Add(side.Line);

			Linedef targetLine = MapSet.NearestLinedef(lines, hitpos);
			if(targetLine == null) return;

			bool isFront = targetLine.SideOfLine(hitpos) > 0;
			Sector.Sector.Fields.BeforeFieldsChange();

			//find an angle to rotate texture
			float sourceAngle = (float)Math.Round(General.ClampAngle(isFront ? -Angle2D.RadToDeg(targetLine.Angle) + 90 : -Angle2D.RadToDeg(targetLine.Angle) - 90), 1);
			if(!isFront) sourceAngle = General.ClampAngle(sourceAngle + 180);

			//update angle
			UniFields.SetFloat(Sector.Sector.Fields, (isFloor ? "rotationfloor" : "rotationceiling"), sourceAngle, 0f);

			//set scale
			UniFields.SetFloat(Sector.Sector.Fields, (isFloor ? "xscalefloor" : "xscaleceiling"), scaleX, 1.0f);
			UniFields.SetFloat(Sector.Sector.Fields, (isFloor ? "yscalefloor" : "yscaleceiling"), scaleY, 1.0f);

			//update offset
			float distToStart = Vector2D.Distance(hitpos, targetLine.Start.Position);
			float distToEnd = Vector2D.Distance(hitpos, targetLine.End.Position);
			Vector2D offset = (distToStart < distToEnd ? targetLine.Start.Position : targetLine.End.Position).GetRotated(Angle2D.DegToRad(sourceAngle));

			if(alignx) 
			{
				if(Texture != null && Texture.IsImageLoaded) offset.x %= Texture.Width / scaleX;
				UniFields.SetFloat(Sector.Sector.Fields, (isFloor ? "xpanningfloor" : "xpanningceiling"), (float)Math.Round(-offset.x), 0f);
			}

			if(aligny) 
			{
				if(Texture != null && Texture.IsImageLoaded) offset.y %= Texture.Height / scaleY;
				UniFields.SetFloat(Sector.Sector.Fields, (isFloor ? "ypanningfloor" : "ypanningceiling"), (float)Math.Round(offset.y), 0f);
			}

			//update geometry
			Sector.UpdateSectorGeometry(false);
		}

		//mxd
		protected void AlignTextureToSlopeLine(Linedef slopeSource, float slopeAngle, bool isFront, bool alignx, bool aligny) 
		{
			bool isFloor = (geometrytype == VisualGeometryType.FLOOR);
			Sector.Sector.Fields.BeforeFieldsChange();
			float sourceAngle = (float)Math.Round(General.ClampAngle(isFront ? -Angle2D.RadToDeg(slopeSource.Angle) + 90 : -Angle2D.RadToDeg(slopeSource.Angle) - 90), 1);

			if(isFloor) 
			{
				if((isFront && slopeSource.Front.Sector.FloorHeight > slopeSource.Back.Sector.FloorHeight) ||
				  (!isFront && slopeSource.Front.Sector.FloorHeight < slopeSource.Back.Sector.FloorHeight)) 
				{
					sourceAngle = General.ClampAngle(sourceAngle + 180);
				}
			} 
			else 
			{
				if((isFront && slopeSource.Front.Sector.CeilHeight < slopeSource.Back.Sector.CeilHeight) ||
				  (!isFront && slopeSource.Front.Sector.CeilHeight > slopeSource.Back.Sector.CeilHeight)) 
				{
					sourceAngle = General.ClampAngle(sourceAngle + 180);
				}
			}

			//update angle
			UniFields.SetFloat(Sector.Sector.Fields, (isFloor ? "rotationfloor" : "rotationceiling"), sourceAngle, 0f);

			//update scaleY
			string xScaleKey = (isFloor ? "xscalefloor" : "xscaleceiling");
			string yScaleKey = (isFloor ? "yscalefloor" : "yscaleceiling");

			float scaleX = Sector.Sector.Fields.GetValue(xScaleKey, 1.0f);
			float scaleY;

			//set scale
			if(aligny) 
			{
				scaleY = (float)Math.Round(scaleX * (1 / (float)Math.Cos(slopeAngle)), 2);
				UniFields.SetFloat(Sector.Sector.Fields, yScaleKey, scaleY, 1.0f);
			} 
			else 
			{
				scaleY = Sector.Sector.Fields.GetValue(yScaleKey, 1.0f);
			}

			//update texture offsets
			Vector2D offset;
			if(isFloor) 
			{
				if((isFront && slopeSource.Front.Sector.FloorHeight < slopeSource.Back.Sector.FloorHeight) ||
				  (!isFront && slopeSource.Front.Sector.FloorHeight > slopeSource.Back.Sector.FloorHeight)) 
				{
					offset = slopeSource.End.Position;
				} 
				else 
				{
					offset = slopeSource.Start.Position;
				}
			} 
			else 
			{
				if((isFront && slopeSource.Front.Sector.CeilHeight > slopeSource.Back.Sector.CeilHeight) ||
				  (!isFront && slopeSource.Front.Sector.CeilHeight < slopeSource.Back.Sector.CeilHeight)) 
				{
					offset = slopeSource.End.Position;
				} 
				else 
				{
					offset = slopeSource.Start.Position;
				}
			}

			offset = offset.GetRotated(Angle2D.DegToRad(sourceAngle));

			if(alignx) 
			{
				if(Texture != null && Texture.IsImageLoaded) offset.x %= Texture.Width / scaleX;
				UniFields.SetFloat(Sector.Sector.Fields, (isFloor ? "xpanningfloor" : "xpanningceiling"), (float)Math.Round(-offset.x), 0f);
			}

			if(aligny) 
			{
				if(Texture != null && Texture.IsImageLoaded) offset.y %= Texture.Height / scaleY;
				UniFields.SetFloat(Sector.Sector.Fields, (isFloor ? "ypanningfloor" : "ypanningceiling"), (float)Math.Round(offset.y), 0f);
			}

			//update geometry
			Sector.UpdateSectorGeometry(false);
		}

		//mxd
		protected void ClearFields(IEnumerable<string> keys, string undodescription, string resultdescription) 
		{
			if(!General.Map.UDMF) return;

			mode.CreateUndo(undodescription);
			mode.SetActionResult(resultdescription);
			level.sector.Fields.BeforeFieldsChange();

			foreach(string key in keys)
			{
				if(level.sector.Fields.ContainsKey(key))
				{
					level.sector.Fields.Remove(key);
					level.sector.UpdateNeeded = true;
				}
			}

			if(level.sector.UpdateNeeded)
			{
				if(level.sector != Sector.Sector && mode.VisualSectorExists(level.sector))
				{
					BaseVisualSector vs = (BaseVisualSector) mode.GetVisualSector(level.sector);
					vs.UpdateSectorGeometry(false);
				}
				else
				{
					Sector.UpdateSectorGeometry(false);
				}
			}
		}
		
		#endregion

		#region ================== Events

		// Unused
		public virtual void OnEditBegin() { }
		public virtual void OnTextureFit(FitTextureOptions options) { } //mxd
		public virtual void OnToggleUpperUnpegged() { }
		public virtual void OnToggleLowerUnpegged() { }
		public virtual void OnResetTextureOffset() { }
		public virtual void OnResetLocalTextureOffset() { } //mxd
		public virtual void OnCopyTextureOffsets() { }
		public virtual void OnPasteTextureOffsets() { }
		public virtual void OnInsert() { }
		protected virtual void SetTexture(string texturename) { }
		public virtual void ApplyUpperUnpegged(bool set) { }
		public virtual void ApplyLowerUnpegged(bool set) { }
		protected abstract void MoveTextureOffset(int offsetx, int offsety);
		protected abstract Point GetTextureOffset();

		// Setup this plane
		public bool Setup() { return this.Setup(this.level, this.extrafloor); }
		public virtual bool Setup(SectorLevel level, Effect3DFloor extrafloor)
		{
			this.level = level;
			this.extrafloor = extrafloor;
			return false;
		}

		// Begin select
		public virtual void OnSelectBegin()
		{
			mode.LockTarget();
			dragstartanglexy = General.Map.VisualCamera.AngleXY;
			dragstartanglez = General.Map.VisualCamera.AngleZ;
			dragorigin = pickintersect;
			startoffsetx = GetTextureOffset().X;
			startoffsety = GetTextureOffset().Y;
			prevoffsetx = GetTextureOffset().X;
			prevoffsety = GetTextureOffset().Y;
		}
		
		// Select or deselect
		public virtual void OnSelectEnd()
		{
			mode.UnlockTarget();
			
			// Was dragging?
			if(uvdragging)
			{
				// Dragging stops now
				uvdragging = false;
			}
			else
			{
				if(this.selected)
				{
					this.selected = false;
					mode.RemoveSelectedObject(this);
				}
				else
				{
					this.selected = true;
					mode.AddSelectedObject(this);
				}
			}
		}

		// Moving the mouse
		public virtual void OnMouseMove(MouseEventArgs e)
		{
			if(!General.Map.UDMF) return; //mxd. Cannot change texture offsets in other map formats...
			
			// Dragging UV?
			if(uvdragging)
			{
				UpdateDragUV();
			}
			else
			{
				// Select button pressed?
				if(General.Actions.CheckActionActive(General.ThisAssembly, "visualselect"))
				{
					// Check if tolerance is exceeded to start UV dragging
					float deltaxy = General.Map.VisualCamera.AngleXY - dragstartanglexy;
					float deltaz = General.Map.VisualCamera.AngleZ - dragstartanglez;
					if((Math.Abs(deltaxy) + Math.Abs(deltaz)) > DRAG_ANGLE_TOLERANCE)
					{
						mode.PreAction(UndoGroup.TextureOffsetChange);
						mode.CreateUndo("Change texture offsets");

						// Start drag now
						uvdragging = true;
						mode.Renderer.ShowSelection = false;
						mode.Renderer.ShowHighlight = false;
						UpdateDragUV();
					}
				}
			}
		}

		// Delete texture
		public virtual void OnDelete() 
		{
			// Remove texture
			mode.CreateUndo("Delete texture");
			mode.SetActionResult("Deleted a texture.");
			SetTexture("-");

			// Update
			if(mode.VisualSectorExists(level.sector))
			{
				BaseVisualSector vs = (BaseVisualSector)mode.GetVisualSector(level.sector);
				vs.UpdateSectorGeometry(false);
			}
		}
		
		// Processing
		public virtual void OnProcess(long deltatime)
		{
			// If the texture was not loaded, but is loaded now, then re-setup geometry
			if(setuponloadedtexture != 0)
			{
				ImageData t = General.Map.Data.GetFlatImage(setuponloadedtexture);
				if(t != null)
				{
					if(t.IsImageLoaded)
					{
						setuponloadedtexture = 0;
						Setup();
					}
				}
			}
		}

		// Flood-fill textures
		public virtual void OnTextureFloodfill()
		{
			if(BuilderPlug.Me.CopiedFlat != null)
			{
				string oldtexture = GetTextureName();
				string newtexture = BuilderPlug.Me.CopiedFlat;
				if(newtexture != oldtexture)
				{
					// Get the texture
					ImageData newtextureimage = General.Map.Data.GetFlatImage(newtexture);
					if(newtextureimage != null)
					{
						bool fillceilings = (this is VisualCeiling);
						
						if(fillceilings)
						{
							mode.CreateUndo("Flood-fill ceilings with " + newtexture);
							mode.SetActionResult("Flood-filled ceilings with " + newtexture + ".");
						}
						else
						{
							mode.CreateUndo("Flood-fill floors with " + newtexture);
							mode.SetActionResult("Flood-filled floors with " + newtexture + ".");
						}

						mode.Renderer.SetCrosshairBusy(true);
						General.Interface.RedrawDisplay();

						if(mode.IsSingleSelection)
						{
							// Clear all marks, this will align everything it can
							General.Map.Map.ClearMarkedSectors(false);
						}
						else
						{
							// Limit the alignment to selection only
							General.Map.Map.ClearMarkedSectors(true);
							List<Sector> sectors = mode.GetSelectedSectors();
							foreach(Sector s in sectors) s.Marked = false;
						}

						//mxd. We potentially need to deal with 2 textures (because of long and short texture names)...
						HashSet<long> oldtexturehashes = new HashSet<long> { Texture.LongName, Lump.MakeLongName(oldtexture) };
						
						// Do the fill
						Tools.FloodfillFlats(this.Sector.Sector, fillceilings, oldtexturehashes, newtexture, false);

						// Get the changed sectors
						List<Sector> changes = General.Map.Map.GetMarkedSectors(true);
						foreach(Sector s in changes)
						{
							// Update the visual sector
							if(mode.VisualSectorExists(s))
							{
								BaseVisualSector vs = (BaseVisualSector)mode.GetVisualSector(s);
								if(fillceilings) vs.Ceiling.Setup();
								else vs.Floor.Setup();
							}
						}

						General.Map.Data.UpdateUsedTextures();
						mode.Renderer.SetCrosshairBusy(false);
						mode.ShowTargetInfo();
					}
				}
			}
		}

		//mxd. Auto-align texture offsets
		public virtual void OnTextureAlign(bool alignx, bool aligny) 
		{
			if(!General.Map.UDMF) return;

			//create undo
			string rest;
			if(alignx && aligny) rest = "(X and Y)";
			else if(alignx)	rest = "(X)";
			else rest = "(Y)";

			mode.CreateUndo("Auto-align textures " + rest);
			mode.SetActionResult("Auto-aligned textures " + rest + ".");

			//get selection
			List<VisualGeometry> selection = mode.GetSelectedSurfaces();

			//align textures on slopes
			foreach(VisualGeometry vg in selection) 
			{
				if(vg.GeometryType == VisualGeometryType.FLOOR || vg.GeometryType == VisualGeometryType.CEILING) 
				{
					if(vg.GeometryType == VisualGeometryType.FLOOR)
						((VisualFloor)vg).AlignTexture(alignx, aligny);
					else
						((VisualCeiling)vg).AlignTexture(alignx, aligny);

					vg.Sector.Sector.UpdateNeeded = true;
					vg.Sector.Sector.UpdateCache();
				}
			}

			// Map is changed
			General.Map.Map.Update();
			General.Map.IsChanged = true;
			General.Interface.RefreshInfo();
		}
		
		// Copy properties
		public virtual void OnCopyProperties()
		{
			BuilderPlug.Me.CopiedSectorProps = new SectorProperties(level.sector);
			mode.SetActionResult("Copied sector properties.");
		}
		
		// Paste properties
		public virtual void OnPasteProperties(bool usecopysettings)
		{
			if(BuilderPlug.Me.CopiedSectorProps != null)
			{
				mode.CreateUndo("Paste sector properties");
				mode.SetActionResult("Pasted sector properties.");

				//mxd. Added "usecopysettings"
				BuilderPlug.Me.CopiedSectorProps.Apply(new List<Sector> { level.sector }, usecopysettings);

				if(mode.VisualSectorExists(level.sector))
				{
					BaseVisualSector vs = (BaseVisualSector)mode.GetVisualSector(level.sector);
					vs.UpdateSectorGeometry(true);
				}

				mode.ShowTargetInfo();
			}
		}
		
		// Select texture
		public virtual void OnSelectTexture()
		{
			if(General.Interface.IsActiveWindow)
			{
				string oldtexture = GetTextureName();
				string newtexture = General.Interface.BrowseFlat(General.Interface, oldtexture);
				if(newtexture != oldtexture)
				{
					mode.ApplySelectTexture(newtexture, true);
				}
			}
		}

		// Apply Texture
		public virtual void ApplyTexture(string texture)
		{
			mode.CreateUndo("Change flat \"" + texture + "\"");
			SetTexture(texture);

			// Update
			if(mode.VisualSectorExists(level.sector))
			{
				BaseVisualSector vs = (BaseVisualSector)mode.GetVisualSector(level.sector);
				vs.UpdateSectorGeometry(false);
			}
		}
		
		// Copy texture
		public virtual void OnCopyTexture()
		{
			//mxd. When UseLongTextureNames is enabled and the image filename is longer than 8 chars, use full name, 
			// otherwise use texture name as stored in Sector
			string texturename = ((General.Map.Options.UseLongTextureNames && Texture != null && Texture.UsedInMap 
				&& Path.GetFileNameWithoutExtension(Texture.Name).Length > DataManager.CLASIC_IMAGE_NAME_LENGTH) 
				? Texture.Name : GetTextureName());

			BuilderPlug.Me.CopiedFlat = texturename;
			if(General.Map.Config.MixTexturesFlats) BuilderPlug.Me.CopiedTexture = texturename;
			mode.SetActionResult("Copied flat \"" + texturename + "\".");
		}
		
		public virtual void OnPasteTexture() { }

		// Return texture name
		public virtual string GetTextureName() { return ""; }
		
		// Edit button released
		public virtual void OnEditEnd()
		{
			if(General.Interface.IsActiveWindow)
			{
				//mxd
				List<Sector> sectors = mode.GetSelectedSectors();
				updatelist = new List<BaseVisualSector>();

				foreach(Sector s in sectors) 
				{
					if(mode.VisualSectorExists(s)) 
						updatelist.Add((BaseVisualSector)mode.GetVisualSector(s));
				}

				General.Interface.OnEditFormValuesChanged += Interface_OnEditFormValuesChanged; //mxd
				mode.StartRealtimeInterfaceUpdate(SelectionType.Sectors); //mxd
				DialogResult result = General.Interface.ShowEditSectors(sectors);
				mode.StopRealtimeInterfaceUpdate(SelectionType.Sectors); //mxd
				General.Interface.OnEditFormValuesChanged -= Interface_OnEditFormValuesChanged; //mxd

				updatelist.Clear(); //mxd
				updatelist = null; //mxd

				if(result == DialogResult.OK) mode.RebuildElementData(); //mxd
			}
		}

		//mxd
		private void Interface_OnEditFormValuesChanged(object sender, EventArgs e) 
		{
			foreach(BaseVisualSector vs in updatelist) vs.UpdateSectorGeometry(true);
		}

		// Sector height change
		public virtual void OnChangeTargetHeight(int amount)
		{
			changed = true;

			ChangeHeight(amount);

			// Rebuild sector
			BaseVisualSector vs;
			if(mode.VisualSectorExists(level.sector)) 
			{
				vs = (BaseVisualSector)mode.GetVisualSector(level.sector);
			} 
			else 
			{
				//mxd. Need this to apply changes to 3d-floor even if control sector doesn't exist as BaseVisualSector
				vs = mode.CreateBaseVisualSector(level.sector);
			}

			if(vs != null) vs.UpdateSectorGeometry(true);
		}
		
		// Sector brightness change
		public virtual void OnChangeTargetBrightness(bool up)
		{
			mode.CreateUndo("Change sector brightness", UndoGroup.SectorBrightnessChange, Sector.Sector.FixedIndex);
			
			if(up)
				Sector.Sector.Brightness = General.Map.Config.BrightnessLevels.GetNextHigher(Sector.Sector.Brightness);
			else
				Sector.Sector.Brightness = General.Map.Config.BrightnessLevels.GetNextLower(Sector.Sector.Brightness);
			
			mode.SetActionResult("Changed sector brightness to " + Sector.Sector.Brightness + ".");

			Sector.Sector.UpdateCache();

			// Rebuild sector
			Sector.UpdateSectorGeometry(false);
		}

		// Texture offset change
		public virtual void OnChangeTextureOffset(int horizontal, int vertical, bool doSurfaceAngleCorrection)
		{
			if(horizontal == 0 && vertical == 0) return; //mxd
			
			//mxd
			if(!General.Map.UDMF) 
			{
				General.Interface.DisplayStatus(StatusType.Warning, "Floor/ceiling texture offsets cannot be changed in this map format!");
				return;
			}

			if((General.Map.UndoRedo.NextUndo == null) || (General.Map.UndoRedo.NextUndo.TicketID != undoticket))
				undoticket = mode.CreateUndo("Change texture offsets");

			//mxd
			changed = true;

			//mxd
			if(doSurfaceAngleCorrection)
			{
				Point p = new Point(horizontal, vertical);
				float angle = Angle2D.RadToDeg(General.Map.VisualCamera.AngleXY);
				if(GeometryType == VisualGeometryType.CEILING) 
					angle += level.sector.Fields.GetValue("rotationceiling", 0f);
				else
					angle += level.sector.Fields.GetValue("rotationfloor", 0f);

				angle = General.ClampAngle(angle);

				if(angle > 315 || angle < 46) 
				{
					//already correct
				} 
				else if(angle > 225) 
				{
					vertical = p.X;
					horizontal = -p.Y;
				} 
				else if(angle > 135) 
				{
					horizontal = -p.X;
					vertical = -p.Y;
				} 
				else 
				{
					vertical = -p.X;
					horizontal = p.Y;
				}
			}

			// Apply offsets
			MoveTextureOffset(-horizontal, -vertical);

			// Rebuild sector
			BaseVisualSector vs;
			if(mode.VisualSectorExists(level.sector))
			{
				vs = (BaseVisualSector)mode.GetVisualSector(level.sector);
			}
			else
			{
				//mxd. Need this to apply changes to 3d-floor even if control sector doesn't exist as BaseVisualSector
				vs = mode.CreateBaseVisualSector(level.sector);
			}

			if(vs != null) vs.UpdateSectorGeometry(false);
		}

		//mxd
		public virtual void OnChangeTextureRotation(float angle) 
		{
			if(!General.Map.UDMF) return;

			if((General.Map.UndoRedo.NextUndo == null) || (General.Map.UndoRedo.NextUndo.TicketID != undoticket))
				undoticket = mode.CreateUndo("Change texture rotation");

			string key = (GeometryType == VisualGeometryType.FLOOR ? "rotationfloor" : "rotationceiling");
			mode.SetActionResult( (GeometryType == VisualGeometryType.FLOOR ? "Floor" : "Ceiling") + " rotation changed to " + angle);

			// Set new angle
			Sector s = GetControlSector();
			s.Fields.BeforeFieldsChange();
			UniFields.SetFloat(s.Fields, key, angle, 0.0f);

			// Mark as changed
			changed = true;

			// Rebuild sector
			BaseVisualSector vs;
			if(mode.VisualSectorExists(level.sector))
			{
				vs = (BaseVisualSector)mode.GetVisualSector(level.sector);
			}
			else
			{
				//mxd. Need this to apply changes to 3d-floor even if control sector doesn't exist as BaseVisualSector
				vs = mode.CreateBaseVisualSector(level.sector);
			}

			if(vs != null) vs.UpdateSectorGeometry(false);
		}

		//mxd
		public virtual void OnChangeScale(int incrementX, int incrementY) 
		{
			if(!General.Map.UDMF || !Texture.IsImageLoaded) return;

			changed = true;

			if((General.Map.UndoRedo.NextUndo == null) || (General.Map.UndoRedo.NextUndo.TicketID != undoticket))
				undoticket = mode.CreateUndo("Change texture scale");

			// Adjust to camera view
			float angle = Angle2D.RadToDeg(General.Map.VisualCamera.AngleXY);
			if(GeometryType == VisualGeometryType.CEILING) angle += level.sector.Fields.GetValue("rotationceiling", 0f);
			else angle += level.sector.Fields.GetValue("rotationfloor", 0f);
			angle = General.ClampAngle(angle);

			if(angle > 315 || angle < 46)
			{
				ChangeTextureScale(incrementX, incrementY);
			}
			else if(angle > 225)
			{
				ChangeTextureScale(incrementY, incrementX);
			}
			else if(angle > 135)
			{
				ChangeTextureScale(incrementX, incrementY);
			}
			else
			{
				ChangeTextureScale(incrementY, incrementX);
			}

			// Rebuild sector
			BaseVisualSector vs;
			if(mode.VisualSectorExists(level.sector))
			{
				vs = (BaseVisualSector)mode.GetVisualSector(level.sector);
			}
			else
			{
				//mxd. Need this to apply changes to 3d-floor even if control sector doesn't exist as BaseVisualSector
				vs = mode.CreateBaseVisualSector(level.sector);
			}

			if(vs != null) vs.UpdateSectorGeometry(false);
		}
		
		#endregion
	}
}
