using AwosFramework.Heapless.Collections.Generic;

Span<int> span = stackalloc int[3];
var list = new SpanList<int>(span);

list.Add(1, 2, 3);
foreach(var item in list)
{
	Console.WriteLine(item);
}