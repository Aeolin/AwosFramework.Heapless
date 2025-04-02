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

var queue = new SpanQueue<int>(span);
queue.Enqueue(1);
queue.Enqueue(2);
queue.Enqueue(3);
Console.WriteLine(queue.Dequeue());
queue.Enqueue(4);
queue.Enqueue(5);
while(queue.Count > 0)
	Console.WriteLine(queue.Dequeue());