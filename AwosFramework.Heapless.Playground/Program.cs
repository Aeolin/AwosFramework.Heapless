using AwosFramework.Heapless.Collections.Generic;

Span<int> span = stackalloc int[4];
var list = new SpanList<int>(span);

list.Add(1, 2, 3);
list.Insert(1, 4);
foreach(var item in list)
{
	Console.WriteLine(item);
}

var stack = new SpanStack<int>(span);
stack.Push(1);
stack.Push(3);
stack.Push(5);
stack.Push(7);
Console.WriteLine(stack.Pop());