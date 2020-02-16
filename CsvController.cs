using Ionic.Zip;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OutputCsvFile.Datapersist;
using OutputCsvFile.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace OutputCsvFile.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CsvController : ControllerBase
    {
        private readonly InfinityDataContext _context;
        private IHostingEnvironment _env;

        private enum identifier { TSQM2, DASS21, HIT6 }

        public CsvController(InfinityDataContext context, IHostingEnvironment env)
        {
            _context = context;
            _env = env;
        }

        /// <summary>
        ///  Performs an important function to zip and export the 3 csv files. 
        ///  This calls the method ProcessWrite to save the files using stream then add the name of the 3 files to zip file.
        ///  Then copy the files into memory stream and save to the zip.
        ///  Return or output the zip file.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("", Name = "GetExport")]
        public async Task<IActionResult> GetExport()
        {
            var contentRootPath = _env.ContentRootPath;

            using (ZipFile zip = new ZipFile())
            {
                zip.AlternateEncodingUsage = ZipOption.AsNecessary;
                zip.AddDirectoryByName("Files");


                //Set the Name of Zip File.
                string zipName = String.Format("Zip_{0}.zip", DateTime.Now.ToString("yyyy-MMM-dd-HHmmss"));
                var memoryStream = new MemoryStream();
                foreach (identifier val in Enum.GetValues(typeof(identifier)))
                {
                    var file = System.IO.Path.Combine(contentRootPath, val.ToString());
                    ProcessWrite(file);
                    zip.AddFile(val.ToString() + ".csv", "Files");

                    using (var stream = new FileStream(file + ".csv", FileMode.Open))
                    {
                        await stream.CopyToAsync(memoryStream);
                    }
                }

                //Save the Zip File to MemoryStream.
                zip.Save(memoryStream);

                memoryStream.Position = 0;
                return File(memoryStream, System.Net.Mime.MediaTypeNames.Application.Octet, zipName);
            }
        }

        /// <summary>
        ///  Calls the database(query) and  returns a list.
        ///  Calls the method FormatTsqm to format each data in the list to a string separated by semicolon
        ///  Calls the method FormatHeader to format the header or column name in the csv
        ///  Calls the method WriteTextAsync to transfer the data in the form of string into a file
        ///  The string file is the name of the file.
        /// </summary>
        private async void ProcessWrite(string file)
        {
            var buffer = new StringBuilder();
            List<CsvExportDto> list = await _context.Interviews
                .Include(i => i.Patient)
                .ThenInclude(n => n.Institute)
                .Include(i => i.Questionnaires)
                    .ThenInclude(q => q.Answers)
                        .ThenInclude(a => a.Question)
                .Include(i => i.Questionnaires)
                    .ThenInclude(q => q.Definition)
                .Where(i => i.IsCompleted)
                .OrderBy(p => p.CompletionDate)
                .Select(p => CsvExportDto.FromEntity(p, file))
                .ToListAsync();

            foreach (var tsqm in list)
            {
                CsvExportDto.FormatTsqm(buffer, tsqm);
            }

            buffer = CsvExportDto.FormatHeader(file).Append(buffer);

            await CsvExportDto.WriteTextAsync(file, buffer.ToString());
        }
    }
}