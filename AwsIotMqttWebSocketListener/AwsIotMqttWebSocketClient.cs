using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using AwsIotMqttWebSocketListener.Logging;
using AwsIotMqttWebSocketListener.Sessions;
using AwsIotMqttWebSocketSigner;
using D2L.MQTT.Packets;

namespace AwsIotMqttWebSocketListener {

	internal static class AwsIotMqttWebSocketClient {

		public static async Task<ConnectResponse> ConnectAsync(
				string endpoint,
				string region,
				AWSCredentials credentials,
				Action<MqttMessageEventArgs> messageHandler,
				IMqttClientLogger logger,
				CancellationToken cancellationToken
			) {

			ClientWebSocket socket = new ClientWebSocket();
			try {
				socket.Options.AddSubProtocol( "mqtt" );
				socket.Options.KeepAliveInterval = TimeSpan.FromSeconds( 30 );

				ImmutableCredentials resolvedCredentials = await credentials
					.GetCredentialsAsync()
					.ConfigureAwait( continueOnCapturedContext: false );

				string socketUrl = SigV4Utils.GetSignedUri(
						host: endpoint,
						region: region,
						credentials: resolvedCredentials
					);

				await socket
					.ConnectAsync( new Uri( socketUrl ), cancellationToken )
					.ConfigureAwait( continueOnCapturedContext: false );

				ArraySegment<byte> receiveBuffer = new ArraySegment<byte>(
					new byte[ 1024 ]
				);

				ConnectPacket connect = new ConnectPacket(
						protocolLevel: MqttProtocolLevel.Version_3_1_1,
						protocolName: "MQTT",
						clientId: "test",
						cleanSession: true,
						keepAlive: 35,
						userName: null,
						password: null,
						will: null
					);

				await socket
					.SendPacketAsync( connect, cancellationToken )
					.ConfigureAwait( continueOnCapturedContext: false );

				MqttPacket packet = await socket
					.ReceiveMqttPacketAsync( receiveBuffer, cancellationToken )
					.ConfigureAwait( continueOnCapturedContext: false );

				if( packet == null ) {
					throw new MqttProtocolException( "Connack packet not sent" );
				}

				if( packet.PacketType != PacketType.Connack ) {

					string msg = $"Client responsed with a { packet.PacketType } packet instead of Connack";
					throw new InvalidOperationException( msg );
				}

				ConnackPacket connack = (ConnackPacket)packet;
				if( connack.ReturnCode != ConnectReturnCode.ConnectionAccepted ) {

					socket.Dispose();

					return new ConnectResponse(
						returnCode: connack.ReturnCode,
						sessionPressent: connack.SessionPresent,
						session: DisconnectedMqttSession.Instance
					);
				}

				IMqttSession session = new MqttWebSocketSession(
						logger,
						socket,
						receiveBuffer: receiveBuffer,
						messageHandler: messageHandler
					);

				return new ConnectResponse(
					returnCode: ConnectReturnCode.ConnectionAccepted,
					sessionPressent: connack.SessionPresent,
					session: session
				);

			} catch {
				socket.Dispose();
				throw;
			}
		}
	}
}
