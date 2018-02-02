using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using AwsIotMqttWebSocketSigner;
using D2L.MQTT.Packets;

namespace AwsIotMqttWebSocketListener {

	internal static class Program {

		internal static void Main( string[] args ) {

			try {
				string endpointAddress = args[ 0 ];
				string region = args[ 1 ];
				AWSCredentials credentails = FallbackCredentialsFactory.GetCredentials();

				Console.WriteLine( "Press <enter> to exit" );

				using( AwsIotMqttWebSocketClient client = new AwsIotMqttWebSocketClient(
						endpointAddress,
						region,
						credentails
					) ) {



					Console.ReadLine();
				}

			} catch( TaskCanceledException ) {

			} catch( Exception err ) {
				Console.Error.WriteLine( err.ToString() );
			}
		}

	}
}
