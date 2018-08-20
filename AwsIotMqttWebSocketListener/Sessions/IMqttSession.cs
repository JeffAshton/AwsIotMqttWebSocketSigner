using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using D2L.MQTT.Packets;

namespace AwsIotMqttWebSocketListener.Sessions {

	public interface IMqttSession : IDisposable {

		bool IsConnected { get; }

		Task RunAsync(
				CancellationToken cancellationToken
			);

		Task PublishAsync(
				string topic,
				byte[] message,
				QualityOfService qos,
				CancellationToken cancellationToken
			);

		Task SubscribeAsync(
				IEnumerable<Subscription> subscriptions,
				CancellationToken cancellationToken
			);
	}
}
