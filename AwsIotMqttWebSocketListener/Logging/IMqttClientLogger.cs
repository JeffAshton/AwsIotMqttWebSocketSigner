using System;

namespace AwsIotMqttWebSocketListener.Logging {

	public interface IMqttClientLogger {
		void Error( string message, Exception err );
	}
}
