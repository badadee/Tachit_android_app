using System;
using System.Collections.Generic;
using Android.App;

using Android.Content.PM;
using Android.Views;
using Android.Widget;
using Android.OS;
using ZXing;
using ZXing.Mobile;
using Android.Content;
using System.Json;
using System.Threading.Tasks;
using System.Net;
using System.IO;


using System.Text;

namespace QRC_phase1
{
	[Activity (Label = "QRC_phase1", MainLauncher = true, Theme = "@android:style/Theme.Holo.Light", Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		Button buttonScanDefaultView;
		MobileBarcodeScanner scanner;


		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			//Create a new instance of our Scanner
			scanner = new MobileBarcodeScanner ();

			// Get our button from the layout resource,
			// and attach an event to it
			buttonScanDefaultView = FindViewById<Button> (Resource.Id.buttonScanDefaultView);

			buttonScanDefaultView.Click += async delegate {
				//Tell our scanner to use the default overlay
				scanner.UseCustomOverlay = false;
				//We can customize the top and bottom text of the default overlay
				scanner.TopText = "Hold the camera up to the barcode\nAbout 6 inches away";
				scanner.BottomText = "Wait for the barcode to automatically scan!";

				//Start scanning

				var result = await scanner.Scan ();
				var intent = new Intent (this, typeof(MessageInputActivity));
				if (result != null) {
					intent.PutExtra ("ScannedString", result.ToString ());
				}
				StartActivity (intent);
			};
		}

		private async Task<JsonValue> FetchResultAsync (string url)
		{
			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create (new Uri (url));
			request.ContentType = "application/json";
			request.Method = "GET";

			// Send the request to the server and wait for the response:
			using (WebResponse response = await request.GetResponseAsync ()) {
				// Get a stream representation of the HTTP web response:
				using (Stream stream = response.GetResponseStream ()) {
					// Use this stream to build a JSON document object:
					JsonValue jsonDoc = await Task.Run (() => JsonObject.Load (stream));
					Console.Out.WriteLine ("Response: {0}", jsonDoc.ToString ());

					// Return the JSON document:
					return jsonDoc;
				}
			}
		}

		private static async Task<string> HttpPost (string url, 
		                                            string[] paramName, string[] paramVal)
		{
			HttpWebRequest req = WebRequest.Create (new System.Uri (url)) 
				as HttpWebRequest;
			req.Method = "POST";  
			req.ContentType = "application/x-www-form-urlencoded";

			// Build a string with all the params, properly encoded.
			// We assume that the arrays paramName and paramVal are
			// of equal length:
			StringBuilder paramz = new StringBuilder ();
			for (int i = 0; i < paramName.Length; i++) {
				paramz.Append (paramName [i]);
				paramz.Append ("=");
				paramz.Append (WebUtility.UrlEncode (paramVal [i]));
				paramz.Append ("&");
			}

			// Encode the parameters as form data:
			byte[] formData =
				UTF8Encoding.UTF8.GetBytes (paramz.ToString ());
			req.ContentLength = formData.Length;

			// Send the request:
			using (Stream post = req.GetRequestStream ()) {  
				post.Write (formData, 0, formData.Length);  
			}
			string result = null;
			// Pick up the response:
			using (HttpWebResponse resp = await req.GetResponseAsync ()	as HttpWebResponse) {  
				StreamReader reader = 
					new StreamReader (resp.GetResponseStream ());
				result = reader.ReadToEnd ();
				return result;

			}


		}


	}
}


