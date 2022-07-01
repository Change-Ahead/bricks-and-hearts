using System.ComponentModel.DataAnnotations.Schema;

namespace BricksAndHearts.Models;

[Table("User")]
public class UserDbModel
{
    public int Id { get; set; }

    public string GoogleAccountId { get; set; } = null!;

    public string GoogleUserName { get; set; } = null!;

    public string GoogleEmail { get; set; } = null!;

    public bool IsAdmin { get; set; }
}