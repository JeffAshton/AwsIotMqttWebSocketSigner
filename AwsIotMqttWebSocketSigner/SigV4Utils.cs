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

		private static StringBuilder AppendHex( this StringBuilder sb, byte[] bytes ) {
			for( int i = 0; i < bytes.Length; i++ ) {
				sb.AppendFormat( "{0:x2}", bytes[ i ] );
			}
			return sb;
		}

		private static StringBuilder AppendSha256Hex( this StringBuilder sb, string data ) {
			byte[] bytes = m_encoding.GetBytes( data );
			using( SHA256 algo = SHA256.Create() ) {
				byte[] hash = algo.ComputeHash( bytes );
				sb.AppendHex( hash );
			}
			return sb;
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

			StringBuilder canonicalQuerystring = new StringBuilder( 800 )
				.Append( "X-Amz-Algorithm=" )
				.Append( algorithm )
				.Append( "&X-Amz-Credential=" )
				.Append( WebUtility.UrlEncode( credentials.AccessKey ) )
				.Append( "%2F" )
				.Append( WebUtility.UrlEncode( credentialScope ) )
				.Append( "&X-Amz-Date=" )
				.Append( datetime )
				.Append( "&X-Amz-SignedHeaders=host" );

			string canonicalRequest = new StringBuilder( 350 )
				.Append( method )
				.Append( '\n' )
				.Append( uri )
				.Append( '\n' )
				.Append( canonicalQuerystring )
				.Append( "\nhost:" )
				.Append( host )
				.Append( "\n\nhost\n" )
				.AppendSha256Hex( String.Empty )
				.ToString();

			string stringToSign = new StringBuilder( 200 )
				.Append( algorithm )
				.Append( '\n' )
				.Append( datetime )
				.Append( '\n' )
				.Append( credentialScope )
				.Append( '\n' )
				.AppendSha256Hex( canonicalRequest )
				.ToString();

			byte[] signingKey = GetSignatureKey( credentials.SecretKey, date, region, service );
			byte[] signatureBytes = GetHmac( signingKey, stringToSign );

			canonicalQuerystring
				.Append( "&X-Amz-Signature=" )
				.AppendHex( signatureBytes );

			if( !String.IsNullOrEmpty( credentials.Token ) ) {

				canonicalQuerystring
					.Append( "&X-Amz-Security-Token=" )
					.Append( WebUtility.UrlEncode( credentials.Token ) );
			}

			string requestUrl = protocol + "://" + host + uri + "?" + canonicalQuerystring;
			return requestUrl;
		}
	}
}