using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Strings.Common
{
	public interface IStringExtractor : IDisposable
	{
		IEnumerable<SearchResult> Search(CancellationToken cancelToken = default(CancellationToken));
	}
}
