using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.Heapless.Collections.Generic
{
	public interface ISpanCollectionCore<T>
	{
		public int Count { get; }
		public bool IsExpandable { get; }
		public ReadOnlySpan<T> ReadAccess { get; }
	}
}
