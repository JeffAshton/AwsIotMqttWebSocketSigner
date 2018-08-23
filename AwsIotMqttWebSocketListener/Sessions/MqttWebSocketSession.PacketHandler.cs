using System;
using System.Threading;
using System.Threading.Tasks;
using D2L.MQTT.Packets;

namespace AwsIotMqttWebSocketListener.Sessions {

	partial class MqttWebSocketSession {

		private sealed class PacketHandler : IAsyncMqttPacketVisitor {

			private readonly MqttWebSocketSession m_session;

			public PacketHandler( MqttWebSocketSession session ) {
				m_session = session;
			}

			// PUBLISH
			async Task IAsyncMqttPacketVisitor.VisitAsync( PublishPacket packet, CancellationToken cancellationToken ) {

				try {
					MqttMessageEventArgs args = new MqttMessageEventArgs(
							topic: packet.Topic,
							message: packet.Message,
							qos: packet.QoS
						);

					m_session.OnMessageReceived( args );

				} catch( Exception err ) {
					m_session.m_logger.Error( "Failed to raise message", err );
				}

				switch( packet.QoS ) {

					case QualityOfService.QoS1:

						PubackPacket puback = new PubackPacket( packet.PacketIdentifier.Value );

						await m_session
							.SendPacketAsync( puback, cancellationToken )
							.ConfigureAwait( continueOnCapturedContext: false );

						break;

					case QualityOfService.QoS2:

						PubrecPacket pubrec = new PubrecPacket( packet.PacketIdentifier.Value );

						await m_session
							.SendPacketAsync( pubrec, cancellationToken )
							.ConfigureAwait( continueOnCapturedContext: false );

						break;
				}
			}

			// PUBACK
			Task IAsyncMqttPacketVisitor.VisitAsync( PubackPacket packet, CancellationToken cancellationToken ) {
				return Task.FromResult( true );
			}

			// PUBREL
			async Task IAsyncMqttPacketVisitor.VisitAsync( PubrelPacket packet, CancellationToken cancellationToken ) {

				PubcompPacket pubcomp = new PubcompPacket( packet.PacketIdentifier );

				await m_session
					.SendPacketAsync( pubcomp, cancellationToken )
					.ConfigureAwait( continueOnCapturedContext: false );
			}

			// PUBREC
			async Task IAsyncMqttPacketVisitor.VisitAsync( PubrecPacket packet, CancellationToken cancellationToken ) {

				PubrelPacket pubrel = new PubrelPacket( packet.PacketIdentifier );

				await m_session
					.SendPacketAsync( pubrel, cancellationToken )
					.ConfigureAwait( continueOnCapturedContext: false );
			}

			// PUBCOMP
			Task IAsyncMqttPacketVisitor.VisitAsync( PubcompPacket packet, CancellationToken cancellationToken ) {
				return Task.FromResult( true );
			}

			// SUBACK
			Task IAsyncMqttPacketVisitor.VisitAsync( SubackPacket packet, CancellationToken cancellationToken ) {
				return Task.FromResult( true );
			}

			// UNSUBACK
			Task IAsyncMqttPacketVisitor.VisitAsync( UnsubackPacket packet, CancellationToken cancellationToken ) {
				return Task.FromResult( true );
			}

			// PINGREQ
			async Task IAsyncMqttPacketVisitor.VisitAsync( PingreqPacket packet, CancellationToken cancellationToken ) {

				PingrespPacket pingresp = new PingrespPacket();

				await m_session
					.SendPacketAsync( pingresp, cancellationToken )
					.ConfigureAwait( continueOnCapturedContext: false );
			}

			// PINGRSP
			Task IAsyncMqttPacketVisitor.VisitAsync( PingrespPacket packet, CancellationToken cancellationToken ) {
				Console.WriteLine( "PINGRSP" );
				return Task.FromResult( true );
			}

			#region Unexpected packets

			// CONNECT
			Task IAsyncMqttPacketVisitor.VisitAsync( ConnectPacket packet, CancellationToken cancellationToken ) {
				return HandleUnexpectedPacket( packet );
			}

			// CONNACK
			Task IAsyncMqttPacketVisitor.VisitAsync( ConnackPacket packet, CancellationToken cancellationToken ) {
				return HandleUnexpectedPacket( packet );
			}

			// DISCONNECT
			Task IAsyncMqttPacketVisitor.VisitAsync( DisconnectPacket packet, CancellationToken cancellationToken ) {
				return HandleUnexpectedPacket( packet );
			}

			// SUBSCRIBE
			Task IAsyncMqttPacketVisitor.VisitAsync( SubscribePacket packet, CancellationToken cancellationToken ) {
				return HandleUnexpectedPacket( packet );
			}

			// UNSUBSCRIBE
			Task IAsyncMqttPacketVisitor.VisitAsync( UnsubscribePacket packet, CancellationToken cancellationToken ) {
				return HandleUnexpectedPacket( packet );
			}

			private Task HandleUnexpectedPacket( MqttPacket packet ) {
				throw new MqttProtocolException( $"Unexpected packet from broker: { packet.PacketType.ToString().ToUpper() }" );
			}

			#endregion

		}

		private void OnMessageReceived( MqttMessageEventArgs args ) {

			try {
				m_messageHandler( args );
			} catch( Exception err ) {
				m_logger.Error( "Unhandled exception from MessageReceived event handlers", err );
			}
		}
	}
}