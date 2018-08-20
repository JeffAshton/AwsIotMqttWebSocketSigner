using System;

namespace AwsIotMqttWebSocketListener {

	public sealed class DisconnectEventArgs : EventArgs {

		public DisconnectEventArgs( DisconnectReason reason ) {
			Reason = reason;
		}

		public DisconnectReason Reason { get; }
	}
}
