using System;
using System.ComponentModel.DataAnnotations;

public class BaseUser
{
    [Key]
    public int UserId { get; set; }
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? Email { get; set; }
    public string? Gender { get; set; }
    public DateTime BirthDate { get; set; }
    public string? ProfileImage { get; set; }

    public User? User { get; set; }
}
