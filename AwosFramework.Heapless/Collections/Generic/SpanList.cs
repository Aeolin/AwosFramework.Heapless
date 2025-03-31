using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.Heapless.Collections.Generic
{
	public ref partial struct SpanList<T> : IList<T>
	{
		private const double EXPANSION_FACTOR = 1.618;
		private const int INITIAL_CAPACITY = 4;

		private IMemoryOwner<T>? _owner;
		private readonly MemoryPool<T>? _pool;

		private readonly SemaphoreSlim _spanLock = new(1);
		private Span<T> _span;
		private readonly IEqualityComparer<T> _comparer;
		private int _count;

		public bool Expandable => _pool != null;
		public bool IsReadOnly => false;
		public int Count => _count;

		public SpanList(MemoryPool<T> memoryPool, int capacity = INITIAL_CAPACITY, IEqualityComparer<T>? equality = null)
		{
			_pool = memoryPool;
			_owner = memoryPool.Rent(capacity);
			_span = _owner.Memory.Span;
			_comparer = equality ?? EqualityComparer<T>.Default;
			_count = 0;
		}

		public SpanList(Span<T> span, int count = 0, IEqualityComparer<T>? equality = null)
		{
			_span = span;
			_count = count;
			_comparer = equality ?? EqualityComparer<T>.Default;
		}

		private int GetNewCapacity(int minCapacity)
		{
			return (int)(minCapacity * EXPANSION_FACTOR);
		}

		private void CheckExpand()
		{
			if (_count >= _span.Length)
			{
				if (_pool == null)
					throw new OutOfMemoryException($"{nameof(SpanList<T>)} is not expandable");

				var newCap = GetNewCapacity(_count);
				var newMemory = _pool.Rent(newCap);
				_span.CopyTo(newMemory.Memory.Span);
				_span = newMemory.Memory.Span;
				_owner?.Dispose();
				_owner = newMemory;
			}
		}

		private void ThrowIfOutOfRange(int index)
		{
			if (index < 0 || index >= _count)
				throw new IndexOutOfRangeException();
		}

		public ref T ItemAt(int index)
		{
			ThrowIfOutOfRange(index);
			return ref _span[index];
		}

		public T this[int index]
		{
			get => ItemAt(index);
			set => ItemAt(index) = value;
		}

		public int IndexOf(T item)
		{
			for (int i = 0; i < _count; i++)
			{
				if (EqualityComparer<T>.Default.Equals(_span[i], item))
					return i;
			}

			return -1;
		}

		private void InsertImpl(int index, T item)
		{
			ThrowIfOutOfRange(index);
			_spanLock.Wait();

			try
			{
				for (int i = index; i < _count; i++)
					_span[i+1] = _span[i];

				_span[index] = item;
			}
			finally
			{
				_spanLock.Release();
			}
		}

		public void Insert(int index, T item)
		{
			if (index == _count)
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
			ThrowIfOutOfRange(index);
			_spanLock.Wait();
			try
			{
				for (int i = index; i < _count - 1; i++)
					_span[i] = _span[i + 1];

				_count--;
			}
			finally
			{
				_spanLock.Release();
			}
		}

		private void AddImpl(T item)
		{
			CheckExpand();
			_span[_count++] = item;
		}

		public void AddRange(IEnumerable<T> items)
		{
			_spanLock.Wait();
			try
			{
				CheckExpand();

				foreach (var item in items)
					AddImpl(item);
			}
			finally
			{
				_spanLock.Release();
			}
		}

		public void Add(params T[] items) => AddRange(items);

		public void Add(T item)
		{
			_spanLock.Wait();
			try
			{
				AddImpl(item);
			}
			catch
			{
				_spanLock.Release();
			}
		}

		public void Clear()
		{
			_count = 0;
		}

		public bool Contains(T item)
		{
			return IndexOf(item) != -1;
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			if (array.Length - arrayIndex < _count)
				throw new ArgumentException("Array is too small", nameof(array));

			Span<T> array_span = array;
			_span.CopyTo(array_span.Slice(arrayIndex));
		}

		public bool Remove(T item)
		{
			int index = IndexOf(item);
			if (index == -1)
				return false;

			RemoveAt(index);
			return true;
		}

		public T[] ToArray() => _span.Slice(0, _count).ToArray();

		public Span<T>.Enumerator GetEnumerator() => _span.Slice(0, _count).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => new SpanEnumerator<T>(_owner?.Memory.Slice(0, _count) ?? ToArray());
		IEnumerator<T> IEnumerable<T>.GetEnumerator() => new SpanEnumerator<T>(_owner?.Memory.Slice(0, _count) ?? ToArray());
	}
}
