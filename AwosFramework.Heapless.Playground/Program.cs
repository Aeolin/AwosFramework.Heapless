using AwosFramework.Heapless.Collections.Generic;

Span<int> span = stackalloc int[4];
var list = new SpanList<int>(span);

list.Add(1, 2, 3);
list.Insert(1, 4);
foreach(var item in list)
{
	Console.WriteLine(item);
}