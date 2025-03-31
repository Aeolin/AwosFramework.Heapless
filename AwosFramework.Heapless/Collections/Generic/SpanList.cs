using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.Heapless.Collections.Generic
{
	public ref struct SpanList<T> : IList<T>, ISpanCollection<T>
	{
		private readonly IEqualityComparer<T> _comparer;
		private readonly SpanCollectionCore<T> _core;

		public bool IsReadOnly => false;
		public bool IsExpandable => _core.IsExpandable;
		public ReadOnlySpan<T> ReadAccess => _core.ReadAccess;
		public int Count => _core.Count;

		public SpanList(MemoryPool<T> memoryPool, int capacity = SpanCollectionCore<T>.INITIAL_CAPACITY, IEqualityComparer<T>? equality = null)
		{
			_core = new SpanCollectionCore<T>(memoryPool, capacity);
			_comparer = equality ?? EqualityComparer<T>.Default;
		}

		public SpanList(Span<T> span, int count = 0, IEqualityComparer<T>? equality = null)
		{
			_core = new SpanCollectionCore<T>(span, count);
			_comparer = equality ?? EqualityComparer<T>.Default;
		}

		public ref T ItemAt(int index)
		{
			_core.ThrowIfOutOfRange(index);
			return ref _core.WriteAccess[index];
		}

		public T this[int index]
		{
			get => ItemAt(index);
			set => ItemAt(index) = value;
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

		private void InsertImpl(int index, T item)
		{
			_core.ThrowIfOutOfRange(index);
			_core.CheckExpand(out var span);
			span.Slice(index, _core.Count - index).CopyTo(span.Slice(index + 1));
			span[index] = item;
			_core.IncCount();
		}

		public void Insert(int index, T item)
		{
			if (index == _core.Count)
			{
				Add(item);
			}
			else
			{
				InsertImpl(index, item);
			}
		}

		public void RemoveAt(int index)
		{
			var span = _core.WriteAccess;
			for (int i = index; i < _core.Count - 1; i++)
				span[i] = span[i + 1];

			_core.DecCount();
		}

		private void AddImpl(T item)
		{
			_core.CheckExpand(out var span);
			span[_core.IncCount()] = item;
		}

		public void AddRange(IEnumerable<T> items)
		{
			foreach (var item in items)
				AddImpl(item);
		}

		public void Add(params T[] items) => AddRange(items);

		public void Add(T item) => AddImpl(item);
		

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

		public bool Remove(T item)
		{
			int index = IndexOf(item);
			if (index == -1)
				return false;

			RemoveAt(index);
			return true;
		}

		public T[] ToArray() => _core.ToArray();

		public Span<T>.Enumerator GetEnumerator() => _core.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _core.GetHeapEnumerator();
		IEnumerator<T> IEnumerable<T>.GetEnumerator() => _core.GetHeapEnumerator();
	}
}
