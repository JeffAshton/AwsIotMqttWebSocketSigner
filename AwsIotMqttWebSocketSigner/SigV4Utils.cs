/***************************************************************************************
*    Title: AWS IoT MQTT Over the WebSocket Protocol
*    Author: Amazon Web Services, Inc.
*    Date: 2018-01-31
*    Availability: https://docs.aws.amazon.com/iot/latest/developerguide/protocols.html
*
***************************************************************************************/

using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Amazon.Runtime;

namespace AwsIotMqttWebSocketSigner {

	public static class SigV4Utils {

		private static readonly UTF8Encoding m_encoding = new UTF8Encoding( false );

		private static string AsHex( this byte[] bytes ) {

			StringBuilder sb = new StringBuilder( bytes.Length * 2 );
			for( int i = 0; i < bytes.Length; i++ ) {
				sb.AppendFormat( "{0:x2}", bytes[ i ] );
			}
			return sb.ToString();
		}

		private static string GetSha256( string data ) {

			using( SHA256 algo = SHA256.Create() ) {
				byte[] bytes = m_encoding.GetBytes( data );
				byte[] hash = algo.ComputeHash( bytes );
				return hash.AsHex();
			}
		}

		private static byte[] GetHmac( string key, string data ) {

			byte[] keyBytes = m_encoding.GetBytes( key );
			return GetHmac( keyBytes, data );
		}

		private static byte[] GetHmac( byte[] key, string data ) {

			using( HMACSHA256 kha = new HMACSHA256( key ) ) {
				byte[] dataBytes = m_encoding.GetBytes( data );
				byte[] hash = kha.ComputeHash( dataBytes );
				return hash;
			}
		}

		private static byte[] GetSignatureKey( string key, string date, string region, string service ) {

			byte[] kDate = GetHmac( "AWS4" + key, date );
			byte[] kRegion = GetHmac( kDate, region );
			byte[] kService = GetHmac( kRegion, service );
			byte[] kCredentials = GetHmac( kService, "aws4_request" );
			return kCredentials;
		}

		public static string GetSignedUri(
					string host,
					string region,
					ImmutableCredentials credentials
				) {

			return GetSignedUri(
				host: host,
				region: region,
				credentials: credentials,
				now: DateTime.UtcNow
			);
		}

		internal static string GetSignedUri(
				string host,
				string region,
				ImmutableCredentials credentials,
				DateTime now
			) {

			DateTime utcNow = now.ToUniversalTime();
			string datetime = utcNow.ToString( "yyyyMMddTHHmmssZ" );
			string date = datetime.Substring( 0, 8 );

			const string method = "GET";
			const string protocol = "wss";
			const string uri = "/mqtt";
			const string service = "iotdevicegateway";
			const string algorithm = "AWS4-HMAC-SHA256";

			string credentialScope = date + "/" + region + "/" + service + "/" + "aws4_request";
			string canonicalQuerystring = "X-Amz-Algorithm=" + algorithm;
			canonicalQuerystring += "&X-Amz-Credential=" + WebUtility.UrlEncode( credentials.AccessKey + "/" + credentialScope );
			canonicalQuerystring += "&X-Amz-Date=" + datetime;
			canonicalQuerystring += "&X-Amz-SignedHeaders=host";

			string canonicalHeaders = "host:" + host + "\n";
			string payloadHash = GetSha256( "" );
			string canonicalRequest = method + "\n" + uri + "\n" + canonicalQuerystring + "\n" + canonicalHeaders + "\nhost\n" + payloadHash;

			string stringToSign = algorithm + "\n" + datetime + "\n" + credentialScope + "\n" + GetSha256( canonicalRequest );
			byte[] signingKey = GetSignatureKey( credentials.SecretKey, date, region, service );
			string signature = GetHmac( signingKey, stringToSign ).AsHex();

			canonicalQuerystring += "&X-Amz-Signature=" + signature;
			if( !String.IsNullOrEmpty( credentials.Token ) ) {
				canonicalQuerystring += "&X-Amz-Security-Token=" + WebUtility.UrlEncode( credentials.Token );
			}

			string requestUrl = protocol + "://" + host + uri + "?" + canonicalQuerystring;
			return requestUrl;
		}
	}
}