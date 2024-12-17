using System.ComponentModel;

namespace FileServer.Models;

public class AccessModel
{
    public int Id { get; set; }

    [DisplayName("Пользователь")]
    public int? UserModelId { get; set; }

    [DisplayName("Файл")]
    public int FileModelId { get; set; }

    [DisplayName("Доступ")]
    public int AccessLevel { get; set; }


    public UserModel UserModel { get; set; }

    public FileModel FileModel { get; set; }
}