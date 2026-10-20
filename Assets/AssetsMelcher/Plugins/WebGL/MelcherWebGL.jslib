// Javascript helper library. Contains functions for interrogating information
// about the browser (is it mobile, top-level URL of browser, etc.)

var plugin = {};

plugin.JS_IsMobilePlatform =
    function()
    {
        return Module.SystemInfo.mobile;
    };

/// @brief Get the top-level URL of the browser. This is the URL of the site
///        in the user's browser, which may be different from the URL of the
///        hosted content. E.g., a WebGL build may be hosted on one site and
///        referenced/embedded in another.
///
plugin.JS_GetTopLevelUrl =
    function()
    {
        // Based on Unity discussion found here:
        // https://forum.unity.com/threads/get-the-website-url-which-is-displaying-the-unity-webgl-component.438179/

        // This line can cause issues due to cross-origin security. The following
        // line, document.referrer, doesn't have this problem but may have issues
        // when the content is nested within multiple iframes

        //var returnStr = window.top.location.href;

        var locationStr = null;

        try
        {
            locationStr =
                (window.location != window.parent.location)
                    ? document.referrer
                    : document.location.href;
        }
        catch (error)
        {
            console.error("JS_GetTopLevelUrl: Exception fetching URL: " + error);

            return null;
        }

        if (locationStr == null)
        {
            console.error("JS_GetTopLevelUrl: Location not found.");

            return null;
        }

        var bufferSize = lengthBytesUTF8(locationStr) + 1;

        var buffer = _malloc(bufferSize);

        if (buffer == null)
        {
            console.error("JS_GetTopLevelUrl: Could not allocate buffer to pass location to Unity.");

            return null;
        }

        stringToUTF8(locationStr, buffer, bufferSize);

        return buffer;
    };

/// @brief Check if a URL parameter exists. This only checks the active iframe's
///        URL because checking the parent's URL is basically impossible with
///        modern browser security (CORS).
///
/// This does not distinguish between a parameter with nothing after the @c{=} ,
/// and a parameter that doesn't have a @c{=} altogether.
///
/// @return @c true if @param paramKey exists; otherwise, @c false
///
plugin.JS_HasUrlParameter =
    function(paramKey, includeTop)
    {
        if (paramKey == null)
        {
            return false;
        }

        var urlParams = new URLSearchParams(location.search);

        var param = urlParams.get(UTF8ToString(paramKey));

        return param != null;
    };

plugin.JS_GetUrlParameter =
    function(paramKey)
    {
        if (paramKey == null)
        {
            return null;
        }

        var urlParams = new URLSearchParams(location.search);

        // UTF8ToString may need to be Pointer_stringify in older versions of Unity
        var param = urlParams.get(UTF8ToString(paramKey));

        if (param == null)
        {
            // Key does not exist

            return null;
        }

        var bufferSize = lengthBytesUTF8(param) + 1;

        var buffer = _malloc(bufferSize);

        if (buffer == null)
        {
            console.error("JS_GetUrlParameter: Could not allocate buffer!");

            return null;
        }

        stringToUTF8(param, buffer, bufferSize);

        return buffer;
    };

/// @brief Change the current tab to a new URL.
///
/// @param newUrl The new URL to visit. If this is @c null , the function will
///               do nothing.
///
plugin.JS_ChangeUrl =
    function(newUrl)
    {
        var url = UTF8ToString(newUrl);

        if (url == null)
        {
            return;
        }

        window.top.location.href = url;
    };

/// @brief Request the browser to save an array of bytes as a file on the local
///        machine.
///
/// @param dataBytes The byte array representing the file to download.
///
/// @param dataByteCount The number of valid bytes in the data array.
///
/// @param fileName The suggested file name for the downloaded file.
///
plugin.JS_TryDownloadingFile =
    function(dataBytes, dataByteCount, fileName)
    {
        // Implementation from @JJJohan's post here:
        // https://discussions.unity.com/t/user-image-download-from-in-webgl-app/667124/8

        fileName = UTF8ToString(fileName);

        var bytes = new Uint8Array(dataByteCount);
        for (var i = 0; i < dataByteCount; ++i)
        {
           bytes[i] = HEAPU8[dataBytes + i];
        }

        var blob = new Blob([bytes]);
        var link = document.createElement('a');
        link.href = window.URL.createObjectURL(blob);
        link.download = fileName;

        var event = document.createEvent("MouseEvents");
        event.initMouseEvent("click");
        link.dispatchEvent(event);
        window.URL.revokeObjectURL(link.href);
    };

mergeInto(LibraryManager.library, plugin);
