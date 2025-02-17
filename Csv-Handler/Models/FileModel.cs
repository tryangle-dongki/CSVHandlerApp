using CsvHelper.Configuration.Attributes;

namespace csv_handler.Models;

public class FileModel
{
    [Name("名前")]
    public string? Name { get; set; }
    [Name("年齢")]
    public int Age { get; set; }
    [Name("国語")]
    public decimal Language { get; set; }
    [Name("数学")]
    public decimal Mathematics { get; set; }
    [Name("英語")]
    public decimal English { get; set; }
    [Name("合計")]
    public decimal Sum { get; set; }
    [Name("平均点")]
    public decimal Average { get; set; }
    [Name("順位")]
    public int Rank { get; set; }
}