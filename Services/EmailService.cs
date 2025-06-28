using System.Net;
using System.Net.Mail;
using System.Text;
using System.IO;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VocabAutomation.Services.Interfaces;
using VocabAutomation.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;


namespace VocabAutomation.Services
{
    public class EmailService(IConfiguration config, ILogger<EmailService> logger) : IEmailService
    {
        private readonly string _gmailSender = config["GMAIL_SENDER"];
        private readonly string _gmailPassword = config["GMAIL_APP_PASSWORD"];
        private readonly string _recipient = config["RECIPIENT"];
        private readonly ILogger<EmailService> _logger = logger;

        public async Task SendVocabEmailAsync(List<VocabEntry> vocabList,
                                            string generalStory,
                                            string softwareStory,
                                            string subject)
        {
            try
            {
            
                var htmlBody = BuildHtmlBody(vocabList);
                var pdfBytes = GeneratePdf(vocabList, generalStory, softwareStory);

                using var message = new MailMessage
                {
                    From = new MailAddress(_gmailSender),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                message.To.Add(_recipient);

                var pdfStream = new MemoryStream(pdfBytes);
                var attachment = new Attachment(pdfStream, "vocab.pdf", "application/pdf");
                message.Attachments.Add(attachment);

                using var smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential(_gmailSender, _gmailPassword),
                    EnableSsl = true
                };

                await smtp.SendMailAsync(message);
                _logger.LogInformation("📧 Email sent to {Recipient} with attachment: vocab.pdf", _recipient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send vocab email.");
            }
        }


        private string BuildHtmlBody(List<VocabEntry> vocabList)
        {
            var sb = new StringBuilder("<h2>📘 Today's Vocabulary</h2><ul>");
            foreach (var entry in vocabList)
                sb.Append($"<li><b>{entry.Word}</b>: {entry.Meaning}</li>");
            sb.Append("</ul><p>Happy learning! 🌱</p>");
            return sb.ToString();
        }

private byte[] GeneratePdf(List<VocabEntry> vocabList, string generalStory, string softwareStory)
{
    using var ms = new MemoryStream();
    var document = new PdfDocument();
    var page = document.AddPage();
    var gfx = XGraphics.FromPdfPage(page);

    var fontRegular = new XFont("Arial", 12, XFontStyle.Regular);
    var fontBold = new XFont("Arial", 14, XFontStyle.Bold);
    double margin = 40;
    double y = margin;

    // Title: Vocabulary List
    gfx.DrawString("📘 Vocabulary List", fontBold, XBrushes.DarkBlue, new XRect(margin, y, page.Width, 20), XStringFormats.TopLeft);
    y += 30;

    foreach (var entry in vocabList)
    {
        gfx.DrawString($"{entry.Word}: {entry.Meaning}", fontRegular, XBrushes.Black, new XRect(margin, y, page.Width - 2 * margin, 20), XStringFormats.TopLeft);
        y += 20;
        if (y > page.Height - 100)
        {
            page = document.AddPage();
            gfx = XGraphics.FromPdfPage(page);
            y = margin;
        }
    }

    // Section: General Story
    y += 20;
    gfx.DrawString("📖 General Story", fontBold, XBrushes.DarkBlue, new XRect(margin, y, page.Width, 20), XStringFormats.TopLeft);
    y += 30;
    DrawWrappedText(ref page, ref gfx, generalStory, fontRegular, margin, ref y);

    // Section: Software Story
    y += 20;
    gfx.DrawString("💻 Software Story", fontBold, XBrushes.DarkBlue, new XRect(margin, y, page.Width, 20), XStringFormats.TopLeft);
    y += 30;
    DrawWrappedText(ref page, ref gfx, softwareStory, fontRegular, margin, ref y);

    document.Save(ms, false);
    return ms.ToArray();
}

private void DrawWrappedText(ref PdfPage page, ref XGraphics gfx, string text, XFont font, double margin, ref double y)
{
    var maxWidth = page.Width - 2 * margin;
    var words = text.Split(' ');
    var line = "";

    foreach (var word in words)
    {
        var testLine = line + word + " ";
        var size = gfx.MeasureString(testLine, font);
        if (size.Width > maxWidth)
        {
            gfx.DrawString(line, font, XBrushes.Black, new XRect(margin, y, maxWidth, 20), XStringFormats.TopLeft);
            y += 20;
            line = word + " ";
        }
        else
        {
            line = testLine;
        }

        if (y > page.Height - 50)
        {
            page = page.Owner.AddPage();
            gfx = XGraphics.FromPdfPage(page);
            y = margin;
        }
    }

    if (!string.IsNullOrWhiteSpace(line))
    {
        gfx.DrawString(line, font, XBrushes.Black, new XRect(margin, y, maxWidth, 20), XStringFormats.TopLeft);
        y += 20;
    }
}


    }
}
