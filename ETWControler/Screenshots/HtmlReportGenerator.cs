using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWControler.Screenshots
{
    /// <summary>
    /// Generate from a direcotry of jpg files a HTML page where one can quickly see what was happening in chronological order.
    /// </summary>
    class HtmlReportGenerator
    {
        public const string HtmlReportFileName = "Report.html";

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
        /// Generate report with current settings. Currently there are none but if demand exists they can easily be added.
        /// </summary>
        /// <returns>Generated report file name</returns>
        public string GenerateReport()
        {
            string htmlFile = Path.Combine(ScreenshotDirectory, HtmlReportFileName);

            using (var stream = new FileStream(htmlFile, FileMode.Create))
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.WriteLine("<html>");
                    writer.WriteLine($"<h1>Screenshot Report for {Environment.MachineName} from {DateTime.Now}</h1>");
                    writer.WriteLine("<hr>");
                    foreach(var jpg in JpgsByCreationDate)
                    {
                        writer.WriteLine("<div>");
                        writer.WriteLine($"File {jpg.Name} at {jpg.CreationTime.ToString("HH:mm:ss.fff")}");
                        writer.WriteLine("</div>");
                        writer.WriteLine("<div>");
                        writer.WriteLine($"<a href=\"{jpg.Name}\">");
                        writer.WriteLine($"<img src=\"{jpg.Name}\" width=\"50%\" height=\"50%\" alt=\"Not captured or deleted\" title=\"Input event {jpg.Name}\"/>");
                        writer.WriteLine("</a>");
                        writer.WriteLine("</div>");
                        writer.WriteLine("<hr>");
                    }
                    writer.WriteLine("</html>");
                }
            }

            return htmlFile;
        }
    }
}
