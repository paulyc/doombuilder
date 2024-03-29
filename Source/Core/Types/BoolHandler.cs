
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
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Data;
using System.IO;
using System.Diagnostics;
using CodeImp.DoomBuilder.Config;

#endregion

namespace CodeImp.DoomBuilder.Types
{
	[TypeHandler(UniversalType.Boolean, "Boolean", true)]
	internal class BoolHandler : TypeHandler
	{
		#region ================== Constants

		#endregion

		#region ================== Variables

		private EnumList list;
		private bool value;

		#endregion

		#region ================== Properties

		public override bool IsEnumerable { get { return true; } }
		public override bool IsLimitedToEnums { get { return true; } }
		
		#endregion

		#region ================== Constructor

		// When set up for an argument
		public BoolHandler() : base()
		{
			// Make enums
			list = new EnumList();
			list.Add(new EnumItem("true", "True"));
			list.Add(new EnumItem("false", "False"));
		}

		#endregion

		#region ================== Methods

		public override void SetValue(object value)
		{
			bool result;

			// null?
			if(value == null)
			{
				this.value = false;
			}
			// Compatible type?
			else if((value is int) || (value is float) || (value is bool))
			{
				this.value = Convert.ToBoolean(value);
			}
			// string?
			else if(value is string)
			{
				// Try parsing as string
				if(value.ToString().ToLowerInvariant().StartsWith("t"))
					this.value = true;
				else
					this.value = false;
			}
			else
			{
				this.value = false;
			}
		}

		public override object GetValue()
		{
			return this.value;
		}

		public override int GetIntValue()
		{
			if(this.value) return 1; else return 0;
		}

		public override string GetStringValue()
		{
			return this.value.ToString();
		}

		// This returns an enum list
		public override EnumList GetEnumList()
		{
			return list;
		}
		
		#endregion
	}
}
