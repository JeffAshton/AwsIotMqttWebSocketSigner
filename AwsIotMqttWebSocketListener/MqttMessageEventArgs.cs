using System;
using D2L.MQTT.Packets;

namespace AwsIotMqttWebSocketListener {

	public sealed class MqttMessageEventArgs : EventArgs {

		public MqttMessageEventArgs(
				string topic,
				byte[] message,
				QualityOfService qos
			) {

			Topic = topic;
			Message = message;
			QoS = qos;
		}

		public string Topic { get; }
		public byte[] Message { get; }
		public QualityOfService QoS { get; }
	}
}
