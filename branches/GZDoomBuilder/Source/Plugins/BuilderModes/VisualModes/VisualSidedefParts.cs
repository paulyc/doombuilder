
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

#endregion

namespace CodeImp.DoomBuilder.BuilderModes
{
	internal struct VisualSidedefParts
	{
		// Members
		public VisualUpper upper;
		public VisualLower lower;
		public VisualMiddleDouble middledouble;
		public VisualMiddleSingle middlesingle;
		public List<VisualMiddle3D> middle3d;
		public List<VisualMiddleBack> middleback;//mxd
		
		// Constructor
		public VisualSidedefParts(VisualUpper u, VisualLower l, VisualMiddleDouble m, List<VisualMiddle3D> e, List<VisualMiddleBack> eb)
		{
			this.upper = u;
			this.lower = l;
			this.middledouble = m;
			this.middlesingle = null;
			this.middle3d = e;
			this.middleback = eb;//mxd
		}
		
		// Constructor
		public VisualSidedefParts(VisualMiddleSingle m)
		{
			this.upper = null;
			this.lower = null;
			this.middledouble = null;
			this.middlesingle = m;
			this.middle3d = null;
			this.middleback = null; //mxd
		}
		
		// This calls Setup() on all parts
		public void SetupAllParts()
		{
			if(lower != null) lower.Setup();
			if(middledouble != null) middledouble.Setup();
			if(middlesingle != null) middlesingle.Setup();
			if(upper != null) upper.Setup();
			if(middle3d != null)
			{
				foreach(VisualMiddle3D m in middle3d)
					m.Setup();
			}
			if(middleback != null) {
				foreach(VisualMiddleBack m in middleback)
					m.Setup();
			}
		}
	}
}