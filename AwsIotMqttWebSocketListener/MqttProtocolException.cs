using System;

namespace AwsIotMqttWebSocketListener {

	public sealed class MqttProtocolException : Exception {

		public MqttProtocolException( string message )
			: base( message ) {
		}
	}
}
