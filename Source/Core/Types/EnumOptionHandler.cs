
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
	[TypeHandler(UniversalType.EnumOption, "Setting", false)]
	internal class EnumOptionHandler : TypeHandler
	{
		#region ================== Constants

		#endregion

		#region ================== Variables

		private EnumList list;
		private EnumItem value;
		
		#endregion

		#region ================== Properties

		public override bool IsBrowseable { get { return true; } }
		public override bool IsEnumerable { get { return true; } }
		
		#endregion

		#region ================== Constructor

		// When set up for an argument
		public override void SetupArgument(TypeHandlerAttribute attr, ArgumentInfo arginfo)
		{
			base.SetupArgument(attr, arginfo);

			// Keep enum list reference
			list = arginfo.Enum;
		}

		// When set up for a universal field
		public override void SetupField(TypeHandlerAttribute attr, UniversalFieldInfo fieldinfo)
		{
			base.SetupField(attr, fieldinfo);

			// Keep enum list reference
			if(fieldinfo != null) list = fieldinfo.Enum; else list = new EnumList();
		}

		#endregion
		
		#region ================== Methods
		
		public override void SetValue(object value)
		{
			this.value = null;

			// Input null?
			if(value == null)
			{
				this.value = new EnumItem("0", "NULL");
			}
			else
			{
				// Compatible type?
				if((value is int) || (value is float) || (value is bool))
				{
					int intvalue = Convert.ToInt32(value);

					// First try to match the value against the enum values
					foreach(EnumItem item in list)
					{
						// Matching value?
						if(item.GetIntValue() == intvalue)
						{
							// Set this value
							this.value = item;
						}
					}
				}

				// No match found yet?
				if(this.value == null)
				{
					// First try to match the value against the enum values
					foreach(EnumItem item in list)
					{
						// Matching value?
						if(item.Value == value.ToString())
						{
							// Set this value
							this.value = item;
						}
					}
				}

				// No match found yet?
				if(this.value == null)
				{
					// Try to match against the titles
					foreach(EnumItem item in list)
					{
						// Matching value?
						if(item.Title.ToLowerInvariant() == value.ToString().ToLowerInvariant())
						{
							// Set this value
							this.value = item;
						}
					}
				}

				// Still no match found?
				if(this.value == null)
				{
					// Make a dummy value
					this.value = new EnumItem(value.ToString(), value.ToString());
					this.value = new EnumItem(this.value.GetIntValue().ToString(CultureInfo.InvariantCulture), value.ToString());
				}
			}
		}

		public override object GetValue()
		{
			return GetIntValue();
		}
		
		public override int GetIntValue()
		{
			if(this.value != null)
			{
				// Parse the value to integer
				int result;
				if(int.TryParse(this.value.Value, NumberStyles.Integer,
								CultureInfo.InvariantCulture, out result))
				{
					return result;
				}
				else
				{
					return 0;
				}
			}
			else
			{
				return 0;
			}
		}
		
		public override string GetStringValue()
		{
			if(this.value != null) return this.value.Title; else return "NULL";
		}

		// This returns an enum list
		public override EnumList GetEnumList()
		{
			return list;
		}

		// This returns the type to display for fixed fields
		// Must be a custom usable type
		public override TypeHandlerAttribute GetDisplayType()
		{
			return General.Types.GetAttribute((int)UniversalType.Integer);
		}
		
		#endregion
	}
}
