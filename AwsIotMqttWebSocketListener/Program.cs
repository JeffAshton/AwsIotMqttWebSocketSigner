using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using AwsIotMqttWebSocketListener.Logging;
using AwsIotMqttWebSocketListener.Sessions;
using D2L.MQTT.Packets;

namespace AwsIotMqttWebSocketListener {

	internal static class Program {

		internal static void Main( string[] args ) {

			Stopwatch watch = Stopwatch.StartNew();

			try {
				MainAsync( args ).GetAwaiter().GetResult();
			} catch( Exception err ) {
				Console.Error.WriteLine( err.ToString() );
				Console.Out.WriteLine( watch.ToString() );
			}
		}

		private static async Task MainAsync( string[] args ) {

			string endpointAddress = args[ 0 ];
			string region = args[ 1 ];
			AWSCredentials credentails = FallbackCredentialsFactory.GetCredentials();

			Console.WriteLine( "Enter \"DISCONNECT\" to exit" );

			Action<MqttMessageEventArgs> messageHandler = ( message ) => {
				string str = Encoding.UTF8.GetString( message.Message );
				Console.WriteLine( "MESSAGE: {0}", str );
			};

			using( ConnectResponse response = await AwsIotMqttWebSocketClient.ConnectAsync(
					endpoint: endpointAddress,
					region: region,
					credentials: credentails,
					messageHandler: messageHandler,
					logger: NullMqttClientLogger.Instance,
					cancellationToken: CancellationToken.None
				) ) {

				if( response.ReturnCode == ConnectReturnCode.ConnectionAccepted ) {

					Console.WriteLine(
							"Connected to {0}",
							endpointAddress
						);

					await RunAsync( response.Session );

				} else {

					Console.WriteLine(
							"Failed to connect to {0}: {1}",
							endpointAddress,
							response.ReturnCode
						);
				}
			}
		}

		private static async Task RunAsync( IMqttSession session ) {

			using( CancellationTokenSource cancellation = new CancellationTokenSource() ) {

				Task task = session.RunAsync( CancellationToken.None );
				try {

					for(; ; ) {

						string line = Console.ReadLine();
						if( line.Length > 0 ) {

							string[] parts = line.Split( ' ' );
							switch( parts[ 0 ] ) {

								case "PUBLISH":

									if( parts.Length != 3 ) {
										Console.WriteLine( "PUBLISH requires 2 arguments" );
										break;
									}

									await session.PublishAsync(
											topic: parts[ 1 ],
											message: Encoding.UTF8.GetBytes( parts[ 2 ] ),
											qos: QualityOfService.QoS0,
											cancellationToken: CancellationToken.None
										);
									break;

								case "SUBSCRIBE":

									if( parts.Length != 2 ) {
										Console.WriteLine( "SUBSCRIBE requires 1 argument" );
										break;
									}

									await session.SubscribeAsync(
											subscriptions: new[] {
												new Subscription(
													topicFilter: parts[ 1 ],
													qos: QualityOfService.QoS0
												)
											},
											cancellationToken: CancellationToken.None
										);

									break;

								case "DISCONNECT":
									return;

								default:
									Console.WriteLine( "Unknown command: {0}", parts[ 0 ] );
									break;
							}
						}
					}

				} finally {
					cancellation.Cancel();
				}
			}
		}
	}
}
