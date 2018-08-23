using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using AwsIotMqttWebSocketListener.Logging;
using D2L.MQTT.Packets;

namespace AwsIotMqttWebSocketListener.Sessions {

	internal sealed partial class MqttWebSocketSession : IMqttSession {

		private const ushort NoPacketIdentifier = 0;

		private readonly IMqttClientLogger m_logger;
		private readonly ClientWebSocket m_socket;
		private readonly ArraySegment<byte> m_receiveBuffer;
		private readonly Action<MqttMessageEventArgs> m_messageHandler;

		private readonly TimeSpan m_disconnectTimeout = TimeSpan.FromSeconds( 10 );

		private readonly IAsyncMqttPacketVisitor m_packetHandler;
		private readonly SemaphoreSlim m_sendLock = new SemaphoreSlim( 1, 1 );

		private ushort m_packetIdentifierCounter = 1;
		private readonly object m_packetIdentifierCounterLock = new object();

		private readonly TimeSpan m_keepAlive = TimeSpan.FromSeconds( 30 );

		public MqttWebSocketSession(
				IMqttClientLogger logger,
				ClientWebSocket socket,
				ArraySegment<byte> receiveBuffer,
				Action<MqttMessageEventArgs> messageHandler
			) {

			m_logger = logger;
			m_socket = socket;
			m_receiveBuffer = receiveBuffer;
			m_messageHandler = messageHandler;

			m_packetHandler = new PacketHandler( this );
		}

		void IDisposable.Dispose() {
			m_sendLock.Dispose();
			m_socket.Dispose();
		}

		bool IMqttSession.IsConnected {
			get {
				bool connected = m_socket.State == WebSocketState.Open;
				return connected;
			}
		}

		async Task IMqttSession.RunAsync(
				CancellationToken cancellationToken
			) {

			try {

				Task<MqttPacket> receiveTask = m_socket.ReceiveMqttPacketAsync( m_receiveBuffer, cancellationToken );

				while( !cancellationToken.IsCancellationRequested ) {

					Task keepAliveTask = Task.Delay( m_keepAlive );

					Task task = await Task
						.WhenAny( receiveTask, keepAliveTask )
						.ConfigureAwait( continueOnCapturedContext: false );

					if( Object.ReferenceEquals( task, receiveTask ) ) {

						MqttPacket packet = receiveTask.Result;
						if( packet.PacketType == PacketType.Disconnect ) {
							return;
						}

						receiveTask = m_socket.ReceiveMqttPacketAsync( m_receiveBuffer, cancellationToken );

						await packet
							.VisitAsync( m_packetHandler, cancellationToken )
							.ConfigureAwait( continueOnCapturedContext: false );

					} else {

						Console.WriteLine( "PINGREQ" );

						await SendPacketAsync( new PingreqPacket(), cancellationToken )
							.ConfigureAwait( continueOnCapturedContext: false );
					}
				}

			} catch( OperationCanceledException err ) when(
					Object.ReferenceEquals( cancellationToken, err.CancellationToken )
				) {

				using( CancellationTokenSource disconnectCancellation = new CancellationTokenSource( m_disconnectTimeout ) ) {

					DisconnectPacket disconnect = new DisconnectPacket();

					await SendPacketAsync( disconnect, disconnectCancellation.Token )
						.ConfigureAwait( continueOnCapturedContext: false );
				}

			} finally {
				m_socket.Dispose();
			}
		}

		async Task IMqttSession.PublishAsync(
				string topic,
				byte[] message,
				QualityOfService qos,
				CancellationToken cancellationToken
			) {

			ushort packetIdentifier = ( qos != QualityOfService.QoS0 )
				? NextPacketIdentifier()
				: NoPacketIdentifier;

			PublishPacket packet = new PublishPacket(
					topic: topic,
					message: message,
					qos: qos,
					packetIdentifier: packetIdentifier,
					retain: false,
					duplicate: false
				);

			await SendPacketAsync( packet, cancellationToken )
				.ConfigureAwait( continueOnCapturedContext: false );
		}

		async Task IMqttSession.SubscribeAsync(
				IEnumerable<Subscription> subscriptions,
				CancellationToken cancellationToken
			) {

			ushort packetIdentifier = NextPacketIdentifier();

			SubscribePacket packet = new SubscribePacket(
					packetIdentifier,
					subscriptions
				);

			await SendPacketAsync( packet, cancellationToken )
				.ConfigureAwait( continueOnCapturedContext: false );
		}

		private async Task SendPacketAsync(
				MqttPacket packet,
				CancellationToken cancellationToken
			) {

			await m_sendLock
				.WaitAsync( cancellationToken )
				.ConfigureAwait( continueOnCapturedContext: false );

			try {
				await m_socket
					.SendPacketAsync( packet, cancellationToken )
					.ConfigureAwait( continueOnCapturedContext: false );

			} finally {
				m_sendLock.Release();
			}
		}

		private ushort NextPacketIdentifier() {

			lock( m_packetIdentifierCounterLock ) {
				unchecked {

					ushort id = m_packetIdentifierCounter++;
					if( id == 0 ) {
						id = m_packetIdentifierCounter++;
					}

					return id;
				}
			}
		}
	}
}
