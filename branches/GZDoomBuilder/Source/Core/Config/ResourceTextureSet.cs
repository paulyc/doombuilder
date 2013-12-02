
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

using System.Collections.Generic;
using CodeImp.DoomBuilder.Data;

#endregion

namespace CodeImp.DoomBuilder.Config
{
	internal sealed class ResourceTextureSet : TextureSet, IFilledTextureSet
	{
		#region ================== Constants
		
		#endregion

		#region ================== Variables

		// Matching textures and flats
		private Dictionary<long, ImageData> textures;
		private Dictionary<long, ImageData> flats;
		private DataLocation location;

		#endregion

		#region ================== Properties
		
		public ICollection<ImageData> Textures { get { return textures.Values; } }
		public ICollection<ImageData> Flats { get { return General.Map.Config.MixTexturesFlats ? textures.Values : flats.Values; } }
		public DataLocation Location { get { return location; } }
		
		#endregion

		#region ================== Constructor / Destructor

		// New texture set constructor
		public ResourceTextureSet(string name, DataLocation location)
		{
			this.name = name;
			this.location = location;
			this.textures = new Dictionary<long, ImageData>();
			this.flats = new Dictionary<long, ImageData>();
		}
		
		#endregion

		#region ================== Methods
		
		// Add a texture
		internal void AddTexture(ImageData image)
		{
			if(textures.ContainsKey(image.LongName))
				General.ErrorLogger.Add(ErrorType.Warning, "Texture \"" + image.Name + "\" is double defined in resource \"" + this.Location.location + "\".");
			textures[image.LongName] = image;
		}

		// Add a flat
		internal void AddFlat(ImageData image)
		{
			if(flats.ContainsKey(image.LongName))
				General.ErrorLogger.Add(ErrorType.Warning, "Flat \"" + image.Name + "\" is double defined in resource \"" + this.Location.location + "\".");
			flats[image.LongName] = image;
		}

		// Check if this set has a texture
		internal bool TextureExists(ImageData image)
		{
			return textures.ContainsKey(image.LongName);
		}

		// Check if this set has a flat
		internal bool FlatExists(ImageData image)
		{
			if(General.Map.Config.MixTexturesFlats) return textures.ContainsKey(image.LongName); //mxd
			return flats.ContainsKey(image.LongName);
		}

		// Mix the textures and flats
		internal void MixTexturesAndFlats()
		{
			Dictionary<long, ImageData> newflats = new Dictionary<long, ImageData>(); //mxd
			
			// Add flats to textures
			foreach(KeyValuePair<long, ImageData> f in flats) {
				if(!textures.ContainsKey(f.Key))
					textures.Add(f.Key, f.Value);
				else
					newflats.Add(f.Key, f.Value); //mxd
			}

			flats = newflats; //mxd
		}
		
		#endregion
	}
}
