using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.Heapless.Collections.Generic
{
	public ref struct SpanRing<T>
	{
		private int _readIndex;
		private int _writeIndex;
		private Span<T> _span;
		private int _count;

		public int Count => _count;

		public bool IsFull => _count == _span.Length;
		public bool IsEmpty => _count == 0;

		public bool IsReadOnly => false;
		public bool IsExpandable => false;
		public bool Overwrite { get; set; } = false;

		public SpanRing(Span<T> span, int readIndex = 0, int count = 0)
		{
			Debug.Assert(readIndex < span.Length);
			Debug.Assert(count <= span.Length);

			_span = span;
			_readIndex = readIndex;
			_count = count;
			_writeIndex = (readIndex+count)%_span.Length;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int IncRead()
		{
			var old = _readIndex;
			if (++_readIndex == _span.Length)
			{
				_readIndex = 0;
			}

			_count--;
			return old;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int IncWrite()
		{
			var old = _writeIndex;
			if (++_writeIndex == _span.Length)
			{
				_writeIndex = 0;
			}

			_count++;
			return old;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryWrite(T item)
		{
			var isFull = IsFull;
			if (isFull && Overwrite == false)
				return false;

			WriteUnchecked(item, isFull);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(T item)
		{
			var isFull = IsFull;
			if (isFull && Overwrite == false)
				throw new InvalidOperationException("The ring is full");

			WriteUnchecked(item, isFull);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void WriteUnchecked(T item, bool isFull)
		{
			_span[IncWrite()] = item;
			if (isFull)
				IncRead();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryRead(out T item)
		{
			if (IsEmpty)
			{
				item = default!;
				return false;
			}

			item = Read();
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Read()
		{
			if (IsEmpty)
				throw new InvalidOperationException("The ring is empty");

			return _span[IncRead()];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryPeek(out T item)
		{
			if (IsEmpty)
			{
				item = default!;
				return false;
			}

			item = _span[_readIndex];
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref T RefPeek()
		{
			if (IsEmpty)
				throw new InvalidOperationException("The ring is empty");
			return ref _span[_readIndex];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Peek()
		{
			if (IsEmpty)
				throw new InvalidOperationException("The ring is empty");

			return _span[_readIndex];
		}
	}
}
