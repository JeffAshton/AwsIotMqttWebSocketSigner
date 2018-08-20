using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using D2L.MQTT.Packets;

namespace AwsIotMqttWebSocketListener {

	internal static class MqttClientWebSocketExtensions {

		public static async Task<MqttPacket> ReceiveMqttPacketAsync(
				this ClientWebSocket socket,
				ArraySegment<byte> buffer,
				CancellationToken cancellationToken
			) {

			MqttFixedHeader fixedHeader = await socket
				.ReceiveMqttFixedHeaderAsync( buffer, cancellationToken )
				.ConfigureAwait( continueOnCapturedContext: false );

			if( fixedHeader == null ) {
				return null;
			}

			PacketType packetType = fixedHeader.PacketType;

			using( MemoryStream ms = new MemoryStream( fixedHeader.RemainingLength ) ) {

				int remaining = fixedHeader.RemainingLength;
				while( remaining > 0 ) {

					int requestedBytes = ( remaining < buffer.Count )
						? remaining
						: buffer.Count;

					ArraySegment<byte> packetBuffer = new ArraySegment<byte>( buffer.Array, buffer.Offset, requestedBytes );

					WebSocketReceiveResult result = await socket
						.ReceiveAsync( packetBuffer, cancellationToken )
						.ConfigureAwait( continueOnCapturedContext: false );

					// end of stream
					int actualBytes = result.Count;
					if( actualBytes < requestedBytes ) {

						throw new PacketFormatException(
								packetType,
								$"Incomplete packet. Only received { actualBytes } of { requestedBytes } bytes."
							);
					}

					ms.Write( packetBuffer.Array, packetBuffer.Offset, actualBytes );

					remaining -= packetBuffer.Count;
				}

				ms.Position = 0;

				MqttPacket packet = MqttPacketReader.ReadRemainingPacket( fixedHeader, ms );
				return packet;
			}
		}

		private static async Task<MqttFixedHeader> ReceiveMqttFixedHeaderAsync(
				this ClientWebSocket socket,
				ArraySegment<byte> buffer,
				CancellationToken cancellationToken
			) {

			ArraySegment<byte> fixedHeader = new ArraySegment<byte>( buffer.Array, 0, 2 );

			WebSocketReceiveResult result = await socket
				.ReceiveAsync( fixedHeader, cancellationToken )
				.ConfigureAwait( continueOnCapturedContext: false );

			// end of stream
			if( result.Count == 0 ) {
				return null;
			}

			// only got the first byte of the fixed header
			if( result.Count == 1 ) {

				result = await socket
					.ReceiveAsync( new ArraySegment<byte>( buffer.Array, 1, 1 ), cancellationToken )
					.ConfigureAwait( continueOnCapturedContext: false );

				// end of stream
				if( result.Count == 0 ) {
					fixedHeader = new ArraySegment<byte>( buffer.Array, 0, 1 );
				}
			}

			using( MemoryStream ms = fixedHeader.AsReadableMemoryStream() ) {
				return MqttPacketReader.ReadFixedHeader( ms );
			}
		}

		public static async Task SendPacketAsync(
				this ClientWebSocket socket,
				MqttPacket packet,
				CancellationToken cancellationToken
			) {

			using( MemoryStream ms = new MemoryStream() ) {
				packet.WriteTo( ms );

				ArraySegment<byte> packetSegment = new ArraySegment<byte>(
						ms.GetBuffer(),
						0,
						(int)ms.Position
					);

				await socket.SendAsync(
						packetSegment,
						WebSocketMessageType.Binary,
						endOfMessage: true,
						cancellationToken: cancellationToken
					);
			}
		}

		private static MemoryStream AsReadableMemoryStream( this ArraySegment<byte> buffer ) {
			return new MemoryStream( buffer.Array, buffer.Offset, buffer.Count );
		}
	}
}
