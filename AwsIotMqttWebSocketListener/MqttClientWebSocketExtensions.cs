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

			if( fixedHeader.RemainingLength < buffer.Count ) {

				ArraySegment<byte> packetBuffer = new ArraySegment<byte>( buffer.Array, 0, fixedHeader.RemainingLength );
				await socket
					.ReceiveRemainingPacketAsync( fixedHeader.PacketType, packetBuffer, cancellationToken )
					.ConfigureAwait( continueOnCapturedContext: false );

				using( MemoryStream ms = packetBuffer.AsReadableMemoryStream() ) {

					MqttPacket packet = MqttPacketReader.ReadRemainingPacket( fixedHeader, ms );
					return packet;
				}
			}

			throw new NotImplementedException( "Packet too big for now" );
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

		private static async Task ReceiveRemainingPacketAsync(
				this ClientWebSocket socket,
				PacketType packetType,
				ArraySegment<byte> packet,
				CancellationToken cancellationToken
			) {

			int count = 0;

			while( count < packet.Count ) {

				ArraySegment<byte> segment = new ArraySegment<byte>(
						packet.Array,
						offset: count,
						count: packet.Count - count
					);

				WebSocketReceiveResult result = await socket
					.ReceiveAsync( segment, cancellationToken )
					.ConfigureAwait( continueOnCapturedContext: false );

				// end of stream
				if( result.Count == 0 ) {

					throw new PacketFormatException(
							packetType,
							$"Incomplete packet. Only received { count } of { packet.Count } bytes."
						);
				}

				count += result.Count;
			}
		}

		public static async Task SendPacketAsync(
				this ClientWebSocket socket,
				MqttPacket packet,
				ArraySegment<byte> buffer,
				CancellationToken cancellationToken
			) {

			using( MemoryStream ms = new MemoryStream( buffer.Array, buffer.Offset, buffer.Count, writable: true ) ) {
				packet.WriteTo( ms );

				ArraySegment<byte> packetSegment = new ArraySegment<byte>(
						buffer.Array,
						buffer.Offset,
						(int)ms.Position - buffer.Offset
					);

				await socket.SendAsync(
						packetSegment,
						WebSocketMessageType.Binary,
						endOfMessage: false,
						cancellationToken: cancellationToken
					);
			}
		}

		private static MemoryStream AsReadableMemoryStream( this ArraySegment<byte> buffer ) {
			return new MemoryStream( buffer.Array, buffer.Offset, buffer.Count );
		}
	}
}
