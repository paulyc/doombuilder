﻿using System.IO;
using System.Collections.Generic;
using System.Globalization;
using CodeImp.DoomBuilder.ZDoom;
using CodeImp.DoomBuilder.GZBuilder.Data;

//mxd. ACS parser used to create ScriptItems for use in script editor's navigator
namespace CodeImp.DoomBuilder.GZBuilder.GZDoom
{
	internal sealed class AcsParserSE : ZDTextParser
	{
		internal delegate void IncludeDelegate(AcsParserSE parser, string includefile);
		internal IncludeDelegate OnInclude;

		private readonly HashSet<string> parsedlumps;
		private readonly HashSet<string> includes;
		private List<string> includestoskip;
		private string libraryname;

		private readonly List<ScriptItem> namedscripts;
		private readonly List<ScriptItem> numberedscripts;
		private readonly List<ScriptItem> functions;

		internal List<ScriptItem> NamedScripts { get { return namedscripts; } }
		internal List<ScriptItem> NumberedScripts { get { return numberedscripts; } }
		internal List<ScriptItem> Functions { get { return functions; } }
		internal HashSet<string> Includes { get { return includes; } }
		internal bool IsLibrary { get { return !string.IsNullOrEmpty(libraryname); } }
		internal string LibraryName { get { return libraryname; } }

		internal bool AddArgumentsToScriptNames;
		internal bool IsMapScriptsLump;

		internal AcsParserSE() 
		{
			namedscripts = new List<ScriptItem>();
			numberedscripts = new List<ScriptItem>();
			functions = new List<ScriptItem>();
			parsedlumps = new HashSet<string>();
			includes = new HashSet<string>();
			includestoskip = new List<string>();
			specialtokens += "(,)";
		}

		public override bool Parse(Stream stream, string sourcefilename) 
		{
			return Parse(stream, sourcefilename, new List<string>(), false, false);
		}

		public bool Parse(Stream stream, string sourcefilename, bool processincludes, bool isinclude)
		{
			return Parse(stream, sourcefilename, includestoskip, processincludes, isinclude);
		}

		public bool Parse(Stream stream, string sourcefilename, List<string> configincludes, bool processincludes, bool isinclude) 
		{
			// Integrity check
			if(stream == null || stream.Length == 0)
			{
				ReportError("Unable to load " + (isinclude ? "include" : "") + " file '" + sourcefilename + "'!");
				return false;
			}

			base.Parse(stream, sourcefilename);

			// Already parsed this?
			if(parsedlumps.Contains(sourcefilename)) return false;
			parsedlumps.Add(sourcefilename);
			if(isinclude && !includes.Contains(sourcefilename)) includes.Add(sourcefilename);
			includestoskip = configincludes;
			int bracelevel = 0;

			// Keep local data
			Stream localstream = datastream;
			string localsourcename = sourcename;
			BinaryReader localreader = datareader;

			// Continue until at the end of the stream
			while(SkipWhitespace(true)) 
			{
				string token = ReadToken();
				if(string.IsNullOrEmpty(token)) continue;

				// Ignore inner scope stuff
				if(token == "{") { bracelevel++; continue; }
				if(token == "}") { bracelevel--; continue; }
				if(bracelevel > 0) continue;

				switch(token.ToLowerInvariant())
				{
					case "script":
					{
						SkipWhitespace(true);
						int startpos = (int)stream.Position;
						token = ReadToken();

						//is it named script?
						if(token.IndexOf('"') != -1) 
						{
							startpos += 1;
							string scriptname = StripTokenQuotes(token);

							// Try to parse argument names
							List<KeyValuePair<string, string>> args = ParseArgs();
							List<string> argnames = new List<string>();
							foreach(KeyValuePair<string, string> group in args) argnames.Add(group.Value);

							// Make full name
							if(AddArgumentsToScriptNames) scriptname += " " + GetArgumentNames(args);

							// Add to collection
							namedscripts.Add(new ScriptItem(scriptname, argnames, startpos, isinclude));
						} 
						else //should be numbered script
						{ 
							int n;
							if(int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out n)) 
							{
								// Try to parse argument names
								List<KeyValuePair<string, string>> args = ParseArgs();
								
								// Now find the opening brace
								do 
								{
									if(!SkipWhitespace(true)) break;
									token = ReadToken();
								} while (!string.IsNullOrEmpty(token) && token != "{");

								token = ReadLine();
								string name = "";
								bracelevel = 1;

								if(!string.IsNullOrEmpty(token))
								{
									int commentstart = token.IndexOf("//", System.StringComparison.Ordinal);
									if(commentstart != -1) //found comment
									{ 
										commentstart += 2;
										name = token.Substring(commentstart, token.Length - commentstart).Trim();
									}
								}

								bool customname = (name.Length > 0);
								name = (customname ? name + " [Script " + n + "]" : "Script " + n);
								
								List<string> argnames = new List<string>();
								foreach(KeyValuePair<string, string> group in args) argnames.Add(group.Value);

								// Make full name
								if(AddArgumentsToScriptNames) name += " " + GetArgumentNames(args);

								// Add to collection
								numberedscripts.Add(new ScriptItem(n, name, argnames, startpos, isinclude, customname));
							}
						}
					}
					break;

					case "function":
					{
						SkipWhitespace(true);
						int startpos = (int)stream.Position;
						string funcname = ReadToken(); //read return type
						SkipWhitespace(true);
						funcname += " " + ReadToken(); //read function name

						// Try to parse argument names
						List<KeyValuePair<string, string>> args = ParseArgs();
						List<string> argnames = new List<string>();
						foreach(KeyValuePair<string, string> group in args) argnames.Add(group.Value);

						// Make full name
						if(AddArgumentsToScriptNames) funcname += GetArgumentNames(args);

						// Add to collection
						functions.Add(new ScriptItem(funcname, argnames, startpos, isinclude));
					}
					break;

					case "#library":
						if(IsMapScriptsLump)
						{
							ReportError("Error in '" + sourcefilename + "' at line " + GetCurrentLineNumber() + ": SCRIPTS lump can not be compiled as library!");
							return false;
						}
						
						SkipWhitespace(true);
						libraryname = ReadToken();

						if(string.IsNullOrEmpty(libraryname) || !libraryname.StartsWith("\"") || !libraryname.EndsWith("\""))
						{
							ReportError("Error in '" + sourcefilename + "' at line " + GetCurrentLineNumber() + ": invalid #library directive!");
							return false;
						}

						libraryname = StripTokenQuotes(libraryname);
						break;

					default:
						if(processincludes && (token == "#include" || token == "#import")) 
						{
							SkipWhitespace(true);
							string includelump = StripTokenQuotes(ReadToken()).ToLowerInvariant();

							if(!string.IsNullOrEmpty(includelump)) 
							{
								string includename = Path.GetFileName(includelump);
								if(includestoskip.Contains(includename) || includes.Contains(includename)) continue;
							
								// Callback to parse this file
								if(OnInclude != null) OnInclude(this, includelump.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));

								// Set our buffers back to continue parsing
								datastream = localstream;
								datareader = localreader;
								sourcename = localsourcename;
							} 
							else 
							{
								ReportError("Error in '" + sourcefilename + "' at line " + GetCurrentLineNumber() + ": got #include directive without include path!");
								return false;
							}
						}
						break;
				}
			}
			return true;
		}

		private List<KeyValuePair<string, string>> ParseArgs() //type, name
		{
			List<KeyValuePair<string, string>> argnames = new List<KeyValuePair<string, string>>();
			SkipWhitespace(true);
			string token = ReadToken();
			
			// Should be ENTER/OPEN etc. script type
			if(token != "(")
			{
				argnames.Add(new KeyValuePair<string, string>(token.ToUpperInvariant(), string.Empty));
				return argnames;
			}

			while(SkipWhitespace(true))
			{
				string argtype = ReadToken(); // should be type
				if(IsSpecialToken(argtype)) break;
				if(argtype.ToUpperInvariant() == "VOID")
				{
					argnames.Add(new KeyValuePair<string, string>("void", string.Empty));
					break;
				}

				SkipWhitespace(true);
				token = ReadToken(); // should be arg name
				argnames.Add(new KeyValuePair<string, string>(argtype, token));

				SkipWhitespace(true);
				token = ReadToken(); // should be comma or ")"
				if(token != ",") break;
			}

			return argnames;
		}

		private static string GetArgumentNames(List<KeyValuePair<string, string>> args)
		{
			// Make full name
			if(args.Count > 0)
			{
				List<string> argdescs = new List<string>(args.Count);
				foreach(KeyValuePair<string, string> group in args)
					argdescs.Add((group.Key + " " + group.Value).TrimEnd());

				return "(" + string.Join(", ", argdescs.ToArray()) + ")";
			}

			return "(void)";
		}
	}
}