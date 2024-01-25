using System;
using System.IO;
using System.Text.RegularExpressions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

class Program
{
    private static readonly string[] headers = ["ATCUD", "Fatura", "NIF Adquirente", "Data", "Total", "Base", "IVA", "IRS"];

    static void Main()
    {
        string pdfFilePath = "C:\\Users\\funny\\OneDrive\\Ambiente de Trabalho\\report.pdf";
        string resultFilePath = "C:\\Users\\funny\\OneDrive\\Ambiente de Trabalho\\result.csv";

        // Create a PdfReader object to read the PDF file
        using var pdfReader = new PdfReader(pdfFilePath);
        // Create a PdfDocument object to represent the PDF document
        using var pdfDocument = new PdfDocument(pdfReader);

        using var resultWriter = new StreamWriter(resultFilePath, false);

        resultWriter.WriteLine(string.Join(";", headers));

        // Iterate through all the pages in the PDF document
        for (int pageNumber = 1; pageNumber <= pdfDocument.GetNumberOfPages(); pageNumber++)
        {
            // Extract text from each page
            string text = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(pageNumber));

            var lines = text.Split('\n');

            var numero_fatura = GetFieldByBegginingOfLine(lines, "Fatura  Nº");

            var atcud = GetFieldByBegginingOfLine(lines, "ATCUD");

            // Create a pattern for a word that starts with the letter "M"
            string pattern = @"\d{9}";

            // Create a Regex
            var rg = new Regex(pattern);

            var fiscalNumbers = rg.Matches(text);

            if (fiscalNumbers.Count != 2)
            {
                throw new Exception("Falhou em encontrar exatamente 2 NIFs");
            }

            var recievingNIF = fiscalNumbers[0].Value;

            var emittingNIF = fiscalNumbers[1].Value;

            var date = GetFieldByPartialMatch(lines, "Data");

            var totalIliquido = GetFieldByPartialMatch(lines, "Total Ilíquido").Replace('.', ',');

            var totalIva = GetFieldByPartialMatch(lines, "Total IVA").Replace('.', ',');

            var totalNeto = GetFieldByPartialMatch(lines, "TOTAL Neto").Replace('.', ',');

            if (!float.TryParse(totalIliquido, out float totalIliquidoAsNumber))
            {
                throw new Exception($"Falhou ao converter {totalIliquido} para número - ILIQUIDO");
            }

            if (!float.TryParse(totalIva, out float totalIvaAsNumber))
            {
                throw new Exception($"Falhou ao converter {totalIva} para número - IVA");
            }

            if (!float.TryParse(totalNeto, out float totalNetoAsNumber))
            {
                throw new Exception($"Falhou ao converter {totalNeto} para número - NETO");
            }

            var totalDocAsNumber = totalIliquidoAsNumber + totalIvaAsNumber;
            Console.WriteLine($"Total documento: {totalDocAsNumber:0.00}");

            var totalIrsAsNumber = totalDocAsNumber - totalNetoAsNumber;
            Console.WriteLine($"Valor IRS: {totalIrsAsNumber:0.00}");


            //["ATCUD", "Nº Fatura", "NIF Adquirente", "Data", "Total", "Base", "IVA", "IRS"];
            string[] documentInfo = [atcud, numero_fatura, emittingNIF, date, EscapeCurrencyValue(totalDocAsNumber), EscapeCurrencyValue(totalIliquidoAsNumber), EscapeCurrencyValue(totalIvaAsNumber), EscapeCurrencyValue(totalIrsAsNumber)];

            resultWriter.WriteLine(string.Join(";", documentInfo));

        }
    }

    private static string EscapeCurrencyValue(float value)
    {
        return $"\"{value:0.00}\"";
    }

    private static string GetFieldByBegginingOfLine(string[] lines, string begginingOfLine)
    {
        var targetField = lines.Where(l => l.StartsWith(begginingOfLine)).SingleOrDefault();

        if (targetField == default)
        {
            throw new Exception($"Não encontrou apenas um inicio de frase: {begginingOfLine}");
        }

        targetField = targetField.Replace(begginingOfLine, string.Empty);

        targetField = targetField.Trim();

        Console.WriteLine($"{begginingOfLine}: {targetField}");

        return targetField;

    }

    private static string GetFieldByPartialMatch(string[] lines, string fieldStart)
    {
        var targetField = lines.Where(l => l.Contains(fieldStart, StringComparison.CurrentCultureIgnoreCase)).SingleOrDefault();

        if (targetField == default)
        {
            throw new Exception($"Não encontrou apenas um conteudo parcial: {fieldStart}");
        }

        var indexOfFieldStart = targetField.IndexOf(fieldStart, StringComparison.CurrentCultureIgnoreCase);

        var targetFieldStart = indexOfFieldStart + fieldStart.Length;

        var targetContent = targetField[targetFieldStart..];

        targetContent = targetContent.Trim();

        Console.WriteLine($"{fieldStart}: {targetContent}");

        return targetContent;
    }
}
