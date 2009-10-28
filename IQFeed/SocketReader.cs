using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace IQFeed
{
	public delegate bool StringCallback(string s);

	public class SocketReader
	{
		private Socket _socket;
		public StringCallback Callback { get; set; }
		private StateObject _state = new StateObject();
		List<byte> _pendingBytes = new List<byte>();

		public SocketReader(Socket socket)
		{
			_socket = socket;
		}

		public void Begin()
		{
			_socket.BeginReceive(_state.buffer, 0, StateObject.BufferSize, SocketFlags.None,
				new AsyncCallback(PrivateCallback), _state);
		}

		private void PrivateCallback(IAsyncResult ar)
		{
			if (_socket.Connected)
			{
				int bytesRead = _socket.EndReceive(ar);

				bool bContinue = true;

				StringCallback callback = Callback;

				for (int i = 0; i < bytesRead; i++)
				{
					byte b = _state.buffer[i];
					_pendingBytes.Add(b);
					if (b == 10)
					{
						//	New line
						string s = Encoding.ASCII.GetString(_pendingBytes.ToArray());
						if (callback != null)
						{
							if (!callback(s))
							{
								bContinue = false;
							}
						}
						_pendingBytes.Clear();
					}
				}

				if (bContinue && _socket.Connected)
				{
					Begin();
				}
			}
			
		}

	}
}
