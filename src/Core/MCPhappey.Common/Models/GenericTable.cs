namespace MCPhappey.Common.Models;

public class GenericTable
{
    public List<string> Columns { get; set; } = [];
    
    public List<Dictionary<string, object?>> Rows { get; set; } = [];
}