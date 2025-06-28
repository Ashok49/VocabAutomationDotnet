using System.Net;
using System.Net.Mail;
using System.Text;
using System.IO;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VocabAutomation.Services.Interfaces;
using VocabAutomation.Models;

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
                var pdfPath = GeneratePdf(vocabList, generalStory, softwareStory);

                using var message = new MailMessage(_gmailSender, _recipient, subject, htmlBody);
                message.IsBodyHtml = true;
                message.Attachments.Add(new Attachment(pdfPath));

                using var smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential(_gmailSender, _gmailPassword),
                    EnableSsl = true
                };

                await smtp.SendMailAsync(message);

                _logger.LogInformation("📧 Email sent to {Recipient} with attachment: {PdfPath}", _recipient, pdfPath);
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

        private string GeneratePdf(List<VocabEntry> vocabList, string generalStory, string softwareStory)
        {
            var fileName = $"vocab_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var writer = new PdfWriter(fileName);
            var pdf = new PdfDocument(writer);
            var doc = new Document(pdf);

            doc.Add(new Paragraph("📘 Vocabulary List").SetBold().SetFontSize(14));
            foreach (var entry in vocabList)
            {
                doc.Add(new Paragraph($"{entry.Word}: {entry.Meaning}").SetFontSize(11));
            }

            doc.Add(new Paragraph("\n📖 General Story").SetBold().SetFontSize(14));
            doc.Add(new Paragraph(generalStory).SetFontSize(11).SetTextAlignment(TextAlignment.JUSTIFIED));

            doc.Add(new Paragraph("\n💻 Software Story").SetBold().SetFontSize(14));
            doc.Add(new Paragraph(softwareStory).SetFontSize(11).SetTextAlignment(TextAlignment.JUSTIFIED));

            doc.Close();
            return fileName;
        }
    }
}
