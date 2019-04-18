using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Java.IO;
using Java.Lang;
using Size= PdfViewer.PDFium.Utils.Size;
using Android.Util;
using Java.Util;
using Object = Java.Lang.Object;

namespace PdfViewer.PDFium
{
    public class PdfiumCore
    {
        private static readonly string Tag = typeof(PdfiumCore).Name;
        private static readonly Class FdClass = Class.FromType(typeof(FileDescriptor));
        private const string FdFieldName = "descriptor";

        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeOpenDocument")]
        private static extern IntPtr OpenDocument(IntPtr env,IntPtr thiz, int fd, string password);

        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeOpenMemDocument")]
        private static extern IntPtr OpenMemDocument(IntPtr env, IntPtr thiz, byte[] data, string password);

        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeCloseDocument")]
        private static extern void CloseDocument(IntPtr env, IntPtr thiz, IntPtr docPtr);

        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeGetPageCount")]
        private static extern int GetPageCount(IntPtr env, IntPtr thiz, IntPtr docPtr);

        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeLoadPage")]
        private static extern IntPtr LoadPage(IntPtr env, IntPtr thiz, IntPtr docPtr, int pageIndex);

        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeLoadPages")]
        private static extern IntPtr[] LoadPages(IntPtr env, IntPtr thiz, IntPtr docPtr, int fromIndex, int toIndex);

        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeClosePage")]
        private static extern void ClosePage(IntPtr env, IntPtr thiz, IntPtr pagePtr);
        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeClosePages")]
        private static extern void ClosePages(IntPtr env, IntPtr thiz, IntPtr[] pagesPtr);

        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeGetPageWidthPixel")]
        private static extern int GetPageWidthPixel(IntPtr env, IntPtr thiz, IntPtr pagePtr, DisplayMetricsDensity dpi);

        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeGetPageHeightPixel")]
        private static extern int GetPageHeightPixel(IntPtr env, IntPtr thiz, IntPtr pagePtr, DisplayMetricsDensity dpi);

        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeGetPageWidthPoint")]
        private static extern int GetPageWidthPoint(IntPtr env, IntPtr thiz, IntPtr pagePtr);
        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeGetPageHeightPoint")]
        private static extern int GetPageHeightPoint(IntPtr env, IntPtr thiz, IntPtr pagePtr);
        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeRenderPage")]
        private static extern void RenderPage(IntPtr env, IntPtr thiz, IntPtr pagePtr, Surface surface, DisplayMetricsDensity dpi,
            int startX, int startY,
            int drawSizeHor, int drawSizeVer,
            bool renderAnnot);

        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeRenderPageBitmap")]
        private static extern void RenderPageBitmap(IntPtr env, IntPtr thiz, IntPtr pagePtr, IntPtr bitmap, int dpi,
            int startX, int startY,
            int drawSizeHor, int drawSizeVer,
            bool renderAnnot);

        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeGetDocumentMetaText")]
        private static extern IntPtr GetDocumentMetaText(IntPtr env, IntPtr thiz, IntPtr docPtr, IntPtr tag);

        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeGetFirstChildBookmark")]
        private static extern IntPtr GetFirstChildBookmark(IntPtr env, IntPtr thiz, IntPtr docPtr, IntPtr bookmarkPtr);

        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeGetSiblingBookmark")]
        private static extern IntPtr GetSiblingBookmark(IntPtr env, IntPtr thiz, IntPtr docPtr, IntPtr bookmarkPtr);

        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeGetBookmarkTitle")]
        private static extern string GetBookmarkTitle(IntPtr env, IntPtr thiz, IntPtr bookmarkPtr);

        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeGetBookmarkDestIndex")]
        private static extern long GetBookmarkDestIndex(IntPtr env, IntPtr thiz, IntPtr docPtr, IntPtr bookmarkPtr);

        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeGetPageWidthByIndex")]
        private static extern int GetPageWidthByIndex(IntPtr env, IntPtr thiz, IntPtr docPtr, int pageIndex, int dpi);
        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeGetPageHeightByIndex")]
        private static extern int GetPageHeightByIndex(IntPtr env, IntPtr thiz, IntPtr docPtr, int pageIndex, int dpi);
        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeGetPageLinks")]
        private static extern IntPtr GetPageLinks(IntPtr env, IntPtr thiz, IntPtr pagePtr);
        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeGetDestPageIndex")]
        private static extern int GetDestPageIndex(IntPtr env, IntPtr thiz, IntPtr docPtr, IntPtr linkPtr);
        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeGetLinkURI")]
        private static extern string GetLinkURI(IntPtr env, IntPtr thiz, IntPtr docPtr, IntPtr linkPtr);
        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeGetLinkRectLeft")]
        private static extern int GetLinkRectLeft(IntPtr env, IntPtr thiz, IntPtr linkPtr);
        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeGetLinkRectTop")]
        private static extern int GetLinkRectTop(IntPtr env, IntPtr thiz, IntPtr linkPtr);
        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeGetLinkRectRight")]
        private static extern int GetLinkRectRight(IntPtr env, IntPtr thiz, IntPtr linkPtr);
        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativeGetLinkRectBottom")]
        private static extern int GetLinkRectBottom(IntPtr env, IntPtr thiz, IntPtr linkPtr);
        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativePageCoordsToDeviceX")]
        private static extern int PageCoordsToDeviceX(IntPtr env, IntPtr thiz, IntPtr pagePtr, int startX, int startY, int sizeX,
                                                      int sizeY, int rotate, double pageX, double pageY);
        [DllImport("jniPdfium", EntryPoint = "Java_com_shockwave_pdfium_PdfiumCore_nativePageCoordsToDeviceY")]
        private static extern int PageCoordsToDeviceY(IntPtr env, IntPtr thiz, IntPtr pagePtr, int startX, int startY, int sizeX,
            int sizeY, int rotate, double pageX, double pageY);

        private static readonly object Obj = new object();
        private static Java.Lang.Reflect.Field mFdField;
        private readonly DisplayMetricsDensity mCurrentDpi;

        public PdfiumCore(Context ctx)
        {
            mCurrentDpi = ctx.Resources.DisplayMetrics.DensityDpi;
        }
     
        public static int GetNumFd(ParcelFileDescriptor fdObj)
        {
              
            try
            {
                if (mFdField != null) return mFdField.GetInt(fdObj.FileDescriptor);
                mFdField = FdClass.GetDeclaredField(FdFieldName);
                mFdField.Accessible = true;

                return mFdField.GetInt(fdObj.FileDescriptor);
            }
            catch (NoSuchFieldException e)
            {
                e.PrintStackTrace();
                return -1;
            }
            catch (IllegalAccessException e)
            {
                e.PrintStackTrace();
                return -1;
            }
        }

        public PdfDocument NewDocument(ParcelFileDescriptor fd)
        {
            return NewDocument(fd, null);
        }

        public PdfDocument NewDocument(ParcelFileDescriptor fd, string password)
        {
            var document = new PdfDocument { ParcelFileDescriptor = fd };
            lock (Obj)
            {
                document.MNativeDocPtr = OpenDocument(JNIEnv.Handle,IntPtr.Zero,GetNumFd(fd), password);
            }

            return document;
        }
        public PdfDocument NewDocument(byte[] data)
        {
            return NewDocument(data, null);
        }

        public PdfDocument NewDocument(byte[] data, string password)
        {
            var document = new PdfDocument();
            lock (Obj)
            {
                document.MNativeDocPtr = OpenMemDocument(IntPtr.Zero,IntPtr.Zero,data, password);
            }
            return document;
        }
        public int GetPageCount(PdfDocument doc)
        {
            lock (Obj)
            {
                return GetPageCount(JNIEnv.Handle,IntPtr.Zero,doc.MNativeDocPtr);
            }
        }

        public IntPtr OpenPage(PdfDocument doc, int pageIndex)
        {
            lock (Obj)
            {
                var pagePtr = LoadPage(IntPtr.Zero, IntPtr.Zero, doc.MNativeDocPtr, pageIndex);
                doc.MNativePagesPtr.Add(pageIndex, pagePtr);
                return pagePtr;
            }

        }

        public IntPtr[] OpenPage(PdfDocument doc, int fromIndex, int toIndex)
        {
            lock (Obj)
            {
                var pagesPtr = LoadPages(IntPtr.Zero, IntPtr.Zero, doc.MNativeDocPtr, fromIndex, toIndex);
                var pageIndex = fromIndex;
                foreach (var page in pagesPtr)
                {
                    if (pageIndex > toIndex) break;
                    doc.MNativePagesPtr.Add(pageIndex, page);
                    pageIndex++;
                }

                return pagesPtr;
            }
        }

        public int GetPageWidth(PdfDocument doc, int index)
        {
            lock (Obj)
            {
                try
                {
                    var pagePtr = doc.MNativePagesPtr.Values.ElementAt(index);
                    return GetPageWidthPixel(IntPtr.Zero, IntPtr.Zero, pagePtr, mCurrentDpi);
                }
                catch(ArgumentNullException)
                {
                    return 0;
                }
               
            }
        }

        public int GetPageHeight(PdfDocument doc, int index)
        {
            lock (Obj)
            {
                try
                {
                    var pagePtr = doc.MNativePagesPtr.Values.ElementAt(index);
                    return GetPageHeightPixel(IntPtr.Zero, IntPtr.Zero, pagePtr, mCurrentDpi);
                }
                catch (ArgumentNullException)
                {
                    return 0;
                }
            }
        }

        public int GetPageWidthPoint(PdfDocument doc, int index)
        {
            lock (Obj)
            {
                try
                {
                    var pagePtr = doc.MNativePagesPtr.Values.ElementAt(index);
                    return GetPageWidthPoint(IntPtr.Zero, IntPtr.Zero, pagePtr);
                }
                catch (ArgumentNullException)
                {
                    return 0;
                }
            }
        }

        public int GetPageHeightPoint(PdfDocument doc, int index)
        {
            lock (Obj)
            {
                try
                {
                    var pagePtr = doc.MNativePagesPtr.Values.ElementAt(index);
                    return GetPageHeightPoint(IntPtr.Zero, IntPtr.Zero, pagePtr);
                }
                catch (ArgumentNullException)
                {
                    return 0;
                }
            }
        }
        public Size GetPageSize(PdfDocument doc, int index)
        {
            lock (Obj)
            {
                var w=GetPageWidthByIndex(JNIEnv.Handle, IntPtr.Zero, doc.MNativeDocPtr, index, (int)mCurrentDpi);
                var h =GetPageHeightByIndex(JNIEnv.Handle, IntPtr.Zero, doc.MNativeDocPtr, index, (int)mCurrentDpi);
                return new Size()
                {
                    Width = w,
                    Height = h
                };
            }
        }
        public void RenderPage(PdfDocument doc, Surface surface, int pageIndex,
            int startX, int startY, int drawSizeX, int drawSizeY)
        {
            RenderPage(doc, surface, pageIndex, startX, startY, drawSizeX, drawSizeY, false);
        }

        public void RenderPage(PdfDocument doc, Surface surface, int pageIndex,
            int startX, int startY, int drawSizeX, int drawSizeY,
            bool renderAnnot)
        {
            lock (Obj)
            {
                    RenderPage(IntPtr.Zero, IntPtr.Zero, doc.MNativePagesPtr[pageIndex], surface, mCurrentDpi,
                        startX, startY, drawSizeX, drawSizeY, renderAnnot);
            }
        }

        public void RenderPageBitmap(PdfDocument doc, Bitmap bitmap, int pageIndex,
            int startX, int startY, int drawSizeX, int drawSizeY)
        {
            RenderPageBitmap(doc, bitmap, pageIndex, startX, startY, drawSizeX, drawSizeY, false);
        }

        public void RenderPageBitmap(PdfDocument doc, Bitmap bitmap, int pageIndex,
            int startX, int startY, int drawSizeX, int drawSizeY,
            bool renderAnnot)
        {
            lock (Obj)
            {
                    RenderPageBitmap(JNIEnv.Handle, IntPtr.Zero, doc.MNativePagesPtr[pageIndex],bitmap.Handle, (int)mCurrentDpi,
                        startX, startY, drawSizeX, drawSizeY, renderAnnot);
            }
        }

        public void CloseDocument(PdfDocument doc)
        {
            lock (Obj)
            {
                foreach (var index in doc.MNativePagesPtr.Keys)
                {
                    ClosePage(IntPtr.Zero, IntPtr.Zero, doc.MNativePagesPtr.Values.ElementAt(index));
                }
                doc.MNativePagesPtr.Clear();

                 CloseDocument(IntPtr.Zero, IntPtr.Zero, doc.MNativeDocPtr);

                if (doc.ParcelFileDescriptor == null) return; //if document was loaded from file
                try
                {
                    doc.ParcelFileDescriptor.Close();
                }
                catch (IOException e)
                {
                    /* ignore */
                }
                doc.ParcelFileDescriptor = null;
            }
        }

        public PdfDocument.Meta GetDocumentMeta(PdfDocument doc)
        {
            lock (Obj)
            {            
                var meta = new PdfDocument.Meta
                {  
                    Title =Object.GetObject<Java.Lang.String>(JNIEnv.Handle,GetDocumentMetaText(JNIEnv.Handle, IntPtr.Zero, doc.MNativeDocPtr, tag:new Java.Lang.String("Title").Handle),JniHandleOwnership.TransferLocalRef).ToString(),
                    Author = Object.GetObject<Java.Lang.String>(JNIEnv.Handle, GetDocumentMetaText(JNIEnv.Handle, IntPtr.Zero, doc.MNativeDocPtr, tag: new Java.Lang.String("Author").Handle), JniHandleOwnership.TransferLocalRef).ToString(),
                    Subject = Object.GetObject<Java.Lang.String>(JNIEnv.Handle, GetDocumentMetaText(JNIEnv.Handle, IntPtr.Zero, doc.MNativeDocPtr, tag: new Java.Lang.String("Subject").Handle), JniHandleOwnership.TransferLocalRef).ToString(),
                    Keywords = Object.GetObject<Java.Lang.String>(JNIEnv.Handle, GetDocumentMetaText(JNIEnv.Handle, IntPtr.Zero, doc.MNativeDocPtr, tag: new Java.Lang.String("Keywords").Handle), JniHandleOwnership.TransferLocalRef).ToString(),
                    Creator = Object.GetObject<Java.Lang.String>(JNIEnv.Handle, GetDocumentMetaText(JNIEnv.Handle, IntPtr.Zero, doc.MNativeDocPtr, tag: new Java.Lang.String("Creator").Handle), JniHandleOwnership.TransferLocalRef).ToString(),
                    Producer = Object.GetObject<Java.Lang.String>(JNIEnv.Handle, GetDocumentMetaText(JNIEnv.Handle, IntPtr.Zero, doc.MNativeDocPtr, tag: new Java.Lang.String("Producer").Handle), JniHandleOwnership.TransferLocalRef).ToString(),
                    CreationDate = Object.GetObject<Java.Lang.String>(JNIEnv.Handle, GetDocumentMetaText(JNIEnv.Handle, IntPtr.Zero, doc.MNativeDocPtr, tag: new Java.Lang.String("CreationDate").Handle), JniHandleOwnership.TransferLocalRef).ToString(),
                    ModDate = Object.GetObject<Java.Lang.String>(JNIEnv.Handle, GetDocumentMetaText(JNIEnv.Handle, IntPtr.Zero, doc.MNativeDocPtr, tag: new Java.Lang.String("ModDate").Handle), JniHandleOwnership.TransferLocalRef).ToString()

                };
                return meta;
            }
        }

        public List<PdfDocument.Bookmark> GetTableOfContents(PdfDocument doc)
        {
            lock (Obj)
            {
                var topLevel = new List<PdfDocument.Bookmark>();
                var first = GetFirstChildBookmark(JNIEnv.Handle, IntPtr.Zero, doc.MNativeDocPtr,IntPtr.Zero);
                if (first != IntPtr.Zero)
                {
                    RecursiveGetBookmark(topLevel, doc, first);
                }
                return topLevel;
            }
        }

        private void RecursiveGetBookmark(ICollection<PdfDocument.Bookmark> tree, PdfDocument doc, IntPtr bookmarkPtr)
        {
            var bookmark = new PdfDocument.Bookmark
            {
                MNativePtr = bookmarkPtr,
                Title = GetBookmarkTitle(IntPtr.Zero, IntPtr.Zero, bookmarkPtr),
                PageIdx = GetBookmarkDestIndex(IntPtr.Zero, IntPtr.Zero, doc.MNativeDocPtr, bookmarkPtr)
            };
            tree.Add(bookmark);

            var child = GetFirstChildBookmark(IntPtr.Zero, IntPtr.Zero, doc.MNativeDocPtr, bookmarkPtr);
            if(child!=IntPtr.Zero)
                RecursiveGetBookmark(bookmark.Children, doc, child);
            var sibling = GetSiblingBookmark(JNIEnv.Handle, IntPtr.Zero, doc.MNativeDocPtr, bookmarkPtr);
            if(sibling!=IntPtr.Zero)
                RecursiveGetBookmark(tree, doc, sibling);
        }
        public List<PdfDocument.Link> GetPageLinks(PdfDocument doc, int pageIndex)
        {
            lock (Obj)
            {
                var links = new List<PdfDocument.Link>();
                var nativePagePtr = doc.MNativePagesPtr[pageIndex];
                if (nativePagePtr == IntPtr.Zero)
                {
                    return links;
                }
                var linkPtrs = GetPageLinks(JNIEnv.Handle, IntPtr.Zero, nativePagePtr);
               var array=JNIEnv.GetArray<long>(linkPtrs);

                foreach (var linkPtr in array)
                {
                    var index = GetDestPageIndex(JNIEnv.Handle, IntPtr.Zero, doc.MNativeDocPtr,new IntPtr(linkPtr));
                    var uri = GetLinkURI(JNIEnv.Handle, IntPtr.Zero, doc.MNativeDocPtr, new IntPtr(linkPtr));
                    var left = GetLinkRectLeft(JNIEnv.Handle, IntPtr.Zero, new IntPtr(linkPtr));
                    var top = GetLinkRectTop(JNIEnv.Handle, IntPtr.Zero, new IntPtr(linkPtr));
                    var right = GetLinkRectRight(JNIEnv.Handle, IntPtr.Zero, new IntPtr(linkPtr));
                    var bottom = GetLinkRectBottom(JNIEnv.Handle, IntPtr.Zero, new IntPtr(linkPtr));
                    var rect = new RectF(left,top,right,bottom);
                    links.Add(new PdfDocument.Link(rect, index, uri));

                }
                return links;
            }
        }
        public Point MapPageCoordsToDevice(PdfDocument doc, int pageIndex, int startX, int startY, int sizeX,
                                      int sizeY, int rotate, double pageX, double pageY)
        {
            var pagePtr = doc.MNativePagesPtr[pageIndex];
            var x= PageCoordsToDeviceX(JNIEnv.Handle, IntPtr.Zero, pagePtr, startX, startY, sizeX, sizeY, rotate, pageX, pageY);
            var y= PageCoordsToDeviceY(JNIEnv.Handle, IntPtr.Zero, pagePtr, startX, startY, sizeX, sizeY, rotate, pageX, pageY);
            return new Point(x,y);
        }

        public RectF MapRectToDevice(PdfDocument doc, int pageIndex, int startX, int startY, int sizeX,
                                     int sizeY, int rotate, RectF coords)
        {

            var leftTop = MapPageCoordsToDevice(doc, pageIndex, startX, startY, sizeX, sizeY, rotate,
                    coords.Left, coords.Top);
            var rightBottom = MapPageCoordsToDevice(doc, pageIndex, startX, startY, sizeX, sizeY, rotate,
                    coords.Right, coords.Bottom);
            return new RectF(leftTop.X, leftTop.Y, rightBottom.X, rightBottom.Y);
        }
    }
}