namespace Goatrello.Models;

public class UserListUser
{
    public int ListId { get; set; }
    public UserList List { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}