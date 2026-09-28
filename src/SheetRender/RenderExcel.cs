using TUnit.Core.Executors;
using Application = Microsoft.Office.Interop.Excel.Application;

[STAThreadExecutor]
public class RenderExcel
{
    [Test]
    [Explicit]
    public void Run()
    {
        var directory = ProjectFiles.SolutionDirectory;
        var excelFiles = Directory.EnumerateFiles(directory, "*.verified.xlsx", SearchOption.AllDirectories).ToList();
        foreach (var file in excelFiles)
        {
            var name = Path.GetFileName(file);
            if (name.StartsWith("~"))
            {
                continue;
            }

            Convert(file);
        }
    }

    [Test]
    [Explicit]
    public void RunSingle()
    {
        var directory = ProjectFiles.SolutionDirectory;
        var path = Path.Combine(directory, @"Excelsior.Tests\UsageTests.Test.verified.xlsx");
        Convert(path);
    }

    static void Convert(string excelPath)
    {
        Thread.Sleep(100);
        Application? excel = null;
        Workbook? book = null;

        try
        {
            excel = new()
            {
                Visible = true,
                DisplayAlerts = true,
                ScreenUpdating = true
            };

            // Open workbook
            book = excel.Workbooks.Open(excelPath);

            foreach (Worksheet sheet in book.Sheets)
            {
                RenderSheet(excelPath, sheet);
            }
        }
        catch (Exception exception)
        {
            throw new("Failed for: " + excelPath, exception);
        }
        finally
        {
            try
            {
                if (book != null)
                {
                    Marshal.ReleaseComObject(book);
                }

                if (excel != null)
                {
                    excel.Quit();
                    Marshal.ReleaseComObject(excel);
                }
            }
            finally
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }

    static void RenderSheet(string excelPath, Worksheet sheet)
    {
        try
        {
            var imageFile = excelPath
                .Replace(".verified", "")
                .Replace(".xlsx", $"_{sheet.Name}.png");

            if (File.Exists(imageFile) &&
                File.GetLastWriteTimeUtc(imageFile) > File.GetLastWriteTimeUtc(excelPath))
            {
                return;
            }

            var range = sheet.UsedRange;
            sheet.Activate();
            range.Select();
            Clipboard.Clear();
            Thread.Sleep(100);
            range.Copy();
            Exception? lastException = null;
            for (var i = 0; i < 10; i++)
            {
                try
                {
                    range.CopyPicture(XlPictureAppearance.xlScreen, XlCopyPictureFormat.xlBitmap);
                    lastException = null;
                    break;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
                finally
                {
                    Thread.Sleep(500);
                }
            }

            if (lastException != null)
            {
                throw new($"CopyPicture failed after 10 attempts for sheet '{sheet.Name}'", lastException);
            }

            using var image = Clipboard.GetImage();
            if (image == null)
            {
                throw new($"Failed to capture image from clipboard for sheet '{sheet.Name}'");
            }

            image.Save(imageFile, ImageFormat.Png);
        }
        finally
        {
            Marshal.ReleaseComObject(sheet);
        }
    }
}
