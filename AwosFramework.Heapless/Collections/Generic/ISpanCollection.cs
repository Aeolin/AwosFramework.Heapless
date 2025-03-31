using AwosFramework.Heapless.Utlis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.Heapless.Collections.Generic
{
	public interface ISpanCollection<T> : ICollection<T>, ISpanCollectionCore<T>
	{
		public ref T ItemAt(int index);
	}
}
