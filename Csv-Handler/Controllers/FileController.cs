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

    // ファイル保存ディレクトリが存在しない場合は作成
    public FileController()
    {
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    // CSVファイルをアップロードするAPI
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        // ファイルが選択されていない場合のエラーチェック
        if (file == null || file.Length == 0)
            return BadRequest("ファイルを選択してください");

        var filePath = Path.Combine(_storagePath, file.FileName);
        var processedFilePath = Path.Combine(_storagePath, "processed_" + file.FileName);

        // ファイルを保存
        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // CSVファイルを読み込み、処理して保存
        var records = ProcessCsv(filePath);

        // 処理されたCSVファイルを保存
        SaveProcessedCsv(records, processedFilePath);
        
        return Ok(new { fileName = "processed_" + file.FileName });
    }

    // ファイルをダウンロードするAPI
    [HttpGet("download/{fileName}")]
    public IActionResult DownloadFile(string fileName)
    {
        // ファイル名が無効な場合のエラーチェック
        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequest("ファイル名が無効です");

        var filePath = Path.Combine(_storagePath, fileName);

        // ファイルが見つからない場合のエラーチェック
        if (!System.IO.File.Exists(filePath))
            return NotFound(new { message = "ファイルが見つかりません" });

        var fileBytes = System.IO.File.ReadAllBytes(filePath);
        return File(fileBytes, "text/csv", fileName);
    }
    
    // CSVファイルを処理してSumとAverageを計算
    private List<FileModel> ProcessCsv(string filePath)
    {
        var records = new List<FileModel>();

        using var reader = new StreamReader(filePath, Encoding.UTF8);
        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
        {
            // CSVデータを読み込んでオブジェクトに変換
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

                // 合計と平均を計算
                record.Sum = new[] { record.Language, record.Mathematics, record.English }.Sum();
                record.Average = new[] { record.Language, record.Mathematics, record.English }.Average();
                record.Rank = 0; // 初期ランクを0に設定

                records.Add(record);
            }
        }

        // 平均点を基にランクを付ける
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

    // 修正されたCSVファイルを保存
    private void SaveProcessedCsv(List<FileModel> records, string filePath)
    {
        using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
        using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
        {
            // CSVファイルのヘッダーを書き込み
            csv.WriteHeader<FileModel>();
            csv.NextRecord();
            csv.WriteRecords(records);
        }
    }
}

