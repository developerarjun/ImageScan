using edu.stanford.nlp.ie.crf;

using FormRecogn.Models;

using IronOcr;

using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

using Microsoft.AspNetCore.Mvc;

using NameParser;

using Newtonsoft.Json;

using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace FormRecogn.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ResumeController : ControllerBase
    {
        [HttpPost("IronOCR")]
        public async Task<ActionResult> GetResume(IFormFile card)
        {
            using (OcrInput input = new OcrInput())
            {
                var ocr = new IronTesseract();
                var stream = card.OpenReadStream();
                input.AddPdf(stream);
                OcrResult result = ocr.Read(input);
                var resume = new Resume();
                // Extract name
                resume.Name = Regex.Match(result.Text, @"^(.+)$", RegexOptions.Multiline).Groups[1].Value.Trim();
                // Extract position
                resume.Position = Regex.Match(result.Text, @"(?<=\n)[A-Za-z\s]+(?=\n)").Value.Trim();
                // Extract address
                resume.Address = Regex.Match(result.Text, @"(?<=\n)[A-Za-z0-9\s,]+(?=\nPhone)").Value.Trim();
                // Extract phone number
                resume.Phone = Regex.Match(result.Text, @"\+?\d{1,4}?[-.\s]?\(?\d{1,3}?\)?[-.\s]?\d{1,4}[-.\s]?\d{1,4}[-.\s]?\d{1,9}").Value.Trim();
                // Extract email
                resume.Email = Regex.Match(result.Text, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b").Value.Trim();

                return Ok(resume);
            }
        }

        [HttpPost("itext7")]
        public async Task<ActionResult> GetResumeItext(IFormFile card)
        {
            var stream = card.OpenReadStream();
            StringBuilder text = new StringBuilder();
            var reader = new PdfReader(stream);
            iText.Kernel.Pdf.PdfDocument pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader);
            int numberofpages = pdfDoc.GetNumberOfPages();
            for (int page = 1; page <= numberofpages; page++)
            {
                iText.Kernel.Pdf.Canvas.Parser.Listener.ITextExtractionStrategy strategy = new iText.Kernel.Pdf.Canvas.Parser.Listener.SimpleTextExtractionStrategy();
                string currentText = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page), strategy);
                currentText = Encoding.UTF8.GetString(ASCIIEncoding.Convert(
                    Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(currentText)));
                text.Append(currentText);
            }
            var resume = new Resume();
            var txt = text.ToString();
            // Extract name
            txt = txt.Replace("\n", string.Empty).Replace("\r", string.Empty);
            var jarRoot = @"D:\stanford-ner-4.2.0\stanford-ner-2020-11-17";
            var classifiersDirectory = jarRoot + @"\classifiers";

            var classifier = CRFClassifier.getClassifierNoExceptions(
           classifiersDirectory + @"\english.all.3class.distsim.crf.ser.gz");

            var xmlResume = classifier.classifyWithInlineXML(txt);

            String St = xmlResume;

            int pFrom = xmlResume.IndexOf("<PERSON>") + "<PERSON>".Length;
            int pTo = xmlResume.IndexOf("</PERSON>");
            //Full Name
            resume.Name = xmlResume.Substring(pFrom, pTo - pFrom);

            int lFrom = xmlResume.IndexOf("<LOCATION>") + "<LOCATION>".Length;
            int lTo = xmlResume.IndexOf("</LOCATION>");
            //Full Name
            resume.Address = xmlResume.Substring(lFrom, lTo - lFrom);
            // Extract position
            resume.Position = Regex.Match(txt, @"(?<=\n)[A-Za-z\s]+(?=\n)").Value.Trim();
            // Extract address
            //resume.Address = Regex.Match(txt, @"(?<=\n)[A-Za-z0-9\s,]+(?=\nPhone)").Value.Trim();
            // Extract phone number
            resume.Phone = Regex.Match(txt, @"\+?\d{1,4}?[-.\s]?\(?\d{1,3}?\)?[-.\s]?\d{1,4}[-.\s]?\d{1,4}[-.\s]?\d{1,9}").Value.Trim();
            // Extract email
            resume.Email = Regex.Match(txt, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b").Value.Trim();

            return Ok(resume);
        }
    }
}
