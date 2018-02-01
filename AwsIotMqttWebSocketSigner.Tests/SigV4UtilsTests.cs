using System;
using System.Net;
using Amazon.Runtime;
using NUnit.Framework;

namespace AwsIotMqttWebSocketSigner.Tests {

	[TestFixture]
	public sealed class SigV4UtilsTests {

		[Test]
		public void GetSignedUri() {

			DateTime now = DateTime.Parse( "2018-02-01T01:34:16Z" );

			string url = SigV4Utils.GetSignedUri(
				host: "hhhhhhhhhhhhh.iot.us-east-1.amazonaws.com",
				region: "us-east-1",
				credentials: new ImmutableCredentials(
					awsAccessKeyId: "AAAAAAAAAA/AAAAAAAAA",
					awsSecretAccessKey: "KKKKKKKKKKKKKKKKKKK/KKKKKKKKKKKKKKKKKKKK",
					token: "FQoDYXdzECIaDPbKjb+bjsordZDleyKyAqJkCdi62dXvhNApoGILpqj0IV1zqKwxgRApCX7dOY2078lk/1KSSOEOiiFy997CuhohkS0tsatZ5hHoxu/gHQlZWB3yLTgKrdhtXWAIuPRyomSxjr81jAi6VDN0wGJIsncISpDzKbvYlgVPhlME3k7J+eS7ozprgSrEgeA8Kuqjuo/OxFzdwe2P1ptbOGF1DMk4Wa89aUUD07Of7pQUUQENI2W+y6Ik/jZLxbiYp5aY7yTiN0PI25ej4yYIZ3Rfh2b+91ZwaAN6ZNBzggbtBsW55qs0xYsyiWT0ovIXJag2rqmAq3PMlAbGj7Ss4Wweh1zXqm23SeKuiIZ7bpeM8jKtOdY4Y0L9DUCojXU997niVDC34yWFLwYGPyGSN8iUOUCetRla+9A80luWZl6d+VGHqijxyMnTBQ=="
				),
				now: now
			);

			Assert.AreEqual(
				"wss://hhhhhhhhhhhhh.iot.us-east-1.amazonaws.com/mqtt?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=AAAAAAAAAA%2FAAAAAAAAA%2F20180201%2Fus-east-1%2Fiotdevicegateway%2Faws4_request&X-Amz-Date=20180201T013416Z&X-Amz-SignedHeaders=host&X-Amz-Signature=001ce951660ebfc126c535407643aac7a183600e54e6923194e38225931461e8&X-Amz-Security-Token=FQoDYXdzECIaDPbKjb%2BbjsordZDleyKyAqJkCdi62dXvhNApoGILpqj0IV1zqKwxgRApCX7dOY2078lk%2F1KSSOEOiiFy997CuhohkS0tsatZ5hHoxu%2FgHQlZWB3yLTgKrdhtXWAIuPRyomSxjr81jAi6VDN0wGJIsncISpDzKbvYlgVPhlME3k7J%2BeS7ozprgSrEgeA8Kuqjuo%2FOxFzdwe2P1ptbOGF1DMk4Wa89aUUD07Of7pQUUQENI2W%2By6Ik%2FjZLxbiYp5aY7yTiN0PI25ej4yYIZ3Rfh2b%2B91ZwaAN6ZNBzggbtBsW55qs0xYsyiWT0ovIXJag2rqmAq3PMlAbGj7Ss4Wweh1zXqm23SeKuiIZ7bpeM8jKtOdY4Y0L9DUCojXU997niVDC34yWFLwYGPyGSN8iUOUCetRla%2B9A80luWZl6d%2BVGHqijxyMnTBQ%3D%3D",
				url
			);
		}
	}
}
