
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

#endregion

namespace CodeImp.DoomBuilder.Types
{
	[TypeHandler(0)]
	internal class IntegerHandler : TypeHandler
	{
		#region ================== Constants

		#endregion

		#region ================== Variables

		private int value;
		
		#endregion

		#region ================== Properties

		public override bool IsCustomType { get { return true; } }

		#endregion

		#region ================== Methods

		public override void SetValue(object value)
		{
			int result;
			
			// Already an int or float?
			if((value is int) || (value is float))
			{
				// Return the same
				this.value = (int)value;
			}
			else
			{
				// Try parsing as string
				if(int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.CurrentCulture, out result))
				{
					this.value = result;
				}
				else
				{
					this.value = 0;
				}
			}
		}

		public override int GetIntValue()
		{
			return this.value;
		}

		public override string GetStringValue()
		{
			return this.value.ToString();
		}
		
		#endregion
	}
}
