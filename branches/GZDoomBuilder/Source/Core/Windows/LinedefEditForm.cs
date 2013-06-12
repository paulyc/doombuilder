
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
using System.Windows.Forms;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.GZBuilder.Data; //mxd
using CodeImp.DoomBuilder.Types;
using CodeImp.DoomBuilder.Controls;
using CodeImp.DoomBuilder.GZBuilder.Tools;
using CodeImp.DoomBuilder.GZBuilder.Controls; //mxd

#endregion

namespace CodeImp.DoomBuilder.Windows
{
	internal partial class LinedefEditForm : DelayedForm
	{
		// Variables
		private ICollection<Linedef> lines;
		private bool preventchanges = false;

		private List<PairedFieldsControl> frontUdmfControls; //mxd
		private List<CheckBox> frontUdmfFlags; //mxd
		private List<PairedFieldsControl> backUdmfControls; //mxd
		private List<CheckBox> backUdmfFlags; //mxd
		
		// Constructor
		public LinedefEditForm()
		{
			// Initialize
			InitializeComponent();
			
			// Fill flags list
			foreach(KeyValuePair<string, string> lf in General.Map.Config.LinedefFlags)
				flags.Add(lf.Value, lf.Key);

			// Fill actions list
			action.GeneralizedCategories = General.Map.Config.GenActionCategories;
			action.AddInfo(General.Map.Config.SortedLinedefActions.ToArray());

			// Fill activations list
			activation.Items.AddRange(General.Map.Config.LinedefActivates.ToArray());
			foreach(LinedefActivateInfo ai in General.Map.Config.LinedefActivates) udmfactivates.Add(ai.Title, ai);
			
			// Fill universal fields list
			fieldslist.ListFixedFields(General.Map.Config.LinedefFields);
			
			// Initialize image selectors
			fronthigh.Initialize();
			frontmid.Initialize();
			frontlow.Initialize();
			backhigh.Initialize();
			backmid.Initialize();
			backlow.Initialize();

			// Initialize custom fields editor
			fieldslist.Setup("linedef");
			
			// Mixed activations? (UDMF)
			if(General.Map.FormatInterface.HasMixedActivations)
				udmfpanel.Visible = true;
			else if(General.Map.FormatInterface.HasPresetActivations)
				hexenpanel.Visible = true;
			
			// Action arguments?
			if(General.Map.FormatInterface.HasActionArgs)
				argspanel.Visible = true;
			
			// Arrange panels
			if(General.Map.FormatInterface.HasPresetActivations)
			{
				//mxd
				actiongroup.Top = settingsGroup.Top;
                actiongroup.Height = hexenpanel.Location.Y + hexenpanel.Height;
				this.Height = heightpanel1.Height;
			} else if(!General.Map.FormatInterface.HasMixedActivations &&
				    !General.Map.FormatInterface.HasActionArgs &&
				    !General.Map.FormatInterface.HasPresetActivations)
			{
				actiongroup.Top = settingsGroup.Top;
				actiongroup.Height = action.Bottom + action.Top + (actiongroup.Width - actiongroup.ClientRectangle.Width);
				this.Height = heightpanel2.Height;
			}
			
			// Tag?
			if(General.Map.FormatInterface.HasLinedefTag)
			{
				// Match position after the action group
				idgroup.Top = actiongroup.Bottom + actiongroup.Margin.Bottom + idgroup.Margin.Top;
			}
			else
			{
				idgroup.Visible = false;
			}

            //mxd. Setup UDMF controls
			if(General.Map.FormatInterface.HasCustomFields) {
				frontUdmfControls = new List<PairedFieldsControl>() { pfcFrontOffsetTop, pfcFrontOffsetMid, pfcFrontOffsetBottom, pfcFrontScaleTop, pfcFrontScaleMid, pfcFrontScaleBottom };
				frontUdmfFlags = new List<CheckBox>() { cbLightAbsoluteFront, cblightfogFront, cbnodecalsFront, cbnofakecontrastFront, cbWrapMidtexFront, cbsmoothlightingFront, cbClipMidtexFront };
				backUdmfControls = new List<PairedFieldsControl>() { pfcBackOffsetTop, pfcBackOffsetMid, pfcBackOffsetBottom, pfcBackScaleTop, pfcBackScaleMid, pfcBackScaleBottom };
				backUdmfFlags = new List<CheckBox>() { cbLightAbsoluteBack, cblightfogBack, cbnodecalsBack, cbnofakecontrastBack, cbWrapMidtexBack, cbsmoothlightingBack, cbClipMidtexBack };
				fsAlpha.SetLimits(0f, 1f);
			} else {
				tabs.TabPages.Remove(tabcustom);

				settingsGroup.Visible = false;

				customfrontbutton.Visible = false;
				custombackbutton.Visible = false;

				labelLightFront.Visible = false;
				lightFront.Visible = false;
				cbLightAbsoluteFront.Visible = false;

				labelLightBack.Visible = false;
				lightBack.Visible = false;
				cbLightAbsoluteBack.Visible = false;

				udmfPropertiesFront.Visible = false;
				udmfPropertiesBack.Visible = false;
			}
		}
		
		// This sets up the form to edit the given lines
		public void Setup(ICollection<Linedef> lines)
		{
			LinedefActivateInfo sai;
			Linedef fl;

			preventchanges = true;
			
			// Keep this list
			this.lines = lines;
			if(lines.Count > 1) this.Text = "Edit Linedefs (" + lines.Count + ")";
			
			////////////////////////////////////////////////////////////////////////
			// Set all options to the first linedef properties
			////////////////////////////////////////////////////////////////////////

			// Get first line
			fl = General.GetByIndex(lines, 0);
			
			// Flags
			foreach(CheckBox c in flags.Checkboxes)
				if(fl.Flags.ContainsKey(c.Tag.ToString())) c.Checked = fl.Flags[c.Tag.ToString()];
			
			// Activations
			foreach(LinedefActivateInfo ai in activation.Items)
				if((fl.Activate & ai.Index) == ai.Index) activation.SelectedItem = ai;

			// UDMF Activations
			foreach(CheckBox c in udmfactivates.Checkboxes)
			{
				LinedefActivateInfo ai = (c.Tag as LinedefActivateInfo);
				if(fl.Flags.ContainsKey(ai.Key)) c.Checked = fl.Flags[ai.Key];
			}

            //mxd. setup arg0str
            arg0str.Location = arg0.Location;

            // Custom fields
            fieldslist.SetValues(fl.Fields, true);

			//mxd. UDMF Settings
			if(General.Map.FormatInterface.HasCustomFields) {
				string renderStyle = fl.Fields.GetValue("renderstyle", "");
				cbRenderStyle.SelectedIndex = (renderStyle == "add" ? 1 : 0);
				fsAlpha.SetValueFrom(fl.Fields);
				lockNumber.Text = fl.Fields.GetValue("locknumber", 0).ToString();
			}

			// Action/tags
			action.Value = fl.Action;

			if(General.Map.FormatInterface.HasLinedefTag) {//mxd
				tagSelector.Setup();
				tagSelector.SetTag(fl.Tag);
			}

			arg0.SetValue(fl.Args[0]);
			arg1.SetValue(fl.Args[1]);
			arg2.SetValue(fl.Args[2]);
			arg3.SetValue(fl.Args[3]);
			arg4.SetValue(fl.Args[4]);
			
			// Front side and back side checkboxes
			frontside.Checked = (fl.Front != null);
			backside.Checked = (fl.Back != null);

			// Front settings
			if(fl.Front != null)
			{
				fronthigh.TextureName = fl.Front.HighTexture;
				frontmid.TextureName = fl.Front.MiddleTexture;
				frontlow.TextureName = fl.Front.LowTexture;
				fronthigh.Required = fl.Front.HighRequired();
				frontmid.Required = fl.Front.MiddleRequired();
				frontlow.Required = fl.Front.LowRequired();
				frontsector.Text = fl.Front.Sector.Index.ToString();

                //mxd
				if(General.Map.FormatInterface.HasCustomFields) {
					//front settings
					foreach(PairedFieldsControl pfc in frontUdmfControls)
						pfc.SetValuesFrom(fl.Front.Fields);

					lightFront.Text = UDMFTools.GetInteger(fl.Front.Fields, lightFront.Tag.ToString(), 0).ToString();
					
					foreach(CheckBox cb in frontUdmfFlags){
						string key = cb.Tag.ToString();
						if(fl.Front.Fields != null)
							cb.CheckState = (fl.Front.Fields.GetValue(key, false) ? CheckState.Checked : CheckState.Unchecked);
					}
                }

                frontoffsetx.Text = fl.Front.OffsetX.ToString();
                frontoffsety.Text = fl.Front.OffsetY.ToString();
			}

			// Back settings
			if(fl.Back != null)
			{
				backhigh.TextureName = fl.Back.HighTexture;
				backmid.TextureName = fl.Back.MiddleTexture;
				backlow.TextureName = fl.Back.LowTexture;
				backhigh.Required = fl.Back.HighRequired();
				backmid.Required = fl.Back.MiddleRequired();
				backlow.Required = fl.Back.LowRequired();
				backsector.Text = fl.Back.Sector.Index.ToString();

                //mxd
				if(General.Map.FormatInterface.HasCustomFields) {
					//front settings
					foreach(PairedFieldsControl pfc in backUdmfControls)
						pfc.SetValuesFrom(fl.Back.Fields);

					lightBack.Text = UDMFTools.GetInteger(fl.Back.Fields, lightBack.Tag.ToString(), 0).ToString();

					foreach(CheckBox cb in backUdmfFlags) {
						string key = cb.Tag.ToString();
						if(fl.Back.Fields != null)
							cb.CheckState = (fl.Back.Fields.GetValue(key, false) ? CheckState.Checked : CheckState.Unchecked);
					}
                }
 
                backoffsetx.Text = fl.Back.OffsetX.ToString();
                backoffsety.Text = fl.Back.OffsetY.ToString();
			}

			////////////////////////////////////////////////////////////////////////
			// Now go for all lines and change the options when a setting is different
			////////////////////////////////////////////////////////////////////////

			// Go for all lines
			foreach(Linedef l in lines)
			{
				// Flags
				foreach(CheckBox c in flags.Checkboxes)
				{
					if(l.Flags.ContainsKey(c.Tag.ToString()))
					{
						if(l.Flags[c.Tag.ToString()] != c.Checked)
						{
							c.ThreeState = true;
							c.CheckState = CheckState.Indeterminate;
						}
					}
				}

				// Activations
				if(activation.Items.Count > 0)
				{
					sai = (activation.Items[0] as LinedefActivateInfo);
					foreach(LinedefActivateInfo ai in activation.Items)
						if((l.Activate & ai.Index) == ai.Index) sai = ai;
					if(sai != activation.SelectedItem) activation.SelectedIndex = -1;
				}

				// UDMF Activations
				foreach(CheckBox c in udmfactivates.Checkboxes)
				{
					LinedefActivateInfo ai = (c.Tag as LinedefActivateInfo);
					if(l.Flags.ContainsKey(ai.Key))
					{
						if(c.Checked != l.Flags[ai.Key])
						{
							c.ThreeState = true;
							c.CheckState = CheckState.Indeterminate;
						}
					}
				}

				//mxd. UDMF Settings
				if(General.Map.FormatInterface.HasCustomFields) {
					int i = (l.Fields.GetValue("renderstyle", "") == "add" ? 1 : 0);

					if(cbRenderStyle.SelectedIndex != -1 && i != cbRenderStyle.SelectedIndex)
						cbRenderStyle.SelectedIndex = -1;

					fsAlpha.SetValueFrom(l.Fields);
					if(!string.IsNullOrEmpty(lockNumber.Text) && lockNumber.GetResult(0) != fl.Fields.GetValue("locknumber", 0))
						lockNumber.Text = "";
				}

				// Action/tags
				if(l.Action != action.Value) action.Empty = true;
				if(General.Map.FormatInterface.HasLinedefTag && l.Tag != fl.Tag) tagSelector.ClearTag(); //mxd
				if(l.Args[0] != arg0.GetResult(-1)) arg0.ClearValue();
				if(l.Args[1] != arg1.GetResult(-1)) arg1.ClearValue();
				if(l.Args[2] != arg2.GetResult(-1)) arg2.ClearValue();
				if(l.Args[3] != arg3.GetResult(-1)) arg3.ClearValue();
				if(l.Args[4] != arg4.GetResult(-1)) arg4.ClearValue();

				//mxd. Check if we have different arg0str values
				if(Array.IndexOf(GZBuilder.GZGeneral.ACS_SPECIALS, action.Value) != -1 && cbArgStr.Checked && !string.IsNullOrEmpty(arg0str.Text) && l.Fields.ContainsKey("arg0str") && l.Fields["arg0str"].Value.ToString() != arg0str.Text) {
					arg0str.SelectedIndex = -1;
					arg0str.Text = string.Empty;
				}
				
				// Front side checkbox
				if((l.Front != null) != frontside.Checked)
				{
					frontside.ThreeState = true;
					frontside.CheckState = CheckState.Indeterminate;
					frontside.AutoCheck = false;
				}

				// Back side checkbox
				if((l.Back != null) != backside.Checked)
				{
					backside.ThreeState = true;
					backside.CheckState = CheckState.Indeterminate;
					backside.AutoCheck = false;
				}

				// Front settings
				if(l.Front != null)
				{
					if(fronthigh.TextureName != l.Front.HighTexture) fronthigh.TextureName = "";
					if(frontmid.TextureName != l.Front.MiddleTexture) frontmid.TextureName = "";
					if(frontlow.TextureName != l.Front.LowTexture) frontlow.TextureName = "";
					if(fronthigh.Required != l.Front.HighRequired()) fronthigh.Required = false;
					if(frontmid.Required != l.Front.MiddleRequired()) frontmid.Required = false;
					if(frontlow.Required != l.Front.LowRequired()) frontlow.Required = false;
					if(frontsector.Text != l.Front.Sector.Index.ToString()) frontsector.Text = "";

					//mxd
					if(General.Map.FormatInterface.HasCustomFields) {
						foreach(PairedFieldsControl pfc in frontUdmfControls)
							pfc.SetValuesFrom(l.Front.Fields);

						if(!string.IsNullOrEmpty(lightFront.Text) && lightFront.Text != UDMFTools.GetInteger(fl.Front.Fields, lightFront.Tag.ToString(), 0).ToString()) lightFront.Text = "";

						foreach(CheckBox cb in frontUdmfFlags) {
							if(cb.CheckState == CheckState.Indeterminate) continue;
							
							string key = cb.Tag.ToString();
							if(l.Front.Fields != null ) {
								CheckState state = (l.Front.Fields.GetValue(key, false) ? CheckState.Checked : CheckState.Unchecked);
								if(cb.CheckState != state) {
									cb.ThreeState = true;
									cb.CheckState = CheckState.Indeterminate;
								}
							}
						}
                    }
 
                    if (frontoffsetx.Text != l.Front.OffsetX.ToString()) frontoffsetx.Text = "";
                    if (frontoffsety.Text != l.Front.OffsetY.ToString()) frontoffsety.Text = "";
				}

				// Back settings
				if(l.Back != null)
				{
					if(backhigh.TextureName != l.Back.HighTexture) backhigh.TextureName = "";
					if(backmid.TextureName != l.Back.MiddleTexture) backmid.TextureName = "";
					if(backlow.TextureName != l.Back.LowTexture) backlow.TextureName = "";
					if(backhigh.Required != l.Back.HighRequired()) backhigh.Required = false;
					if(backmid.Required != l.Back.MiddleRequired()) backmid.Required = false;
					if(backlow.Required != l.Back.LowRequired()) backlow.Required = false;
					if(backsector.Text != l.Back.Sector.Index.ToString()) backsector.Text = "";

                    //mxd
					if(General.Map.FormatInterface.HasCustomFields) {
						foreach(PairedFieldsControl pfc in backUdmfControls)
							pfc.SetValuesFrom(l.Back.Fields);

						if(!string.IsNullOrEmpty(lightBack.Text) && lightBack.Text != UDMFTools.GetInteger(fl.Back.Fields, lightBack.Tag.ToString(), 0).ToString())
							lightBack.Text = "";

						foreach(CheckBox cb in backUdmfFlags) {
							if(cb.CheckState == CheckState.Indeterminate) continue;

							string key = cb.Tag.ToString();
							if(l.Back.Fields != null) {
								CheckState state = (l.Back.Fields.GetValue(key, false) ? CheckState.Checked : CheckState.Unchecked);
								if(cb.CheckState != state) {
									cb.ThreeState = true;
									cb.CheckState = CheckState.Indeterminate;
								}
							}
						}
                    }
 
                    if (backoffsetx.Text != l.Back.OffsetX.ToString()) backoffsetx.Text = "";
                    if (backoffsety.Text != l.Back.OffsetY.ToString()) backoffsety.Text = "";

					if(General.Map.FormatInterface.HasCustomFields) custombackbutton.Visible = true;
				}
				
				// Custom fields
				fieldslist.SetValues(l.Fields, false);
			}
			
			// Refresh controls so that they show their image
			backhigh.Refresh();
			backmid.Refresh();
			backlow.Refresh();
			fronthigh.Refresh();
			frontmid.Refresh();
			frontlow.Refresh();

			preventchanges = false;
		}

        //mxd
        private void setNumberedScripts(Linedef l) {
            arg0str.Items.Clear();

            if (General.Map.NumberedScripts.Count > 0) {
                foreach (ScriptItem si in General.Map.NumberedScripts) {
                    arg0str.Items.Add(si);
                    if (si.Index == l.Args[0])
                        arg0str.SelectedIndex = arg0str.Items.Count - 1;
                }

                //script number is not among known scripts...
                if (arg0str.SelectedIndex == -1 && l.Args[0] > 0) {
                    arg0str.Items.Add(new ScriptItem(l.Args[0], "Script " + l.Args[0]));
                    arg0str.SelectedIndex = arg0str.Items.Count - 1;
                }

            } else if (l.Args[0] > 0) {
                arg0str.Items.Add(new ScriptItem(l.Args[0], "Script " + l.Args[0]));
                arg0str.SelectedIndex = 0;
            }
        }

        //mxd
        private void setNamedScripts(string selectedValue) {
            arg0str.Items.Clear();

            //update arg0str items
            if (General.Map.NamedScripts.Count > 0) {
                ScriptItem[] sn = new ScriptItem[General.Map.NamedScripts.Count];
                General.Map.NamedScripts.CopyTo(sn, 0);
                arg0str.Items.AddRange(sn);

                for (int i = 0; i < sn.Length; i++) {
                    if (sn[i].Name == selectedValue) {
                        arg0str.SelectedIndex = i;
                        break;
                    }
                }

            } else {
                arg0str.Text = selectedValue;
            }
        }
		
		// Front side (un)checked
		private void frontside_CheckStateChanged(object sender, EventArgs e)
		{
			// Enable/disable panel
			// NOTE: Also enabled when checkbox is grayed!
			frontgroup.Enabled = (frontside.CheckState != CheckState.Unchecked);
		}

		// Back side (un)checked
		private void backside_CheckStateChanged(object sender, EventArgs e)
		{
			// Enable/disable panel
			// NOTE: Also enabled when checkbox is grayed!
			backgroup.Enabled = (backside.CheckState != CheckState.Unchecked);
		}

		// This selects all text in a textbox
		private void SelectAllText(object sender, EventArgs e)
		{
			(sender as TextBox).SelectAll();
		}

		// Apply clicked
		private void apply_Click(object sender, EventArgs e)
		{
			string undodesc = "linedef";
			Sector s;
			int index;
			
			// Verify the tag
			if(General.Map.FormatInterface.HasLinedefTag)
			{
				tagSelector.ValidateTag(); //mxd
				if(((tagSelector.GetTag(0) < General.Map.FormatInterface.MinTag) || (tagSelector.GetTag(0) > General.Map.FormatInterface.MaxTag))) {
					General.ShowWarningMessage("Linedef tag must be between " + General.Map.FormatInterface.MinTag + " and " + General.Map.FormatInterface.MaxTag + ".", MessageBoxButtons.OK);
					return;
				}
			}
			
			// Verify the action
			if((action.Value < General.Map.FormatInterface.MinAction) || (action.Value > General.Map.FormatInterface.MaxAction))
			{
				General.ShowWarningMessage("Linedef action must be between " + General.Map.FormatInterface.MinAction + " and " + General.Map.FormatInterface.MaxAction + ".", MessageBoxButtons.OK);
				return;
			}
			
			// Make undo
			if(lines.Count > 1) undodesc = lines.Count + " linedefs";
			General.Map.UndoRedo.CreateUndo("Edit " + undodesc);

            //mxd
            bool hasAcs = !action.Empty && Array.IndexOf(GZBuilder.GZGeneral.ACS_SPECIALS, action.Value) != -1;
            bool hasArg0str = General.Map.UDMF && hasAcs && !string.IsNullOrEmpty(arg0str.Text);
			int lockNum = lockNumber.GetResult(0);
			
			// Go for all the lines
			foreach(Linedef l in lines)
			{
				// Apply all flags
				foreach(CheckBox c in flags.Checkboxes)
				{
					if(c.CheckState == CheckState.Checked) l.SetFlag(c.Tag.ToString(), true);
					else if(c.CheckState == CheckState.Unchecked) l.SetFlag(c.Tag.ToString(), false);
				}
				
				// Apply chosen activation flag
				if(activation.SelectedIndex > -1)
					l.Activate = (activation.SelectedItem as LinedefActivateInfo).Index;
				
				// UDMF activations
				foreach(CheckBox c in udmfactivates.Checkboxes)
				{
					LinedefActivateInfo ai = (c.Tag as LinedefActivateInfo);
					if(c.CheckState == CheckState.Checked) l.SetFlag(ai.Key, true);
					else if(c.CheckState == CheckState.Unchecked) l.SetFlag(ai.Key, false);
				}
				
				// Action/tags
				l.Tag = tagSelector.GetTag(l.Tag); //mxd
				if(!action.Empty) l.Action = action.Value;
                
                //mxd
                if (hasAcs && !cbArgStr.Checked) {
                    if (arg0str.SelectedItem != null)
                        l.Args[0] = ((ScriptItem)arg0str.SelectedItem).Index;
                    else if (!int.TryParse(arg0str.Text.Trim(), out l.Args[0])) 
                        l.Args[0] = 0;
                } else {
                    l.Args[0] = arg0.GetResult(l.Args[0]);
                }
				l.Args[1] = arg1.GetResult(l.Args[1]);
				l.Args[2] = arg2.GetResult(l.Args[2]);
				l.Args[3] = arg3.GetResult(l.Args[3]);
				l.Args[4] = arg4.GetResult(l.Args[4]);
				
				// Remove front side?
				if((l.Front != null) && (frontside.CheckState == CheckState.Unchecked))
				{
					l.Front.Dispose();
				}
				// Create or modify front side?
				else if(frontside.CheckState == CheckState.Checked)
				{
					// Make sure we have a valid sector (make a new one if needed)
					if(l.Front != null) index = l.Front.Sector.Index; else index = -1;
					index = frontsector.GetResult(index);
					if((index > -1) && (index < General.Map.Map.Sectors.Count))
					{
						s = General.Map.Map.GetSectorByIndex(index);
						if(s == null) s = General.Map.Map.CreateSector();
						if(s != null)
						{
							// Create new sidedef?
							if(l.Front == null) General.Map.Map.CreateSidedef(l, true, s);
							if(l.Front != null)
							{
								// Change sector?
								if(l.Front.Sector != s) l.Front.SetSector(s);

								// Apply settings
                                int min = General.Map.FormatInterface.MinTextureOffset;
                                int max = General.Map.FormatInterface.MaxTextureOffset;
								if(General.Map.FormatInterface.HasCustomFields) { //mxd
									l.Front.Fields.BeforeFieldsChange();

									foreach(PairedFieldsControl pfc in frontUdmfControls)
										pfc.ApplyTo(l.Front.Fields, min, max);

									if(!string.IsNullOrEmpty(lightFront.Text)) {
										string key = lightFront.Tag.ToString();
										bool absolute = (cbLightAbsoluteFront.CheckState == CheckState.Checked);
										int value = General.Clamp(lightFront.GetResult(UDMFTools.GetInteger(l.Front.Fields, key, 0)), (absolute ? 0 : -255), 255);
										UDMFTools.SetInteger(l.Front.Fields, key, value, 0, false);
									}

									foreach(CheckBox cb in frontUdmfFlags) {
										if(cb.CheckState == CheckState.Indeterminate) continue;
										string key = cb.Tag.ToString();
										bool oldValue = l.Front.Fields.GetValue(key, false);
										
										if(cb.CheckState == CheckState.Checked ){
											if(!oldValue) l.Front.Fields[key] = new UniValue(UniversalType.Boolean, true);
										} else if(l.Front.Fields.ContainsKey(key)) {
											l.Front.Fields.Remove(key);
										}
									}
                                }
 
                                l.Front.OffsetX = General.Clamp(frontoffsetx.GetResult(l.Front.OffsetX), min, max);
								l.Front.OffsetY = General.Clamp(frontoffsety.GetResult(l.Front.OffsetY), min, max);

								l.Front.SetTextureHigh(fronthigh.GetResult(l.Front.HighTexture));
								l.Front.SetTextureMid(frontmid.GetResult(l.Front.MiddleTexture));
								l.Front.SetTextureLow(frontlow.GetResult(l.Front.LowTexture));
							}
						}
					}
				}

				// Remove back side?
				if((l.Back != null) && (backside.CheckState == CheckState.Unchecked))
				{
					l.Back.Dispose();
				}
				// Create or modify back side?
				else if(backside.CheckState == CheckState.Checked)
				{
					// Make sure we have a valid sector (make a new one if needed)
					if(l.Back != null) index = l.Back.Sector.Index; else index = -1;
					index = backsector.GetResult(index);
					if((index > -1) && (index < General.Map.Map.Sectors.Count))
					{
						s = General.Map.Map.GetSectorByIndex(index);
						if(s == null) s = General.Map.Map.CreateSector();
						if(s != null)
						{
							// Create new sidedef?
							if(l.Back == null) General.Map.Map.CreateSidedef(l, false, s);
							if(l.Back != null)
							{
								// Change sector?
								if(l.Back.Sector != s) l.Back.SetSector(s);

								// Apply settings
                                //mxd
                                int min = General.Map.FormatInterface.MinTextureOffset;
                                int max = General.Map.FormatInterface.MaxTextureOffset;
								if(General.Map.FormatInterface.HasCustomFields) { //mxd
									l.Back.Fields.BeforeFieldsChange();

									foreach(PairedFieldsControl pfc in backUdmfControls)
										pfc.ApplyTo(l.Back.Fields, min, max);

									if(!string.IsNullOrEmpty(lightBack.Text)) {
										string key = lightBack.Tag.ToString();
										bool absolute = (cbLightAbsoluteBack.CheckState == CheckState.Checked);
										int value = General.Clamp(lightBack.GetResult(UDMFTools.GetInteger(l.Back.Fields, key, 0)), (absolute ? 0 : -255), 255);
										UDMFTools.SetInteger(l.Back.Fields, key, value, 0, false);
									}

									foreach(CheckBox cb in backUdmfFlags) {
										if(cb.CheckState == CheckState.Indeterminate) continue;
										string key = cb.Tag.ToString();
										bool oldValue = l.Back.Fields.GetValue(key, false);

										if(cb.CheckState == CheckState.Checked) {
											if(!oldValue) l.Back.Fields[key] = new UniValue(UniversalType.Boolean, true);
										} else if(l.Back.Fields.ContainsKey(key)) {
											l.Back.Fields.Remove(key);
										}
									}
								}

                                l.Back.OffsetX = General.Clamp(backoffsetx.GetResult(l.Back.OffsetX), min, max);
								l.Back.OffsetY = General.Clamp(backoffsety.GetResult(l.Back.OffsetY), min, max);

								l.Back.SetTextureHigh(backhigh.GetResult(l.Back.HighTexture));
								l.Back.SetTextureMid(backmid.GetResult(l.Back.MiddleTexture));
								l.Back.SetTextureLow(backlow.GetResult(l.Back.LowTexture));
							}
						}
					}
				}

				// Custom fields
				fieldslist.Apply(l.Fields);

				//mxd. UDMF Settings
				if(General.Map.FormatInterface.HasCustomFields) {
					l.Fields.BeforeFieldsChange();
					if(cbRenderStyle.SelectedIndex == 1) { //add
						l.Fields["renderstyle"] = new UniValue(UniversalType.String, "add");
					} else if(l.Fields.ContainsKey("renderstyle")) {
						l.Fields.Remove("renderstyle");
					}

					fsAlpha.ApplyTo(l.Fields);

					if(lockNum > 0)
						l.Fields["locknumber"] = new UniValue(UniversalType.Integer, lockNum);
					else if(l.Fields.ContainsKey("locknumber"))
						l.Fields.Remove("locknumber");
				}

                //mxd. apply arg0str
				if(cbArgStr.Visible && cbArgStr.Checked && hasArg0str) {
					if(l.Fields.ContainsKey("arg0str"))
						l.Fields["arg0str"].Value = arg0str.Text;
					else
						l.Fields.Add("arg0str", new UniValue(2, arg0str.Text));
				} else if(l.Fields.ContainsKey("arg0str") && (string.IsNullOrEmpty(l.Fields["arg0str"].Value.ToString()) || !hasAcs || (hasAcs && !cbArgStr.Checked))) {
					l.Fields.Remove("arg0str");
				}
			}

			// Update the used textures
			General.Map.Data.UpdateUsedTextures();
			
			// Done
			General.Map.IsChanged = true;
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		// Cancel clicked
		private void cancel_Click(object sender, EventArgs e)
		{
			// Be gone
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		// Action changes
		private void action_ValueChanges(object sender, EventArgs e)
		{
			int showaction = 0;
			
			// Only when line type is known
			if(General.Map.Config.LinedefActions.ContainsKey(action.Value)) showaction = action.Value;
			
			// Change the argument descriptions
			arg0label.Text = General.Map.Config.LinedefActions[showaction].Args[0].Title + ":";
			arg1label.Text = General.Map.Config.LinedefActions[showaction].Args[1].Title + ":";
			arg2label.Text = General.Map.Config.LinedefActions[showaction].Args[2].Title + ":";
			arg3label.Text = General.Map.Config.LinedefActions[showaction].Args[3].Title + ":";
			arg4label.Text = General.Map.Config.LinedefActions[showaction].Args[4].Title + ":";
			arg0label.Enabled = General.Map.Config.LinedefActions[showaction].Args[0].Used;
			arg1label.Enabled = General.Map.Config.LinedefActions[showaction].Args[1].Used;
			arg2label.Enabled = General.Map.Config.LinedefActions[showaction].Args[2].Used;
			arg3label.Enabled = General.Map.Config.LinedefActions[showaction].Args[3].Used;
			arg4label.Enabled = General.Map.Config.LinedefActions[showaction].Args[4].Used;
			if(arg0label.Enabled) arg0.ForeColor = SystemColors.WindowText; else arg0.ForeColor = SystemColors.GrayText;
			if(arg1label.Enabled) arg1.ForeColor = SystemColors.WindowText; else arg1.ForeColor = SystemColors.GrayText;
			if(arg2label.Enabled) arg2.ForeColor = SystemColors.WindowText; else arg2.ForeColor = SystemColors.GrayText;
			if(arg3label.Enabled) arg3.ForeColor = SystemColors.WindowText; else arg3.ForeColor = SystemColors.GrayText;
			if(arg4label.Enabled) arg4.ForeColor = SystemColors.WindowText; else arg4.ForeColor = SystemColors.GrayText;
			arg0.Setup(General.Map.Config.LinedefActions[showaction].Args[0]);
			arg1.Setup(General.Map.Config.LinedefActions[showaction].Args[1]);
			arg2.Setup(General.Map.Config.LinedefActions[showaction].Args[2]);
			arg3.Setup(General.Map.Config.LinedefActions[showaction].Args[3]);
			arg4.Setup(General.Map.Config.LinedefActions[showaction].Args[4]);

			// mxd. Apply action's default arguments 
			if(!preventchanges) {
				if(showaction != 0 && General.Map.Config.LinedefActions.ContainsKey(showaction)) {
					arg0.SetDefaultValue();
					arg1.SetDefaultValue();
					arg2.SetDefaultValue();
					arg3.SetDefaultValue();
					arg4.SetDefaultValue();
				} else { //or set them to 0
					arg0.SetValue(0);
					arg1.SetValue(0);
					arg2.SetValue(0);
					arg3.SetValue(0);
					arg4.SetValue(0);
				}
			} 

            //mxd. update arg0str
            if (Array.IndexOf(GZBuilder.GZGeneral.ACS_SPECIALS, showaction) != -1) {
                arg0str.Visible = true;

                if (General.Map.UDMF && fieldslist.GetValue("arg0str") != null) {
                    cbArgStr.Visible = true;
                    cbArgStr.Checked = true;
                    setNamedScripts((string)fieldslist.GetValue("arg0str"));
                } else { //use script numbers
                    cbArgStr.Visible = General.Map.UDMF;
                    cbArgStr.Checked = false;
                    Linedef l = General.GetByIndex(lines, 0);
                    setNumberedScripts(l);
                }
            } else {
				if(cbArgStr.Checked) cbArgStr.Checked = false;
                cbArgStr.Visible = false;
				arg0label.Text = General.Map.Config.LinedefActions[showaction].Args[0].Title + ":";
                arg0str.Visible = false;
            }
		}

		// Browse Action clicked
		private void browseaction_Click(object sender, EventArgs e)
		{
			action.Value = ActionBrowserForm.BrowseAction(this, action.Value);
		}

		// Custom fields on front sides
		private void customfrontbutton_Click(object sender, EventArgs e)
		{
			// Make collection of front sides
			List<MapElement> sides = new List<MapElement>(lines.Count);
			foreach(Linedef l in lines) if(l.Front != null) sides.Add(l.Front);

			// Make undo
			string undodesc = "sidedef";
			if(sides.Count > 1) undodesc = sides.Count + " sidedefs";
			General.Map.UndoRedo.CreateUndo("Edit " + undodesc);
			
			// Edit these
			if(!CustomFieldsForm.ShowDialog(this, "Front side custom fields", "sidedef", sides, General.Map.Config.SidedefFields))
				General.Map.UndoRedo.WithdrawUndo();
		}

		// Custom fields on back sides
		private void custombackbutton_Click(object sender, EventArgs e)
		{
			// Make collection of back sides
			List<MapElement> sides = new List<MapElement>(lines.Count);
			foreach(Linedef l in lines) if(l.Back != null) sides.Add(l.Back);

			// Make undo
			string undodesc = "sidedef";
			if(sides.Count > 1) undodesc = sides.Count + " sidedefs";
			General.Map.UndoRedo.CreateUndo("Edit " + undodesc);
			
			// Edit these
			if(!CustomFieldsForm.ShowDialog(this, "Back side custom fields", "sidedef", sides, General.Map.Config.SidedefFields))
				General.Map.UndoRedo.WithdrawUndo();
		}

        //mxd
        private void cbArgStr_CheckedChanged(object sender, EventArgs e) {
            arg0str.Text = "";

            if (cbArgStr.Checked) {
                setNamedScripts((string)fieldslist.GetValue("arg0str"));
            } else if (!cbArgStr.Checked) {
                setNumberedScripts(General.GetByIndex(lines, 0));
            }

            arg0label.Text = cbArgStr.Checked ? "Script name:" : "Script number:";
        }

        //mxd
        private void arg0str_Leave(object sender, EventArgs e) {
            if (cbArgStr.Checked) fieldslist.SetValue("arg0str", arg0str.Text, CodeImp.DoomBuilder.Types.UniversalType.String);
        }

        //mxd
        private void fieldslist_OnFieldValueChanged(string fieldname) {
            if (cbArgStr.Checked && fieldname == "arg0str")
                arg0str.Text = (string)fieldslist.GetValue(fieldname);
        }

		//mxd
		private void tabcustom_MouseEnter(object sender, EventArgs e) {
			fieldslist.Focus();
		}

		// Help!
		private void LinedefEditForm_HelpRequested(object sender, HelpEventArgs hlpevent)
		{
			General.ShowHelp("w_linedefedit.html");
			hlpevent.Handled = true;
		}
	}
}