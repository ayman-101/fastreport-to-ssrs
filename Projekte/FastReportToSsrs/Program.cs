using System;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
using System.Linq;
using System.Text;

namespace FastReportToSsrs
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: FastReportToSsrs <path-to.frx>");
                return;
            }

            string frxPath = args[0];
            if (!File.Exists(frxPath))
            {
                Console.WriteLine($"File not found: {frxPath}");
                return;
            }

            string outputPath = Path.ChangeExtension(frxPath, ".rdl");
            ConvertFrxToRdl(frxPath, outputPath);
            Console.WriteLine($"Generated: {outputPath}");
        }

        static void ConvertFrxToRdl(string frxPath, string rdlPath)
        {
            XDocument frxDoc;
            
            // .frx kann komprimiert sein (GZip) oder Plain XML
            try
            {
                frxDoc = XDocument.Load(frxPath);
            }
            catch
            {
                // Versuch als komprimiertes XML zu laden
                using var fs = File.OpenRead(frxPath);
                using var gzip = new GZipStream(fs, CompressionMode.Decompress);
                frxDoc = XDocument.Load(gzip);
            }

            // SSRS RDL Grundgerüst
            var rdl = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("Report",
                    new XAttribute(XNamespace.Xmlns + "rd", "http://schemas.microsoft.com/SQLServer/reporting/reportdesigner"),
                    new XElement("ReportSections",
                        new XElement("ReportSection",
                            new XElement("Body",
                                new XElement("ReportItems")
                            ),
                            new XElement("Width", "8.5in"),
                            new XElement("Page",
                                new XElement("PageHeader"),
                                new XElement("PageFooter")
                            )
                        )
                    ),
                    new XElement("DataSets")
                )
            );

            var reportItems = rdl.Root
                .Element("ReportSections")
                .Element("ReportSection")
                .Element("Body")
                .Element("ReportItems");

            var dataSets = rdl.Root.Element("DataSets");

            // Bands parsen und konvertieren
            var bands = frxDoc.Descendants()
                .Where(e => e.Name.LocalName.Contains("Band"))
                .ToList();

            double topPosition = 0;
            
            foreach (var band in bands)
            {
                string bandType = band.Name.LocalName;
                
                switch (bandType)
                {
                    case "ReportTitleBand":
                        // Report Header
                        break;
                    case "PageHeaderBand":
                        // Page Header
                        break;
                    case "PageFooterBand":
                        // Page Footer
                        break;
                    case "GroupHeaderBand":
                        // Group Header -> Tablix Group
                        break;
                    case "GroupFooterBand":
                        // Group Footer
                        break;
                    case "DataBand":
                        // Detail -> Tablix Row
                        ConvertDataBand(band, reportItems, ref topPosition);
                        break;
                }
            }

            // DataSources aus dem Code-befüllten Bereich
            // Hinweis: Die eigentlichen Daten kommen zur Laufzeit
            
            rdl.Save(rdlPath);
        }

        static void ConvertDataBand(XElement band, XElement parent, ref double top)
        {
            // TextObjects finden
            var texts = band.Descendants()
                .Where(e => e.Name.LocalName == "TextObject")
                .ToList();

            foreach (var text in texts)
            {
                var name = text.Attribute("Name")?.Value ?? "Textbox1";
                var textValue = text.Element("Text")?.Value ?? "";
                var left = text.Attribute("Left")?.Value ?? "0";
                var topAttr = text.Attribute("Top")?.Value ?? "0";
                var width = text.Element("Width")?.Value ?? "1in";
                var height = text.Element("Height")?.Value ?? "0.25in";

                var textbox = new XElement("Textbox",
                    new XAttribute("Name", name),
                    new XElement("CanGrow", "true"),
                    new XElement("Value", textValue),
                    new XElement("Left", left),
                    new XElement("Top", topAttr),
                    new XElement("Width", width),
                    new XElement("Height", height)
                );

                parent.Add(textbox);
            }

            // Images
            var images = band.Descendants()
                .Where(e => e.Name.LocalName == "PictureObject")
                .ToList();

            foreach (var img in images)
            {
                var name = img.Attribute("Name")?.Value ?? "Image1";
                var left = img.Attribute("Left")?.Value ?? "0";
                var topAttr = img.Attribute("Top")?.Value ?? "0";
                var width = img.Element("Width")?.Value ?? "1in";
                var height = img.Element("Height")?.Value ?? "1in";

                var image = new XElement("Image",
                    new XAttribute("Name", name),
                    new XElement("Source", "External"),
                    new XElement("Value", ""), // Zur Laufzeit setzen
                    new XElement("Left", left),
                    new XElement("Top", topAttr),
                    new XElement("Width", width),
                    new XElement("Height", height)
                );

                parent.Add(image);
            }
        }
    }
}