using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.Heapless.Collections.Generic
{
	internal ref struct SpanCollectionCore<T> : ISpanCollectionCore<T>
	{
		private const double EXPANSION_FACTOR = 1.618;
		internal const int INITIAL_CAPACITY = 4;

		private IMemoryOwner<T>? _owner;
		private readonly MemoryPool<T>? _pool;

		private Span<T> _span;
		private int _count;

		public int Count => _count;
		public bool IsExpandable => _pool != null;

		public SpanCollectionCore(MemoryPool<T> memoryPool, int capacity = INITIAL_CAPACITY)
		{
			_pool = memoryPool;
			_owner = memoryPool.Rent(capacity);
			_span = _owner.Memory.Span;
			_count = 0;
		}

		public SpanCollectionCore(Span<T> span, int count = 0)
		{
			_span = span;
			_count = count;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ThrowIfOutOfRange(int index)
		{
			if (index < 0 || index >= _count)
				throw new IndexOutOfRangeException();
		}

		public void SetCount(int count)
		{
			if (count < 0 || count > _span.Length)
				throw new ArgumentException("Invalid count", nameof(count));

			_count = count;
		}

		/// <summary>
		/// Increments the count and returns the old count
		/// </summary>
		/// <returns>Count before incrementing</returns>
		/// <exception cref="InvalidOperationException"></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int IncCount()
		{
			if (_count == _span.Length)
				throw new InvalidOperationException("Count is already at capacity");

			return _count++;
		}

		/// <summary>
		/// Decrements the count and returns the new count
		/// </summary>
		/// <returns>Count after decrementing</returns>
		/// <exception cref="InvalidOperationException"></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int DecCount()
		{
			if(_count == 0)
				throw new InvalidOperationException("Count is already zero");

			return --_count;
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int GetNewCapacity(int minCapacity)
		{
			return (int)(minCapacity * EXPANSION_FACTOR);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void CheckExpand(out Span<T> newSpan)
		{
			if (_count >= _span.Length)
			{
				if (_pool == null)
					throw new OutOfMemoryException($"{nameof(SpanList<T>)} is not expandable");

				var newCap = GetNewCapacity(_count);
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

	
		public ReadOnlySpan<T> ReadAccess { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _span.Slice(0, Count); }
		public Span<T> WriteAccess { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _span; }

		[MethodImpl(MethodImplOptions.AggressiveInlining)] 
		public T[] ToArray() => ReadAccess.Slice(0, Count).ToArray();

		[MethodImpl(MethodImplOptions.AggressiveInlining)] 
		public Span<T>.Enumerator GetEnumerator() => WriteAccess.Slice(0, Count).GetEnumerator();

		[MethodImpl(MethodImplOptions.AggressiveInlining)] 
		public IEnumerator<T> GetHeapEnumerator() => new SpanEnumerator<T>(_owner?.Memory.Slice(0, _count) ?? ToArray());

	}
}
