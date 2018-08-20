using System;
using AwsIotMqttWebSocketListener.Sessions;
using D2L.MQTT.Packets;

namespace AwsIotMqttWebSocketListener {

	public sealed class ConnectResponse : IDisposable {

		public ConnectResponse(
				ConnectReturnCode returnCode,
				bool sessionPressent,
				IMqttSession session
			) {

			ReturnCode = returnCode;
			SessionPressent = sessionPressent;
			Session = session;
		}

		public ConnectReturnCode ReturnCode { get; }
		public bool SessionPressent { get; }
		public IMqttSession Session { get; }

		void IDisposable.Dispose() {
			this.Session.Dispose();
		}
	}
}
