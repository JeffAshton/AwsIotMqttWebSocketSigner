using System;

namespace AwsIotMqttWebSocketListener.Logging {

	internal sealed class NullMqttClientLogger : IMqttClientLogger {

		public static readonly IMqttClientLogger Instance = new NullMqttClientLogger();

		private NullMqttClientLogger() {
		}

		void IMqttClientLogger.Error( string message, Exception err ) {
		}
	}
}
