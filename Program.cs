using System;
using System.IO;
using System.Text.RegularExpressions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

public class Program
{
    private static readonly string[] headers = ["NIF Estafeta","NIF Adquirente", "ATCUD", "Fatura", "Data", "Total", "IVA", "Base", "IRS"];

    public static void Main()
    {
        string pdfFilePath = "C:\\Users\\funny\\OneDrive\\Ambiente de Trabalho\\Glovo Ubers\\All reports\\report.pdf";
        string resultFilePath = "C:\\Users\\funny\\OneDrive\\Ambiente de Trabalho\\Glovo Ubers\\All reports\\result.csv";

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

            var emittingNIF = "515642428";

            var recievingNIF = lines[7];

            if(string.IsNullOrWhiteSpace(recievingNIF)) 
            {
                throw new Exception("Não encontrou NIF recetor");
            }

            var dateAsString = GetFieldByPartialMatch(lines, "Data");

            var date = DateOnly.Parse(dateAsString);

            dateAsString = date.ToString("yyyy-MM-dd");

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


            //["NIF Adquirente", "ATCUD", "Fatura", "Data", "Total", "Base", "IVA", "IRS"];
            string[] documentInfo = [recievingNIF,emittingNIF, atcud, numero_fatura, dateAsString, EscapeCurrencyValue(totalDocAsNumber), EscapeCurrencyValue(totalIvaAsNumber), EscapeCurrencyValue(totalIliquidoAsNumber), EscapeCurrencyValue(totalIrsAsNumber)];

            resultWriter.WriteLine(string.Join(";", documentInfo));

        }
    }

    private static string EscapeCurrencyValue(float value)
    {
        var absoluteValue = Math.Abs(value);
        return $"\"{absoluteValue:0.00}\"";
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
