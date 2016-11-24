﻿#region ================== Namespaces

using System;
using System.Collections.Generic;
using System.IO;
using CodeImp.DoomBuilder.Config;

#endregion

namespace CodeImp.DoomBuilder.Data.Scripting
{
	public sealed class ScriptResource
	{
		#region ================== Variables

		private string filename;
		private string filepathname; 
		private string resourcedisplayname;
		private int lumpindex = -1;
		private DataReader resource;
		private string resourcepath;
		private HashSet<string> entries;
		private ScriptType scripttype;
		private bool isreadonly;

		// Special cases...
		private string parentresourcelocation;

		#endregion

		#region ================== Properties

		public string Filename { get { return filename; } } // Path to text file inside of Resource
		public string FilePathName { get { return filepathname; } } // Resource location and file path inside resource combined
		public int LumpIndex { get { return lumpindex; } } // Text lump index if Resource is wad, -1 otherwise
		internal DataReader Resource { get { return GetResource(); } }
		public HashSet<string> Entries { get { return entries; } } // Actors/models/sounds etc.
		public ScriptType ScriptType { get { return scripttype; } }
		public bool IsReadOnly { get { return isreadonly; } }

		#endregion

		#region ================== Constructor

		public ScriptResource(TextResourceData source, ScriptType type)
		{
			resource = source.Source;
			resourcepath = resource.Location.location;
			resourcedisplayname = resource.Location.GetDisplayName();
			filename = source.Filename.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			filepathname = Path.Combine(resourcepath, filename);
			entries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			lumpindex = source.LumpIndex;
			scripttype = type;
			isreadonly = resource.IsReadOnly;

			// Embedded resources require additional tender loving care...
			if(resource is WADReader)
			{
				WADReader wr = (WADReader)resource;
				if(wr.ParentResource is PK3Reader)
					parentresourcelocation = wr.ParentResource.Location.location;
			}
		}

		#endregion

		#region ================== Methods

		private DataReader GetResource()
		{
			if(resource == null || resource.IsDisposed)
			{
				resource = null;

				// Try to re-aquire resource
				if(!string.IsNullOrEmpty(parentresourcelocation))
				{
					// Special case: WAD resource inside of PK3 resource.
					// Resource resourcepath will be different after reloading resources, because it's randomly generated.
					// So resolve using displayname and parent resource location...
					foreach(DataReader reader in General.Map.Data.Containers)
					{
						// Found parent
						if(reader.Location.location == parentresourcelocation && reader is PK3Reader)
						{
							PK3Reader pr = (PK3Reader)reader;
							foreach(WADReader wr in pr.Wads)
							{
								if(wr.Location.GetDisplayName() == resourcedisplayname)
								{
									// Found it
									resource = reader;

									// Some paths need updating...
									resourcepath = resource.Location.location;
									filepathname = Path.Combine(resourcepath, filename);

									break;
								}
							}
						}
					}
				}
				else
				{
					foreach(DataReader reader in General.Map.Data.Containers)
					{
						if(reader.Location.location == resourcepath)
						{
							// Found it
							resource = reader;
							break;
						}
					}
				}
			}

			return resource;
		}

		// Used as tab and script navigator item title
		public override string ToString()
		{
			return (lumpindex != -1 ? lumpindex + ":" : "") + Path.GetFileName(filename);
		}

		#endregion
	}
}
