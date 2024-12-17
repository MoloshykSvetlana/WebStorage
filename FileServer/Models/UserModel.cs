namespace FileServer.Models;

public class UserModel
{
    public int Id { get; set; }

    public string Login { get; set; }

    public string Password { get; set; }

    public int RoleModelId { get; set; }


    public List<FileModel> Files { get; set; }

    public List<AccessModel> Access { get; set; }

    public RoleModel RoleModel { get; set; }
}
