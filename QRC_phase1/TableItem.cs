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
using Android.Graphics;

namespace QRC_phase1
{
	public class TableItem
	{
		public string Heading { get; set; }

		public string SubHeading { get; set; }

		public Bitmap Image { get; set; }
	}
}