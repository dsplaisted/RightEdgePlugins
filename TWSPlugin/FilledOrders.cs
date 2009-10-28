using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RightEdge.Common;

namespace RightEdge.TWSCSharpPlugin
{
	class FilledOrders
	{
		Queue<FilledOrder> _filledQueue = new Queue<FilledOrder>();
		HashSet<string> _filledSet = new HashSet<string>();

		public void RecordFill(string orderId, DateTime filledTime)
		{
			_filledSet.Add(orderId);
			_filledQueue.Enqueue(new FilledOrder() { OrderId = orderId, FilledTime = filledTime });

			Trim(filledTime);
		}

		public bool WasFilled(string orderId)
		{
			return _filledSet.Contains(orderId);
		}

		private void Trim(DateTime currentTime)
		{
			if (_filledQueue.Count == 0)
			{
				return;
			}
			while (currentTime.Subtract(_filledQueue.Peek().FilledTime).TotalMinutes > 1)
			{
				_filledSet.Remove(_filledQueue.Peek().OrderId);
				_filledQueue.Dequeue();
			}
		}

		class FilledOrder
		{
			public string OrderId { get; set; }
			public DateTime FilledTime { get; set; }
		}
	}
}
