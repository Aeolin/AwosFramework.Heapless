using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.Heapless.Collections.Generic
{
	public ref struct ReadOnlySpanList<T> : IReadOnlyList<T>
	{
		public int Count => _span.Length;
		private readonly ReadOnlySpan<T> _span;

		public ReadOnlySpanList(ReadOnlySpan<T> span, int? count = null)
		{
			if (count.HasValue)
			{

				if (count < 0 || count > _span.Length)
					throw new ArgumentException("Invalid count", nameof(count));

				_span = _span.Slice(0, count.Value);
			}

			_span = span;
		}

		public T this[int index] => _span[index];

		public ReadOnlySpan<T>.Enumerator GetEnumerator() => _span.GetEnumerator();

		IEnumerator<T> IEnumerable<T>.GetEnumerator() => new SpanEnumerator<T>(_span.ToArray());
		IEnumerator IEnumerable.GetEnumerator() => new SpanEnumerator<T>(_span.ToArray());
	}
}
