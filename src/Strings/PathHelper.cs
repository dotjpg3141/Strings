using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

		public static string GetDirectoryOfExecutable()
		{
			var executablePath = Assembly.GetExecutingAssembly().Location;
			var baseDirectory = Path.GetDirectoryName(executablePath);
			return baseDirectory;
		}
	}
}
