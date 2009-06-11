
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
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Plugins;
using CodeImp.DoomBuilder.Windows;
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;
using System.Diagnostics;
using CodeImp.DoomBuilder.Actions;

#endregion

namespace CodeImp.DoomBuilder.Editing
{
	public class UndoManager
	{
		#region ================== Constants

		// Maximum undo/redo levels
		private const int MAX_UNDO_LEVELS = 1000;
		
		// Default stream capacity
		private const int STREAM_CAPACITY = 1000;
		
		// Stream codes
		// "Prp" stands for property changes (uses the ReadWrite functions)
		// "Ref" stands for reference changes
		private enum StreamCodes : byte
		{
			AddVertex,
			RemVertex,
			PrpVertex,
			AddLinedef,
			RemLinedef,
			PrpLinedef,
			RefLinedefStart,
			RefLinedefEnd,
			RefLinedefFront,
			RefLinedefBack,
			AddSidedef,
			RemSidedef,
			PrpSidedef,
			RefSidedefSector,
			AddSector,
			RemSector,
			PrpSector,
			AddThing,
			RemThing,
			PrpThing,
		}
		
		#endregion

		#region ================== Variables

		// Undo and redo stacks
		private List<UndoSnapshot> undos;
		private List<UndoSnapshot> redos;

		// Grouping
		private Plugin lastgroupplugin;
		private int lastgroupid;
		private int lastgrouptag;
		
		// Unique tickets
		private int ticketid;
		
		// Writing stream
		private UndoSnapshot snapshot;
		private bool isundosnapshot;
		private MemoryStream stream;
		private SerializerStream ss;
		private int commandswritten;
		private long prevstreamlength;
		private bool ignorepropchanges;
		private bool isrecordingcommand;
		private MapElement propsrecorded;
		
		// Background thread
		private volatile bool dobackgroundwork;
		private Thread backgroundthread;
		
		// Disposing
		private bool isdisposed = false;

		#endregion

		#region ================== Properties

		public UndoSnapshot NextUndo
		{
			get
			{
				if(!isundosnapshot && (snapshot != null))
					return snapshot;
				else if(undos.Count > 0)
					return undos[0];
				else
					return null;
			}
		}
		
		public UndoSnapshot NextRedo
		{
			get
			{
				if(isundosnapshot && (snapshot != null))
					return snapshot;
				else if(redos.Count > 0)
					return redos[0];
				else
					return null;
			}
		}
		
		public bool IsDisposed { get { return isdisposed; } }
		
		/// <summary>
		/// This can be used to ignore insignificant element property changes. Any property changes
		/// that are made while this is set to True will not be undoable. Use with great care!
		/// </summary>
		public bool IgnorePropChanges { get { return ignorepropchanges; } set { ignorepropchanges = value; } }
		
		#endregion

		#region ================== Constructor / Disposer

		// Constructor
		internal UndoManager()
		{
			// Initialize
			ticketid = 1;
			undos = new List<UndoSnapshot>(MAX_UNDO_LEVELS + 1);
			redos = new List<UndoSnapshot>(MAX_UNDO_LEVELS + 1);
			
			// Bind any methods
			General.Actions.BindMethods(this);

			// Start background thread
			backgroundthread = new Thread(new ThreadStart(BackgroundThread));
			backgroundthread.Name = "Snapshot Compressor";
			backgroundthread.Priority = ThreadPriority.Lowest;
			backgroundthread.IsBackground = true;
			backgroundthread.Start();
			
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

				// Stop the thread and wait for it to end
				backgroundthread.Interrupt();
				backgroundthread.Join();
				backgroundthread = null;
				
				// Clean up
				ClearUndos();
				ClearRedos();
				General.WriteLogLine("All undo and redo levels cleared.");
				
				// Done
				isdisposed = true;
			}
		}

		#endregion

		#region ================== Private Methods

		// This clears the redos
		private void ClearRedos()
		{
			lock(redos)
			{
				// Dispose all redos
				foreach(UndoSnapshot u in redos) u.Dispose();
				redos.Clear();
			}
		}

		// This clears the undos
		private void ClearUndos()
		{
			lock(undos)
			{
				// Dispose all undos
				foreach(UndoSnapshot u in undos) u.Dispose();
				undos.Clear();
			}
		}

		// This checks and removes a level when the limit is reached
		private void LimitUndoRedoLevel(List<UndoSnapshot> list)
		{
			UndoSnapshot u;
			
			// Too many?
			if(list.Count > MAX_UNDO_LEVELS)
			{
				// Remove one and dispose map
				u = list[list.Count - 1];
				u.Dispose();
				list.RemoveAt(list.Count - 1);
			}
		}

		// Background thread
		private void BackgroundThread()
		{
			while(true)
			{
				if(dobackgroundwork)
				{
					// First set dobackgroundwork to false before performing the work so
					// that it can be set to true again when another pass is needed
					dobackgroundwork = false;

					int undolevel = 0;
					UndoSnapshot us;
					while(true)
					{
						// Get the next snapshot or leave
						lock(undos)
						{
							if(undolevel < undos.Count)
								us = undos[undolevel];
							else
								break;
						}

						// Write to file or load from file, if needed
						if(us.StoreOnDisk && !us.IsOnDisk)
							us.WriteToFile();
						else if(!us.StoreOnDisk && us.IsOnDisk)
							us.RestoreFromFile();

						// Next
						undolevel++;
					}

					int redolevel = 0;
					while(true)
					{
						// Get the next snapshot or leave
						lock(redos)
						{
							if(redolevel < redos.Count)
								us = redos[redolevel];
							else
								break;
						}

						// Write to file or load from file, if needed
						if(us.StoreOnDisk && !us.IsOnDisk)
							us.WriteToFile();
						else if(!us.StoreOnDisk && us.IsOnDisk)
							us.RestoreFromFile();

						// Next
						redolevel++;
					}
				}

				try { Thread.Sleep(30); }
				catch(ThreadInterruptedException) { break; }
			}
		}
		
		// This starts a new recording
		private void StartRecording(string description)
		{
			stream = new MemoryStream(STREAM_CAPACITY);
			ss = new SerializerStream(stream);
			ss.Begin();
			commandswritten = 0;
			propsrecorded = null;
			snapshot = new UndoSnapshot(description, stream, ticketid);
		}

		// This finishes recording
		private void FinishRecording()
		{
			// End current recording
			if(stream != null)
			{
				propsrecorded = null;
				ss.wInt(commandswritten);
				ss.End();
			}
		}
		
		// This begins writing to the record stream
		private bool BeginRecordData(StreamCodes code)
		{
			if(ss == null) return false;
			isrecordingcommand = true;
			prevstreamlength = stream.Length;
			ss.wByte((byte)code);
			return true;
		}

		// This ends writing to the record stream
		private void EndRecordData()
		{
			// We write the difference in bytes to the stream so that
			// the stream can be read from the end backwards
			int delta = (int)(stream.Length - prevstreamlength);
			ss.wInt(delta);
			commandswritten++;
			isrecordingcommand = false;
		}

		// This outputs record info, if desired
		private void LogRecordInfo(string info)
		{
			#if DEBUG
				//General.WriteLogLine(info);
			#endif
		}

		// This plays back a stream in reverse
		private void PlaybackStream(MemoryStream pstream)
		{
			General.Map.Map.AutoRemove = false;
			
			General.Map.Map.ClearAllMarks(false);
			
			pstream.Seek(0, SeekOrigin.Begin);
			DeserializerStream ds = new DeserializerStream(pstream);
			ds.Begin();
			
			if(pstream.Length > 4)
			{
				// Start at the end
				pstream.Seek(ds.EndPosition - 4, SeekOrigin.Begin);
				int numcmds; ds.rInt(out numcmds);
				pstream.Seek(-8, SeekOrigin.Current);
				while(numcmds > 0)
				{
					// Go back up the stream to the beginning of the prev command
					int len; ds.rInt(out len);
					pstream.Seek(-(len + 4), SeekOrigin.Current);

					// Play back the command
					long beginpos = pstream.Position;
					byte cmd; ds.rByte(out cmd);
					switch((StreamCodes)cmd)
					{
						case StreamCodes.AddVertex: PlayAddVertex(ds); break;
						case StreamCodes.RemVertex: PlayRemVertex(ds); break;
						case StreamCodes.PrpVertex: PlayPrpVertex(ds); break;
						case StreamCodes.AddLinedef: PlayAddLinedef(ds); break;
						case StreamCodes.RemLinedef: PlayRemLinedef(ds); break;
						case StreamCodes.PrpLinedef: PlayPrpLinedef(ds); break;
						case StreamCodes.RefLinedefStart: PlayRefLinedefStart(ds); break;
						case StreamCodes.RefLinedefEnd: PlayRefLinedefEnd(ds); break;
						case StreamCodes.RefLinedefFront: PlayRefLinedefFront(ds); break;
						case StreamCodes.RefLinedefBack: PlayRefLinedefBack(ds); break;
						case StreamCodes.AddSidedef: PlayAddSidedef(ds); break;
						case StreamCodes.RemSidedef: PlayRemSidedef(ds); break;
						case StreamCodes.PrpSidedef: PlayPrpSidedef(ds); break;
						case StreamCodes.RefSidedefSector: PlayRefSidedefSector(ds); break;
						case StreamCodes.AddSector: PlayAddSector(ds); break;
						case StreamCodes.RemSector: PlayRemSector(ds); break;
						case StreamCodes.PrpSector: PlayPrpSector(ds); break;
						case StreamCodes.AddThing: PlayAddThing(ds); break;
						case StreamCodes.RemThing: PlayRemThing(ds); break;
						case StreamCodes.PrpThing: PlayPrpThing(ds); break;
					}
					
					// Sanity check
					if((beginpos + len) != pstream.Position)
						throw new Exception("The last command did not read the same amount of data that was written for this command!");
					
					// Go back for next command
					pstream.Seek(-(len + 4), SeekOrigin.Current);

					numcmds--;
				}
			}
			
			General.Map.Map.AutoRemove = true;
		}
		
		#endregion
		
		#region ================== Public Methods

		// This clears all redos
		public void ClearAllRedos()
		{
			ClearRedos();
			General.MainWindow.UpdateInterface();
		}

		/// <summary>
		/// This makes an undo and returns the unique ticket id. Also automatically indicates that the map is changed.
		/// </summary>
		/// <param name="description">Any description you want the undo to be named. Should be something related to the changes you are about to make.</param>
		/// <returns>Ticket ID that identifies the created undo level.</returns>
		public int CreateUndo(string description)
		{
			return CreateUndo(description, null, 0, 0);
		}
		
		/// <summary>
		/// This makes an undo and returns the unique ticket id. Also automatically indicates that the map is changed.
		/// </summary>
		/// <param name="description">Any description you want the undo to be named. Should be something related to the changes you are about to make.</param>
		/// <param name="groupsource">The object creating the undo. All objects from within the same plugin are equal, so it is safe to just use 'this' everywhere. This is only used for undo grouping and you can use 'null' if you don't want undo grouping.</param>
		/// <param name="groupid">The undo group id you want this undo level to group with (undos only group together if the previous undo has the same source, id and tag). Group 0 indicates no grouping.</param>
		/// <param name="grouptag">The undo group tag you want this undo level to group with (undos only group together if the previous undo has the same source, id and tag). Use at your own discretion.</param>
		/// <returns>Ticket ID that identifies the created undo level. Returns -1 when no undo level was created.</returns>
		public int CreateUndo(string description, object groupsource, int groupid, int grouptag)
		{
			UndoSnapshot u;
			Plugin p = null;
			string groupsourcename = "Null";
			
			// Figure out the source plugin
			if(groupsource != null)
			{
				p = General.Plugins.FindPluginByAssembly(groupsource.GetType().Assembly);
				if(p != null) groupsourcename = p.Name;
			}

			// Not the same as previous group, or no grouping desired...
			if((p == null) || (lastgroupplugin == null) || (p != lastgroupplugin) ||
			   (groupid == 0) || (lastgroupid == 0) || (groupid != lastgroupid) ||
			   (grouptag != lastgrouptag))
			{
				FinishRecording();
				
				// Next ticket id
				if(++ticketid == int.MaxValue) ticketid = 1;
				
				General.WriteLogLine("Creating undo snapshot \"" + description + "\", Source " + groupsourcename + ", Group " + groupid + ", Tag " + grouptag + ", Ticket ID " + ticketid + "...");
				
				if((snapshot != null) && !isundosnapshot)
				{
					lock(undos)
					{
						// The current top of the stack can now be written to disk
						// because it is no longer the next immediate undo level
						if(undos.Count > 0) undos[0].StoreOnDisk = true;
						
						// Put it on the stack
						undos.Insert(0, snapshot);
						LimitUndoRedoLevel(undos);
					}
				}

				StartRecording(description);
				isundosnapshot = false;

				// Clear all redos
				ClearRedos();

				// Keep grouping info
				lastgroupplugin = p;
				lastgroupid = groupid;
				lastgrouptag = grouptag;
				
				// Map changes!
				General.Map.IsChanged = true;

				// Update
				dobackgroundwork = true;
				General.MainWindow.UpdateInterface();

				// Done
				return ticketid;
			}
			else
			{
				return -1;
			}
		}

		// This removes a previously made undo
		public void WithdrawUndo(int ticket)
		{
			// Anything to undo?
			if(undos.Count > 0)
			{
				// Check if the ticket id matches
				if(ticket == undos[0].TicketID)
				{
					General.WriteLogLine("Withdrawing undo snapshot \"" + undos[0].Description + "\", Ticket ID " + ticket + "...");
					
					if(snapshot != null)
					{
						// Just trash this recording
						// You must call CreateUndo first before making any more changes
						FinishRecording();
						isundosnapshot = false;
						snapshot = null;
					}
					else
					{
						throw new Exception("No undo is recording that can be withdrawn");
					}
					
					// Update
					dobackgroundwork = true;
					General.MainWindow.UpdateInterface();
				}
			}
		}

		// This performs an undo
		[BeginAction("undo")]
		public void PerformUndo()
		{
			UndoSnapshot u = null;
			Cursor oldcursor = Cursor.Current;
			Cursor.Current = Cursors.WaitCursor;
			
			// Anything to undo?
			if((undos.Count > 0) || ((snapshot != null) && !isundosnapshot))
			{
				// Let the plugins know
				if(General.Plugins.OnUndoBegin())
				{
					// Call UndoBegin event
					if(General.Editing.Mode.OnUndoBegin())
					{
						// Cancel volatile mode, if any
						// This returns false when mode was not volatile
						if(!General.CancelVolatileMode())
						{
							FinishRecording();
							
							if(isundosnapshot)
							{
								if(snapshot != null)
								{
									// This snapshot was made by a previous call to this
									// function and should go on the redo list
									lock(redos)
									{
										// The current top of the stack can now be written to disk
										// because it is no longer the next immediate redo level
										if(redos.Count > 0) redos[0].StoreOnDisk = true;
										
										// Put it on the stack
										redos.Insert(0, snapshot);
										LimitUndoRedoLevel(redos);
									}
								}
							}
							else
							{
								// The snapshot can be undone immediately and it will
								// be recorded for the redo list
								if(snapshot != null)
									u = snapshot;
							}
							
							// No immediate snapshot to undo? Then get the next one from the stack
							if(u == null)
							{
								lock(undos)
								{
									// Get undo snapshot
									u = undos[0];
									undos.RemoveAt(0);
									
									// Make the current top of the stack load into memory
									// because it just became the next immediate undo level
									if(undos.Count > 0) undos[0].StoreOnDisk = false;
								}
							}
							
							General.WriteLogLine("Performing undo \"" + u.Description + "\", Ticket ID " + u.TicketID + "...");
							General.Interface.DisplayStatus(StatusType.Action, u.Description + " undone.");
							
							// Make a snapshot for redo
							StartRecording(u.Description);
							isundosnapshot = true;
							
							// Reset grouping
							lastgroupplugin = null;

							// Play back the stream in reverse
							MemoryStream data = u.GetStream();
							PlaybackStream(data);
							data.Dispose();
							
							// Remove selection
							General.Map.Map.ClearAllSelected();
							
							// Update map
							General.Map.Map.Update();
							foreach(Thing t in General.Map.Map.Things) if(t.Marked) t.UpdateConfiguration();
							General.Map.ThingsFilter.Update();
							General.Map.Data.UpdateUsedTextures();
							General.MainWindow.RedrawDisplay();
							
							// Done
							General.Editing.Mode.OnUndoEnd();
							General.Plugins.OnUndoEnd();

							// Update interface
							dobackgroundwork = true;
							General.MainWindow.UpdateInterface();
						}
					}
				}
			}
			
			Cursor.Current = oldcursor;
		}
		
		// This performs a redo
		[BeginAction("redo")]
		public void PerformRedo()
		{
			UndoSnapshot r = null;
			Cursor oldcursor = Cursor.Current;
			Cursor.Current = Cursors.WaitCursor;
			
			// Anything to redo?
			if((redos.Count > 0) || ((snapshot != null) && isundosnapshot))
			{
				// Let the plugins know
				if(General.Plugins.OnRedoBegin())
				{
					// Call RedoBegin event
					if(General.Editing.Mode.OnRedoBegin())
					{
						// Cancel volatile mode, if any
						General.CancelVolatileMode();

						FinishRecording();
						
						if(isundosnapshot)
						{
							// This snapshot was started by PerformUndo, which means
							// it can directly be used to redo to previous undo
							if(snapshot != null)
								r = snapshot;
						}
						else
						{
							if(snapshot != null)
							{
								// This snapshot was made by a previous call to this
								// function and should go on the undo list
								lock(undos)
								{
									// The current top of the stack can now be written to disk
									// because it is no longer the next immediate undo level
									if(undos.Count > 0) undos[0].StoreOnDisk = true;

									// Put it on the stack
									undos.Insert(0, snapshot);
									LimitUndoRedoLevel(undos);
								}
							}
						}
						
						// No immediate snapshot to redo? Then get the next one from the stack
						if(r == null)
						{
							lock(redos)
							{
								// Get redo snapshot
								r = redos[0];
								redos.RemoveAt(0);
								
								// Make the current top of the stack load into memory
								// because it just became the next immediate undo level
								if(redos.Count > 0) redos[0].StoreOnDisk = false;
							}
						}

						General.WriteLogLine("Performing redo \"" + r.Description + "\", Ticket ID " + r.TicketID + "...");
						General.Interface.DisplayStatus(StatusType.Action, r.Description + " redone.");

						StartRecording(r.Description);
						isundosnapshot = false;
						
						// Reset grouping
						lastgroupplugin = null;

						// Play back the stream in reverse
						MemoryStream data = r.GetStream();
						PlaybackStream(data);
						data.Dispose();
						
						// Remove selection
						General.Map.Map.ClearAllSelected();

						// Update map
						General.Map.Map.Update();
						foreach(Thing t in General.Map.Map.Things) if(t.Marked) t.UpdateConfiguration();
						General.Map.ThingsFilter.Update();
						General.Map.Data.UpdateUsedTextures();
						General.MainWindow.RedrawDisplay();
						
						// Done
						General.Editing.Mode.OnRedoEnd();
						General.Plugins.OnRedoEnd();

						// Update interface
						dobackgroundwork = true;
						General.MainWindow.UpdateInterface();
					}
				}
			}
			
			Cursor.Current = oldcursor;
		}

		#endregion
		
		#region ================== Record and Playback

		internal void RecAddVertex(Vertex v)
		{
			if(!BeginRecordData(StreamCodes.AddVertex)) return;
			ss.wInt(v.Index);
			EndRecordData();

			LogRecordInfo("REC: Adding vertex " + v.Index + " at " + v.Position);
			propsrecorded = null;
		}

		internal void PlayAddVertex(DeserializerStream ds)
		{
			int index; ds.rInt(out index);
			LogRecordInfo("PLY: Removing vertex " + index);
			Vertex v = General.Map.Map.GetVertexByIndex(index);
			v.Dispose();
		}

		internal void RecRemVertex(Vertex v)
		{
			if(!BeginRecordData(StreamCodes.RemVertex)) return;
			ss.wInt(v.Index);
			ss.wVector2D(v.Position);
			v.ReadWrite(ss);
			EndRecordData();

			LogRecordInfo("REC: Removing vertex " + v.Index + " (at " + v.Position + ")");
			propsrecorded = null;
		}

		internal void PlayRemVertex(DeserializerStream ds)
		{
			int index; ds.rInt(out index);
			Vector2D pos; ds.rVector2D(out pos);
			LogRecordInfo("PLY: Adding vertex " + index + " at " + pos);
			Vertex v = General.Map.Map.CreateVertex(index, pos);
			v.ReadWrite(ds);
			v.Marked = true;
		}
		
		internal void RecPrpVertex(Vertex v)
		{
			if(!ignorepropchanges && !isrecordingcommand && !object.ReferenceEquals(v, propsrecorded))
			{
				if(!BeginRecordData(StreamCodes.PrpVertex)) return;
				ss.wInt(v.Index);
				v.ReadWrite(ss);
				EndRecordData();
				propsrecorded = v;
			}
		}

		internal void PlayPrpVertex(DeserializerStream ds)
		{
			int index; ds.rInt(out index);
			Vertex v = General.Map.Map.GetVertexByIndex(index);
			v.ReadWrite(ds);
			v.Marked = true;
		}

		internal void RecAddLinedef(Linedef l)
		{
			if(!BeginRecordData(StreamCodes.AddLinedef)) return;
			ss.wInt(l.Index);
			EndRecordData();

			LogRecordInfo("REC: Adding linedef " + l.Index + " from " + ((l.Start != null) ? l.Start.Index : -1) + " to " + ((l.End != null) ? l.End.Index : -1));
			propsrecorded = null;
		}

		internal void PlayAddLinedef(DeserializerStream ds)
		{
			int index; ds.rInt(out index);
			LogRecordInfo("PLY: Removing linedef " + index);
			Linedef l = General.Map.Map.GetLinedefByIndex(index);
			l.Dispose();
		}

		internal void RecRemLinedef(Linedef l)
		{
			if(!BeginRecordData(StreamCodes.RemLinedef)) return;
			ss.wInt(l.Index);
			ss.wInt(l.Start.Index);
			ss.wInt(l.End.Index);
			l.ReadWrite(ss);
			EndRecordData();

			LogRecordInfo("REC: Removing linedef " + l.Index + " (from " + ((l.Start != null) ? l.Start.Index : -1) + " to " + ((l.End != null) ? l.End.Index : -1) + ")");
			propsrecorded = null;
		}

		internal void PlayRemLinedef(DeserializerStream ds)
		{
			int index; ds.rInt(out index);
			int sindex; ds.rInt(out sindex);
			int eindex; ds.rInt(out eindex);
			LogRecordInfo("PLY: Adding linedef " + index + " from " + sindex + " to " + eindex);
			Vertex vs = General.Map.Map.GetVertexByIndex(sindex);
			Vertex ve = General.Map.Map.GetVertexByIndex(eindex);
			Linedef l = General.Map.Map.CreateLinedef(index, vs, ve);
			l.ReadWrite(ds);
			l.Marked = true;
		}

		internal void RecPrpLinedef(Linedef l)
		{
			if(!ignorepropchanges && !isrecordingcommand && !object.ReferenceEquals(l, propsrecorded))
			{
				if(!BeginRecordData(StreamCodes.PrpLinedef)) return;
				ss.wInt(l.Index);
				l.ReadWrite(ss);
				EndRecordData();
				propsrecorded = l;
			}
		}

		internal void PlayPrpLinedef(DeserializerStream ds)
		{
			int index; ds.rInt(out index);
			Linedef l = General.Map.Map.GetLinedefByIndex(index);
			l.ReadWrite(ds);
			l.Marked = true;
		}

		internal void RecRefLinedefStart(Linedef l)
		{
			if(!BeginRecordData(StreamCodes.RefLinedefStart)) return;
			ss.wInt(l.Index);
			if(l.Start != null) ss.wInt(l.Start.Index); else ss.wInt(-1);
			EndRecordData();

			LogRecordInfo("REC: Setting linedef " + l.Index + " start vertex " + ((l.Start != null) ? l.Start.Index : -1));
			propsrecorded = null;
		}

		internal void PlayRefLinedefStart(DeserializerStream ds)
		{
			int index; ds.rInt(out index);
			Linedef l = General.Map.Map.GetLinedefByIndex(index);
			int vindex; ds.rInt(out vindex);
			LogRecordInfo("PLY: Setting linedef " + index + " start vertex " + vindex);
			Vertex v = (vindex >= 0) ? General.Map.Map.GetVertexByIndex(vindex) : null;
			l.SetStartVertex(v);
			l.Marked = true;
			if(v != null) v.Marked = true;
		}

		internal void RecRefLinedefEnd(Linedef l)
		{
			if(!BeginRecordData(StreamCodes.RefLinedefEnd)) return;
			ss.wInt(l.Index);
			if(l.End != null) ss.wInt(l.End.Index); else ss.wInt(-1);
			EndRecordData();

			LogRecordInfo("REC: Setting linedef " + l.Index + " end vertex " + ((l.End != null) ? l.End.Index : -1));
			propsrecorded = null;
		}

		internal void PlayRefLinedefEnd(DeserializerStream ds)
		{
			int index; ds.rInt(out index);
			Linedef l = General.Map.Map.GetLinedefByIndex(index);
			int vindex; ds.rInt(out vindex);
			LogRecordInfo("PLY: Setting linedef " + index + " end vertex " + vindex);
			Vertex v = (vindex >= 0) ? General.Map.Map.GetVertexByIndex(vindex) : null;
			l.SetEndVertex(v);
			l.Marked = true;
			if(v != null) v.Marked = true;
		}

		internal void RecRefLinedefFront(Linedef l)
		{
			if(!BeginRecordData(StreamCodes.RefLinedefFront)) return;
			ss.wInt(l.Index);
			if(l.Front != null) ss.wInt(l.Front.Index); else ss.wInt(-1);
			EndRecordData();

			LogRecordInfo("REC: Setting linedef " + l.Index + " front sidedef " + ((l.Front != null) ? l.Front.Index : -1));
			propsrecorded = null;
		}

		internal void PlayRefLinedefFront(DeserializerStream ds)
		{
			int index; ds.rInt(out index);
			Linedef l = General.Map.Map.GetLinedefByIndex(index);
			int sindex; ds.rInt(out sindex);
			LogRecordInfo("PLY: Setting linedef " + index + " front sidedef " + sindex);
			Sidedef sd = (sindex >= 0) ? General.Map.Map.GetSidedefByIndex(sindex) : null;
			l.AttachFront(sd);
			l.Marked = true;
			if(sd != null) sd.Marked = true;
		}

		internal void RecRefLinedefBack(Linedef l)
		{
			if(!BeginRecordData(StreamCodes.RefLinedefBack)) return;
			ss.wInt(l.Index);
			if(l.Back != null) ss.wInt(l.Back.Index); else ss.wInt(-1);
			EndRecordData();

			LogRecordInfo("REC: Setting linedef " + l.Index + " back sidedef " + ((l.Back != null) ? l.Back.Index : -1));
			propsrecorded = null;
		}

		internal void PlayRefLinedefBack(DeserializerStream ds)
		{
			int index; ds.rInt(out index);
			Linedef l = General.Map.Map.GetLinedefByIndex(index);
			int sindex; ds.rInt(out sindex);
			LogRecordInfo("PLY: Setting linedef " + index + " back sidedef " + sindex);
			Sidedef sd = (sindex >= 0) ? General.Map.Map.GetSidedefByIndex(sindex) : null;
			l.AttachBack(sd);
			l.Marked = true;
			if(sd != null) sd.Marked = true;
		}

		internal void RecAddSidedef(Sidedef s)
		{
			if(!BeginRecordData(StreamCodes.AddSidedef)) return;
			ss.wInt(s.Index);
			EndRecordData();

			LogRecordInfo("REC: Adding sidedef " + s.Index + " to linedef " + s.Line.Index + (s.IsFront ? " front" : " back") + " and sector " + s.Sector.Index);
			propsrecorded = null;
		}

		internal void PlayAddSidedef(DeserializerStream ds)
		{
			int index; ds.rInt(out index);
			LogRecordInfo("PLY: Removing sidedef " + index);
			Sidedef s = General.Map.Map.GetSidedefByIndex(index);
			s.Dispose();
		}

		internal void RecRemSidedef(Sidedef s)
		{
			if(!BeginRecordData(StreamCodes.RemSidedef)) return;
			ss.wInt(s.Index);
			ss.wInt(s.Line.Index);
			ss.wBool(s.IsFront);
			ss.wInt(s.Sector.Index);
			s.ReadWrite(ss);
			EndRecordData();

			LogRecordInfo("REC: Removing sidedef " + s.Index + " (from linedef " + s.Line.Index + (s.IsFront ? " front" : " back") + " and sector " + s.Sector.Index + ")");
			propsrecorded = null;
		}

		internal void PlayRemSidedef(DeserializerStream ds)
		{
			int index; ds.rInt(out index);
			int dindex; ds.rInt(out dindex);
			bool front; ds.rBool(out front);
			int sindex; ds.rInt(out sindex);
			LogRecordInfo("PLY: Adding sidedef " + index + " to linedef " + dindex + (front ? " front" : " back") + " and sector " + sindex);
			Linedef l = General.Map.Map.GetLinedefByIndex(dindex);
			Sector s = General.Map.Map.GetSectorByIndex(sindex);
			Sidedef sd = General.Map.Map.CreateSidedef(index, l, front, s);
			sd.ReadWrite(ds);
			sd.Marked = true;
		}

		internal void RecPrpSidedef(Sidedef s)
		{
			if(!ignorepropchanges && !isrecordingcommand && !object.ReferenceEquals(s, propsrecorded))
			{
				if(!BeginRecordData(StreamCodes.PrpSidedef)) return;
				ss.wInt(s.Index);
				s.ReadWrite(ss);
				EndRecordData();
				propsrecorded = s;
			}
		}

		internal void PlayPrpSidedef(DeserializerStream ds)
		{
			int index; ds.rInt(out index);
			Sidedef s = General.Map.Map.GetSidedefByIndex(index);
			s.ReadWrite(ds);
			s.Marked = true;
		}

		internal void RecRefSidedefSector(Sidedef s)
		{
			if(!BeginRecordData(StreamCodes.RefSidedefSector)) return;
			ss.wInt(s.Index);
			if(s.Sector != null) ss.wInt(s.Sector.Index); else ss.wInt(-1);
			EndRecordData();

			LogRecordInfo("REC: Setting sidedef " + s.Index + " sector " + ((s.Sector != null) ? s.Sector.Index : -1));
			propsrecorded = null;
		}

		internal void PlayRefSidedefSector(DeserializerStream ds)
		{
			int index; ds.rInt(out index);
			Sidedef sd = General.Map.Map.GetSidedefByIndex(index);
			int sindex; ds.rInt(out sindex);
			LogRecordInfo("PLY: Setting sidedef " + index + " sector " + sindex);
			Sector sc = (sindex >= 0) ? General.Map.Map.GetSectorByIndex(sindex) : null;
			sd.SetSector(sc);
			sd.Marked = true;
			if(sc != null) sc.Marked = true;
		}

		internal void RecAddSector(Sector s)
		{
			if(!BeginRecordData(StreamCodes.AddSector)) return;
			ss.wInt(s.Index);
			EndRecordData();

			LogRecordInfo("REC: Adding sector " + s.Index);
			propsrecorded = null;
		}

		internal void PlayAddSector(DeserializerStream ds)
		{
			int index; ds.rInt(out index);
			LogRecordInfo("PLY: Removing sector " + index);
			Sector s = General.Map.Map.GetSectorByIndex(index);
			s.Dispose();
		}

		internal void RecRemSector(Sector s)
		{
			if(!BeginRecordData(StreamCodes.RemSector)) return;
			ss.wInt(s.Index);
			s.ReadWrite(ss);
			EndRecordData();

			LogRecordInfo("REC: Removing sector " + s.Index);
			propsrecorded = null;
		}

		internal void PlayRemSector(DeserializerStream ds)
		{
			int index; ds.rInt(out index);
			LogRecordInfo("PLY: Adding sector " + index);
			Sector s = General.Map.Map.CreateSector(index);
			s.ReadWrite(ds);
			s.Marked = true;
		}

		internal void RecPrpSector(Sector s)
		{
			if(!ignorepropchanges && !isrecordingcommand && !object.ReferenceEquals(s, propsrecorded))
			{
				if(!BeginRecordData(StreamCodes.PrpSector)) return;
				ss.wInt(s.Index);
				s.ReadWrite(ss);
				EndRecordData();
				propsrecorded = s;
			}
		}

		internal void PlayPrpSector(DeserializerStream ds)
		{
			int index; ds.rInt(out index);
			Sector s = General.Map.Map.GetSectorByIndex(index);
			s.ReadWrite(ds);
			s.Marked = true;
		}

		internal void RecAddThing(Thing t)
		{
			if(!BeginRecordData(StreamCodes.AddThing)) return;
			ss.wInt(t.Index);
			EndRecordData();

			LogRecordInfo("REC: Adding thing " + t.Index);
			propsrecorded = null;
		}

		internal void PlayAddThing(DeserializerStream ds)
		{
			int index; ds.rInt(out index);
			LogRecordInfo("PLY: Removing thing " + index);
			Thing t = General.Map.Map.GetThingByIndex(index);
			t.Dispose();
		}

		internal void RecRemThing(Thing t)
		{
			if(!BeginRecordData(StreamCodes.RemThing)) return;
			ss.wInt(t.Index);
			t.ReadWrite(ss);
			EndRecordData();

			LogRecordInfo("REC: Removing thing " + t.Index);
			propsrecorded = null;
		}

		internal void PlayRemThing(DeserializerStream ds)
		{
			int index; ds.rInt(out index);
			LogRecordInfo("PLY: Adding thing " + index);
			Thing t = General.Map.Map.CreateThing(index);
			t.ReadWrite(ds);
			t.Marked = true;
		}

		internal void RecPrpThing(Thing t)
		{
			if(!ignorepropchanges && !isrecordingcommand && !object.ReferenceEquals(t, propsrecorded))
			{
				if(!BeginRecordData(StreamCodes.PrpThing)) return;
				ss.wInt(t.Index);
				t.ReadWrite(ss);
				EndRecordData();
				propsrecorded = t;
			}
		}

		internal void PlayPrpThing(DeserializerStream ds)
		{
			int index; ds.rInt(out index);
			Thing t = General.Map.Map.GetThingByIndex(index);
			t.ReadWrite(ds);
			t.Marked = true;
		}

		#endregion
	}
}
