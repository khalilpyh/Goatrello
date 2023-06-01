namespace Goatrello.Models;

public class UserList
{
    public int Id { get; set; }
    public ICollection<UserListUser> Users { get; set; } = new HashSet<UserListUser>();
}