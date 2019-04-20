
# Xamarin android pdf viewer

Library for displaying PDF documents on Xamarin Android, with `animations`, `gestures`, `zoom` and `double tap` support. It is a port from [Android PdfViewer](https://github.com/barteksc/AndroidPdfViewer) for Xamarin.android. It is based on [PdfiumAndroid](https://github.com/barteksc/PdfiumAndroid) for decoding PDF files. Works on API 11 (Android 3.0) and higher. Licensed under Apache License 2.0.


#  Installation

Available on NuGet: [Xamarin.Android.PdfViewer](https://www.nuget.org/packages/Xamarin.Android.PdfViewer) 

## Include PDFView in your layout
`<PdfViewer.PdfView
        android:id="@+id/pdfView"
        android:layout_width="match_parent"
        android:layout_height="match_parent"/>`

## Load PDF file
All available options with default values:

        pdfView.FromUri(Uri)
    or
    pdfView.FromFile(File)
    or
    pdfView.FromBytes(byte[])
    or
    pdfView.FromStream(InputStream) // stream is written to bytearray - native code cannot use Java Streams
    or
    pdfView.FromSource(DocumentSource)
    or
    pdfView.FromAsset(String)
        .Pages(0, 2, 1, 3, 3, 3) // all pages are displayed by default
        .EnableSwipe(true) // allows to block changing pages using swipe
        .SwipeHorizontal(false)
        .EnableDoubletap(true)
        .DefaultPage(0)
        // allows to draw something on the current page, usually visible in the middle of the screen
        .SetOnDraw(onDrawListener)
        // allows to draw something on all pages, separately for every page. Called only for visible pages
        .SetOnDrawAll(onDrawListener)
        .SetOnLoad(onLoadCompleteListener) // called after document is loaded and starts to be rendered
        .SetOnPageChange(onPageChangeListener)
        .SetOnPageScroll(onPageScrollListener)
        .SetOnError(onErrorListener)
        .SetOnPageError(onPageErrorListener)
        .SetOnRender(onRenderListener) // called after document is rendered for the first time
        // called on single tap, return true if handled, false to toggle scroll handle visibility
        .SetOnTap(onTapListener)
        .SetOnLongPress(onLongPressListener)
        .EnableAnnotationRendering(false) // render annotations (such as comments, colors or forms)
        .Password(null)
        .ScrollHandle(null)
        .EnableAntialiasing(true) // improve rendering a little bit on low-res screens
        // spacing between pages in dp. To define spacing color, set view background
        .Spacing(0)
        .AutoSpacing(false) // add dynamic spacing to fit each page on its own on the screen
        .LinkHandler(DefaultLinkHandler)
        .SetPageFitPolicy(FitPolicy.WIDTH)
        .SetPageSnap(true) // snap pages to screen boundaries
        .SetPageFling(false) // make a fling change only a single page like ViewPager
        .NightMode(false) // toggle night mode
        .Load();

## Scroll handle
putting **PDFView** in **RelativeLayout** to use **ScrollHandle** is not required, you can use any layout.

To use scroll handle just register it using method `Configurator#scrollHandle()`. This method accepts implementations of **ScrollHandle** interface.

There is default implementation shipped with AndroidPdfViewer, and you can use it with `.scrollHandle(new DefaultScrollHandle(this))`. **DefaultScrollHandle** is placed on the right (when scrolling vertically) or on the bottom (when scrolling horizontally). By using constructor with second argument (`new DefaultScrollHandle(this, true)`), handle can be placed left or top.

You can also create custom scroll handles, just implement **ScrollHandle** interface. All methods are documented as Javadoc comments on interface [source](https://github.com/barteksc/AndroidPdfViewer/tree/master/android-pdf-viewer/src/main/java/com/github/barteksc/pdfviewer/scroll/ScrollHandle.java).

## Document sources

Every provider implements **DocumentSource** Abstract class. Predefined providers are available in **com.github.barteksc.pdfviewer.source** package and can be used as samples for creating custom ones.

Predefined providers can be used with shorthand methods:

```
pdfView.FromUri(Uri)
pdfView.FromFile(File)
pdfView.FromBytes(byte[])
pdfView.FromStream(InputStream)
pdfView.FromAsset(String)

```

Custom providers may be used with `pdfView.FromSource(DocumentSource)` method.

## Links

By default, **DefaultLinkHandler** is used and clicking on link that references page in same document causes jump to destination page and clicking on link that targets some URI causes opening it in default application.

You can also create custom link handlers, just implement **LinkHandler** interface and set it using `Configurator#linkHandler(LinkHandler)` method. Take a look at [DefaultLinkHandler](https://github.com/barteksc/AndroidPdfViewer/tree/master/android-pdf-viewer/src/main/java/com/github/barteksc/pdfviewer/link/DefaultLinkHandler.java) source to implement custom behavior.


## Pages fit policy


-   WIDTH - width of widest page is equal to screen width
-   HEIGHT - height of highest page is equal to screen height
-   BOTH - based on widest and highest pages, every page is scaled to be fully visible on screen

Apart from selected policy, every page is scaled to have size relative to other pages.

Fit policy can be set using `Configurator#pageFitPolicy(FitPolicy)`. Default policy is **WIDTH**.
## Additional options

### Bitmap quality

By default, generated bitmaps are _compressed_ with `RGB_565` format to reduce memory consumption. Rendering with `ARGB_8888` can be forced by using `pdfView.IsBestQuality = true` method.

### Double tap zooming

There are three zoom levels: min (default 1), mid (default 1.75) and max (default 3). On first double tap, view is zoomed to mid level, on second to max level, and on third returns to min level. If you are between mid and max levels, double tapping causes zooming to max and so on.

Zoom levels can be changed using following methods:

pdfView.MinZoom
pdfView.MidZoom
pdfView.MaxZoom

## Possible questions
### Why resulting apk is so big?

Android PdfViewer depends on PdfiumAndroid, which is set of native libraries (almost 16 MB) for many architectures. Apk must contain all this libraries to run on every device available on market. Fortunately, Google Play allows us to upload multiple apks, e.g. one per every architecture. There is good article on automatically splitting your application into multiple apks, available [here](http://ph0b.com/android-studio-gradle-and-ndk-integration/). Most important section is _Improving multiple APKs creation and versionCode handling with APK Splits_, but whole article is worth reading. You only need to do this in your application, no need for forking PdfiumAndroid or so.

### Why I cannot open PDF from URL?

Downloading files is long running process which must be aware of Activity lifecycle, must support some configuration, data cleanup and caching, so creating such module will probably end up as new library.

### How can I show last opened page after configuration change?

You have to store current page number and then set it with `pdfView.DefaultPage(page)`, refer to sample app

### How can I fit document to screen width (eg. on orientation change)?

Use `FitPolicy.WIDTH` policy or add following snippet when you want to fit desired page in document with different page sizes:

Configurator.SetOnRender((s, e) =>
                {
                    pdfView.FitToWidth(e.NbPages);
                })

### How can I scroll through single pages like a ViewPager?

You can use a combination of the following settings to get scroll and fling behaviour similar to a ViewPager:

    .SwipeHorizontal(true)
    .SetpageSnap(true)
    .SetautoSpacing(true)
    .SetpageFling(true)

## One more thing

If you like my project You can Donate to this project using Paypal(https://camo.githubusercontent.com/11b2f47d7b4af17ef3a803f57c37de3ac82ac039/68747470733a2f2f696d672e736869656c64732e696f2f62616467652f70617970616c2d646f6e6174652d79656c6c6f772e737667)](https://www.paypal.me/aliparsa64)
If you have any suggestions on making this lib better, write me, create issue or write some code and send pull request.
```
