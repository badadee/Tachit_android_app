
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.IO;
using Android.Graphics;
using Android.Provider;
using Android.Content.PM;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Json;
using Android.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace QRC_phase1
{
	using Java.IO;

	using Environment = Android.OS.Environment;
	using Uri = Android.Net.Uri;

	public static class App
	{
		public static File _file;
		public static File _dir;
		public static Bitmap bitmap;
	}

	public class Data
	{
		public string name { get; set; }

		public string description { get; set; }

		public string media_type { get; set; }

		public string user_name { get; set; }

		public Data (string n, string d, string mt, string un)
		{
			this.name = n;
			this.description = "testing";
			this.media_type = mt;
			this.user_name = "eddywang";
		}

	}

	public class UploadContainer
	{
		public string link_url { get; set; }

		public List<Data> data { get; set; }

		public UploadContainer (string link_url, List<Data> dataList)
		{
			this.link_url = link_url;
			this.data = dataList;
		}
	}

	public class UploadInfo
	{
		public string name { get; set; }

		public string uploadFilePath { get; set; }

		public string presignedUploadURL { get; set; }

		public UploadInfo (string name, string uploadFilePath, string presignedUploadURL)
		{
			this.name = name;
			this.uploadFilePath = uploadFilePath;
			this.presignedUploadURL = presignedUploadURL;
		}
	}

	public class PostResult
	{
		public string presignedUploadURL{ get; set; }

		public string name { get; set; }

		public PostResult (string presignedURL, string name)
		{
			this.presignedUploadURL = presignedURL;
			this.name = name;
		}
	}



	[Activity (Label = "MessageInputActivity", Theme = "@android:style/Theme.Holo.Light")]			
	public class MessageInputActivity : Activity
	{
		private List<int> imageViewIdList = new List<int> ();
		//just for imageViews dude

		private List<Data> dataList = new List<Data> ();
		//all the data here will be uploaded
		private List<UploadInfo> uploadInfoList = new List<UploadInfo> ();
		private Button _doneButton;
		private String amazonPostUrl;
		private String uploadFilePath;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			//set layout
			SetContentView (Resource.Layout.MessageInputLayout);

			//set UI elements
			var scannedResultTextBox = FindViewById<EditText> (Resource.Id.scannedResultId);
			var addPictureButton = FindViewById<Button> (Resource.Id.addPictureBut);
			_doneButton = FindViewById<Button> (Resource.Id.DoneBut);

			//get Intent Extra~
			var scannedResult = Intent.GetStringExtra ("ScannedString");
			amazonPostUrl = Intent.GetStringExtra ("AmazonPostUrl");
			//display Intent Extra content 
			scannedResultTextBox.Text = scannedResult;


			//camera stuff
			if (IsThereAnAppToTakePictures ()) {
				CreateDirectoryForPictures ();

				Button button = FindViewById<Button> (Resource.Id.addPictureBut);
//				if (App.bitmap != null) {
//					newImageView.SetImageBitmap (App.bitmap);
//					App.bitmap = null;
//				}
				button.Click += TakeAPicture;

			}
			_doneButton.Click += async delegate(object sender, EventArgs e) {
				string result = await HttpPost (
					                "http://tachitnow.com/api/link",
					                ConstructRequestArrayFromData () //JsonObject
				                );
				JObject resultJson = JObject.Parse (result);
				IList<JToken> resultList = resultJson ["result"].ToList ();
				foreach (JToken item in resultList) {
					PostResult postResult = JsonConvert.DeserializeObject<PostResult> (item.ToString ());
					foreach (UploadInfo u in uploadInfoList) {
						if (u.name.Equals (postResult.name)) {
							UploadObject (postResult.presignedUploadURL, u.uploadFilePath);
						}
					}

				}

				HandleScanResult ("upload success");
				var uri = Android.Net.Uri.Parse ("http://tachitnow.com/1126021");
				var intent = new Intent (Intent.ActionView, uri); 
				StartActivity (intent);  
			};
//			_doneButton.Click += (object sender, EventArgs e) => {
//
//				var intent = new Intent (this, typeof(FinishActivity));
//				StartActivity (intent);
//			};

		}

		private string ConstructRequestArrayFromData ()
		{
//			JsonObject JO = new JsonObject ();
//			JO.Add ("link_url", "1126001");
//			JO.Add ("data", JsonConvert.SerializeObject (dataList));
			UploadContainer UC = new UploadContainer ("1126021", dataList);
			string ret = JsonConvert.SerializeObject (UC);
			System.Console.WriteLine (ret);
			return ret;
		}

		private void AddNewImageView (int height, int width)
		{
			//make new imageView
			ImageView newImageView = new ImageView (this);
			newImageView.Id = View.GenerateViewId ();

			//put it in the layout
			LinearLayout layoutForScroll = FindViewById<LinearLayout> (Resource.Id.layoutForScroll);
			layoutForScroll.AddView (newImageView);
			LinearLayout.LayoutParams ll = new LinearLayout.LayoutParams (width, height);
			ll.SetMargins (0, 10, 0, 10);
			newImageView.LayoutParameters = ll;
			imageViewIdList.Add (newImageView.Id);
		}

		protected override void OnActivityResult (int requestCode, Result resultCode, Intent data)
		{
			base.OnActivityResult (requestCode, resultCode, data);

			// make it available in the gallery
			Intent mediaScanIntent = new Intent (Intent.ActionMediaScannerScanFile);
			Uri contentUri = Uri.FromFile (App._file);
			mediaScanIntent.SetData (contentUri);
			SendBroadcast (mediaScanIntent);

			//get imageView first
			ImageView imgView = FindViewById<ImageView> (imageViewIdList.Last ());
			// display in ImageView. We will resize the bitmap to fit the display
			// Loading the full sized image will consume to much memory 
			// and cause the application to crash.
			int height = Resources.DisplayMetrics.HeightPixels;
			int width = imgView.Height;
			App.bitmap = App._file.Path.LoadAndResizeBitmap (width, height);
			imgView.SetImageBitmap (App.bitmap);
			_doneButton.Enabled = true;
			uploadFilePath = App._file.Path;
		}

		private void TakeAPicture (object sender, EventArgs eventArgs)
		{
			Intent intent = new Intent (MediaStore.ActionImageCapture);
			Guid g = Guid.NewGuid ();
			App._file = new File (App._dir, String.Format ("myPhoto_{0}.jpg", g));

			intent.PutExtra (MediaStore.ExtraOutput, Uri.FromFile (App._file));

			//IMPORTANT SHIT
			AddNewImageView (200, 300);
			//IMPORTANT SHIT2
			dataList.Add (new Data (g.ToString (), "testing", "picture", "eddywang"));
			uploadInfoList.Add (new UploadInfo (g.ToString (), App._file.Path, ""));

			StartActivityForResult (intent, 0);
		}

		static void UploadObject (string url, string filePath)
		{
			HttpWebRequest httpRequest = WebRequest.Create (url) as HttpWebRequest;
			httpRequest.Method = "PUT";
			using (Stream dataStream = httpRequest.GetRequestStream ()) {
				byte[] buffer = new byte[8000];
				using (FileStream fileStream = new FileStream (filePath, FileMode.Open, FileAccess.Read)) {
					int bytesRead = 0;
					while ((bytesRead = fileStream.Read (buffer, 0, buffer.Length)) > 0) {
						dataStream.Write (buffer, 0, bytesRead);
					}
				}
			}

			HttpWebResponse response = httpRequest.GetResponse () as HttpWebResponse;
		}

		private static async Task<string> HttpPost (string url, string jsonString)
		{
			HttpWebRequest req = WebRequest.Create (new System.Uri (url)) 
				as HttpWebRequest;
			req.Method = "POST";  
			req.ContentType = "application/json";

			// Build a string with all the params, properly encoded.
			// We assume that the arrays paramName and paramVal are
			// of equal length:
//			StringBuilder paramz = new StringBuilder ();
//			for (int i = 0; i < paramName.Length; i++) {
//				paramz.Append (paramName [i]);
//				paramz.Append ("=");
//				paramz.Append (WebUtility.UrlEncode (paramVal [i]));
//				paramz.Append ("&");
//			}

			// Encode the parameters as form data:
			byte[] formData =
				UTF8Encoding.UTF8.GetBytes (jsonString);
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


		private bool IsThereAnAppToTakePictures ()
		{
			Intent intent = new Intent (MediaStore.ActionImageCapture);
			IList<ResolveInfo> availableActivities = PackageManager.QueryIntentActivities (intent, PackageInfoFlags.MatchDefaultOnly);
			return availableActivities != null && availableActivities.Count > 0;
		}

		private void CreateDirectoryForPictures ()
		{
			App._dir = new File (Environment.GetExternalStoragePublicDirectory (Environment.DirectoryPictures), "QRC_phase1");
			if (!App._dir.Exists ()) {
				App._dir.Mkdirs ();
			}
		}



		void HandleScanResult (string result)
		{
			string msg = "";

			if (result != null && !string.IsNullOrEmpty (result))
				msg = "Found Barcode: " + result;
			else
				msg = "Scanning Canceled!";

			this.RunOnUiThread (() => Toast.MakeText (this, msg, ToastLength.Short).Show ());
		}

		private int PixelsToDp (int pixels)
		{
			return (int)TypedValue.ApplyDimension (ComplexUnitType.Dip, pixels, Resources.DisplayMetrics);
		}
	}


}

