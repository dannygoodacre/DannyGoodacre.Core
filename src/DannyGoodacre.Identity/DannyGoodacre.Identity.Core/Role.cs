using System.ComponentModel.DataAnnotations;

namespace DannyGoodacre.Identity.Core;

public class Role
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }
}
