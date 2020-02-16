using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutputCsvFile.Models
{
    public class CsvExportDto
    {
        public Guid? Id { get; set; }
        public string PatientId { get; set; }
        public string Institute { get; set; }
        public string VisitId { get; set; }
        public DateTime? CompleteDate { get; set; }
        public IEnumerable<Answer> TsqmAnswers { get; set; }

        /// <summary>
        ///  Assign the CsvExportDto class properties to the values in database and returns a list.
        ///  The entity is the model class
        ///  The identifier is the identification or the names of the three files DASS, HIT, TSQM
        /// </summary>
        public static CsvExportDto FromEntity(Interview entity, string identifier)
        {
            if (entity == null)
                return null;

            return new CsvExportDto
            {
                Id = entity.Id,
                PatientId = entity.Patient.Identifier,
                Institute = entity.Patient.Institute.Identifier,
                VisitId = entity.Identifier,
                CompleteDate = entity.CompletionDate,
                TsqmAnswers = entity.Questionnaires
                .Where(q => q.Definition.Identifier == identifier && q.IsCompleted == true)
                .Where(q => q.IsCompleted == true)
                .SelectMany(q => q.Answers).ToList<Answer>()
                .OrderBy(a => a.Question.Order)
            };
        }

        /// <summary>
        ///  Transfers the string to a file via FileStream, the filemode is in create mode meaning if the file already exists.
        ///  it will be overwritten.
        ///  The filePath is the path and name of the file to be created.
        ///  The string text are the data that are in the form of string then converted into bytes then write into a file. 
        /// </summary>
        public static async Task WriteTextAsync(string filePath, string text)
        {
            byte[] encodedText = Encoding.Unicode.GetBytes(text);

            using (FileStream sourceStream = new FileStream(filePath + ".csv",
                FileMode.Create, FileAccess.Write, FileShare.None,
                bufferSize: 4096, useAsync: true))
            {
                await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
                sourceStream.Flush();
            };
        }

        /// <summary>
        ///  A string builder that formats the contents\data into a string separated by semicolon.
        ///  It separates each value in the list by a semicolon and output as a string.
        ///  The stringbuilder buffer where data is to be stored
        ///  The CsvExportDto tsqm is the IENUMERABLE class
        /// </summary>
        public static void FormatTsqm(StringBuilder buffer, CsvExportDto tsqm)
        {
            if (tsqm != null)
            {
                buffer.AppendFormat($"{tsqm.PatientId};");
                buffer.AppendFormat($"{tsqm.Institute};");
                buffer.AppendFormat($"{tsqm.VisitId};");
                buffer.AppendFormat($"{(tsqm.CompleteDate?.ToString("dd.MM.yyyy hh:mm"))};");
                foreach (var item in tsqm.TsqmAnswers)
                {
                    if (item.Value != null)
                    {
                        buffer.AppendFormat($"{item.Value.GetValue("v1").ToString().Trim(new Char[] { '[', ']', ' ' }).TrimStart().TrimEnd()};");
                    }
                    else
                    {
                        buffer.AppendFormat($"{9999};");
                    }
                }

                buffer.Remove(buffer.Length - 1, 1);
                buffer.Append("\n");
            }
        }

        /// <summary>
        ///  A string builder that formats the header column of the csv file separated by column.
        ///  Returns a stringbuilder    
        /// </summary>
        public static StringBuilder FormatHeader(string file)
        {
            CsvExportDto tsqm = new CsvExportDto();
            StringBuilder str = new StringBuilder();
            int count = 0;

            foreach (var prop in tsqm.GetType().GetProperties())
            {

                if (prop.Name == "TsqmAnswers")
                {
                    if (file == "TSQM2")
                    {
                        while (count < 11)
                        {
                            count++;
                            str.AppendFormat($"{"Question" + count};");
                        }
                    }
                    else if (file == "DASS21")
                    {
                        while (count < 21)
                        {
                            count++;
                            str.AppendFormat($"{"Question" + count};");
                        }
                    }
                    else
                    {
                        while (count < 6)
                        {
                            count++;
                            str.AppendFormat($"{"Question" + count};");
                        }
                    }
                }
                else if (prop.Name != "Id")
                { str.AppendFormat($"{prop.Name};"); }

            }
            str.Remove(str.Length - 1, 1);
            str.Append("\n");
            return str;
        }
    }
}
