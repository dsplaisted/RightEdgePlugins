using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace RightEdge.DataStorage
{
	class TimeOperation : IDisposable
	{
		string _name;
		Stopwatch _stopwatch;
		public TimeOperation(string name)
		{
			_name = name;
			_stopwatch = new Stopwatch();
			_stopwatch.Start();
		}

		#region IDisposable Members

		public void Dispose()
		{
			_stopwatch.Stop();
			Debug.WriteLine(string.Format("{0}: {1}", _name, _stopwatch.Elapsed));
		}

		#endregion
	}
}
