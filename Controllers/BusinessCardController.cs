using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;

using edu.stanford.nlp.ie.crf;
using FormRecogn.Models;

using iText.Kernel.Pdf.Canvas.Parser;

using iText.Kernel.Pdf;

using Microsoft.AspNetCore.Mvc;

using System.Text;
using IronOcr;
using System.Text.RegularExpressions;

namespace FormRecogn.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BusinessCardController : ControllerBase
    {

        [HttpPost("IronOCR")]
        public async Task<ActionResult> GetCard(IFormFile card)
        {
            using (OcrInput input = new OcrInput())
            {
                var ocr = new IronTesseract();
                var stream = card.OpenReadStream();
                input.AddImage(stream);
                OcrResult result = ocr.Read(input);
                var txt = result.Text;
                var resume = new Resume();
                txt = txt.Replace("\r", string.Empty).Replace("\n", " ");
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
                resume.Address = xmlResume.Substring(lFrom, lTo - lFrom);
                return Ok(resume);
            }
        }
    }
}