using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ETWController.Screenshots
{
    /// <summary>
    /// Generate from a directory of jpg files a HTML page where one can quickly see what was happening in chronological order.
    /// </summary>
    class HtmlReportGenerator
    {
        public const string HtmlReportFileName = "Report.html";

        const string TitlePlaceHolder = "@@TITLE@@";

        const string ImagesPlaceHolder = "@@IMAGES@@";

        FileInfo[] JpgsByCreationDate;
        string ScreenshotDirectory;

        /// <summary>
        /// Create report for this directory
        /// </summary>
        /// <param name="screenshotDirectory"></param>
        public HtmlReportGenerator(string screenshotDirectory)
        {
            if( !Directory.Exists(screenshotDirectory))
            {
                throw new DirectoryNotFoundException($"The screenshot directory {screenshotDirectory} was not found.");
            }

            JpgsByCreationDate = Directory.GetFiles(screenshotDirectory, "*.jpg")
                                          .Select(jpg => new FileInfo(jpg))
                                          .OrderBy(d => d.CreationTime)
                                          .ToArray();

            ScreenshotDirectory = screenshotDirectory;
        }




        /// <summary>
        /// Generate a self contained single page screenshot viewer. Instead of dumping all images below each other
        /// the report shows one screenshot at a time and offers next/previous navigation, mouse wheel zoom, panning
        /// and a Shift+Drag rubber band to zoom into a region of interest. The current zoom/pan is kept while
        /// stepping through the images so the zoomed region stays fixed which makes it easy to compare the very same
        /// screen region across consecutive screenshots. In addition the viewer can jump straight to the next/previous
        /// interaction event (the Screenshot_dd / Screenshot_ddAfter500ms files captured for one mouse or keyboard
        /// interaction) so the click and the resulting UI reaction 500ms later can be inspected quickly.
        /// </summary>
        /// <returns>Generated report file name</returns>
        public string GenerateReport()
        {
            string htmlFile = Path.Combine(ScreenshotDirectory, HtmlReportFileName);

            string title = $"Screenshot Report for {Environment.MachineName} from {DateTime.Now}";

            string html = ViewerTemplate.Replace(TitlePlaceHolder, HtmlEncode(title))
                                        .Replace(ImagesPlaceHolder, BuildImageData());

            using (var stream = new FileStream(htmlFile, FileMode.Create))
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(html);
                }
            }

            return htmlFile;
        }

        /// <summary>
        /// Build a JavaScript array literal with one entry per screenshot holding the file name (n), the
        /// creation time (t) and the interaction event number (e, -1 when the screenshot does not belong to an
        /// interaction event) which is consumed by the viewer script.
        /// </summary>
        string BuildImageData()
        {
            var builder = new StringBuilder();
            builder.Append("[");
            for (int i = 0; i < JpgsByCreationDate.Length; i++)
            {
                FileInfo jpg = JpgsByCreationDate[i];
                if (i > 0)
                {
                    builder.Append(",");
                }

                builder.Append("{\"n\":\"")
                       .Append(EscapeJsString(jpg.Name))
                       .Append("\",\"t\":\"")
                       .Append(EscapeJsString(jpg.CreationTime.ToString("HH:mm:ss.fff")))
                       .Append("\",\"e\":")
                       .Append(GetInteractionEventId(jpg.Name).ToString(System.Globalization.CultureInfo.InvariantCulture))
                       .Append("}");
            }
            builder.Append("]");
            return builder.ToString();
        }

        /// <summary>
        /// Parse the interaction event number from a screenshot file name. Each captured keyboard/mouse event gets an
        /// increasing id which is used as file name e.g. Screenshot_12.jpg (the click), Screenshot_12After500ms.jpg
        /// (the UI reaction 500ms later) and Screenshot_12_Enter.jpg (Enter key). Forced/idle screenshots like
        /// Screenshot_Forced_12.34.56.789.jpg carry no interaction id.
        /// </summary>
        /// <param name="fileName">Screenshot file name.</param>
        /// <returns>The interaction event number or -1 if the file does not belong to an interaction event.</returns>
        static int GetInteractionEventId(string fileName)
        {
            if (String.IsNullOrEmpty(fileName))
            {
                return -1;
            }

            string name = Path.GetFileNameWithoutExtension(fileName);
            const string prefix = "Screenshot_";
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(prefix.Length);
            }

            int digits = 0;
            while (digits < name.Length && Char.IsDigit(name[digits]))
            {
                digits++;
            }

            if (digits == 0)
            {
                return -1;
            }

            int id;
            if (Int32.TryParse(name.Substring(0, digits), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out id))
            {
                return id;
            }

            return -1;
        }

        /// <summary>
        /// Escape a string so it can be safely embedded as a JavaScript/JSON string value.
        /// </summary>
        static string EscapeJsString(string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return String.Empty;
            }

            var builder = new StringBuilder(value.Length + 8);
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '"': builder.Append("\\\""); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    case '<': builder.Append("\\u003c"); break;
                    case '>': builder.Append("\\u003e"); break;
                    case '&': builder.Append("\\u0026"); break;
                    default:
                        if (c < ' ')
                        {
                            builder.Append("\\u").Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            builder.Append(c);
                        }
                        break;
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// Minimal HTML encoding for text which is placed into an element body (the report title).
        /// </summary>
        static string HtmlEncode(string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return String.Empty;
            }

            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        /// <summary>
        /// Self contained HTML/CSS/JavaScript single page image viewer. The tokens @@TITLE@@ and @@IMAGES@@ are
        /// replaced with the report title and the JavaScript image array before the file is written. Only single
        /// quotes are used inside the markup so the whole document can live inside a C# verbatim string.
        /// </summary>
        const string ViewerTemplate = @"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='utf-8' />
<meta name='viewport' content='width=device-width, initial-scale=1' />
<title>Screenshot Report</title>
<style>
* { box-sizing: border-box; }
html, body { margin: 0; padding: 0; height: 100%; font-family: Tahoma, Geneva, Verdana, sans-serif; background: #2b2b2b; color: #f0f0f0; }
body { display: flex; flex-direction: column; }
header { padding: 8px 14px; background: #1e1e1e; border-bottom: 1px solid #000; }
header h1 { margin: 0; font-size: 15px; font-weight: 600; }
.toolbar { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; padding: 8px 14px; background: #1e1e1e; border-bottom: 1px solid #000; }
.toolbar button { background: #3a3a3a; color: #f0f0f0; border: 1px solid #555; border-radius: 4px; padding: 6px 12px; cursor: pointer; font-size: 13px; }
.toolbar button:hover:not(:disabled) { background: #4a4a4a; }
.toolbar button:disabled { opacity: 0.4; cursor: default; }
.toolbar .info { font-size: 13px; white-space: nowrap; }
.toolbar .spacer { flex: 1 1 auto; }
.toolbar input[type=range] { width: 240px; }
.viewport { position: relative; flex: 1 1 auto; overflow: hidden; background: #181818; cursor: grab; }
.viewport.panning { cursor: grabbing; }
.viewport img { position: absolute; top: 0; left: 0; transform-origin: 0 0; -webkit-user-drag: none; user-select: none; max-width: none; }
.selection { position: absolute; border: 1px dashed #0a84ff; background: rgba(10,132,255,0.15); pointer-events: none; display: none; }
.hint { padding: 5px 14px; font-size: 12px; color: #aaa; background: #1e1e1e; border-top: 1px solid #000; }
</style>
</head>
<body>
<header><h1>@@TITLE@@</h1></header>
<div class='toolbar'>
<button id='prevBtn' title='Previous image (Left arrow)'>&#9664; Prev</button>
<button id='nextBtn' title='Next image (Right arrow)'>Next &#9654;</button>
<button id='prevEventBtn' title='Previous interaction event (Page Up)'>&#9664;&#9664; Prev event</button>
<button id='nextEventBtn' title='Next interaction event (Page Down)'>Next event &#9654;&#9654;</button>
<input id='slider' type='range' min='0' max='0' value='0' />
<span class='info' id='counter'></span>
<span class='spacer'></span>
<span class='info' id='fileInfo'></span>
<span class='spacer'></span>
<button id='zoomOutBtn' title='Zoom out (-)'>Zoom &#8722;</button>
<button id='zoomInBtn' title='Zoom in (+)'>Zoom +</button>
<button id='zoomResetBtn' title='Fit to window (0)'>Fit</button>
<span class='info' id='zoomInfo'></span>
</div>
<div class='viewport' id='viewport'>
<img id='image' alt='screenshot' />
<div class='selection' id='selection'></div>
</div>
<div class='hint'>Mouse wheel = zoom &middot; Drag = pan &middot; Shift+Drag = zoom into region &middot; Left/Right arrows = previous/next image &middot; Page Up/Down = previous/next interaction event &middot; The zoomed region stays fixed while stepping through the images.</div>
<script>
(function () {
var images = @@IMAGES@@;

var viewport = document.getElementById('viewport');
var img = document.getElementById('image');
var selection = document.getElementById('selection');
var prevBtn = document.getElementById('prevBtn');
var nextBtn = document.getElementById('nextBtn');
var prevEventBtn = document.getElementById('prevEventBtn');
var nextEventBtn = document.getElementById('nextEventBtn');
var slider = document.getElementById('slider');
var counter = document.getElementById('counter');
var fileInfo = document.getElementById('fileInfo');
var zoomInfo = document.getElementById('zoomInfo');
var zoomInBtn = document.getElementById('zoomInBtn');
var zoomOutBtn = document.getElementById('zoomOutBtn');
var zoomResetBtn = document.getElementById('zoomResetBtn');

var MIN_SCALE = 0.02;
var MAX_SCALE = 40;

var current = 0;
var scale = 1, tx = 0, ty = 0;
var hasView = false;
var naturalW = 0, naturalH = 0;

function clampScale(s) { return Math.max(MIN_SCALE, Math.min(s, MAX_SCALE)); }

function clampIndex(i) {
if (i < 0) { return 0; }
if (i > images.length - 1) { return images.length - 1; }
return i;
}

function applyTransform() {
img.style.transform = 'translate(' + tx + 'px,' + ty + 'px) scale(' + scale + ')';
zoomInfo.textContent = Math.round(scale * 100) + '%';
}

function fit() {
if (!naturalW || !naturalH) { return; }
var vw = viewport.clientWidth, vh = viewport.clientHeight;
scale = clampScale(Math.min(vw / naturalW, vh / naturalH));
tx = (vw - naturalW * scale) / 2;
ty = (vh - naturalH * scale) / 2;
hasView = false;
applyTransform();
}

function zoomAt(cx, cy, factor) {
var ix = (cx - tx) / scale;
var iy = (cy - ty) / scale;
scale = clampScale(scale * factor);
tx = cx - ix * scale;
ty = cy - iy * scale;
hasView = true;
applyTransform();
}

function zoomToRegion(x, y, w, h) {
var ix = (x - tx) / scale;
var iy = (y - ty) / scale;
var iw = w / scale;
var ih = h / scale;
var vw = viewport.clientWidth, vh = viewport.clientHeight;
scale = clampScale(Math.min(vw / iw, vh / ih));
tx = vw / 2 - (ix + iw / 2) * scale;
ty = vh / 2 - (iy + ih / 2) * scale;
hasView = true;
applyTransform();
}

function eventStartIndex(eventId) {
if (eventId === -1) { return -1; }
for (var i = 0; i < images.length; i++) {
if (images[i].e === eventId) { return i; }
}
return -1;
}

function nextEventIndex() {
if (images.length === 0) { return -1; }
var curId = images[current].e;
for (var i = current + 1; i < images.length; i++) {
if (images[i].e !== -1 && images[i].e !== curId) { return eventStartIndex(images[i].e); }
}
return -1;
}

function prevEventIndex() {
if (images.length === 0) { return -1; }
var curId = images[current].e;
for (var i = current - 1; i >= 0; i--) {
if (images[i].e !== -1 && images[i].e !== curId) { return eventStartIndex(images[i].e); }
}
return -1;
}

function showEvent(index) {
if (index >= 0) { show(index); }
}

function updateInfo() {
if (images.length === 0) {
counter.textContent = '0 / 0';
fileInfo.textContent = 'No screenshots found';
prevBtn.disabled = true;
nextBtn.disabled = true;
prevEventBtn.disabled = true;
nextEventBtn.disabled = true;
return;
}
counter.textContent = (current + 1) + ' / ' + images.length;
var im = images[current];
var text = im.t ? (im.n + '  (' + im.t + ')') : im.n;
if (im.e !== -1) { text += '  [Event ' + im.e + ']'; }
fileInfo.textContent = text;
slider.value = current;
prevBtn.disabled = current <= 0;
nextBtn.disabled = current >= images.length - 1;
prevEventBtn.disabled = prevEventIndex() < 0;
nextEventBtn.disabled = nextEventIndex() < 0;
}

function show(i) {
if (images.length === 0) { return; }
current = clampIndex(i);
updateInfo();
img.src = images[current].n;
}

img.addEventListener('load', function () {
naturalW = img.naturalWidth;
naturalH = img.naturalHeight;
if (hasView) { applyTransform(); } else { fit(); }
});

img.addEventListener('error', function () {
fileInfo.textContent = images.length ? (images[current].n + '  (not captured or deleted)') : 'No screenshots found';
});

viewport.addEventListener('wheel', function (e) {
e.preventDefault();
var rect = viewport.getBoundingClientRect();
var factor = e.deltaY < 0 ? 1.15 : (1 / 1.15);
zoomAt(e.clientX - rect.left, e.clientY - rect.top, factor);
}, { passive: false });

var dragging = false, selecting = false;
var startX = 0, startY = 0, startTx = 0, startTy = 0;

viewport.addEventListener('mousedown', function (e) {
if (e.button !== 0) { return; }
var rect = viewport.getBoundingClientRect();
startX = e.clientX - rect.left;
startY = e.clientY - rect.top;
if (e.shiftKey) {
selecting = true;
selection.style.display = 'block';
selection.style.left = startX + 'px';
selection.style.top = startY + 'px';
selection.style.width = '0px';
selection.style.height = '0px';
} else {
dragging = true;
startTx = tx;
startTy = ty;
viewport.classList.add('panning');
}
e.preventDefault();
});

window.addEventListener('mousemove', function (e) {
if (!dragging && !selecting) { return; }
var rect = viewport.getBoundingClientRect();
var cx = e.clientX - rect.left;
var cy = e.clientY - rect.top;
if (dragging) {
tx = startTx + (cx - startX);
ty = startTy + (cy - startY);
hasView = true;
applyTransform();
} else {
var x = Math.min(cx, startX), y = Math.min(cy, startY);
var w = Math.abs(cx - startX), h = Math.abs(cy - startY);
selection.style.left = x + 'px';
selection.style.top = y + 'px';
selection.style.width = w + 'px';
selection.style.height = h + 'px';
}
});

window.addEventListener('mouseup', function (e) {
if (dragging) {
dragging = false;
viewport.classList.remove('panning');
}
if (selecting) {
selecting = false;
selection.style.display = 'none';
var rect = viewport.getBoundingClientRect();
var ex = e.clientX - rect.left;
var ey = e.clientY - rect.top;
var x = Math.min(ex, startX), y = Math.min(ey, startY);
var w = Math.abs(ex - startX), h = Math.abs(ey - startY);
if (w > 4 && h > 4) { zoomToRegion(x, y, w, h); }
}
});

prevBtn.addEventListener('click', function () { show(current - 1); });
nextBtn.addEventListener('click', function () { show(current + 1); });
prevEventBtn.addEventListener('click', function () { showEvent(prevEventIndex()); });
nextEventBtn.addEventListener('click', function () { showEvent(nextEventIndex()); });
slider.addEventListener('input', function () { show(parseInt(slider.value, 10)); });
zoomInBtn.addEventListener('click', function () { zoomAt(viewport.clientWidth / 2, viewport.clientHeight / 2, 1.25); });
zoomOutBtn.addEventListener('click', function () { zoomAt(viewport.clientWidth / 2, viewport.clientHeight / 2, 1 / 1.25); });
zoomResetBtn.addEventListener('click', function () { fit(); });

window.addEventListener('keydown', function (e) {
if (e.key === 'ArrowLeft' || e.key === 'ArrowUp') { show(current - 1); e.preventDefault(); }
else if (e.key === 'ArrowRight' || e.key === 'ArrowDown' || e.key === ' ') { show(current + 1); e.preventDefault(); }
else if (e.key === 'PageUp') { showEvent(prevEventIndex()); e.preventDefault(); }
else if (e.key === 'PageDown') { showEvent(nextEventIndex()); e.preventDefault(); }
else if (e.key === '+' || e.key === '=') { zoomAt(viewport.clientWidth / 2, viewport.clientHeight / 2, 1.25); }
else if (e.key === '-' || e.key === '_') { zoomAt(viewport.clientWidth / 2, viewport.clientHeight / 2, 1 / 1.25); }
else if (e.key === '0') { fit(); }
});

window.addEventListener('resize', function () { if (!hasView) { fit(); } });

if (images.length > 0) {
slider.min = 0;
slider.max = images.length - 1;
slider.value = 0;
show(0);
} else {
updateInfo();
}
})();
</script>
</body>
</html>
";
    }
}
