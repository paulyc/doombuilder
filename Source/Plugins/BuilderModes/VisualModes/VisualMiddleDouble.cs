
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
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Types;
using CodeImp.DoomBuilder.VisualModes;
using CodeImp.DoomBuilder.GZBuilder.Tools;

#endregion

namespace CodeImp.DoomBuilder.BuilderModes
{
	internal sealed class VisualMiddleDouble : BaseVisualGeometrySidedef
	{
		#region ================== Constants

		#endregion

		#region ================== Variables

		private bool repeatmidtex;
		private Plane topclipplane;
		private Plane bottomclipplane;
		
		#endregion

		#region ================== Properties

		#endregion

		#region ================== Constructor / Setup

		// Constructor
		public VisualMiddleDouble(BaseVisualMode mode, VisualSector vs, Sidedef s) : base(mode, vs, s)
		{
            //mxd
            geoType = VisualGeometryType.WALL_MIDDLE;
            
            // Set render pass
			this.RenderPass = RenderPass.Mask;
			
			// We have no destructor
			GC.SuppressFinalize(this);
		}
		
		// This builds the geometry. Returns false when no geometry created.
		public override bool Setup()
		{
			//mxd
			if(Sidedef.MiddleTexture.Length == 0 || Sidedef.MiddleTexture == "-") return false;
			
			Vector2D vl, vr;

            //mxd. lightfog flag support
            bool lightabsolute = Sidedef.Fields.GetValue("lightabsolute", false);
            bool ignoreUDMFLight = (!Sidedef.Fields.GetValue("lightfog", false) || !lightabsolute) && Sector.Sector.Fields.ContainsKey("fadecolor");
            int lightvalue = ignoreUDMFLight ? 0 : Sidedef.Fields.GetValue("light", 0); //mxd
            if (ignoreUDMFLight) lightabsolute = false;
			
			Vector2D tscale = new Vector2D(Sidedef.Fields.GetValue("scalex_mid", 1.0f),
										   Sidedef.Fields.GetValue("scaley_mid", 1.0f));
			Vector2D toffset = new Vector2D(Sidedef.Fields.GetValue("offsetx_mid", 0.0f),
											Sidedef.Fields.GetValue("offsety_mid", 0.0f));
			
			// Left and right vertices for this sidedef
			if(Sidedef.IsFront) {
				vl = new Vector2D(Sidedef.Line.Start.Position.x, Sidedef.Line.Start.Position.y);
				vr = new Vector2D(Sidedef.Line.End.Position.x, Sidedef.Line.End.Position.y);
			} else {
				vl = new Vector2D(Sidedef.Line.End.Position.x, Sidedef.Line.End.Position.y);
				vr = new Vector2D(Sidedef.Line.Start.Position.x, Sidedef.Line.Start.Position.y);
			}

			// Load sector data
			SectorData sd = mode.GetSectorData(Sidedef.Sector);
			SectorData osd = mode.GetSectorData(Sidedef.Other.Sector);
			if(!osd.Updated) osd.Update();

			// Load texture
			if ((Sidedef.MiddleTexture.Length > 0) && (Sidedef.MiddleTexture != "-")){
				base.Texture = General.Map.Data.GetTextureImage(Sidedef.LongMiddleTexture);
				if (base.Texture == null){
					base.Texture = General.Map.Data.UnknownTexture3D;
					setuponloadedtexture = Sidedef.LongMiddleTexture;
				} else {
					if (!base.Texture.IsImageLoaded)
						setuponloadedtexture = Sidedef.LongMiddleTexture;
				}
			} else {
				// Use missing texture
				base.Texture = General.Map.Data.MissingTexture3D;
				setuponloadedtexture = 0;
			}

			// Get texture scaled size
			Vector2D tsz = new Vector2D(base.Texture.ScaledWidth, base.Texture.ScaledHeight);
			tsz = tsz / tscale;

			// Get texture offsets
			Vector2D tof = new Vector2D(Sidedef.OffsetX, Sidedef.OffsetY);
			tof = tof + toffset;
			tof = tof / tscale;
			if(General.Map.Config.ScaledTextureOffsets && !base.Texture.WorldPanning)
				tof = tof * base.Texture.Scale;

			// Determine texture coordinates plane as they would be in normal circumstances.
			// We can then use this plane to find any texture coordinate we need.
			// The logic here is the same as in the original VisualMiddleSingle (except that
			// the values are stored in a TexturePlane)
			// NOTE: I use a small bias for the floor height, because if the difference in
			// height is 0 then the TexturePlane doesn't work!
			TexturePlane tp = new TexturePlane();
			float floorbias = (Sidedef.Sector.CeilHeight == Sidedef.Sector.FloorHeight) ? 1.0f : 0.0f;
			float geotop = (float)Math.Min(Sidedef.Sector.CeilHeight, Sidedef.Other.Sector.CeilHeight);
			float geobottom = (float)Math.Max(Sidedef.Sector.FloorHeight, Sidedef.Other.Sector.FloorHeight);
			if(Sidedef.Line.IsFlagSet(General.Map.Config.LowerUnpeggedFlag)) {
				// When lower unpegged is set, the middle texture is bound to the bottom
				tp.tlt.y = tsz.y - (float)(geotop - geobottom);
			}
			tp.trb.x = tp.tlt.x + Sidedef.Line.Length;
			tp.trb.y = tp.tlt.y + ((float)Sidedef.Sector.CeilHeight - ((float)Sidedef.Sector.FloorHeight + floorbias));

			// Apply texture offset
			tp.tlt += tof;
			tp.trb += tof;

			// Transform pixel coordinates to texture coordinates
			tp.tlt /= tsz;
			tp.trb /= tsz;

			// Left top and right bottom of the geometry that
			tp.vlt = new Vector3D(vl.x, vl.y, (float)Sidedef.Sector.CeilHeight);
			tp.vrb = new Vector3D(vr.x, vr.y, (float)Sidedef.Sector.FloorHeight + floorbias);

			// Make the right-top coordinates
			tp.trt = new Vector2D(tp.trb.x, tp.tlt.y);
			tp.vrt = new Vector3D(tp.vrb.x, tp.vrb.y, tp.vlt.z);

			// Keep top and bottom planes for intersection testing
			top = sd.Ceiling.plane;
			bottom = sd.Floor.plane;

			// Create initial polygon, which is just a quad between floor and ceiling
			WallPolygon poly = new WallPolygon();
			poly.Add(new Vector3D(vl.x, vl.y, sd.Floor.plane.GetZ(vl)));
			poly.Add(new Vector3D(vl.x, vl.y, sd.Ceiling.plane.GetZ(vl)));
			poly.Add(new Vector3D(vr.x, vr.y, sd.Ceiling.plane.GetZ(vr)));
			poly.Add(new Vector3D(vr.x, vr.y, sd.Floor.plane.GetZ(vr)));

			// Determine initial color
			int lightlevel = lightabsolute ? lightvalue : sd.Ceiling.brightnessbelow + lightvalue;
			//mxd
			PixelColor wallbrightness = PixelColor.FromInt(mode.CalculateBrightness(lightlevel, Sidedef));
			PixelColor wallcolor = PixelColor.Modulate(sd.Ceiling.colorbelow, wallbrightness);
			poly.color = wallcolor.WithAlpha(255).ToInt();

			// Cut off the part below the other floor and above the other ceiling
			CropPoly(ref poly, osd.Ceiling.plane, true);
			CropPoly(ref poly, osd.Floor.plane, true);

			// Determine if we should repeat the middle texture
			repeatmidtex = Sidedef.IsFlagSet("wrapmidtex") || Sidedef.Line.IsFlagSet("wrapmidtex"); //mxd

			if(!repeatmidtex) {
				// First determine the visible portion of the texture
				float textop, texbottom;

				// Determine top portion height
				if(Sidedef.Line.IsFlagSet(General.Map.Config.LowerUnpeggedFlag))
					textop = geobottom + tof.y + tsz.y;
				else
					textop = geotop + tof.y;

				// Calculate bottom portion height
				texbottom = textop - tsz.y;

				// Create crop planes (we also need these for intersection testing)
				topclipplane = new Plane(new Vector3D(0, 0, -1), textop);
				bottomclipplane = new Plane(new Vector3D(0, 0, 1), -texbottom);

				// Crop polygon by these heights
				CropPoly(ref poly, topclipplane, true);
				CropPoly(ref poly, bottomclipplane, true);
			}

			if(poly.Count > 2) {
				// Keep top and bottom planes for intersection testing
				top = osd.Ceiling.plane;
				bottom = osd.Floor.plane;

				// Process the polygon and create vertices
				List<WorldVertex> verts = CreatePolygonVertices(poly, tp, sd, lightvalue, lightabsolute);
				if(verts.Count > 0) {
					// Apply alpha to vertices
					byte alpha = SetLinedefRenderstyle(true);
					if(alpha < 255) {
						for(int i = 0; i < verts.Count; i++) {
							WorldVertex v = verts[i];
							PixelColor c = PixelColor.FromInt(v.c);
							v.c = c.WithAlpha(alpha).ToInt();
							verts[i] = v;
						}
					}

					base.SetVertices(verts);
					return true;
				}
			}
			
			return false;
		}
		
		#endregion

		#region ================== Methods

		// This performs a fast test in object picking
		public override bool PickFastReject(Vector3D from, Vector3D to, Vector3D dir)
		{
			if(!repeatmidtex)
			{
				// Whe nthe texture is not repeated, leave when outside crop planes
				if((pickintersect.z < bottomclipplane.GetZ(pickintersect)) ||
				   (pickintersect.z > topclipplane.GetZ(pickintersect)))
				   return false;
			}
			
			return base.PickFastReject(from, to, dir);
		}
		
		// Return texture name
		public override string GetTextureName()
		{
			return this.Sidedef.MiddleTexture;
		}

		// This changes the texture
		protected override void SetTexture(string texturename)
		{
			this.Sidedef.SetTextureMid(texturename);
			General.Map.Data.UpdateUsedTextures();
			this.Setup();
		}

		protected override void SetTextureOffsetX(int x)
		{
			Sidedef.Fields.BeforeFieldsChange();
			Sidedef.Fields["offsetx_mid"] = new UniValue(UniversalType.Float, (float)x);
		}

		protected override void SetTextureOffsetY(int y)
		{
			Sidedef.Fields.BeforeFieldsChange();
			Sidedef.Fields["offsety_mid"] = new UniValue(UniversalType.Float, (float)y);
		}

		protected override void MoveTextureOffset(Point xy)
		{
			Sidedef.Fields.BeforeFieldsChange();
			float oldx = Sidedef.Fields.GetValue("offsetx_mid", 0.0f);
			float oldy = Sidedef.Fields.GetValue("offsety_mid", 0.0f);
			float scalex = Sidedef.Fields.GetValue("scalex_mid", 1.0f);
			float scaley = Sidedef.Fields.GetValue("scaley_mid", 1.0f);
            Sidedef.Fields["offsetx_mid"] = new UniValue(UniversalType.Float, getRoundedTextureOffset(oldx, xy.X, scalex, Texture.Width)); //mxd
			Sidedef.Fields["offsety_mid"] = new UniValue(UniversalType.Float, getRoundedTextureOffset(oldy, xy.Y, scaley, Texture.Height)); //mxd
		}

		protected override Point GetTextureOffset()
		{
			float oldx = Sidedef.Fields.GetValue("offsetx_mid", 0.0f);
			float oldy = Sidedef.Fields.GetValue("offsety_mid", 0.0f);
			return new Point((int)oldx, (int)oldy);
		}

		//mxd
		protected override void ResetTextureScale() {
			Sidedef.Fields.BeforeFieldsChange();
			if(Sidedef.Fields.ContainsKey("scalex_mid")) Sidedef.Fields.Remove("scalex_mid");
			if(Sidedef.Fields.ContainsKey("scaley_mid")) Sidedef.Fields.Remove("scaley_mid");
		}

		//mxd
		public override void OnChangeTargetBrightness(bool up) {
			if(!General.Map.UDMF) {
				base.OnChangeTargetBrightness(up);
				return;
			}

			int light = Sidedef.Fields.GetValue("light", 0);
			bool absolute = Sidedef.Fields.GetValue("lightabsolute", false);
			int newLight = 0;

			if(up)
				newLight = General.Map.Config.BrightnessLevels.GetNextHigher(light, absolute);
			else
				newLight = General.Map.Config.BrightnessLevels.GetNextLower(light, absolute);

			if(newLight == light) return;

			//create undo
			mode.CreateUndo("Change middle wall brightness", UndoGroup.SurfaceBrightnessChange, Sector.Sector.FixedIndex);
			Sidedef.Fields.BeforeFieldsChange();

			//apply changes
			Sidedef.Fields["light"] = new UniValue(UniversalType.Integer, newLight);
			mode.SetActionResult("Changed middle wall brightness to " + newLight + ".");
			Sector.Sector.UpdateCache();

			//rebuild sector
			Sector.UpdateSectorGeometry(false);
		}

		//mxd
		public override void OnTextureFit(bool fitWidth, bool fitHeight) {
			if(!General.Map.UDMF) return;
			if(string.IsNullOrEmpty(Sidedef.MiddleTexture) || Sidedef.MiddleTexture == "-" || !Texture.IsImageLoaded) return;

			string s;
			if(fitWidth && fitHeight) s = "width and height";
			else if(fitWidth) s = "width";
			else s = "height";

			//create undo
			mode.CreateUndo("Fit texture (" + s + ")", UndoGroup.TextureOffsetChange, Sector.Sector.FixedIndex);
			Sidedef.Fields.BeforeFieldsChange();

			if(fitWidth) {
				float scaleX = Texture.ScaledWidth / Sidedef.Line.Length;
				UDMFTools.SetFloat(Sidedef.Fields, "scalex_mid", scaleX, 1.0f, false);
				UDMFTools.SetFloat(Sidedef.Fields, "offsetx_mid", -Sidedef.OffsetX, 0.0f, false);
			}

			if(fitHeight && Sidedef.Sector != null) {
				float scaleY = (float)Texture.ScaledHeight / (Sidedef.Sector.CeilHeight - Sidedef.Sector.FloorHeight);
				UDMFTools.SetFloat(Sidedef.Fields, "scaley_mid", scaleY, 1.0f, false);

				float offsetY = mode.GetMiddleOffsetY(Sidedef, -Sidedef.OffsetY, scaleY, true) % Texture.Height;
				UDMFTools.SetFloat(Sidedef.Fields, "offsety_mid", offsetY, 0.0f, false);
			}

			Setup();
		}

		//mxd
		public override void SelectNeighbours(bool select, bool withSameTexture, bool withSameHeight) {
			selectNeighbours(Sidedef.MiddleTexture, select, withSameTexture, withSameHeight);
		}
		
		#endregion
	}
}
