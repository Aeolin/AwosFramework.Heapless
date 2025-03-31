using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.Heapless.Collections.Generic
{
	public ref struct SpanStack<T> : ISpanCollection<T>
	{
		private SpanCollectionCore<T> _core;
		private readonly IEqualityComparer<T> _comparer;

		public SpanStack(MemoryPool<T> memoryPool, int capacity = SpanCollectionCore<T>.INITIAL_CAPACITY, IEqualityComparer<T>? equality = null)
		{
			_core = new SpanCollectionCore<T>(memoryPool, capacity);
			_comparer = equality ?? EqualityComparer<T>.Default;
		}

		public SpanStack(Span<T> span, int count = 0, IEqualityComparer<T>? equality = null)
		{
			_core = new SpanCollectionCore<T>(span, count);
			_comparer = equality ?? EqualityComparer<T>.Default;
		}

		public int Count => _core.Count;
		public bool IsReadOnly => false;
		public bool IsExpandable => _core.IsExpandable;
		public ReadOnlySpan<T> ReadAccess => _core.ReadAccess;

		public void Push(T item) => Add(item);

		public bool TryPop(out T item)
		{
			if (_core.Count == 0)
			{
				item = default!;
				return false;
			}
			
			item = Pop();
			return true;
		}

		public bool TryPeek(out T item)
		{
			if (_core.Count == 0)
			{
				item = default!;
				return false;
			}

			item = Peek();
			return true;
		}

		public T Pop()
		{
			_core.ThrowIfOutOfRange(0);
			return _core.WriteAccess[_core.DecCount()];
		}

		public T Peek()
		{
			_core.ThrowIfOutOfRange(0);
			return _core.ReadAccess[_core.Count - 1];
		}

		public void Add(T item)
		{
			_core.CheckExpand(out var span);
			span[_core.IncCount()] = item;
		}

		public void Clear()
		{
			_core.SetCount(0);
		}

		public bool Contains(T item)
		{
			return IndexOf(item) != -1;
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			if (array.Length - arrayIndex < _core.Count)
				throw new ArgumentException("Array is too small", nameof(array));

			Span<T> array_span = array;
			_core.ReadAccess.CopyTo(array_span.Slice(arrayIndex));
		}

		public int IndexOf(T item)
		{
			for (int i = 0; i < _core.Count; i++)
			{
				if (_comparer.Equals(_core.ReadAccess[i], item))
					return i;
			}

			return -1;
		}

		public ref T ItemAt(int index)
		{
			_core.ThrowIfOutOfRange(index);
			return ref _core.WriteAccess[index];
		}

		public bool Remove(T item)
		{
			var index = IndexOf(item);
			if (index == -1)
				return false;

			var span = _core.WriteAccess;
			for (int i = index; i < _core.Count - 1; i++)
				span[i] = span[i + 1];

			_core.DecCount();
			return true;
		}

		public Span<T>.Enumerator GetEnumerator() => _core.GetEnumerator();
		IEnumerator<T> IEnumerable<T>.GetEnumerator() => _core.GetHeapEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _core.GetHeapEnumerator();
	}
}
