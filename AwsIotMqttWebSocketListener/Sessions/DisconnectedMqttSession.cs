using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using D2L.MQTT.Packets;

namespace AwsIotMqttWebSocketListener.Sessions {

	public sealed class DisconnectedMqttSession : IMqttSession {

		public static readonly IMqttSession Instance = new DisconnectedMqttSession();

		private DisconnectedMqttSession() {
		}

		void IDisposable.Dispose() {
		}

		bool IMqttSession.IsConnected => false;

		Task IMqttSession.RunAsync( CancellationToken cancellationToken ) {
			return Task.CompletedTask;
		}

		Task IMqttSession.PublishAsync( string topic, byte[] message, QualityOfService qos, CancellationToken cancellationToken ) {
			throw new InvalidOperationException( "Mqtt client is disconnected" );
		}

		Task IMqttSession.SubscribeAsync( IEnumerable<Subscription> subscriptions, CancellationToken cancellationToken ) {
			throw new InvalidOperationException( "Mqtt client is disconnected" );
		}
	}
}
