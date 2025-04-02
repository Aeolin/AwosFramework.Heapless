using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.Heapless.Collections.Generic
{
	public interface ISpanCollection<T> : ICollection<T>, ISpanCollectionCore<T>
	{
		public int IndexOf(T item);
		public ref T ItemAt(int index);
	}
}
