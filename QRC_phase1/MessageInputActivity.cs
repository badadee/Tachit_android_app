
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
		private String testID;

		List<TableItem> tableItems = new List<TableItem> ();
		ListView listView;



		protected override void OnStop ()
		{
			base.OnStop ();
		}

		protected override void OnPause ()
		{
			base.OnPause ();
		}

		protected override void OnDestroy ()
		{
			base.OnDestroy ();
		}

		protected override void OnResume ()
		{
			base.OnResume ();
		}

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			//set layout
			SetContentView (Resource.Layout.MessageInputLayout);

			//set UI elements
			var scannedResultTextBox = FindViewById<EditText> (Resource.Id.scannedResultId);
			var addPictureButton = FindViewById<Button> (Resource.Id.addPictureBut);
			_doneButton = FindViewById<Button> (Resource.Id.DoneBut);
			listView = FindViewById<ListView> (Resource.Id.List);
			if (listView.Adapter == null) {
				listView.Adapter = new MessageInputAdapter (this, tableItems);
			}
			//get Intent Extra~
			var scannedResult = Intent.GetStringExtra ("ScannedString");
			amazonPostUrl = Intent.GetStringExtra ("AmazonPostUrl");
			IntentProcessor (Intent);

			//display Intent Extra content 
			scannedResultTextBox.Text = scannedResult;


			//camera stuff
			if (IsThereAnAppToTakePictures ()) {
				CreateDirectoryForPictures ();

				Button _addPhotoButton = FindViewById<Button> (Resource.Id.addPictureBut);
				_addPhotoButton.Click += TakeAPicture;

			}
			//add media - spawn menu
			Button _addMediaButton = FindViewById<Button> (Resource.Id.addMediaBut);

			_addMediaButton.Click += (s, arg) => {

				PopupMenu menu = new PopupMenu (this, _addMediaButton);

				menu.Inflate (Resource.Menu.popup_menu_media_select);

				menu.MenuItemClick += (s1, arg1) => {
					System.Console.WriteLine ("{0} selected", arg1.Item.TitleFormatted);

					switch (arg1.Item.TitleFormatted.ToString ()) {
					case "Photo":
						StartActivityForResult (new Intent (this, typeof(CameraInputActivity)), 69);
						break;
					case "Voice":
						StartActivityForResult (new Intent (this, typeof(VoiceInputActivity)), 70);
						break;
					default:
						break;
					
					}
				};

				// Android 4 now has the DismissEvent
				menu.DismissEvent += (s2, arg2) => {
					System.Console.WriteLine ("menu dismissed"); 
				};

				menu.Show ();
			};
			testID = "1156999";
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
				var uri = Android.Net.Uri.Parse ("http://tachitnow.com/" + testID);
				var intent = new Intent (Intent.ActionView, uri); 
				StartActivity (intent);  
			};

		}

		private string ConstructRequestArrayFromData ()
		{
			UploadContainer UC = new UploadContainer (testID, dataList);
			string ret = JsonConvert.SerializeObject (UC);
			System.Console.WriteLine (ret);
			return ret;
		}

		protected override void OnActivityResult (int requestCode, Result resultCode, Intent data)
		{
			base.OnActivityResult (requestCode, resultCode, data);

			// make it available in the gallery
//			Intent mediaScanIntent = new Intent (Intent.ActionMediaScannerScanFile);
//			Uri contentUri = Uri.FromFile (App._file);
//			mediaScanIntent.SetData (contentUri);
//			SendBroadcast (mediaScanIntent);

			//get imageView first
			//ImageView imgView = new ImageView (this);
			//imgView.Id = View.GenerateViewId ();
			// display in ImageView. We will resize the bitmap to fit the display
			// Loading the full sized image will consume to much memory 
			// and cause the application to crash.
			//int height = 83;
			//int width = 83;
			//App.bitmap = App._file.Path.LoadAndResizeBitmap (width, height);

			if (requestCode == 69) {
				IntentProcessor (data);
			} else {

				((MessageInputAdapter)listView.Adapter).Add (new TableItem () {
					Heading = "TEST",
					SubHeading = "TEST2",
					Image = App.bitmap
				});
				((MessageInputAdapter)listView.Adapter).NotifyDataSetChanged ();
			}

		}

		protected void IntentProcessor (Intent intent)
		{
			if (intent == null || intent.GetStringExtra ("MediaSource") == null)
				return;
			
			string returnedMediaSource = intent.GetStringExtra ("MediaSource");
			Data returnedData = JsonConvert.DeserializeObject<Data> (intent.GetStringExtra ("Data"));
			UploadInfo returnedUploadInfo = JsonConvert.DeserializeObject<UploadInfo> (intent.GetStringExtra ("UploadInfo"));
			String returnedResultFilePath = intent.GetStringExtra ("FilePath");
			if (returnedData != null & returnedUploadInfo != null & returnedResultFilePath != null) {
				
				dataList.Add (returnedData);
				uploadInfoList.Add (returnedUploadInfo);
				switch (returnedMediaSource) {
				case "CameraInputActivity":
					((MessageInputAdapter)listView.Adapter).Add (new TableItem () {
						Heading = "PICTURE",
						SubHeading = ("Name: " + returnedData.name),
						Image = returnedResultFilePath.LoadAndResizeBitmap (83, 83)
					});
					((MessageInputAdapter)listView.Adapter).NotifyDataSetChanged ();
							
					break;
				case "voiceInputActivity":
						
					((MessageInputAdapter)listView.Adapter).Add (new TableItem () {
						Heading = "PICTURE",
						SubHeading = ("Name: " + returnedData.name),
						Image = null
					});
					((MessageInputAdapter)listView.Adapter).NotifyDataSetChanged ();
							
					break;
				default :
					System.Console.WriteLine ("nothing returned dude!");
					break;

				}

			}
			return;

		}

		private void TakeAPicture (object sender, EventArgs eventArgs)
		{
			Intent intent = new Intent (MediaStore.ActionImageCapture);
			Guid g = Guid.NewGuid ();
			App._file = new File (App._dir, String.Format ("myPhoto_{0}.jpg", g));

			intent.PutExtra (MediaStore.ExtraOutput, Uri.FromFile (App._file));

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

