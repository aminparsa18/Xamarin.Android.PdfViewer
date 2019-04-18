Android PdfViewer

AndroidPdfViewer 1.x is available on AndroidPdfViewerV1 repo, where can be developed independently. Version 1.x uses different engine for drawing document on canvas, so if you don't like 2.x version, try 1.x.

Library for displaying PDF documents on Android, with animations, gestures, zoom and double tap support. It is based on PdfiumAndroid for decoding PDF files. Works on API 11 (Android 3.0) and higher. Licensed under Apache License 2.0.
What's new in 3.1.0-beta.1?

    Merge pull request #557 for snapping pages (scrolling page by page)
    merge pull request #618 for night mode
    Merge pull request #566 for OnLongTapListener
    Update PdfiumAndroid to 1.9.0, which uses c++_shared instead of gnustl_static
    Update Gradle Plugin
    Update compile SDK and support library to 26
    Change minimum SDK to 14

Changes in 3.0 API

    Replaced Contants.PRELOAD_COUNT with PRELOAD_OFFSET
    Removed PDFView#fitToWidth() (variant without arguments)
    Removed Configurator#invalidPageColor(int) method as invalid pages are not rendered
    Removed page size parameters from OnRenderListener#onInitiallyRendered(int) method, as document may have different page sizes
    Removed PDFView#setSwipeVertical() method

Installation

Add to build.gradle:

compile 'com.github.barteksc:android-pdf-viewer:3.1.0-beta.1'

or if you want to use more stable version:

compile 'com.github.barteksc:android-pdf-viewer:2.8.2'

Library is available in jcenter repository, probably it'll be in Maven Central soon.
ProGuard

If you are using ProGuard, add following rule to proguard config file:

-keep class com.shockwave.**

Include PDFView in your layout

<com.github.barteksc.pdfviewer.PDFView
        android:id="@+id/pdfView"
        android:layout_width="match_parent"
        android:layout_height="match_parent"/>

Load a PDF file

All available options with default values:

pdfView.fromUri(Uri)
or
pdfView.fromFile(File)
or
pdfView.fromBytes(byte[])
or
pdfView.fromStream(InputStream) // stream is written to bytearray - native code cannot use Java Streams
or
pdfView.fromSource(DocumentSource)
or
pdfView.fromAsset(String)
    .pages(0, 2, 1, 3, 3, 3) // all pages are displayed by default
    .enableSwipe(true) // allows to block changing pages using swipe
    .swipeHorizontal(false)
    .enableDoubletap(true)
    .defaultPage(0)
    // allows to draw something on the current page, usually visible in the middle of the screen
    .onDraw(onDrawListener)
    // allows to draw something on all pages, separately for every page. Called only for visible pages
    .onDrawAll(onDrawListener)
    .onLoad(onLoadCompleteListener) // called after document is loaded and starts to be rendered
    .onPageChange(onPageChangeListener)
    .onPageScroll(onPageScrollListener)
    .onError(onErrorListener)
    .onPageError(onPageErrorListener)
    .onRender(onRenderListener) // called after document is rendered for the first time
    // called on single tap, return true if handled, false to toggle scroll handle visibility
    .onTap(onTapListener)
    .onLongPress(onLongPressListener)
    .enableAnnotationRendering(false) // render annotations (such as comments, colors or forms)
    .password(null)
    .scrollHandle(null)
    .enableAntialiasing(true) // improve rendering a little bit on low-res screens
    // spacing between pages in dp. To define spacing color, set view background
    .spacing(0)
    .autoSpacing(false) // add dynamic spacing to fit each page on its own on the screen
    .linkHandler(DefaultLinkHandler)
    .pageFitPolicy(FitPolicy.WIDTH)
    .pageSnap(true) // snap pages to screen boundaries
    .pageFling(false) // make a fling change only a single page like ViewPager
    .nightMode(false) // toggle night mode
    .load();

    pages is optional, it allows you to filter and order the pages of the PDF as you need

Scroll handle

Scroll handle is replacement for ScrollBar from 1.x branch.

From version 2.1.0 putting PDFView in RelativeLayout to use ScrollHandle is not required, you can use any layout.

To use scroll handle just register it using method Configurator#scrollHandle(). This method accepts implementations of ScrollHandle interface.

There is default implementation shipped with AndroidPdfViewer, and you can use it with .scrollHandle(new DefaultScrollHandle(this)). DefaultScrollHandle is placed on the right (when scrolling vertically) or on the bottom (when scrolling horizontally). By using constructor with second argument (new DefaultScrollHandle(this, true)), handle can be placed left or top.

You can also create custom scroll handles, just implement ScrollHandle interface. All methods are documented as Javadoc comments on interface source.
Document sources

Version 2.3.0 introduced document sources, which are just providers for PDF documents. Every provider implements DocumentSource interface. Predefined providers are available in com.github.barteksc.pdfviewer.source package and can be used as samples for creating custom ones.

Predefined providers can be used with shorthand methods:

pdfView.fromUri(Uri)
pdfView.fromFile(File)
pdfView.fromBytes(byte[])
pdfView.fromStream(InputStream)
pdfView.fromAsset(String)

Custom providers may be used with pdfView.fromSource(DocumentSource) method.
Links

Version 3.0.0 introduced support for links in PDF documents. By default, DefaultLinkHandler is used and clicking on link that references page in same document causes jump to destination page and clicking on link that targets some URI causes opening it in default application.

You can also create custom link handlers, just implement LinkHandler interface and set it using Configurator#linkHandler(LinkHandler) method. Take a look at DefaultLinkHandler source to implement custom behavior.
Pages fit policy

Since version 3.0.0, library supports fitting pages into the screen in 3 modes:

    WIDTH - width of widest page is equal to screen width
    HEIGHT - height of highest page is equal to screen height
    BOTH - based on widest and highest pages, every page is scaled to be fully visible on screen

Apart from selected policy, every page is scaled to have size relative to other pages.

Fit policy can be set using Configurator#pageFitPolicy(FitPolicy). Default policy is WIDTH.
Additional options
Bitmap quality

By default, generated bitmaps are compressed with RGB_565 format to reduce memory consumption. Rendering with ARGB_8888 can be forced by using pdfView.useBestQuality(true) method.
Double tap zooming

There are three zoom levels: min (default 1), mid (default 1.75) and max (default 3). On first double tap, view is zoomed to mid level, on second to max level, and on third returns to min level. If you are between mid and max levels, double tapping causes zooming to max and so on.

Zoom levels can be changed using following methods:

void setMinZoom(float zoom);
void setMidZoom(float zoom);
void setMaxZoom(float zoom);

Possible questions
Why resulting apk is so big?

Android PdfViewer depends on PdfiumAndroid, which is set of native libraries (almost 16 MB) for many architectures. Apk must contain all this libraries to run on every device available on market. Fortunately, Google Play allows us to upload multiple apks, e.g. one per every architecture. There is good article on automatically splitting your application into multiple apks, available here. Most important section is Improving multiple APKs creation and versionCode handling with APK Splits, but whole article is worth reading. You only need to do this in your application, no need for forking PdfiumAndroid or so.
Why I cannot open PDF from URL?

Downloading files is long running process which must be aware of Activity lifecycle, must support some configuration, data cleanup and caching, so creating such module will probably end up as new library.
How can I show last opened page after configuration change?

You have to store current page number and then set it with pdfView.defaultPage(page), refer to sample app
How can I fit document to screen width (eg. on orientation change)?

Use FitPolicy.WIDTH policy or add following snippet when you want to fit desired page in document with different page sizes:

Configurator.onRender(new OnRenderListener() {
    @Override
    public void onInitiallyRendered(int pages, float pageWidth, float pageHeight) {
        pdfView.fitToWidth(pageIndex);
    }
});

How can I scroll through single pages like a ViewPager?

You can use a combination of the following settings to get scroll and fling behaviour similar to a ViewPager:

    .swipeHorizontal(true)
    .pageSnap(true)
    .autoSpacing(true)
    .pageFling(true)

One more thing

If you have any suggestions on making this lib better, write me, create issue or write some code and send pull request.