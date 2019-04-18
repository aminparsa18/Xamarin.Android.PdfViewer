using Android.App;
using Android.Content;
using Android.Widget;
using Android.OS;
using Android.Provider;
using StringBuilder = System.Text.StringBuilder;
using PdfViewer;
using Android.Support.V7.App;
using PdfViewer.Listener;
using Android.Support.V4.Content;
using Android.Support.V4.App;
using PdfViewer.Scroll;
using PdfViewer.Util;
using Android.Runtime;
using PdfViewer.PDFium;
using Android.Util;
using System.Collections.Generic;
using Android.Content.PM;
using Uri = Android.Net.Uri;

namespace PdfTest
{
    [Activity(Label = "PdfTest", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : AppCompatActivity
    {
        private static readonly string Tag = typeof(MainActivity).Name;

        private static readonly int RequestCode = 42;
        public static readonly int PermissionCode = 42042;

        public static readonly string SampleFile = "sample.pdf";
        public static readonly string ReadExternalStorage = "android.permission.READ_EXTERNAL_STORAGE";

        private PdfView pdfView;
        private Uri uri;
        private int pageNumber;

        private string pdfFileName;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.activity_main);
            pdfView = FindViewById<PdfView>(Resource.Id.pdfView);
            DisplayFromAsset("sample.pdf");
        }

        private void PickFile()
        {
            var permissionCheck = ContextCompat.CheckSelfPermission(this,
                ReadExternalStorage);

            if (permissionCheck != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(
                    this,
                    new[] {ReadExternalStorage},
                    PermissionCode
                );

                return;
            }

            LaunchPicker();
        }

        private void LaunchPicker()
        {
            var intent = new Intent(Intent.ActionGetContent);
            intent.SetType("application/pdf");
            try
            {
                StartActivityForResult(intent, RequestCode);
            }
            catch (ActivityNotFoundException)
            {
                //alert user that file manager not working
                Toast.MakeText(this, Resource.String.toast_pick_file_error, ToastLength.Short).Show();
            }
        }

        private void DisplayFromAsset(string assetFileName)
        {
            pdfFileName = assetFileName;

            pdfView.FromAsset(SampleFile)
                .DefaultPage(pageNumber)
                .SetOnPageChanged((s, e) =>
                {
                    pageNumber = e.Page;
                    Title = new StringBuilder(pdfFileName).Append(" ").Append(e.Page + 1).Append("/")
                        .Append(e.PageCount).ToString();
                })
                .EnableAnnotationRendering(true)
                .SetOnLoad(LoadCompleted)
                .ScrollHandle(new DefaultScrollHandle(this))
                .Spacing(10) // in dp
                .SetOnPageError((s, e) => { Log.Error(Tag, "Cannot load page " + e.Page); })
                .SetPageFitPolicy(FitPolicy.Both)
                .Load();
        }


        private void DisplayFromUri(Uri uri)
        {
            pdfFileName = GetFileName(uri);

            pdfView.FromUri(uri)
                .DefaultPage(pageNumber)
                .SetOnPageChanged((s, e) =>
                {
                    pageNumber = e.Page;
                    Title = new StringBuilder(pdfFileName).Append(" ").Append(e.Page + 1).Append("/")
                        .Append(e.PageCount).ToString();
                })
                .EnableAnnotationRendering(true)
                .SetOnLoad(LoadCompleted)
                .ScrollHandle(new DefaultScrollHandle(this))
                .Spacing(10) // in dp
                .SetOnPageError((s, e) => { Log.Error(Tag, "Cannot load page " + e.Page); })
                .Load();
        }

        private void LoadCompleted(object sender, LoadCompletedEventArgs e)
        {
            var meta = pdfView.DocumentMeta;
            Log.Error(Tag, "title = " + meta.Title);
            Log.Error(Tag, "author = " + meta.Author);
            Log.Error(Tag, "subject = " + meta.Subject);
            Log.Error(Tag, "keywords = " + meta.Keywords);
            Log.Error(Tag, "creator = " + meta.Creator);
            Log.Error(Tag, "producer = " + meta.Producer);
            Log.Error(Tag, "creationDate = " + meta.CreationDate);
            Log.Error(Tag, "modDate = " + meta.ModDate);
            PrintBookmarksTree(pdfView.TableOfContents, "-");
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            if (resultCode != Result.Ok) return;
            uri = data.Data;
            DisplayFromUri(uri);
        }

        public string GetFileName(Uri uri)
        {
            {
                string result = null;
                if (uri.Scheme.Equals("content"))
                {
                    var cursor = ContentResolver.Query(uri, null, null, null, null);
                    try
                    {
                        if (cursor != null && cursor.MoveToFirst())
                        {
                            result = cursor.GetString(cursor.GetColumnIndex(OpenableColumns.DisplayName));
                        }
                    }
                    finally
                    {
                        cursor?.Close();
                    }
                }

                return result ?? uri.LastPathSegment;
            }
        }

        public void PrintBookmarksTree(List<PdfDocument.Bookmark> tree, string sep)
        {
            foreach (var b in tree)
            {
                Log.Error(Tag, string.Format("%s %s, p %d", sep, b.Title, b.PageIdx));

                if (b.HasChildren)
                {
                    PrintBookmarksTree(b.Children, sep + "-");
                }
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions,
            [GeneratedEnum] Permission[] grantResults)
        {
            if (requestCode != 0) return;
            if (grantResults.Length > 0
                && grantResults[0] == Permission.Granted)
            {
                DisplayFromAsset("sample.pdf");
            }
        }
    }
}