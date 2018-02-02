using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using AwsIotMqttWebSocketSigner;
using D2L.MQTT.Packets;

namespace AwsIotMqttWebSocketListener {

	internal sealed class AwsIotMqttWebSocketClient : IDisposable {

		private readonly string m_endpointAddress;
		private readonly string m_region;
		private readonly AWSCredentials m_credentails;

		private readonly CancellationTokenSource m_cancellationTokenSource = new CancellationTokenSource();
		private readonly Task m_runner;

		public AwsIotMqttWebSocketClient(
				string endpointAddress,
				string region,
				AWSCredentials credentials
			) {

			m_endpointAddress = endpointAddress;
			m_region = region;
			m_credentails = credentials;
			m_runner = Run( m_cancellationTokenSource.Token );
		}

		void IDisposable.Dispose() {

			m_cancellationTokenSource.Cancel();
			try {
				m_runner.ConfigureAwait( continueOnCapturedContext: false ).GetAwaiter().GetResult();

			} catch( OperationCanceledException ) {

			} finally {
				m_cancellationTokenSource.Dispose();
			}
		}

		private async Task Run( CancellationToken cancellationToken ) {

			try {

				using( ClientWebSocket socket = new ClientWebSocket() ) {
					socket.Options.AddSubProtocol( "mqtt" );

					ImmutableCredentials resolvedCredentials = await m_credentails
						.GetCredentialsAsync()
						.ConfigureAwait( continueOnCapturedContext: false );

					string socketUrl = SigV4Utils.GetSignedUri(
							host: m_endpointAddress,
							region: m_region,
							credentials: resolvedCredentials
						);

					Console.WriteLine( "Connecting to {0}", socketUrl );
					await socket
						.ConnectAsync( new Uri( socketUrl ), cancellationToken )
						.ConfigureAwait( continueOnCapturedContext: false );

					ArraySegment<byte> buffer = new ArraySegment<byte>(
						new byte[ 1024 ]
					);

					ConnectPacket connect = new ConnectPacket(
							protocolLevel: MqttProtocolLevel.Version_3_1_1,
							protocolName: "MQTT",
							clientId: "test",
							cleanSession: true,
							keepAlive: 300,
							userName: null,
							password: null,
							will: null
						);

					Console.WriteLine( "Sending connect packet" );
					await socket
						.SendPacketAsync( connect, buffer, cancellationToken )
						.ConfigureAwait( continueOnCapturedContext: false );

					MqttPacket packet = await socket
						.ReceiveMqttPacketAsync( buffer, cancellationToken )
						.ConfigureAwait( continueOnCapturedContext: false );

					if( packet == null ) {
						return;
					}

					if( packet.PacketType != PacketType.Connack ) {
						throw new InvalidOperationException( $"Invalid packet returned: { packet.PacketType }" );
					}

					Console.WriteLine( "Connected" );

					await ServeSocketConnectionAsync( socket, buffer, cancellationToken )
						.ConfigureAwait( continueOnCapturedContext: false );

					Console.WriteLine( "Sending disconnect packet" );
					
					DisconnectPacket disconnect = new DisconnectPacket();
					await socket
						.SendPacketAsync( disconnect, buffer, CancellationToken.None )
						.ConfigureAwait( continueOnCapturedContext: false );
				}

			} catch( OperationCanceledException ) {

			} catch( Exception err ) {
				Console.Error.WriteLine( err.ToString() );
			}
		}

		private async Task ServeSocketConnectionAsync(
				ClientWebSocket socket,
				ArraySegment<byte> buffer,
				CancellationToken cancellationToken
			) {

			try {
				while( !cancellationToken.IsCancellationRequested ) {

					MqttPacket packet = await socket
						.ReceiveMqttPacketAsync( buffer, cancellationToken )
						.ConfigureAwait( continueOnCapturedContext: false );

					if( packet == null ) {
						return;
					}

					Console.WriteLine( "Received {0} packet", packet.PacketType );
				}

			} catch( OperationCanceledException ) {
				return;
			}
		}
	}
}
