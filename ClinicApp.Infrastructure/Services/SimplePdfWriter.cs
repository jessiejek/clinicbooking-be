using System.Text;

namespace ClinicApp.Infrastructure.Services;

public sealed record PdfTextLine(string Text, float FontSize = 11f, bool Bold = false, float SpaceBefore = 0f);

public static class SimplePdfWriter
{
    private const float PageWidth = 595f;
    private const float PageHeight = 842f;
    private const float MarginLeft = 42f;
    private const float MarginRight = 42f;
    private const float MarginTop = 42f;
    private const float MarginBottom = 48f;

    public static byte[] CreateDocument(IReadOnlyList<PdfTextLine> headerLines, IReadOnlyList<PdfTextLine> bodyLines)
    {
        var pages = BuildPages(headerLines, bodyLines);
        return BuildPdf(pages);
    }

    private static List<List<PdfTextLine>> BuildPages(IReadOnlyList<PdfTextLine> headerLines, IReadOnlyList<PdfTextLine> bodyLines)
    {
        var pages = new List<List<PdfTextLine>>();
        var currentPage = new List<PdfTextLine>();
        var currentHeight = 0f;
        var maxHeight = PageHeight - MarginTop - MarginBottom;

        void startNewPage()
        {
            if (currentPage.Count > 0)
            {
                pages.Add(currentPage);
            }

            currentPage = new List<PdfTextLine>();
            currentHeight = 0f;

            foreach (var headerLine in headerLines)
            {
                AppendLine(headerLine);
            }

            AppendLine(new PdfTextLine(string.Empty, 6f));
        }

        void AppendLine(PdfTextLine line)
        {
            var wrappedLines = WrapText(line.Text, line.FontSize);
            if (wrappedLines.Count == 0)
            {
                wrappedLines = [string.Empty];
            }

            foreach (var wrapped in wrappedLines)
            {
                var lineHeight = Math.Max(12f, line.FontSize * 1.35f) + line.SpaceBefore;
                if (currentPage.Count > 0 && currentHeight + lineHeight > maxHeight)
                {
                    pages.Add(currentPage);
                    currentPage = new List<PdfTextLine>();
                    currentHeight = 0f;

                    foreach (var headerLine in headerLines)
                    {
                        AppendLine(headerLine);
                    }

                    AppendLine(new PdfTextLine(string.Empty, 6f));
                }

                currentPage.Add(line with { Text = wrapped });
                currentHeight += lineHeight;
            }
        }

        startNewPage();

        foreach (var line in bodyLines)
        {
            AppendLine(line);
        }

        if (currentPage.Count > 0)
        {
            pages.Add(currentPage);
        }

        return pages;
    }

    private static byte[] BuildPdf(IReadOnlyList<List<PdfTextLine>> pages)
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            string.Empty,
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };

        var pageObjects = new List<(int PageObjectId, int ContentObjectId)>();
        var nextObjectId = 4;

        foreach (var _ in pages)
        {
            pageObjects.Add((nextObjectId, nextObjectId + 1));
            objects.Add(string.Empty); // page
            objects.Add(string.Empty); // content
            nextObjectId += 2;
        }

        var pagesKids = string.Join(" ", pageObjects.Select(x => $"{x.PageObjectId} 0 R"));
        objects[1] = $"<< /Type /Pages /Kids [{pagesKids}] /Count {pages.Count} >>";

        for (var pageIndex = 0; pageIndex < pages.Count; pageIndex++)
        {
            var page = pages[pageIndex];
            var pageObjectId = pageObjects[pageIndex].PageObjectId;
            var contentObjectId = pageObjects[pageIndex].ContentObjectId;
            var content = BuildPageContent(page);
            var contentLength = Encoding.UTF8.GetByteCount(content);

            objects[pageObjectId - 1] =
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth:0} {PageHeight:0}] /Resources << /Font << /F1 3 0 R /F2 3 0 R >> >> /Contents {contentObjectId} 0 R >>";
            objects[contentObjectId - 1] = $"<< /Length {contentLength} >>\nstream\n{content}\nendstream";
        }

        var result = new List<byte>();
        result.AddRange(Encoding.ASCII.GetBytes("%PDF-1.4\n"));

        var offsets = new List<int> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(result.Count);
            result.AddRange(Encoding.ASCII.GetBytes($"{i + 1} 0 obj\n{objects[i]}\nendobj\n"));
        }

        var xrefPosition = result.Count;
        var xrefBuilder = new StringBuilder();
        xrefBuilder.AppendLine("xref");
        xrefBuilder.AppendLine($"0 {objects.Count + 1}");
        xrefBuilder.AppendLine("0000000000 65535 f ");
        for (var i = 1; i < offsets.Count; i++)
        {
            xrefBuilder.AppendLine($"{offsets[i]:0000000000} 00000 n ");
        }

        xrefBuilder.AppendLine("trailer");
        xrefBuilder.AppendLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
        xrefBuilder.AppendLine("startxref");
        xrefBuilder.AppendLine(xrefPosition.ToString());
        xrefBuilder.Append("%%EOF");

        result.AddRange(Encoding.ASCII.GetBytes(xrefBuilder.ToString()));
        return result.ToArray();
    }

    private static string BuildPageContent(IReadOnlyList<PdfTextLine> lines)
    {
        var builder = new StringBuilder();
        var y = PageHeight - MarginTop;
        var textWidth = PageWidth - MarginLeft - MarginRight;

        foreach (var line in lines)
        {
            y -= Math.Max(0f, line.SpaceBefore);
            var fontName = line.Bold ? "F2" : "F1";
            var fontSize = line.FontSize <= 0 ? 11f : line.FontSize;
            var wrapped = WrapText(line.Text, fontSize, textWidth);

            foreach (var wrappedLine in wrapped)
            {
                y -= Math.Max(12f, fontSize * 1.35f);
                builder.AppendLine("BT");
                builder.AppendLine($"/{fontName} {fontSize:0.##} Tf");
                builder.AppendLine($"1 0 0 1 {MarginLeft:0.##} {y:0.##} Tm");
                builder.AppendLine($"({EscapePdfText(wrappedLine)}) Tj");
                builder.AppendLine("ET");
            }
        }

        return builder.ToString();
    }

    private static List<string> WrapText(string? text, float fontSize, float availableWidth = 511f)
    {
        var raw = (text ?? string.Empty)
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n');

        var maxChars = Math.Max(28, (int)Math.Floor(availableWidth / Math.Max(4.5f, fontSize * 0.52f)));
        var result = new List<string>();

        foreach (var paragraph in raw)
        {
            var trimmed = paragraph.TrimEnd();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                result.Add(string.Empty);
                continue;
            }

            var words = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var current = new StringBuilder();
            foreach (var word in words)
            {
                if (current.Length == 0)
                {
                    current.Append(word);
                    continue;
                }

                if (current.Length + 1 + word.Length > maxChars)
                {
                    result.Add(current.ToString());
                    current.Clear();
                    current.Append(word);
                }
                else
                {
                    current.Append(' ').Append(word);
                }
            }

            if (current.Length > 0)
            {
                result.Add(current.ToString());
            }
        }

        return result;
    }

    private static string EscapePdfText(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)");
    }
}
