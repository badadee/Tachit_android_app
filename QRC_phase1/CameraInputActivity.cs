
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

	[Activity (Label = "CameraInputActivity")]			
	public class CameraInputActivity : Activity
	{
		private Data cameraInputData{ get; set; }

		private UploadInfo cameraInputUploadinfo { get; set; }

		private Bitmap resultBitmap { get; set; }

		private File resultFile { get; set; }

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			//set layout
			SetContentView (Resource.Layout.CameraInput);


			if (IsThereAnAppToTakePictures ()) {
				CreateDirectoryForPictures ();

				Button addPictureBut_inCamInput = FindViewById<Button> (Resource.Id.addPictureBut_inCamInput);
				addPictureBut_inCamInput.Click += TakeAPicture;
			}
			Button _cameraInputDoneBut = FindViewById<Button> (Resource.Id.cameraInputDoneBut);
			_cameraInputDoneBut.Click += (s, arg) => {
				var intent = new Intent ();
//				Bundle mBundle = new Bundle ();
//				mBundle.PutParcelable ("Bitmap", resultBitmap);
				intent.PutExtra ("Data", JsonConvert.SerializeObject (cameraInputData));
				intent.PutExtra ("UploadInfo", JsonConvert.SerializeObject (cameraInputUploadinfo));
				intent.PutExtra ("FilePath", resultFile.Path);
				intent.PutExtra ("MediaSource", "CameraInputActivity");
				//intent.PutExtras (mBundle);
				SetResult (Result.Ok, intent);
				Finish ();
			};

		}

		private void TakeAPicture (object sender, EventArgs eventArgs)
		{
			Intent intent = new Intent (MediaStore.ActionImageCapture);
			Guid g = Guid.NewGuid ();
			resultFile = new File (App._dir, String.Format ("myPhoto_{0}.jpg", g));

			intent.PutExtra (MediaStore.ExtraOutput, Uri.FromFile (resultFile));

			cameraInputData = new Data (g.ToString (), "testing", "picture", "eddywang");
			cameraInputUploadinfo = new UploadInfo (g.ToString (), resultFile.Path, "");

			StartActivityForResult (intent, 0);
		}

		protected override void OnActivityResult (int requestCode, Result resultCode, Intent data)
		{
			base.OnActivityResult (requestCode, resultCode, data);

			// make it available in the gallery
			Intent mediaScanIntent = new Intent (Intent.ActionMediaScannerScanFile);
			Uri contentUri = Uri.FromFile (resultFile);
			mediaScanIntent.SetData (contentUri);
			SendBroadcast (mediaScanIntent);

			//get imageView first
			//ImageView imgView = new ImageView (this);
			//imgView.Id = View.GenerateViewId ();
			// display in ImageView. We will resize the bitmap to fit the display
			// Loading the full sized image will consume to much memory 
			// and cause the application to crash.
			int height = 83;
			int width = 83;
			resultBitmap = resultFile.Path.LoadAndResizeBitmap (width, height);
		}

		private bool IsThereAnAppToTakePictures ()
		{
			Intent intent = new Intent (MediaStore.ActionImageCapture);
			IList<ResolveInfo> availableActivities = PackageManager.QueryIntentActivities (intent, PackageInfoFlags.MatchDefaultOnly);
			return availableActivities != null && availableActivities.Count > 0;
		}

		private void CreateDirectoryForPictures ()
		{
			App._dir = new File (Android.OS.Environment.GetExternalStoragePublicDirectory (Android.OS.Environment.DirectoryPictures), "QRC_phase1");
			if (!App._dir.Exists ()) {
				App._dir.Mkdirs ();
			}
		}
	}
}

