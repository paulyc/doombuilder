
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
using System.Diagnostics;
using CodeImp.DoomBuilder.Actions;
using ICSharpCode.SharpZipLib.BZip2;

#endregion

namespace CodeImp.DoomBuilder.Editing
{
	public class CopyPasteManager
	{
		#region ================== Constants
		
		private const string CLIPBOARD_DATA_FORMAT = "DOOM_BUILDER_GEOMETRY";

		#endregion
		
		#region ================== Variables
		
		// Disposing
		private bool isdisposed = false;
		
		// Last inserted prefab
		private string lastprefabfile;
		
		#endregion
		
		#region ================== Properties
		
		public bool IsDisposed { get { return isdisposed; } }
		public bool IsPreviousPrefabAvailable { get { return (lastprefabfile != null); } }

		#endregion
		
		#region ================== Constructor / Disposer
		
		// Constructor
		internal CopyPasteManager()
		{
			// Initialize
			
			// Bind any methods
			General.Actions.BindMethods(this);
			
			// We have no destructor
			GC.SuppressFinalize(this);
		}

		// Disposer
		internal void Dispose()
		{
			// Not already disposed?
			if(!isdisposed)
			{
				// Unbind any methods
				General.Actions.UnbindMethods(this);
				
				// Done
				isdisposed = true;
			}
		}
		
		#endregion
		
		#region ================== Methods
		
		// This makes a prefab of the selection. Returns null when cancelled.
		internal MemoryStream MakePrefab()
		{
			// Let the plugins know
			if(General.Plugins.OnCopyBegin())
			{
				// Ask the editing mode to prepare selection for copying.
				// The edit mode should mark all vertices, lines and sectors
				// that need to be copied.
				if(General.Editing.Mode.OnCopyBegin())
				{
					// Copy the marked geometry
					// This links sidedefs that are not linked to a marked sector to a virtual sector
					MapSet copyset = General.Map.Map.CloneMarked();
					
					// Convert flags and activations to UDMF fields, if needed
					if(!(General.Map.FormatInterface is UniversalMapSetIO)) copyset.TranslateToUDMF();

					// Write data to stream
					MemoryStream memstream = new MemoryStream();
					UniversalStreamWriter writer = new UniversalStreamWriter();
					writer.RememberCustomTypes = false;
					writer.Write(copyset, memstream, null);

					// Compress the stream
					MemoryStream compressed = new MemoryStream((int)memstream.Length);
					memstream.Seek(0, SeekOrigin.Begin);
					BZip2.Compress(memstream, compressed, 900000);

					// Done
					memstream.Dispose();
					General.Editing.Mode.OnCopyEnd();
					General.Plugins.OnCopyEnd();
					return compressed;
				}
			}

			// Aborted
			return null;
		}

		// This pastes a prefab. Returns false when paste was cancelled.
		internal void PastePrefab(Stream filedata)
		{
			// Create undo
			General.Map.UndoRedo.CreateUndo("Insert prefab");

			// Decompress stream
			MemoryStream decompressed = new MemoryStream((int)filedata.Length * 3);
			filedata.Seek(0, SeekOrigin.Begin);
			BZip2.Decompress(filedata, decompressed);
			MemoryStream memstream = new MemoryStream(decompressed.ToArray());
			decompressed.Dispose();
			
			// Mark all current geometry
			General.Map.Map.ClearAllMarks(true);

			// Read data stream
			UniversalStreamReader reader = new UniversalStreamReader();
			reader.StrictChecking = false;
			reader.Read(General.Map.Map, memstream);
			
			// The new geometry is not marked, so invert the marks to get it marked
			General.Map.Map.InvertAllMarks();

			// Convert UDMF fields back to flags and activations, if needed
			if(!(General.Map.FormatInterface is UniversalMapSetIO)) General.Map.Map.TranslateFromUDMF();

			// Done
			memstream.Dispose();
			General.Map.Map.UpdateConfiguration();
			General.Map.ThingsFilter.Update();
			General.Editing.Mode.OnPasteEnd();
			General.Plugins.OnPasteEnd();
		}
		
		// This performs the copy. Returns false when copy was cancelled.
		private bool DoCopySelection()
		{
			// Let the plugins know
			if(General.Plugins.OnCopyBegin())
			{
				// Ask the editing mode to prepare selection for copying.
				// The edit mode should mark all vertices, lines and sectors
				// that need to be copied.
				if(General.Editing.Mode.OnCopyBegin())
				{
					// Copy the marked geometry
					// This links sidedefs that are not linked to a marked sector to a virtual sector
					MapSet copyset = General.Map.Map.CloneMarked();
					
					// Convert flags and activations to UDMF fields, if needed
					if(!(General.Map.FormatInterface is UniversalMapSetIO)) copyset.TranslateToUDMF();
					
					// Write data to stream
					MemoryStream memstream = new MemoryStream();
					UniversalStreamWriter writer = new UniversalStreamWriter();
					writer.RememberCustomTypes = false;
					writer.Write(copyset, memstream, null);

					// Set on clipboard
					Clipboard.SetData(CLIPBOARD_DATA_FORMAT, memstream);

					// Done
					memstream.Dispose();
					General.Editing.Mode.OnCopyEnd();
					General.Plugins.OnCopyEnd();
					return true;
				}
			}
			
			// Aborted
			return false;
		}
		
		// This performs the paste. Returns false when paste was cancelled.
		private bool DoPasteSelection()
		{
			// Anything to paste?
			if(Clipboard.ContainsData(CLIPBOARD_DATA_FORMAT))
			{
				// Cancel volatile mode
				General.DisengageVolatileMode();
				
				// Let the plugins know
				if(General.Plugins.OnPasteBegin())
				{
					// Ask the editing mode to prepare selection for pasting.
					if(General.Editing.Mode.OnPasteBegin())
					{
						// Create undo
						General.Map.UndoRedo.CreateUndo("Paste");
						
						// Read from clipboard
						Stream memstream = (Stream)Clipboard.GetData(CLIPBOARD_DATA_FORMAT);
						memstream.Seek(0, SeekOrigin.Begin);

						// Mark all current geometry
						General.Map.Map.ClearAllMarks(true);

						// Read data stream
						UniversalStreamReader reader = new UniversalStreamReader();
						reader.StrictChecking = false;
						reader.Read(General.Map.Map, memstream);
						
						// The new geometry is not marked, so invert the marks to get it marked
						General.Map.Map.InvertAllMarks();

						// Convert UDMF fields back to flags and activations, if needed
						if(!(General.Map.FormatInterface is UniversalMapSetIO)) General.Map.Map.TranslateFromUDMF();

						// Done
						memstream.Dispose();
						General.Map.Map.UpdateConfiguration();
						General.Map.ThingsFilter.Update();
						General.Editing.Mode.OnPasteEnd();
						General.Plugins.OnPasteEnd();
						return true;
					}
				}
				
				// Aborted
				return false;
			}
			else
			{
				// Nothing usefull on the clipboard
				General.MessageBeep(MessageBeepType.Warning);
				return false;
			}
		}
		
		#endregion
		
		#region ================== Actions
		
		// This copies the current selection
		[BeginAction("copyselection")]
		public void CopySelection()
		{
			DoCopySelection();
		}
		
		// This cuts the current selection
		[BeginAction("cutselection")]
		public void CutSelection()
		{
			// Copy selected geometry
			if(DoCopySelection())
			{
				// Get the delete action and check if it's bound
				Action deleteitem = General.Actions["builder_deleteitem"];
				if(deleteitem.BeginBound)
				{
					// Perform delete action
					deleteitem.Begin();
					deleteitem.End();
				}
				else
				{
					// Action not bound
					General.Interface.DisplayWarning("Cannot remove that in this mode.");
				}
			}
		}
		
		// This pastes what is on the clipboard and marks the new geometry
		[BeginAction("pasteselection")]
		public void PasteSelection()
		{
			DoPasteSelection();
		}

		// This creates a new prefab from selection
		[BeginAction("createprefab")]
		public void CreatePrefab()
		{
			Cursor oldcursor = Cursor.Current;
			Cursor.Current = Cursors.WaitCursor;
			
			MemoryStream data = MakePrefab();
			if(data != null)
			{
				Cursor.Current = oldcursor;
				
				SaveFileDialog savefile = new SaveFileDialog();
				savefile.Filter = "Doom Builder Prefabs (*.dbprefab)|*.dbprefab";
				savefile.Title = "Save Prefab As";
				savefile.AddExtension = true;
				savefile.CheckPathExists = true;
				savefile.OverwritePrompt = true;
				savefile.ValidateNames = true;
				if(savefile.ShowDialog(General.MainWindow) == DialogResult.OK)
				{
					try
					{
						Cursor.Current = Cursors.WaitCursor;
						if(File.Exists(savefile.FileName)) File.Delete(savefile.FileName);
						File.WriteAllBytes(savefile.FileName, data.ToArray());
					}
					catch(Exception e)
					{
						Cursor.Current = oldcursor;
						General.WriteLogLine("ERROR: " + e.GetType().Name + " while writing prefab to file: " + e.Message);
						General.ShowErrorMessage("Error while writing prefab to file! See log file for error details.", MessageBoxButtons.OK);
					}
				}
				data.Dispose();
			}
			else
			{
				// Can't make a prefab right now
				General.MessageBeep(MessageBeepType.Warning);
			}
			
			// Done
			General.MainWindow.UpdateInterface();
			Cursor.Current = oldcursor;
		}
		
		// This pastes a prefab from file
		[BeginAction("insertprefabfile")]
		public void InsertPrefabFile()
		{
			// Cancel volatile mode
			General.DisengageVolatileMode();

			// Let the plugins know
			if(General.Plugins.OnPasteBegin())
			{
				// Ask the editing mode to prepare selection for pasting.
				if(General.Editing.Mode.OnPasteBegin())
				{
					Cursor oldcursor = Cursor.Current;

					OpenFileDialog openfile = new OpenFileDialog();
					openfile.Filter = "Doom Builder Prefabs (*.dbprefab)|*.dbprefab";
					openfile.Title = "Open Prefab";
					openfile.AddExtension = false;
					openfile.CheckFileExists = true;
					openfile.Multiselect = false;
					openfile.ValidateNames = true;
					if(openfile.ShowDialog(General.MainWindow) == DialogResult.OK)
					{
						FileStream stream = null;
						
						try
						{
							Cursor.Current = Cursors.WaitCursor;
							stream = File.OpenRead(openfile.FileName);
						}
						catch(Exception e)
						{
							Cursor.Current = oldcursor;
							General.WriteLogLine("ERROR: " + e.GetType().Name + " while reading prefab from file: " + e.Message);
							General.ShowErrorMessage("Error while reading prefab from file! See log file for error details.", MessageBoxButtons.OK);
						}

						if(stream != null)
						{
							PastePrefab(stream);
							lastprefabfile = openfile.FileName;
						}
						General.MainWindow.UpdateInterface();
						stream.Dispose();
					}
					
					Cursor.Current = oldcursor;
				}
			}
		}
		
		// This pastes the previously inserted prefab
		[BeginAction("insertpreviousprefab")]
		public void InsertPreviousPrefab()
		{
			// Is there a previously inserted prefab?
			if(IsPreviousPrefabAvailable)
			{
				// Does the file still exist?
				if(File.Exists(lastprefabfile))
				{
					// Cancel volatile mode
					General.DisengageVolatileMode();

					// Let the plugins know
					if(General.Plugins.OnPasteBegin())
					{
						// Ask the editing mode to prepare selection for pasting.
						if(General.Editing.Mode.OnPasteBegin())
						{
							Cursor oldcursor = Cursor.Current;
							FileStream stream = null;

							try
							{
								Cursor.Current = Cursors.WaitCursor;
								stream = File.OpenRead(lastprefabfile);
							}
							catch(Exception e)
							{
								Cursor.Current = oldcursor;
								General.WriteLogLine("ERROR: " + e.GetType().Name + " while reading prefab from file: " + e.Message);
								General.ShowErrorMessage("Error while reading prefab from file! See log file for error details.", MessageBoxButtons.OK);
							}

							if(stream != null) PastePrefab(stream);
							stream.Dispose();
							General.MainWindow.UpdateInterface();
							Cursor.Current = oldcursor;
						}
					}
				}
				else
				{
					General.MessageBeep(MessageBeepType.Warning);
					lastprefabfile = null;
					General.MainWindow.UpdateInterface();
				}
			}
			else
			{
				General.MessageBeep(MessageBeepType.Warning);
			}
		}
		
		#endregion
	}
}
