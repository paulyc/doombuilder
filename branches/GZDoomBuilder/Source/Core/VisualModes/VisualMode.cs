
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
using CodeImp.DoomBuilder.Windows;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Actions;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Editing;

#endregion

namespace CodeImp.DoomBuilder.VisualModes
{
	/// <summary>
	/// Provides specialized functionality for a visual (3D) Doom Builder editing mode.
	/// </summary>
	public abstract class VisualMode : EditMode
	{
		#region ================== Constants

		private const float MOVE_SPEED_MULTIPLIER = 0.001f;
		
		#endregion

		#region ================== Variables

		// 3D Mode thing
		protected Thing modething;
		
		// Graphics
		protected IRenderer3D renderer;
		private Renderer3D renderer3d;
		
		// Options
		private bool processgeometry;
		private bool processthings;
		
		// Input
		private bool keyforward;
		private bool keybackward;
		private bool keyleft;
		private bool keyright;
        private bool keyup;
        private bool keydown;

        //mxd
        private List<VisualThing> selectedVisualThings;
        private List<VisualSector> selectedVisualSectors;
		//protected List<VisualVertexPair> vertices;
		protected Dictionary<Vertex, VisualVertexPair> vertices;
        //used in "Play From Here" Action
        private Thing playerStart;
        private Vector3D playerStartPosition;
        private float playerStartAngle;

		// Map
		protected VisualBlockMap blockmap;
		protected Dictionary<Thing, VisualThing> allthings;
		protected Dictionary<Sector, VisualSector> allsectors;
		protected List<VisualBlockEntry> visibleblocks;
		protected List<VisualThing> visiblethings;
		protected Dictionary<Sector, VisualSector> visiblesectors;
		protected List<VisualGeometry> visiblegeometry;
		
		#endregion

		#region ================== Properties

		public bool ProcessGeometry { get { return processgeometry; } set { processgeometry = value; } }
		public bool ProcessThings { get { return processthings; } set { processthings = value; } }
		public VisualBlockMap BlockMap { get { return blockmap; } }

		// Rendering
		public IRenderer3D Renderer { get { return renderer; } }
		
		#endregion

		#region ================== Constructor / Disposer

		/// <summary>
		/// Provides specialized functionality for a visual (3D) Doom Builder editing mode.
		/// </summary>
		public VisualMode()
		{
			// Initialize
			this.renderer = General.Map.Renderer3D;
			this.renderer3d = (Renderer3D)General.Map.Renderer3D;
			this.blockmap = new VisualBlockMap();
			this.allsectors = new Dictionary<Sector, VisualSector>(General.Map.Map.Sectors.Count);
			this.allthings = new Dictionary<Thing,VisualThing>(General.Map.Map.Things.Count);
			this.visibleblocks = new List<VisualBlockEntry>();
			this.visiblesectors = new Dictionary<Sector, VisualSector>(50);
			this.visiblegeometry = new List<VisualGeometry>(200);
			this.visiblethings = new List<VisualThing>(100);
			this.processgeometry = true;
			this.processthings = true;

			//mxd
			if(General.Map.UDMF) this.vertices = new Dictionary<Vertex,VisualVertexPair>();

            //mxd. Synch camera position to cursor position or center of the screen in 2d-mode
            if (General.Settings.GZSynchCameras && General.Editing.Mode is ClassicMode) {
                ClassicMode oldmode = General.Editing.Mode as ClassicMode;
                Vector2D pos2d;

                if (oldmode.IsMouseInside)
                    pos2d = new Vector2D(oldmode.MouseMapPos.x, oldmode.MouseMapPos.y);
                 else
                    pos2d = new Vector2D(General.Map.CRenderer2D.Viewport.Left + General.Map.CRenderer2D.Viewport.Width / 2.0f, General.Map.CRenderer2D.Viewport.Top + General.Map.CRenderer2D.Viewport.Height / 2.0f);

                //if position is inside sector - adjust camera.z accordingly
                Sector sector = General.Map.Map.GetSectorByCoordinates(pos2d);

                float posz = 0;
                if (sector != null) {
                    int sectorHeight = sector.CeilHeight - sector.FloorHeight;
                    if (sectorHeight < 41) {
                        posz = sector.FloorHeight + sectorHeight / 2;
                    } else {
                        posz = sector.FloorHeight + 41; // same as in doom
                    }
                }else{
                    posz = General.Map.VisualCamera.Position.z;
                }

                General.Map.VisualCamera.Position = new Vector3D(pos2d.x, pos2d.y, posz);
            }
		}
		
		// Disposer
		public override void Dispose()
		{
			// Not already disposed?
			if(!isdisposed)
			{
				// Clean up
				foreach(KeyValuePair<Sector, VisualSector> s in allsectors) s.Value.Dispose();
				blockmap.Dispose();
				visiblesectors = null;
				visiblegeometry = null;
				visibleblocks = null;
				visiblethings = null;
				allsectors = null;
				allthings = null;
				blockmap = null;

                //mxd
                selectedVisualSectors = null;
                selectedVisualThings = null;
				
				// Done
				base.Dispose();
			}
		}

		#endregion

		#region ================== Start / Stop

		// Mode is engaged
		public override void OnEngage()
		{
			base.OnEngage();
			
			General.Map.VisualCamera.PositionAtThing();
			
			// Update the used textures
			General.Map.Data.UpdateUsedTextures();
			
			// Fill the blockmap
			FillBlockMap();
			
			// Start special input mode
			General.Interface.EnableProcessing();
			General.Interface.StartExclusiveMouseInput();
		}

		// Mode is disengaged
		public override void OnDisengage()
		{
			base.OnDisengage();
			
			// Dispose
			foreach(KeyValuePair<Sector, VisualSector> vs in allsectors)
				if(vs.Value != null) vs.Value.Dispose();

			// Dispose
			foreach(KeyValuePair<Thing, VisualThing> vt in allthings)
				if(vt.Value != null) vt.Value.Dispose();	
			
			// Apply camera position to thing
			General.Map.VisualCamera.ApplyToThing();
			
			// Do not leave the sector on the camera
			General.Map.VisualCamera.Sector = null;

            //mxd
            selectedVisualSectors = null;
            selectedVisualThings = null;
			
			// Stop special input mode
			General.Interface.DisableProcessing();
			General.Interface.StopExclusiveMouseInput();
		}

		#endregion

		#region ================== Events

		public override bool OnUndoBegin()
		{
			renderer.SetCrosshairBusy(true);
			General.Interface.RedrawDisplay();
			return base.OnUndoBegin();
		}

		public override void OnUndoEnd()
		{
			base.OnUndoEnd();
			ResourcesReloadedPartial();
			renderer.SetCrosshairBusy(false);
		}

		public override bool OnRedoBegin()
		{
			renderer.SetCrosshairBusy(true);
			General.Interface.RedrawDisplay();
			return base.OnRedoBegin();
		}

		public override void OnRedoEnd()
		{
			base.OnRedoEnd();
			ResourcesReloadedPartial();
			renderer.SetCrosshairBusy(false);
		}

		public override void OnReloadResources()
		{
			base.OnReloadResources();
			ResourcesReloaded();
		}

        //mxd
        public override bool OnMapTestBegin() {
            if (General.Settings.GZTestFromCurrentPosition) {
                //find Single Player Start. Should have Type 1 in all games
                Thing start = null;

                foreach (Thing t in General.Map.Map.Things) {
                    if (t.Type == 1) {
                        //store thing and position
                        start = t;
                        break;
                    }
                }

                if (start == null) {
                    General.MainWindow.DisplayStatus(StatusType.Warning, "Can't test from current position: no Player 1 start found!");
                    return false;
                }

                //now check if camera is located inside a sector
                Vector3D camPos = General.Map.VisualCamera.Position;
                Sector s = General.Map.Map.GetSectorByCoordinates(new Vector2D(camPos.x, camPos.y));

                if (s == null) {
                    General.MainWindow.DisplayStatus(StatusType.Warning, "Can't test from current position: cursor is not inside sector!");
                    return false;
                }

                //41 = player's height in Doom. Is that so in all other games as well?
                if (s.CeilHeight - s.FloorHeight < 41) {
                    General.MainWindow.DisplayStatus(StatusType.Warning, "Can't test from current position: sector is too low!");
                    return false;
                }

                //check camera Z
				float pz = camPos.z - s.FloorHeight;
				int ceilRel = s.CeilHeight - s.FloorHeight - 41; //relative ceiling height
				if(pz > ceilRel) pz = ceilRel; //above ceiling?
				else if(pz < 0) pz = 0; //below floor?

                //store initial position
                playerStart = start;
                playerStartPosition = start.Position;
                playerStartAngle = start.Angle;

                //everything should be valid, let's move player start here
                start.Move(new Vector3D(camPos.x, camPos.y, pz));
                start.Rotate(General.Map.VisualCamera.AngleXY - Angle2D.PI);// (float)Math.PI);
            }
            return true;
        }

        //mxd
        public override void OnMapTestEnd() {
            if (General.Settings.GZTestFromCurrentPosition) {
                //restore position
                playerStart.Move(playerStartPosition);
                playerStart.Rotate(playerStartAngle);
                playerStart = null;
            }
        }
		
		#endregion
		
		#region ================== Input

		// Mouse input
		public override void OnMouseInput(Vector2D delta)
		{
			base.OnMouseInput(delta);
			General.Map.VisualCamera.ProcessMouseInput(delta);
		}

		[BeginAction("moveforward", BaseAction = true)]
		public virtual void BeginMoveForward()
		{
			keyforward = true;
		}

		[EndAction("moveforward", BaseAction = true)]
		public virtual void EndMoveForward()
		{
			keyforward = false;
		}

		[BeginAction("movebackward", BaseAction = true)]
		public virtual void BeginMoveBackward()
		{
			keybackward = true;
		}

		[EndAction("movebackward", BaseAction = true)]
		public virtual void EndMoveBackward()
		{
			keybackward = false;
		}

		[BeginAction("moveleft", BaseAction = true)]
		public virtual void BeginMoveLeft()
		{
			keyleft = true;
		}

		[EndAction("moveleft", BaseAction = true)]
		public virtual void EndMoveLeft()
		{
			keyleft = false;
		}

		[BeginAction("moveright", BaseAction = true)]
		public virtual void BeginMoveRight()
		{
			keyright = true;
		}

		[EndAction("moveright", BaseAction = true)]
		public virtual void EndMoveRight()
		{
			keyright = false;
		}

        [BeginAction("moveup", BaseAction = true)]
        public virtual void BeginMoveUp()
        {
            keyup = true;
        }

        [EndAction("moveup", BaseAction = true)]
        public virtual void EndMoveUp()
        {
            keyup = false;
        }

        [BeginAction("movedown", BaseAction = true)]
        public virtual void BeginMoveDown()
        {
            keydown = true;
        }

        [EndAction("movedown", BaseAction = true)]
        public virtual void EndMoveDown()
        {
            keydown = false;
        }

        //mxd
        [BeginAction("movethingleft", BaseAction = true)]
        protected void moveSelectedThingsLeft() {
            moveSelectedThings(new Vector2D(0f, -General.Map.Grid.GridSize), false);
        }
        //mxd
        [BeginAction("movethingright", BaseAction = true)]
        protected void moveSelectedThingsRight() {
            moveSelectedThings(new Vector2D(0f, General.Map.Grid.GridSize), false);
        }
        //mxd
        [BeginAction("movethingfwd", BaseAction = true)]
        protected void moveSelectedThingsForward() {
            moveSelectedThings(new Vector2D(-General.Map.Grid.GridSize, 0f), false);
        }
        //mxd
        [BeginAction("movethingback", BaseAction = true)]
        protected void moveSelectedThingsBackward() {
            moveSelectedThings(new Vector2D(General.Map.Grid.GridSize, 0f), false);
        }
        //mxd
        [BeginAction("placethingatcursor", BaseAction = true)]
        protected void placeThingAtCursor() {
            Vector2D hitpos = GetHitPosition();
            if (!hitpos.IsFinite()) {
                General.Interface.DisplayStatus(StatusType.Warning, "Cannot place Thing here");
                return;
            }

            moveSelectedThings(new Vector2D((float)Math.Round(hitpos.x), (float)Math.Round(hitpos.y)), true);
        }

        //mxd. 
        public Vector2D GetHitPosition() {
            Vector3D start = General.Map.VisualCamera.Position;
            Vector3D delta = General.Map.VisualCamera.Target - General.Map.VisualCamera.Position;
            delta = delta.GetFixedLength(General.Settings.ViewDistance * 0.98f);
            VisualPickResult target = PickObject(start, start + delta);

            if (target.picked == null) return new Vector2D(float.NaN, float.NaN);

            //now find where exactly did we hit
            Vector2D hitCoords = new Vector2D();
            if (target.picked is VisualGeometry) {
                VisualGeometry vg = target.picked as VisualGeometry;
                hitCoords = getIntersection(start, start + delta, new Vector3D(vg.BoundingBox[0].X, vg.BoundingBox[0].Y, vg.BoundingBox[0].Z), new Vector3D(vg.Vertices[0].nx, vg.Vertices[0].ny, vg.Vertices[0].nz));
            } else if (target.picked is VisualThing) {
                VisualThing vt = target.picked as VisualThing;
                hitCoords = getIntersection(start, start + delta, new Vector3D(vt.BoundingBox[0].X, vt.BoundingBox[0].Y, vt.BoundingBox[0].Z), D3DDevice.V3D(vt.Center - vt.PositionV3));
            } else {
                return new Vector2D(float.NaN, float.NaN);
            }

            return hitCoords;
        }

        //mxd. This checks intersection between line and plane 
        protected Vector2D getIntersection(Vector3D start, Vector3D end, Vector3D planeCenter, Vector3D planeNormal) {
            Vector3D delta = new Vector3D(planeCenter.x - start.x, planeCenter.y - start.y, planeCenter.z - start.z);
            return start + Vector3D.DotProduct(planeNormal, delta) / Vector3D.DotProduct(planeNormal, end - start) * (end - start);
        }

        //mxd. Should move selected things in specified direction
        protected abstract void moveSelectedThings(Vector2D direction, bool absolutePosition);
		
		#endregion

		#region ================== Visibility Culling
		
		// This preforms visibility culling
		protected void DoCulling()
		{
			Dictionary<Linedef, Linedef> visiblelines = new Dictionary<Linedef, Linedef>(200);
			Vector2D campos2d = (Vector2D)General.Map.VisualCamera.Position;
			float viewdist = General.Settings.ViewDistance;
			
			// Make collections
			visiblesectors = new Dictionary<Sector, VisualSector>(visiblesectors.Count);
			visiblegeometry = new List<VisualGeometry>(visiblegeometry.Capacity);
			visiblethings = new List<VisualThing>(visiblethings.Capacity);

			// Get the blocks within view range
			visibleblocks = blockmap.GetFrustumRange(renderer.Frustum2D);
			
			// Fill collections with geometry and things
			foreach(VisualBlockEntry block in visibleblocks)
			{
				if(processgeometry)
				{
					// Lines
					foreach(Linedef ld in block.Lines)
					{
						// Line not already processed?
						if(!visiblelines.ContainsKey(ld))
						{
							// Add line if not added yet
							visiblelines.Add(ld, ld);

							// Which side of the line is the camera on?
							if(ld.SideOfLine(campos2d) < 0)
							{
								// Do front of line
								if(ld.Front != null) ProcessSidedefCulling(ld.Front);
							}
							else
							{
								// Do back of line
								if(ld.Back != null) ProcessSidedefCulling(ld.Back);
							}
						}
					}
				}

				if(processthings)
				{
					// Things
					foreach(Thing t in block.Things)
					{
						VisualThing vt;

						// Not filtered out?
						if(General.Map.ThingsFilter.IsThingVisible(t))
						{
							if(allthings.ContainsKey(t))
							{
								vt = allthings[t];
							}
							else
							{
								// Create new visual thing
								vt = CreateVisualThing(t);
								allthings.Add(t, vt);
							}

							if(vt != null)
							{
								visiblethings.Add(vt);
							}
						}
					}
				}
			}

			if(processgeometry)
			{
				// Find camera sector
				Linedef nld = MapSet.NearestLinedef(visiblelines.Values, campos2d);
				if(nld != null)
				{
					General.Map.VisualCamera.Sector = GetCameraSectorFromLinedef(nld);
				}
				else
				{
					// Exceptional case: no lines found in any nearby blocks!
					// This could happen in the middle of an extremely large sector and in this case
					// the above code will not have found any sectors/sidedefs for rendering.
					// Here we handle this special case with brute-force. Let's find the sector
					// the camera is in by searching the entire map and render that sector only.
					nld = General.Map.Map.NearestLinedef(campos2d);
					if(nld != null)
					{
						General.Map.VisualCamera.Sector = GetCameraSectorFromLinedef(nld);
						if(General.Map.VisualCamera.Sector != null)
						{
							foreach(Sidedef sd in General.Map.VisualCamera.Sector.Sidedefs)
							{
								float side = sd.Line.SideOfLine(campos2d);
								if(((side < 0) && sd.IsFront) ||
								   ((side > 0) && !sd.IsFront))
									ProcessSidedefCulling(sd);
							}
						}
						else
						{
							// Too far away from the map to see anything
							General.Map.VisualCamera.Sector = null;
						}
					}
					else
					{
						// Map is empty
						General.Map.VisualCamera.Sector = null;
					}
				}
			}
		}

		// This finds and adds visible sectors
		private void ProcessSidedefCulling(Sidedef sd)
		{
			VisualSector vs;
			
			// Find the visualsector and make it if needed
			if(allsectors.ContainsKey(sd.Sector))
			{
				// Take existing visualsector
				vs = allsectors[sd.Sector];
			}
			else
			{
				// Make new visualsector
				vs = CreateVisualSector(sd.Sector);
				if(vs != null) allsectors.Add(sd.Sector, vs);
			}
			
			if(vs != null)
			{
				// Add to visible sectors if not added yet
				if(!visiblesectors.ContainsKey(sd.Sector))
				{
					visiblesectors.Add(sd.Sector, vs);
					visiblegeometry.AddRange(vs.FixedGeometry);
				}
				
				// Add sidedef geometry
				visiblegeometry.AddRange(vs.GetSidedefGeometry(sd));
			}
		}

		// This returns the camera sector from linedef
		private Sector GetCameraSectorFromLinedef(Linedef ld)
		{
			if(ld.SideOfLine(General.Map.VisualCamera.Position) < 0)
			{
				if(ld.Front != null)
					return ld.Front.Sector;
				else
					return null;
			}
			else
			{
				if(ld.Back != null)
					return ld.Back.Sector;
				else
					return null;
			}
		}
		
		#endregion

		#region ================== Object Picking

		// This picks an object from the scene
		public VisualPickResult PickObject(Vector3D from, Vector3D to)
		{
            VisualPickResult result = new VisualPickResult();
			Line2D ray2d = new Line2D(from, to);
			Vector3D delta = to - from;
			
			// Setup no result
			result.picked = null;
			result.hitpos = new Vector3D();
			result.u_ray = 1.0f;
			
			// Find all blocks we are intersecting
			List<VisualBlockEntry> blocks = blockmap.GetLineBlocks(from, to);
			
			// Make collections
			Dictionary<Linedef, Linedef> lines = new Dictionary<Linedef, Linedef>(blocks.Count * 10);
			Dictionary<Sector, VisualSector> sectors = new Dictionary<Sector, VisualSector>(blocks.Count * 10);
			List<IVisualPickable> pickables = new List<IVisualPickable>(blocks.Count * 10);
			
			// Add geometry from the camera sector
			if((General.Map.VisualCamera.Sector != null) && allsectors.ContainsKey(General.Map.VisualCamera.Sector))
			{
				VisualSector vs = allsectors[General.Map.VisualCamera.Sector];
				sectors.Add(General.Map.VisualCamera.Sector, vs);
				foreach(VisualGeometry g in vs.FixedGeometry) pickables.Add(g);
			}
			
			// Go for all lines to see which ones we intersect
			// We will collect geometry from the sectors and sidedefs
			foreach(VisualBlockEntry b in blocks)
			{
				foreach(Linedef ld in b.Lines)
				{
					// Make sure we don't test a line twice
					if(!lines.ContainsKey(ld))
					{
						lines.Add(ld, ld);
						
						// Intersecting?
						float u;
						if(ld.Line.GetIntersection(ray2d, out u))
						{
							// Check on which side we are
							float side = ld.SideOfLine(ray2d.v1);
							
							// Calculate intersection point
							Vector3D intersect = from + delta * u;
							
							// We must add the sectors of both sides of the line
							// If we wouldn't, then aiming at a sector that is just within range
							// could result in an incorrect hit (because the far line of the
							// sector may not be included in this loop)
							if(ld.Front != null)
							{
								// Find the visualsector
								if(allsectors.ContainsKey(ld.Front.Sector))
								{
									VisualSector vs = allsectors[ld.Front.Sector];
									
									// Add sector if not already added
									if(!sectors.ContainsKey(ld.Front.Sector))
									{
										sectors.Add(ld.Front.Sector, vs);
										foreach(VisualGeometry g in vs.FixedGeometry)
										{
											// Must have content
											if(g.Triangles > 0)
												pickables.Add(g);
										}
									}
									
									// Add sidedef if on the front side
									if(side < 0.0f)
									{
										List<VisualGeometry> sidedefgeo = vs.GetSidedefGeometry(ld.Front);
										foreach(VisualGeometry g in sidedefgeo)
										{
											// Must have content
											if(g.Triangles > 0)
											{
												g.SetPickResults(intersect, u);
												pickables.Add(g);
											}
										}
									}
								}
							}
							
							// Add back side also
							if(ld.Back != null)
							{
								// Find the visualsector
								if(allsectors.ContainsKey(ld.Back.Sector))
								{
									VisualSector vs = allsectors[ld.Back.Sector];

									// Add sector if not already added
									if(!sectors.ContainsKey(ld.Back.Sector))
									{
										sectors.Add(ld.Back.Sector, vs);
										foreach(VisualGeometry g in vs.FixedGeometry)
										{
											// Must have content
											if(g.Triangles > 0)
												pickables.Add(g);
										}
									}

									// Add sidedef if on the front side
									if(side > 0.0f)
									{
										List<VisualGeometry> sidedefgeo = vs.GetSidedefGeometry(ld.Back);
										foreach(VisualGeometry g in sidedefgeo)
										{
											// Must have content
											if(g.Triangles > 0)
											{
												g.SetPickResults(intersect, u);
												pickables.Add(g);
											}
										}
									}
								}
							}
						}
					}
				}
			}
			
			// Add all the visible things
			foreach(VisualThing vt in visiblethings)
				pickables.Add(vt);

			//mxd. And all visual vertices
			if(General.Map.UDMF && General.Settings.GZShowVisualVertices) {
				foreach(KeyValuePair<Vertex, VisualVertexPair> pair in vertices)
					pickables.AddRange(pair.Value.Vertices);
			}
			
			// Now we have a list of potential geometry that lies along the trace line.
			// We still don't know what geometry actually hits, but we ruled out that which doesn't get even close.
			// This is still too much for accurate intersection testing, so we do a fast reject pass first.
			Vector3D direction = to - from;
			direction = direction.GetNormal();
			List<IVisualPickable> potentialpicks = new List<IVisualPickable>(pickables.Count);
			foreach(IVisualPickable p in pickables)
			{
				if(p.PickFastReject(from, to, direction)) potentialpicks.Add(p);
			}
			
			// Now we do an accurate intersection test for all resulting geometry
			// We keep only the closest hit!
			foreach(IVisualPickable p in potentialpicks)
			{
				float u = result.u_ray;
				if(p.PickAccurate(from, to, direction, ref u))
				{
					// Closer than previous find?
					if((u > 0.0f) && (u < result.u_ray))
					{
						result.u_ray = u;
						result.picked = p;
					}
				}
			}
			
			// Setup final result
			result.hitpos = from + to * result.u_ray;

			// Done
			return result;
		}
		
		#endregion

		#region ================== Processing
		
		/// <summary>
		/// This disposes all resources. Needed geometry will be rebuild automatically.
		/// </summary>
		protected virtual void ResourcesReloaded()
		{
			// Dispose
			foreach(KeyValuePair<Sector, VisualSector> vs in allsectors)
				if(vs.Value != null) vs.Value.Dispose();
				
			foreach(KeyValuePair<Thing, VisualThing> vt in allthings)
				if(vt.Value != null) vt.Value.Dispose();
				
			// Clear collections
			allsectors.Clear();
			allthings.Clear();
			visiblesectors.Clear();
			visibleblocks.Clear();
			visiblegeometry.Clear();
			visiblethings.Clear();

			if(General.Map.UDMF) vertices.Clear(); //mxd
			
			// Make new blockmap
			FillBlockMap();

			// Visibility culling (this re-creates the needed resources)
			DoCulling();
		}

		/// <summary>
		/// This disposes orphaned resources and resources on changed geometry.
		/// This usually happens when geometry is changed by undo, redo, cut or paste actions
		/// and uses the marks to check what needs to be reloaded.
		/// </summary>
		protected virtual void ResourcesReloadedPartial()
		{
			Dictionary<Sector, VisualSector> newsectors = new Dictionary<Sector,VisualSector>(allsectors.Count);
			
			// Neighbour sectors must be updated as well
			foreach(Sector s in General.Map.Map.Sectors)
			{
				if(s.Marked)
				{
					foreach(Sidedef sd in s.Sidedefs)
						if(sd.Other != null) sd.Other.Marked = true;
				}
			}
			
			// Go for all sidedefs to mark sectors that need updating
			foreach(Sidedef sd in General.Map.Map.Sidedefs)
				if(sd.Marked) sd.Sector.Marked = true;
			
			// Go for all vertices to mark linedefs that need updating
			foreach(Vertex v in General.Map.Map.Vertices)
			{
				if(v.Marked)
				{
					foreach(Linedef ld in v.Linedefs)
						ld.Marked = true;
				}
			}
			
			// Go for all linedefs to mark sectors that need updating
			foreach(Linedef ld in General.Map.Map.Linedefs)
			{
				if(ld.Marked)
				{
					if(ld.Front != null) ld.Front.Sector.Marked = true;
					if(ld.Back != null) ld.Back.Sector.Marked = true;
				}
			}
			
			// Dispose if source was disposed or marked
			foreach(KeyValuePair<Sector, VisualSector> vs in allsectors)
			{
				if(vs.Value != null)
				{
					if(vs.Key.IsDisposed || vs.Key.Marked)
						vs.Value.Dispose();
					else
						newsectors.Add(vs.Key, vs.Value);
				}
			}
			
			// Things depend on the sector they are in and because we can't
			// easily determine which ones changed, we dispose all things
			foreach(KeyValuePair<Thing, VisualThing> vt in allthings)
				if(vt.Value != null) vt.Value.Dispose();
			
			// Apply new lists
			allsectors = newsectors;
			allthings = new Dictionary<Thing, VisualThing>(allthings.Count);
			
			// Clear visibility collections
			visiblesectors.Clear();
			visibleblocks.Clear();
			visiblegeometry.Clear();
			visiblethings.Clear();
			
			// Make new blockmap
			FillBlockMap();
			
			// Visibility culling (this re-creates the needed resources)
			DoCulling();
		}
		
		/// <summary>
		/// Implement this to create an instance of your VisualSector implementation.
		/// </summary>
		protected abstract VisualSector CreateVisualSector(Sector s);

		/// <summary>
		/// Implement this to create an instance of your VisualThing implementation.
		/// </summary>
		protected abstract VisualThing CreateVisualThing(Thing t);
		
		/// <summary>
		/// This returns the VisualSector for the given Sector.
		/// </summary>
		public VisualSector GetVisualSector(Sector s) { return allsectors[s]; }
		
		/// <summary>
		/// This returns the VisualThing for the given Thing.
		/// </summary>
		public VisualThing GetVisualThing(Thing t) { return allthings[t]; }

        //mxd
        public List<VisualThing> GetSelectedVisualThings(bool refreshSelection) {
            if (refreshSelection || selectedVisualThings == null) {
                selectedVisualThings = new List<VisualThing>();
                foreach (KeyValuePair<Thing, VisualThing> group in allthings) {
                    if (group.Value != null && group.Value.Selected)
                        selectedVisualThings.Add(group.Value);
                }

                //if nothing is selected - try to get thing from hilighted object
                if (selectedVisualThings.Count == 0) {
                    Vector3D start = General.Map.VisualCamera.Position;
                    Vector3D delta = General.Map.VisualCamera.Target - General.Map.VisualCamera.Position;
                    delta = delta.GetFixedLength(General.Settings.ViewDistance * 0.98f);
                    VisualPickResult target = PickObject(start, start + delta);

                    //not appropriate way to do this, but...
                    if (target.picked != null && target.picked is VisualThing)
                        selectedVisualThings.Add((VisualThing)target.picked);
                }
            }

            return selectedVisualThings;
        }

        /// <summary>
        /// mxd. This returns list of selected sectors based on surfaces selected in visual mode
        /// </summary>
        public List<VisualSector> GetSelectedVisualSectors(bool refreshSelection) {
            if (refreshSelection || selectedVisualSectors == null) {
                selectedVisualSectors = new List<VisualSector>();
                foreach (KeyValuePair<Sector, VisualSector> group in allsectors) {
                    foreach (VisualGeometry vg in group.Value.AllGeometry) {
                        if (vg.Selected) {
                            selectedVisualSectors.Add(group.Value);
                            break;
                        }
                    }
                }

                //if nothing is selected - try to get sector from hilighted object
                if (selectedVisualSectors.Count == 0) {
                    VisualGeometry vg = getHilightedSurface();
                    if (vg != null) selectedVisualSectors.Add(vg.Sector);
                }
            }
            return selectedVisualSectors;
        }

        /// <summary>
        /// mxd. This returns list of surfaces selected in visual mode
        /// </summary>
        public List<VisualGeometry> GetSelectedSurfaces() {
            List<VisualGeometry> selectedSurfaces = new List<VisualGeometry>();
            foreach (KeyValuePair<Sector, VisualSector> group in allsectors) {
                foreach (VisualGeometry vg in group.Value.AllGeometry) {
                    if (vg.Selected)
                        selectedSurfaces.Add(vg);
                }
            }

            //if nothing is selected - try to get hilighted surface
            if (selectedSurfaces.Count == 0) {
                VisualGeometry vg = getHilightedSurface();
                if (vg != null) selectedSurfaces.Add(vg);
            }
            return selectedSurfaces;
        }

        //mxd
        private VisualGeometry getHilightedSurface() {
            Vector3D start = General.Map.VisualCamera.Position;
            Vector3D delta = General.Map.VisualCamera.Target - General.Map.VisualCamera.Position;
            delta = delta.GetFixedLength(General.Settings.ViewDistance * 0.98f);
            VisualPickResult target = PickObject(start, start + delta);

			if(target.picked != null && target.picked is VisualGeometry) {
                VisualGeometry vg = (VisualGeometry)target.picked;
                if (vg.Sector != null)
                    return vg;
            }
            return null;
        }

		/// <summary>
		/// Returns True when a VisualSector has been created for the specified Sector.
		/// </summary>
		public bool VisualSectorExists(Sector s) { return allsectors.ContainsKey(s) && (allsectors[s] != null); }

		/// <summary>
		/// Returns True when a VisualThing has been created for the specified Thing.
		/// </summary>
		public bool VisualThingExists(Thing t) { return allthings.ContainsKey(t) && (allthings[t] != null); }

		/// <summary>
		/// This is called when the blockmap needs to be refilled, because it was invalidated.
		/// This usually happens when geometry is changed by undo, redo, cut or paste actions.
		/// Lines and Things are added to the block map by the base implementation.
		/// </summary>
		protected virtual void FillBlockMap()
		{
			if(blockmap != null) blockmap.Dispose();
			blockmap = new VisualBlockMap();
			
			blockmap.AddLinedefsSet(General.Map.Map.Linedefs);
			blockmap.AddThingsSet(General.Map.Map.Things);
			blockmap.AddSectorsSet(General.Map.Map.Sectors);
		}
		
		/// <summary>
		/// While this mode is active, this is called continuously to process whatever needs processing.
		/// </summary>
		public override void OnProcess(float deltatime)
		{
			float multiplier;
			
			base.OnProcess(deltatime);
			
			// Camera vectors
			Vector3D camvec = Vector3D.FromAngleXYZ(General.Map.VisualCamera.AngleXY, General.Map.VisualCamera.AngleZ);
			Vector3D camvecstrafe = Vector3D.FromAngleXY(General.Map.VisualCamera.AngleXY + Angle2D.PIHALF);
			Vector3D cammovemul = General.Map.VisualCamera.MoveMultiplier;
			Vector3D camdeltapos = new Vector3D();
            Vector3D upvec = new Vector3D(0.0f, 0.0f, 1.0f);

			// Move the camera
			if(General.Interface.ShiftState) multiplier = MOVE_SPEED_MULTIPLIER * 2.0f; else multiplier = MOVE_SPEED_MULTIPLIER;
			if(keyforward) camdeltapos += camvec * cammovemul * (float)General.Settings.MoveSpeed * multiplier * deltatime;
			if(keybackward) camdeltapos -= camvec * cammovemul * (float)General.Settings.MoveSpeed * multiplier * deltatime;
			if(keyleft) camdeltapos -= camvecstrafe * cammovemul * (float)General.Settings.MoveSpeed * multiplier * deltatime;
			if(keyright) camdeltapos += camvecstrafe * cammovemul * (float)General.Settings.MoveSpeed * multiplier * deltatime;
            if(keyup) camdeltapos += upvec * cammovemul * (float)General.Settings.MoveSpeed * multiplier * deltatime;
            if(keydown) camdeltapos += -upvec * cammovemul * (float)General.Settings.MoveSpeed * multiplier * deltatime;
			
			// Move the camera
			General.Map.VisualCamera.ProcessMovement(camdeltapos);
			
			// Apply new camera matrices
			renderer.PositionAndLookAt(General.Map.VisualCamera.Position, General.Map.VisualCamera.Target);
			
			// Visibility culling
			DoCulling();
			
			// Update labels in main window
			General.MainWindow.UpdateCoordinates((Vector2D)General.Map.VisualCamera.Position);
			
			// Now redraw
			General.Interface.RedrawDisplay();
		}
		
		#endregion

		#region ================== Actions

		#endregion
	}
}