
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
using System.Windows.Forms;
using CodeImp.DoomBuilder.Map;

#endregion

namespace CodeImp.DoomBuilder.Controls
{
	internal partial class SectorInfoPanel : UserControl
	{
		private int fullWidth; //mxd
		
		// Constructor
		public SectorInfoPanel()
		{
			// Initialize
			InitializeComponent();
			fullWidth = floorpanel.Width; //mxd
		}

		// This shows the info
		public void ShowInfo(Sector s)
		{
			string effectinfo = "";
			
			int sheight = s.CeilHeight - s.FloorHeight;

			// Lookup effect description in config
			if(General.Map.Config.SectorEffects.ContainsKey(s.Effect))
				effectinfo = General.Map.Config.SectorEffects[s.Effect].ToString();
			else if(s.Effect == 0)
				effectinfo = s.Effect.ToString() + " - Normal";
			else
				effectinfo = s.Effect.ToString() + " - Unknown";

			// Sector info
			sectorinfo.Text = " Sector " + s.Index + " ";
			effect.Text = effectinfo;
			ceiling.Text = s.CeilHeight.ToString();
			floor.Text = s.FloorHeight.ToString();
			tag.Text = s.Tag.ToString();
			height.Text = sheight.ToString();
			brightness.Text = s.Brightness.ToString();
			floorname.Text = s.FloorTexture;
			ceilingname.Text = s.CeilTexture;
			General.DisplayZoomedImage(floortex, General.Map.Data.GetFlatImage(s.FloorTexture).GetPreview());
			General.DisplayZoomedImage(ceilingtex, General.Map.Data.GetFlatImage(s.CeilTexture).GetPreview());

			//mxd
			bool showExtededFloorInfo = false;
			bool showExtededCeilingInfo = false;
			if(General.Map.UDMF && s.Fields != null) {
				//light
				if(s.Fields.ContainsKey("lightceiling")) {
					showExtededCeilingInfo = true;
					ceilingLight.Enabled = true;
					ceilingLightLabel.Enabled = true;

                    int cl = (int)s.Fields["lightceiling"].Value;

					if(s.Fields.GetValue("lightceilingabsolute", false))
                        ceilingLight.Text = cl + " (abs.)";
                    else
                        ceilingLight.Text = cl + " (" + Math.Min(255, Math.Max(0, (cl + s.Brightness))) + ")";

				} else {
					ceilingLight.Text = "--";
					ceilingLight.Enabled = false;
					ceilingLightLabel.Enabled = false;
				}

				if(s.Fields.ContainsKey("lightfloor")) {
					showExtededFloorInfo = true;
					floorLight.Enabled = true;
					floorLightLabel.Enabled = true;

                    int fl = (int)s.Fields["lightfloor"].Value;

					if(s.Fields.GetValue("lightfloorabsolute", false))
						floorLight.Text = fl + " (abs.)";
                    else
                        floorLight.Text = fl + " (" + Math.Min(255, Math.Max(0, (fl + s.Brightness))) + ")";

				} else {
					floorLight.Text = "--";
					floorLight.Enabled = false;
					floorLightLabel.Enabled = false;
				}

				//ceiling offsets
				float panX = s.Fields.GetValue("xpanningceiling", 0f);
				float panY = s.Fields.GetValue("ypanningceiling", 0f);

				if(panX != 0 || panY != 0) {
					showExtededCeilingInfo = true;
					ceilingOffset.Enabled = true;
					ceilingOffsetLabel.Enabled = true;
                    ceilingOffset.Text = String.Format("{0:0.##}", panX) + ", " + String.Format("{0:0.##}", panY);
				} else {
					ceilingOffset.Text = "--, --";
					ceilingOffset.Enabled = false;
					ceilingOffsetLabel.Enabled = false;
				}

				//floor offsets
				panX = s.Fields.GetValue("xpanningfloor", 0f);
				panY = s.Fields.GetValue("ypanningfloor", 0f);

				if(panX != 0 || panY != 0) {
					showExtededFloorInfo = true;
					floorOffset.Enabled = true;
					floorOffsetLabel.Enabled = true;
                    floorOffset.Text = String.Format("{0:0.##}", panX) + ", " + String.Format("{0:0.##}", panY);
				} else {
					floorOffset.Text = "--, --";
					floorOffset.Enabled = false;
					floorOffsetLabel.Enabled = false;
				}

				//ceiling scale
				float scaleX = s.Fields.GetValue("xscaleceiling", 1.0f);//1.0f;
				float scaleY = s.Fields.GetValue("yscaleceiling", 1.0f);

				if(scaleX != 1.0f || scaleY != 1.0f) {
					showExtededCeilingInfo = true;
					ceilingScale.Enabled = true;
					ceilingScaleLabel.Enabled = true;
                    ceilingScale.Text = String.Format("{0:0.##}", scaleX) + ", " + String.Format("{0:0.##}", scaleY);
				} else {
					ceilingScale.Text = "--, --";
					ceilingScale.Enabled = false;
					ceilingScaleLabel.Enabled = false;
				}

				//floor scale
				scaleX = s.Fields.GetValue("xscalefloor", 1.0f);
				scaleY = s.Fields.GetValue("yscalefloor", 1.0f);

				if(scaleX != 1.0f || scaleY != 1.0f) {
					showExtededFloorInfo = true;
					floorScale.Enabled = true;
					floorScaleLabel.Enabled = true;
                    floorScale.Text = String.Format("{0:0.##}", scaleX) + ", " + String.Format("{0:0.##}", scaleY);
				} else {
					floorScale.Text = "--, --";
					floorScale.Enabled = false;
					floorScaleLabel.Enabled = false;
				}

				//rotation
				if(s.Fields.ContainsKey("rotationceiling")) {
					showExtededCeilingInfo = true;
					ceilingAngle.Enabled = true;
					ceilingAngleLabel.Enabled = true;
					ceilingAngle.Text = s.Fields["rotationceiling"].Value.ToString() + "\u00B0";
				} else {
					ceilingAngle.Text = "-";
					ceilingAngle.Enabled = false;
					ceilingAngleLabel.Enabled = false;
				}

				if(s.Fields.ContainsKey("rotationfloor")) {
					showExtededFloorInfo = true;
					floorAngle.Enabled = true;
					floorAngleLabel.Enabled = true;
					floorAngle.Text = s.Fields["rotationfloor"].Value.ToString() + "\u00B0";
				} else {
					floorAngle.Text = "-";
					floorAngle.Enabled = false;
					floorAngleLabel.Enabled = false;
				}
			}

			//panels size
			if(showExtededCeilingInfo) {
				ceilingpanel.Width = fullWidth;
				ceilingInfo.Visible = true;
			} else {
				ceilingInfo.Visible = false;
				ceilingpanel.Width = 84;
			}

			if(showExtededFloorInfo) {
				floorpanel.Width = fullWidth;
				floorInfo.Visible = true;
			} else {
				floorInfo.Visible = false;
				floorpanel.Width = 84;
			}


			// Show the whole thing
			this.Show();
			this.Update();
		}

		// When visible changed
		protected override void OnVisibleChanged(EventArgs e)
		{
			// Hiding panels
			if(!this.Visible)
			{
				floortex.BackgroundImage = null;
				ceilingtex.BackgroundImage = null;
			}

			// Call base
			base.OnVisibleChanged(e);
		}
	}
}