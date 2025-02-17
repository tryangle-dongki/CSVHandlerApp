using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using csv_handler.Models;

namespace csv_handler.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FileController : Controller
{
    private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");

    public FileController()
    {
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("ファイルを選択してください");

        var filePath = Path.Combine(_storagePath, file.FileName);
        var processedFilePath = Path.Combine(_storagePath, "processed_" + file.FileName);

        // 파일 저장
        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // CSV 파일을 읽어서 가공 후 저장
        var records = ProcessCsv(filePath);

        // 수정된 CSV 파일 저장
        SaveProcessedCsv(records, processedFilePath);

        // return Ok(new { fileName = filePath });
        return Ok(new { fileName = "processed_" + file.FileName });
    }

    [HttpGet("download/{fileName}")]
    public IActionResult DownloadFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequest("ファイル名が無効です");

        var filePath = Path.Combine(_storagePath, fileName);

        if (!System.IO.File.Exists(filePath))
            return NotFound(new { message = "ファイルが見つかりません" });

        var fileBytes = System.IO.File.ReadAllBytes(filePath);
        return File(fileBytes, "text/csv", fileName);
    }
    
    // CSV 파일을 읽고 Sum과 Average 계산 후 반환
    private List<FileModel> ProcessCsv(string filePath)
    {
        var records = new List<FileModel>();

        using var reader = new StreamReader(filePath, Encoding.UTF8);
        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
        {
            // CSV 데이터를 읽어온 후 객체로 변환
            csv.Read();
            csv.ReadHeader();

            while (csv.Read())
            {
                var record = new FileModel
                {
                    Name = csv.GetField<string>("名前"),
                    Age = csv.GetField<int>("年齢"),
                    Language = csv.GetField<decimal>("国語"),
                    Mathematics = csv.GetField<decimal>("数学"),
                    English = csv.GetField<decimal>("英語")
                };

                // 합계와 평균 계산
                record.Sum = record.Language + record.Mathematics + record.English;
                record.Average = Math.Round(record.Sum / 3, 2);
                record.Rank = 0; // 기본값으로 0을 설정. 나중에 랭킹을 추가할 수 있음.

                records.Add(record);
            }
        }

        var rankRecords = records
            .Select((record, index) => new { record, index })
            .OrderByDescending(x => x.record.Average)
            .ToList();
            
        int currentRank = 1;
        for (int i = 0; i < rankRecords.Count; i++)
        {
            if (i > 0 && rankRecords[i].record.Average != rankRecords[i - 1].record.Average)
            {
                currentRank = i + 1;
            }
                
            records[rankRecords[i].index].Rank = currentRank;
        }

        return records;
    }

    // 수정된 CSV 파일 저장
    private void SaveProcessedCsv(List<FileModel> records, string filePath)
    {
        using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
        using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
        {
            csv.WriteHeader<FileModel>();
            csv.NextRecord();
            csv.WriteRecords(records);
        }
    }
}

