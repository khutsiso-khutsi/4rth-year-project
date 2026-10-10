using System.IO;
using Rotativa.AspNetCore;

namespace _4th_year_set_up.Services
{
    /// <summary>
    /// Finds wkhtmltopdf.exe (needed by Rotativa to make PDFs) and tells the
    /// controllers whether real PDF downloads are available.
    ///
    /// Looks in wwwroot\Rotativa first (that's where it must be on the
    /// published server), then in the normal install folder, so it also works
    /// on a laptop where wkhtmltopdf was installed but not copied.
    /// When it isn't found, the PDF pages open as a printable web page instead
    /// of crashing.
    /// </summary>
    public static class PdfSupport
    {
        public static bool Ready { get; private set; }

        public static void Configure(string webRootPath)
        {
            var candidates = new[]
            {
                (Root: webRootPath ?? "", Folder: "Rotativa"),
                (Root: @"C:\Program Files\wkhtmltopdf", Folder: "bin"),
                (Root: @"C:\Program Files (x86)\wkhtmltopdf", Folder: "bin"),
            };

            foreach (var (root, folder) in candidates)
            {
                if (File.Exists(Path.Combine(root, folder, "wkhtmltopdf.exe")))
                {
                    RotativaConfiguration.Setup(root, folder);
                    Ready = true;
                    return;
                }
            }

            Ready = false;
        }
    }
}