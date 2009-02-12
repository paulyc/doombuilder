
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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using CodeImp.DoomBuilder.IO;

#endregion

namespace CodeImp.DoomBuilder.Data
{
	internal abstract class PK3StructuredReader : DataReader
	{
		#region ================== Constants

		protected const string PATCHES_DIR = "patches";
		protected const string TEXTURES_DIR = "textures";
		protected const string FLATS_DIR = "flats";
		protected const string HIRES_DIR = "hires";
		protected const string SPRITES_DIR = "sprites";
		
		#endregion

		#region ================== Variables
		
		// Source
		protected bool roottextures;
		protected bool rootflats;
		
		// WAD files that must be loaded as well
		protected List<WADReader> wads;
		
		#endregion

		#region ================== Properties

		#endregion

		#region ================== Constructor / Disposer
		
		// Constructor
		public PK3StructuredReader(DataLocation dl) : base(dl)
		{
			// Initialize
			this.roottextures = dl.option1;
			this.rootflats = dl.option2;
		}
		
		// Call this to initialize this class
		protected virtual void Initialize()
		{
			// Load all WAD files in the root as WAD resources
			string[] wadfiles = GetFilesWithExt("", "wad", false);
			wads = new List<WADReader>(wadfiles.Length);
			foreach(string w in wadfiles)
			{
				string tempfile = CreateTempFile(w);
				DataLocation wdl = new DataLocation(DataLocation.RESOURCE_WAD, tempfile, false, false);
				wads.Add(new WADReader(wdl));
			}
		}
		
		// Disposer
		public override void Dispose()
		{
			// Not already disposed?
			if(!isdisposed)
			{
				// Clean up
				foreach(WADReader wr in wads) wr.Dispose();
				
				// Remove temp files
				foreach(WADReader wr in wads)
				{
					try { File.Delete(wr.Location.location); }
					catch(Exception) { }
				}
				
				// Done
				base.Dispose();
			}
		}
		
		#endregion
		
		#region ================== Management
		
		// This suspends use of this resource
		public override void Suspend()
		{
			foreach(WADReader wr in wads) wr.Suspend();
			base.Suspend();
		}
		
		// This resumes use of this resource
		public override void Resume()
		{
			foreach(WADReader wr in wads) wr.Resume();
			base.Resume();
		}
		
		#endregion
		
		#region ================== Palette

		// This loads the PLAYPAL palette
		public override Playpal LoadPalette()
		{
			// Error when suspended
			if(issuspended) throw new Exception("Data reader is suspended");
			
			// Palette from wad(s)
			Playpal palette = null;
			foreach(WADReader wr in wads)
			{
				Playpal wadpalette = wr.LoadPalette();
				if(wadpalette != null) return wadpalette;
			}
			
			// Find in root directory
			string foundfile = FindFirstFile("PLAYPAL", false);
			if((foundfile != null) && FileExists(foundfile))
			{
				MemoryStream stream = LoadFile(foundfile);
				palette = new Playpal(stream);
				stream.Dispose();
			}
			
			// Done
			return palette;
		}

		#endregion
		
		#region ================== Textures

		// This loads the textures
		public override ICollection<ImageData> LoadTextures(PatchNames pnames)
		{
			List<ImageData> images = new List<ImageData>();
			ICollection<ImageData> collection;
			List<ImageData> imgset = new List<ImageData>();
			
			// Error when suspended
			if(issuspended) throw new Exception("Data reader is suspended");
			
			// Load from wad files (NOTE: backward order, because the last wad's images have priority)
			for(int i = wads.Count - 1; i >= 0; i--)
			{
				collection = wads[i].LoadTextures(pnames);
				AddImagesToList(images, collection);
			}
			
			// Should we load the images in this directory as textures?
			if(roottextures)
			{
				collection = LoadDirectoryImages("", false, false);
				AddImagesToList(images, collection);
			}
			
			// TODO: Add support for hires texture here
			
			// Add images from texture directory
			collection = LoadDirectoryImages(TEXTURES_DIR, false, true);
			AddImagesToList(images, collection);

			// Load TEXTURES lump file
			imgset.Clear();
			string texturesfile = FindFirstFile("TEXTURES", false);
			if((texturesfile != null) && FileExists(texturesfile))
			{
				MemoryStream filedata = LoadFile(texturesfile);
				WADReader.LoadHighresTextures(filedata, texturesfile, ref imgset);
				filedata.Dispose();
			}

			// Add images from TEXTURES lump files
			AddImagesToList(images, imgset);
			
			// Load TEXTURE1 lump file
			imgset.Clear();
			string texture1file = FindFirstFile("TEXTURE1", false);
			if((texture1file != null) && FileExists(texture1file))
			{
				MemoryStream filedata = LoadFile(texture1file);
				WADReader.LoadTextureSet(filedata, ref imgset, pnames);
				filedata.Dispose();
			}

			// Load TEXTURE2 lump file
			string texture2file = FindFirstFile("TEXTURE2", false);
			if((texture2file != null) && FileExists(texture2file))
			{
				MemoryStream filedata = LoadFile(texture2file);
				WADReader.LoadTextureSet(filedata, ref imgset, pnames);
				filedata.Dispose();
			}

			// Add images from TEXTURE1 and TEXTURE2 lump files
			AddImagesToList(images, imgset);

			return images;
		}
		
		// This returns the patch names from the PNAMES lump
		// A directory resource does not support this lump, but the wads in the directory may contain this lump
		public override PatchNames LoadPatchNames()
		{
			PatchNames pnames;
			
			// Error when suspended
			if(issuspended) throw new Exception("Data reader is suspended");
			
			// Load from wad files
			// Note the backward order, because the last wad's images have priority
			for(int i = wads.Count - 1; i >= 0; i--)
			{
				pnames = wads[i].LoadPatchNames();
				if(pnames != null) return pnames;
			}
			
			// If none of the wads provides patch names, let's see if we can
			string pnamesfile = FindFirstFile("PNAMES", false);
			if((pnamesfile != null) && FileExists(pnamesfile))
			{
				MemoryStream pnamesdata = LoadFile(pnamesfile);
				pnames = new PatchNames(pnamesdata);
				pnamesdata.Dispose();
				return pnames;
			}
			
			return null;
		}
		
		#endregion

		#region ================== Flats
		
		// This loads the textures
		public override ICollection<ImageData> LoadFlats()
		{
			List<ImageData> images = new List<ImageData>();
			ICollection<ImageData> collection;
			
			// Error when suspended
			if(issuspended) throw new Exception("Data reader is suspended");
			
			// Load from wad files
			// Note the backward order, because the last wad's images have priority
			for(int i = wads.Count - 1; i >= 0; i--)
			{
				collection = wads[i].LoadFlats();
				AddImagesToList(images, collection);
			}
			
			// Should we load the images in this directory as flats?
			if(rootflats)
			{
				collection = LoadDirectoryImages("", true, false);
				AddImagesToList(images, collection);
			}
			
			// Add images from flats directory
			collection = LoadDirectoryImages(FLATS_DIR, true, true);
			AddImagesToList(images, collection);
			
			return images;
		}
		
		#endregion

		#region ================== Decorate

		// This finds and returns a sprite stream
		public override Stream GetDecorateData(string pname)
		{
			// Error when suspended
			if(issuspended) throw new Exception("Data reader is suspended");

			// Find in any of the wad files
			for(int i = wads.Count - 1; i >= 0; i--)
			{
				Stream sprite = wads[i].GetDecorateData(pname);
				if(sprite != null) return sprite;
			}

			// Find in root directory
			string filename = Path.GetFileName(pname);
			string pathname = Path.GetDirectoryName(pname);
			string foundfile = (filename.IndexOf('.') > -1) ? FindFirstFileWithExt(pathname, filename, false) : FindFirstFile(pathname, filename, false);
			if((foundfile != null) && FileExists(foundfile))
			{
				return LoadFile(foundfile);
			}

			// Nothing found
			return null;
		}

		#endregion
		
		#region ================== Methods
		
		// This loads the images in this directory
		private ICollection<ImageData> LoadDirectoryImages(string path, bool flats, bool includesubdirs)
		{
			List<ImageData> images = new List<ImageData>();
			string[] files;
			string name;
			
			// Go for all files
			files = GetAllFiles(path, includesubdirs);
			foreach(string f in files)
			{
				// Make the texture name from filename without extension
				name = Path.GetFileNameWithoutExtension(f).ToUpperInvariant();
				if(name.Length > 8) name = name.Substring(0, 8);
				
				// Add image to list
				images.Add(CreateImage(name, f, flats));
			}
			
			// Return result
			return images;
		}
		
		// This copies images from a collection unless they already exist in the list
		private void AddImagesToList(List<ImageData> targetlist, ICollection<ImageData> sourcelist)
		{
			// Go for all source images
			foreach(ImageData src in sourcelist)
			{
				// Check if exists in target list
				bool alreadyexists = false;
				foreach(ImageData tgt in targetlist)
				{
					if(tgt.LongName == src.LongName)
					{
						alreadyexists = true;
						break;
					}
				}
				
				// Add source image to target list
				if(!alreadyexists) targetlist.Add(src);
			}
		}
		
		// This must create an image
		protected abstract ImageData CreateImage(string name, string filename, bool flat);

		// This must return true if the specified file exists
		protected abstract bool FileExists(string filename);
		
		// This must return all files in a given directory
		protected abstract string[] GetAllFiles(string path, bool subfolders);

		// This must return all files in a given directory that match the given extension
		protected abstract string[] GetFilesWithExt(string path, string extension, bool subfolders);

		// This must find the first file that has the specific name, regardless of file extension
		protected abstract string FindFirstFile(string beginswith, bool subfolders);

		// This must find the first file that has the specific name, regardless of file extension
		protected abstract string FindFirstFile(string path, string beginswith, bool subfolders);

		// This must find the first file that has the specific name
		protected abstract string FindFirstFileWithExt(string path, string beginswith, bool subfolders);
		
		// This must load an entire file in memory and returns the stream
		// NOTE: Callers are responsible for disposing the stream!
		protected abstract MemoryStream LoadFile(string filename);

		// This must create a temp file for the speciied file and return the absolute path to the temp file
		// NOTE: Callers are responsible for removing the temp file when done!
		protected abstract string CreateTempFile(string filename);

		// This makes the path relative to the directory, if needed
		protected virtual string MakeRelativePath(string anypath)
		{
			if(Path.IsPathRooted(anypath))
			{
				// Make relative
				string lowpath = anypath.ToLowerInvariant();
				string lowlocation = location.location.ToLowerInvariant();
				if((lowpath.Length > (lowlocation.Length + 1)) && lowpath.StartsWith(lowlocation))
					return anypath.Substring(lowlocation.Length + 1);
				else
					return anypath;
			}
			else
			{
				// Path is already relative
				return anypath;
			}
		}
		
		#endregion
	}
}
