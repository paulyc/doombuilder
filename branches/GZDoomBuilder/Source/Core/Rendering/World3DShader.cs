
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
using System.Drawing;
using System.ComponentModel;
using CodeImp.DoomBuilder.Map;
using SlimDX.Direct3D9;
using SlimDX;
using CodeImp.DoomBuilder.Geometry;
using System.Drawing.Imaging;

#endregion

namespace CodeImp.DoomBuilder.Rendering
{
	internal sealed class World3DShader : D3DShader
	{
		#region ================== Variables

		// Property handlers
		private EffectHandle texture1;
		private EffectHandle worldviewproj;
		private EffectHandle minfiltersettings;
		private EffectHandle magfiltersettings;
		private EffectHandle mipfiltersettings;
		private EffectHandle maxanisotropysetting;
		private EffectHandle modulatecolor;
		private EffectHandle highlightcolor;

        //mxd
        private EffectHandle vertexColorHadle;
        //lights
        private EffectHandle lightPositionAndRadiusHandle;
        private EffectHandle lightColorHandle;
        private EffectHandle worldHandle;

		
		#endregion

		#region ================== Properties

		public Matrix WorldViewProj { set { if(manager.Enabled) effect.SetValue<Matrix>(worldviewproj, value); } }
		public Texture Texture1 { set { if(manager.Enabled) effect.SetTexture(texture1, value); } }

        //mxd
        public Color4 VertexColor { set { if (manager.Enabled) effect.SetValue<Color4>(vertexColorHadle, value); } }
        public Color4 LightColor { set { if (manager.Enabled) effect.SetValue<Color4>(lightColorHandle, value); } }
        public Vector4 LightPositionAndRadius { set { if (manager.Enabled) effect.SetValue(lightPositionAndRadiusHandle, value); } }
        public Matrix World { set { if (manager.Enabled) effect.SetValue<Matrix>(worldHandle, value); } }

		#endregion

		#region ================== Constructor / Disposer

		// Constructor
		public World3DShader(ShaderManager manager) : base(manager)
		{
			// Load effect from file
			effect = LoadEffect("world3d.fx");

			// Get the property handlers from effect
			if(effect != null)
			{
				worldviewproj = effect.GetParameter(null, "worldviewproj");
				texture1 = effect.GetParameter(null, "texture1");
				minfiltersettings = effect.GetParameter(null, "minfiltersettings");
				magfiltersettings = effect.GetParameter(null, "magfiltersettings");
				mipfiltersettings = effect.GetParameter(null, "mipfiltersettings");
				modulatecolor = effect.GetParameter(null, "modulatecolor");
				highlightcolor = effect.GetParameter(null, "highlightcolor");
				maxanisotropysetting = effect.GetParameter(null, "maxanisotropysetting");

                //mxd
                vertexColorHadle = effect.GetParameter(null, "vertexColor");
                //lights
                lightPositionAndRadiusHandle = effect.GetParameter(null, "lightPosAndRadius");
                lightColorHandle = effect.GetParameter(null, "lightColor");
                worldHandle = effect.GetParameter(null, "world");
			}

			// Initialize world vertex declaration
			VertexElement[] elements = new VertexElement[]
			{
				new VertexElement(0, 0, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position, 0),
				new VertexElement(0, 12, DeclarationType.Color, DeclarationMethod.Default, DeclarationUsage.Color, 0),
				new VertexElement(0, 16, DeclarationType.Float2, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0),
				//mxd
                new VertexElement(0, 24, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Normal, 0),
                VertexElement.VertexDeclarationEnd
			};
			vertexdecl = new VertexDeclaration(General.Map.Graphics.Device, elements);

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
				if(texture1 != null) texture1.Dispose();
				if(worldviewproj != null) worldviewproj.Dispose();
				if(minfiltersettings != null) minfiltersettings.Dispose();
				if(magfiltersettings != null) magfiltersettings.Dispose();
				if(mipfiltersettings != null) mipfiltersettings.Dispose();
				if(modulatecolor != null) modulatecolor.Dispose();
				if(highlightcolor != null) highlightcolor.Dispose();
				if(maxanisotropysetting != null) maxanisotropysetting.Dispose();

                //mxd
                if (vertexColorHadle != null) vertexColorHadle.Dispose();
                if (lightColorHandle != null) lightColorHandle.Dispose();
                if (lightPositionAndRadiusHandle != null) lightPositionAndRadiusHandle.Dispose();
                if (worldHandle != null) worldHandle.Dispose();

				// Done
				base.Dispose();
			}
		}

		#endregion

		#region ================== Methods

		// This sets the constant settings
		public void SetConstants(bool bilinear, bool useanisotropic, float maxanisotropy)
		{
			if(manager.Enabled)
			{
				if(bilinear)
				{
					if(useanisotropic)
					{
						effect.SetValue(magfiltersettings, (int)TextureFilter.Linear);
						effect.SetValue(minfiltersettings, (int)TextureFilter.Anisotropic);
						effect.SetValue(mipfiltersettings, (int)TextureFilter.Linear);
						effect.SetValue(maxanisotropysetting, maxanisotropy);
					}
					else
					{
						effect.SetValue(magfiltersettings, (int)TextureFilter.Linear);
						effect.SetValue(minfiltersettings, (int)TextureFilter.Linear);
						effect.SetValue(mipfiltersettings, (int)TextureFilter.Linear);
						effect.SetValue(maxanisotropysetting, 1.0f);
					}
				}
				else
				{
					effect.SetValue(magfiltersettings, (int)TextureFilter.Point);
					effect.SetValue(minfiltersettings, (int)TextureFilter.Point);
					effect.SetValue(mipfiltersettings, (int)TextureFilter.Linear);
					effect.SetValue(maxanisotropysetting, 1.0f);
				}
			}
		}

		// This sets the modulation color
		public void SetModulateColor(int modcolor)
		{
			if(manager.Enabled)
			{
				effect.SetValue(modulatecolor, new Color4(modcolor));
			}
		}

		// This sets the highlight color
		public void SetHighlightColor(int hicolor)
		{
			if(manager.Enabled)
			{
				effect.SetValue(highlightcolor, new Color4(hicolor));
			}
		}

		// This sets up the render pipeline
		public override void BeginPass(int index)
		{
			Device device = manager.D3DDevice.Device;

			if(!manager.Enabled)
			{
				// Sampler settings
				if(General.Settings.VisualBilinear)
				{
					device.SetSamplerState(0, SamplerState.MagFilter, TextureFilter.Linear);
					device.SetSamplerState(0, SamplerState.MinFilter, TextureFilter.Linear);
					device.SetSamplerState(0, SamplerState.MipFilter, TextureFilter.Linear);
					device.SetSamplerState(0, SamplerState.MipMapLodBias, 0f);
				}
				else
				{
					device.SetSamplerState(0, SamplerState.MagFilter, TextureFilter.Point);
					device.SetSamplerState(0, SamplerState.MinFilter, TextureFilter.Point);
					device.SetSamplerState(0, SamplerState.MipFilter, TextureFilter.Linear);
					device.SetSamplerState(0, SamplerState.MipMapLodBias, 0f);
				}

				// Texture addressing
				device.SetSamplerState(0, SamplerState.AddressU, TextureAddress.Wrap);
				device.SetSamplerState(0, SamplerState.AddressV, TextureAddress.Wrap);
				device.SetSamplerState(0, SamplerState.AddressW, TextureAddress.Wrap);
				
				// First texture stage
                //mxd
				//if((index == 0) || (index == 2))
                if ((index == 0) || (index == 2) || (index == 4) || (index == 6) || index > 7)
				{
					// Normal
					device.SetTextureStageState(0, TextureStage.ColorOperation, TextureOperation.Modulate);
					device.SetTextureStageState(0, TextureStage.ColorArg1, TextureArgument.Texture);
					device.SetTextureStageState(0, TextureStage.ColorArg2, TextureArgument.Diffuse);
					device.SetTextureStageState(0, TextureStage.ResultArg, TextureArgument.Current);
					device.SetTextureStageState(0, TextureStage.TexCoordIndex, 0);
				}
				else
				{
					// Full brightness
					device.SetTextureStageState(0, TextureStage.ColorOperation, TextureOperation.SelectArg1);
					device.SetTextureStageState(0, TextureStage.ColorArg1, TextureArgument.Texture);
					device.SetTextureStageState(0, TextureStage.ResultArg, TextureArgument.Current);
					device.SetTextureStageState(0, TextureStage.TexCoordIndex, 0);
				}

				// First alpha stage
				device.SetTextureStageState(0, TextureStage.AlphaOperation, TextureOperation.Modulate);
				device.SetTextureStageState(0, TextureStage.AlphaArg1, TextureArgument.Texture);
				device.SetTextureStageState(0, TextureStage.AlphaArg2, TextureArgument.Diffuse);
				
				// Second texture stage
				device.SetTextureStageState(1, TextureStage.ColorOperation, TextureOperation.Modulate);
				device.SetTextureStageState(1, TextureStage.ColorArg1, TextureArgument.Current);
				device.SetTextureStageState(1, TextureStage.ColorArg2, TextureArgument.TFactor);
				device.SetTextureStageState(1, TextureStage.ResultArg, TextureArgument.Current);
				device.SetTextureStageState(1, TextureStage.TexCoordIndex, 0);

				// Second alpha stage
				device.SetTextureStageState(1, TextureStage.AlphaOperation, TextureOperation.Modulate);
				device.SetTextureStageState(1, TextureStage.AlphaArg1, TextureArgument.Current);
				device.SetTextureStageState(1, TextureStage.AlphaArg2, TextureArgument.TFactor);

				// Highlight?
                //mxd
				//if(index > 1)
                if ((index > 1 && index < 4) || (index > 5 && index < 8))
				{
					// Third texture stage
					device.SetTextureStageState(2, TextureStage.ColorOperation, TextureOperation.AddSigned);
					device.SetTextureStageState(2, TextureStage.ColorArg1, TextureArgument.Current);
					device.SetTextureStageState(2, TextureStage.ColorArg2, TextureArgument.Texture);
					device.SetTextureStageState(2, TextureStage.ColorArg0, TextureArgument.Texture);
					device.SetTextureStageState(2, TextureStage.ResultArg, TextureArgument.Current);
					device.SetTextureStageState(2, TextureStage.TexCoordIndex, 0);

					// Third alpha stage
					device.SetTextureStageState(2, TextureStage.AlphaOperation, TextureOperation.SelectArg1);
					device.SetTextureStageState(2, TextureStage.AlphaArg1, TextureArgument.Current);

					// Fourth texture stage
					device.SetTextureStageState(3, TextureStage.ColorOperation, TextureOperation.AddSigned);
					device.SetTextureStageState(3, TextureStage.ColorArg1, TextureArgument.Current);
					device.SetTextureStageState(3, TextureStage.ColorArg2, TextureArgument.Texture);
					device.SetTextureStageState(3, TextureStage.ColorArg0, TextureArgument.Texture);
					device.SetTextureStageState(3, TextureStage.ResultArg, TextureArgument.Current);
					device.SetTextureStageState(3, TextureStage.TexCoordIndex, 0);

					// Fourth alpha stage
					device.SetTextureStageState(3, TextureStage.AlphaOperation, TextureOperation.SelectArg1);
					device.SetTextureStageState(3, TextureStage.AlphaArg1, TextureArgument.Current);

					// No more further stages
					device.SetTextureStageState(4, TextureStage.ColorOperation, TextureOperation.Disable);
					device.SetTextureStageState(4, TextureStage.AlphaOperation, TextureOperation.Disable);
				}
				else
				{
					// No more further stages
					device.SetTextureStageState(2, TextureStage.ColorOperation, TextureOperation.Disable);
					device.SetTextureStageState(2, TextureStage.AlphaOperation, TextureOperation.Disable);
				}
			}

			base.BeginPass(index);
		}
		
		#endregion
	}
}
