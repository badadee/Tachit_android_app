
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
using Android.Media;


namespace QRC_phase1
{
	using Java.IO;

	[Activity (Label = "VoiceInputActivity")]			
	public class VoiceInputActivity : Activity
	{
		MediaRecorder _recorder;
		MediaPlayer _player;
		Button _startBut;
		Button _stopBut;
		Button _playBut;
		Button _doneBut;

		private Data voiceInputData{ get; set; }

		private UploadInfo voiceInputUploadinfo { get; set; }

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			SetContentView (Resource.Layout.VoiceInput);
			_startBut = FindViewById<Button> (Resource.Id.RecordBut_inVoiceInput);
			_stopBut = FindViewById<Button> (Resource.Id.StopRecordBut_inVoiceInput);
			_playBut = FindViewById<Button> (Resource.Id.PlayBut_inVoiceInput);
			_doneBut = FindViewById<Button> (Resource.Id.voiceInputDoneBut);
			Guid g = Guid.NewGuid ();
			Java.IO.File resultFile = new Java.IO.File (App._dir, String.Format ("myVoice_{0}.3gpp", g));

			_startBut.Click += delegate {
				_startBut.Enabled = false;
				_playBut.Enabled = false;
				_recorder.SetAudioSource (AudioSource.Mic);
				_recorder.SetOutputFormat (OutputFormat.ThreeGpp);
				_recorder.SetAudioEncoder (AudioEncoder.AmrNb);
				_recorder.SetOutputFile (resultFile.Path);
				_recorder.Prepare ();
				_recorder.Start ();
			};

			_stopBut.Click += delegate {
				_startBut.Enabled = true;
				_playBut.Enabled = true;
				_recorder.Stop ();
				_recorder.Reset ();
				_player.Stop ();
				_player.Reset ();
			};

			_playBut.Click += delegate {
				_startBut.Enabled = false;
				_playBut.Enabled = false;
				_player.SetDataSource (resultFile.Path);
				_player.Prepare ();
				_player.Start ();

			};

			_doneBut.Click += (s, arg) => {

				voiceInputData = new Data (g.ToString (), "voiceTesting", "sound", "eddywang");
				voiceInputUploadinfo = new UploadInfo (g.ToString (), resultFile.Path, "");
				var intent = new Intent ();
				intent.PutExtra ("Data", JsonConvert.SerializeObject (voiceInputData));
				intent.PutExtra ("UploadInfo", JsonConvert.SerializeObject (voiceInputUploadinfo));
				intent.PutExtra ("FilePath", resultFile.Path);
				intent.PutExtra ("MediaSource", "VoiceInputActivity");
				//intent.PutExtras (mBundle);
				SetResult (Result.Ok, intent);
				Finish ();

			};


		}

		protected override void OnResume ()
		{
			base.OnResume ();

			_recorder = new MediaRecorder ();
			_player = new MediaPlayer ();

			_player.Completion += (sender, e) => {
				_player.Reset ();
				_startBut.Enabled = !_startBut.Enabled;
			};

		}

		protected override void OnPause ()
		{
			base.OnPause ();

			_player.Release ();
			_recorder.Release ();
			_player.Dispose ();
			_recorder.Dispose ();
			_player = null;
			_recorder = null;
		}

		private void CreateDirectory ()
		{
			App._dir = new File (Android.OS.Environment.GetExternalStoragePublicDirectory (Android.OS.Environment.DirectoryPictures), "QRC_phase1");
			if (!App._dir.Exists ()) {
				App._dir.Mkdirs ();
			}
		}
	}
}

