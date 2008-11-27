
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
using System.Drawing;
using CodeImp.DoomBuilder.Editing;

#endregion

namespace CodeImp.DoomBuilder.BuilderModes
{
	[EditMode(DisplayName = "Find & Replace Mode",
			  SwitchAction = "findmode",
			  Volatile = true,
			  UseByDefault = true)]

	public sealed class FindReplaceMode : BaseClassicMode
	{
		#region ================== Constants

		#endregion

		#region ================== Variables

		#endregion

		#region ================== Properties

		#endregion

		#region ================== Constructor / Disposer

		#endregion

		#region ================== Events

		// Cancelled
		public override void OnCancel()
		{
			// Cancel base class
			base.OnCancel();

			// Return to base mode
			General.Editing.ChangeMode(General.Editing.PreviousStableMode.Name);
		}

		// Mode engages
		public override void OnEngage()
		{
			base.OnEngage();
			renderer.SetPresentation(Presentation.Standard);

			// Show toolbox window
			BuilderPlug.Me.FindReplaceForm.Show((Form)General.Interface);
		}

		// Disenagaging
		public override void OnDisengage()
		{
			base.OnDisengage();

			// Hide object info
			General.Interface.HideInfo();
			
			// Hide toolbox window
			BuilderPlug.Me.FindReplaceForm.Hide();
		}

		// This applies the curves and returns to the base mode
		public override void OnAccept()
		{
			// Snap to map format accuracy
			General.Map.Map.SnapAllToAccuracy();

			// Update caches
			General.Map.Map.Update();
			General.Map.IsChanged = true;

			// Return to base mode
			General.Editing.ChangeMode(General.Editing.PreviousStableMode.Name);
		}

		// Redrawing display
		public override void OnRedrawDisplay()
		{
			// Get the selection
			FindReplaceObject[] selection = BuilderPlug.Me.FindReplaceForm.GetSelection();

			renderer.RedrawSurface();

			// Render lines
			if(renderer.StartPlotter(true))
			{
				renderer.PlotLinedefSet(General.Map.Map.Linedefs);
				BuilderPlug.Me.FindReplaceForm.Finder.PlotSelection(renderer, selection);
				renderer.PlotVerticesSet(General.Map.Map.Vertices);
				renderer.Finish();
			}

			// Render things
			if(renderer.StartThings(true))
			{
				renderer.RenderThingSet(General.Map.Map.Things, 1.0f);
				BuilderPlug.Me.FindReplaceForm.Finder.RenderThingsSelection(renderer, selection);
				renderer.Finish();
			}
			
			// Render overlay
			if(renderer.StartOverlay(true))
			{
				BuilderPlug.Me.FindReplaceForm.Finder.RenderOverlaySelection(renderer, selection);
				renderer.Finish();
			}

			renderer.Present();
		}

		#endregion
	}
}
