using System;

public class UserProfileDto
{
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? Email { get; set; }
    public string? Gender { get; set; }
    public DateTime BirthDate { get; set; }
    public string? ProfileImage { get; set; } // Base64 string formatında resim
}
