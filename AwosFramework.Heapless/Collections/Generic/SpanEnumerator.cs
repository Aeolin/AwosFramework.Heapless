using System.Collections;

namespace AwosFramework.Heapless.Collections.Generic
{
	public struct SpanEnumerator<T> : IEnumerator<T>
	{
		private Memory<T> _memory;
		private int _index;

		public SpanEnumerator(Memory<T> span)
		{
			_memory = span;
			_index = -1;
		}

		public T Current => _memory.Span[_index];
		object IEnumerator.Current => Current;

		public void Dispose()
		{
			_memory = default;
			_index = -1;
		}

		public bool MoveNext()
		{
			_index++;
			return _index < _memory.Length;
		}

		public void Reset()
		{
			_index = -1;
		}
	}
}
