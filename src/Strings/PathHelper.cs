using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Strings
{
	internal static class PathHelper
	{
		public static string Quote(string path)
		{
			if (!path.StartsWith("\""))
			{
				path = "\"" + path + "\"";
			}
			return path;
		}
	}
}
