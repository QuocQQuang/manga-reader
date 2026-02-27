using System.ComponentModel.DataAnnotations;

public class SyncStatus
{
    [Key]
    public string Key { get; set; } = null!;
    
    public string Status { get; set; } = null!;
    
    public string Data { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}