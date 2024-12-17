using System.ComponentModel;

namespace FileServer.Models;

public class FileModel
{
    public int Id { get; set; }

    [DisplayName("Наименование")]
    public string Name { get; set; }

    public int UserModelId { get; set; }

    public int ContentLength { get; set; }

    public long Offset { get; set; }

    [DisplayName("Уровень доступа")]
    public int ShareToAll { get; set; }

    
    public UserModel UserModel { get; set; }

    public List<AccessModel> SharedUsers { get; set; }
}