using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.Heapless.Collections.Generic
{
	public ref struct SpanQueue<T> : ISpanCollection<T>
	{
		private int _writeIndex;
		private int _readIndex;

		private const double EXPANSION_FACTOR = 1.618;
		internal const int INITIAL_CAPACITY = 4;

		private IMemoryOwner<T>? _owner;
		private readonly MemoryPool<T>? _pool;

		private Span<T> _span;


		public bool IsExpandable => _pool != null;
		public int Count { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _writeIndex - _readIndex; } }
		public bool IsReadOnly => false;

		public ReadOnlySpan<T> ReadAccess => _span.Slice(_readIndex, Count);
		private Span<T> WriteAccess => _span.Slice(_writeIndex, Count);

		private readonly IEqualityComparer<T> _comparer;

		public SpanQueue(MemoryPool<T> memoryPool, int capacity = INITIAL_CAPACITY, IEqualityComparer<T>? equality = null)
		{
			_pool = memoryPool;
			_owner = memoryPool.Rent(capacity);
			_span = _owner.Memory.Span;
			_comparer = equality ?? EqualityComparer<T>.Default;
		}

		public SpanQueue(Span<T> span, int readIndex = 0, int writeIndex = 0, IEqualityComparer<T>? equality = null)
		{
			Debug.Assert(readIndex <= writeIndex);
			_writeIndex = writeIndex;
			_readIndex = readIndex;
			_span = span;
			_comparer = equality ?? EqualityComparer<T>.Default;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ThrowIfOutOfRange(int index)
		{
			if (index < 0 || index >= Count)
				throw new IndexOutOfRangeException();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int GetNewCapacity(int minCapacity)
		{
			return (int)(minCapacity * EXPANSION_FACTOR);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void CheckExpand(out Span<T> newSpan)
		{
			if (_writeIndex >= _span.Length && CheckBackFill() == false)
			{
				if (_pool == null)
					throw new OutOfMemoryException($"{nameof(SpanQueue<T>)} is not expandable");

				var newCap = GetNewCapacity(_writeIndex);
				var newMemory = _pool.Rent(newCap);
				_span.CopyTo(newMemory.Memory.Span);
				_owner?.Dispose();
				_owner = newMemory;
				_span = newMemory.Memory.Span;
				newSpan = _span;
			}
			else
			{
				newSpan = _span;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool CheckBackFill()
		{
			if (_readIndex > 0)
			{
				ReadAccess.CopyTo(_span);
				_writeIndex -= _readIndex;
				_readIndex = 0;
				return true;
			}
			else
			{
				return false;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref T ItemAt(int index)
		{
			return ref WriteAccess[index];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Add(T item) => Enqueue(item);


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Enqueue(T item)
		{
			CheckExpand(out var span);
			span[_writeIndex++] = item;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Dequeue()
		{
			if (_readIndex >= _writeIndex)
				throw new InvalidOperationException("Queue is empty");

			return _span[_readIndex++];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Peek()
		{
			if (Count == 0)
				throw new InvalidOperationException("Queue is empty");

			return _span[_readIndex];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryPeek(out T item)
		{
			if (Count == 0)
			{
				item = default;
				return false;
			}

			item = _span[_readIndex];
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryEnqueue(T item)
		{
			if (IsExpandable == false && _writeIndex >= _span.Length)
				return false;

			Enqueue(item);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryDequeue(out T item)
		{
			if (_readIndex == _writeIndex)
			{
				item = default;
				return false;
			}

			item = Dequeue();
			return true;
		}

		public void Clear()
		{
			_readIndex = 0;
			_writeIndex = 0;
		}

		private int IndexOfInternal(T item)
		{
			for (int i = _readIndex; i < _writeIndex; i++)
			{
				if (_comparer.Equals(_span[i], item))
					return i;
			}

			return -1;
		}

		public int IndexOf(T item)
		{
			var index = IndexOfInternal(item);
			if (index >= 0)
				index += _readIndex;

			return index;
		}

		public bool Contains(T item) => IndexOf(item) != -1;

		public void CopyTo(T[] array, int arrayIndex)
		{
			if (array.Length - arrayIndex < Count)
				throw new ArgumentException("Array is too small", nameof(array));

			ReadAccess.CopyTo(array.AsSpan(arrayIndex));
		}

		public bool Remove(T item)
		{
			var index = IndexOfInternal(item);
			if (index == -1)
				return false;


			if (index == _readIndex)
			{
				_readIndex++;
			}
			else
			{
				_span.Slice(index+1).CopyTo(_span.Slice(index));
				_writeIndex--;
			}

			return true;
		}

		public T[] ToArray() => ReadAccess.ToArray();
		public Span<T>.Enumerator GetEnumerator() => WriteAccess.GetEnumerator();

		private IEnumerator<T> GetHeapEnumerator() => new SpanEnumerator<T>(_owner?.Memory.Slice(0, Count) ?? ToArray());
		IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetHeapEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetHeapEnumerator();
	}
}
