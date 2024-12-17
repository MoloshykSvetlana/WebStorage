using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FileServer;
using FileServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OclUdp.Sockets;

namespace Employee.Controllers
{
    public class FilesController : Controller
    {
        private readonly ApplicationContext _context;
        IWebHostEnvironment _appEnvironment;

        public FilesController(ApplicationContext context, IWebHostEnvironment appEnvironment)
        {
            _context = context;
            _appEnvironment = appEnvironment;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = HttpContext.User.Identity.Name;

            var findUser = _context.Users.FirstOrDefault(u => u.Login == user);

            if (findUser.RoleModelId == 1)
            {
                var res = _context.Files // Извлекаем файлы из базы данных, включая связанные сущности UserModel, без отслеживания изменений.
                    .Include(x => x.UserModel)
                    .AsNoTracking()
                    .ToList();

                return View(res);
            }
/*Извлекаем доступы пользователя из базы данных, включая связанные сущности FileModel, где UserModelId равен идентификатору пользователя findUser.Id.
Далее, преобразуем результат в список объектов FileModel.*/
            var acc = _context.Accesses
                .Include(x => x.FileModel)
                .Where(x => x.UserModelId == findUser.Id)
                .Select(x => new FileModel
                {
                    Id = x.FileModel.Id,
                    Name = x.FileModel.Name,
                    UserModelId = x.FileModel.UserModelId,
                    ContentLength = x.FileModel.ContentLength,
                    Offset = x.FileModel.Offset,
                    ShareToAll = x.FileModel.ShareToAll,
                    UserModel = x.FileModel.UserModel,
                    SharedUsers = x.FileModel.SharedUsers
                }).AsEnumerable();

            var query = _context.Files
                .Where(x => x.ShareToAll == 2 || x.UserModelId == findUser.Id)
                .Include(x => x.UserModel)
                .AsNoTracking()
                .AsEnumerable();

            var result = acc.Union(query).ToList();

            _context.DetachEntities(result);
            return View(result);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var user = HttpContext.User.Identity.Name;

            var findUser = _context.Users.FirstOrDefault(u => u.Login == user); 

            var itemToUpdate = await _context.Files
                .Include(x => x.UserModel)
                .FirstAsync(t => t.Id == id);

            _context.DetachEntity(itemToUpdate);
// Ищем доступ, где Id файла соответствует itemToUpdate.Id и уровень доступа равен 2.
            var findAccess = _context.Accesses.FirstOrDefault(u => u.FileModel.Id == itemToUpdate.Id && u.AccessLevel == 2 );

            if (findAccess is not null || findUser.RoleModelId == 1 || itemToUpdate.UserModelId == findUser.Id)
            {
                var radioTypes = new [] {
                    new {Id = 1, Name = "Приватный"},
                    new {Id = 2, Name = "Общий"}
                };

                ViewBag.Radio = radioTypes;

                return View(itemToUpdate);

            }

            return RedirectToAction("Index", "Files");
        }

        [HttpPost]
        public async Task<IActionResult> Update(FileModel item)
        {
            _context.Files.Update(item);
            await _context.SaveChangesAsync();

            _context.DetachEntity(item);

            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var user = HttpContext.User.Identity.Name;

            var findUser = _context.Users.FirstOrDefault(u => u.Login == user);

            var itemToDelete = await _context.Files
                .FirstOrDefaultAsync(t => t.Id == id);

            var findAccess = _context.Accesses.FirstOrDefault(u => u.FileModel.Id == itemToDelete.Id && u.AccessLevel == 2 );

            if (findAccess is not null || findUser.RoleModelId == 1 || itemToDelete.UserModelId == findUser.Id)
            {
                _context.Files.Remove(itemToDelete);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction("Index", "Files"); // Если найден доступ, или роль пользователя с именем findUser равна 1, или UserModelId файла равен идентификатору пользователя findUser.Id,
            // то устанавливаем ViewBag.Radio и возвращаем представление с объектом itemToUpdate.
        }

        public async Task<IActionResult> Download(int id) 
        {
            var itemToUpdate = await _context.Files
                .Include(x => x.UserModel)
                .FirstAsync(t => t.Id == id);
// Извлекаем файл из базы данных по указанному идентификатору, включая связанную сущность UserModel.

            _context.DetachEntity(itemToUpdate);


            string path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..\\FileServer\\bin\\Debug\\net7.0\\Data.txt")); // Получаем полный путь к файлу, который нужно скачать, в данном случае "Data.txt".
            var buf = new byte[itemToUpdate.ContentLength]; //кол-во байт считываемых с файла

            using (FileStream fstream = System.IO.File.OpenRead(path)) //открытие файла
            {
                fstream.Seek(itemToUpdate.Offset, SeekOrigin.Begin); //перемещаемся на оффсет выгружаемого файла
                await fstream.ReadAsync(buf, 0, buf.Length); //загрузка ко-ва байт начиная с оффсет
            }

            return File(buf, "application/pdf", itemToUpdate.Name);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var user = HttpContext.User.Identity.Name;

            var findUser = _context.Users.FirstOrDefault(u => u.Login == user);

            return View();
        }

        public async Task<IActionResult> Create(IFormFile uploadedFile)
        {
            var user = HttpContext.User.Identity.Name;

            var findUser = _context.Users.FirstOrDefault(u => u.Login == user);

            if (uploadedFile != null)
            {
                string path = "/Files/" + uploadedFile.FileName;
                // сохраняем файл в папку Files в каталоге wwwroot
                using (var fileStream = new FileStream(_appEnvironment.WebRootPath + path, FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(fileStream); // Копируем содержимое загруженного файла в FileStream для сохранения на сервере.
                }
            }

            var a = _appEnvironment.WebRootPath + "/Files/" + uploadedFile.FileName;
            
            Stream file = System.IO.File.OpenRead(a);
            byte[] bytes = new byte[file.Length];
            // Читаем содержимое файла в байтовый массив.
            file.Read(bytes);
            
            IPEndPoint ep = new(IPAddress.Loopback, 1488);
            OclUdpClient oclUdpClient = new();
            // Создаем UDP-клиента для отправки данных.

            await oclUdpClient.ConnectAsync(ep);
            // Устанавливаем соединение с указанным IP-адресом и портом.

            OclUdpStream stream = oclUdpClient.GetStream();
            // Получаем поток для отправки данных.
            byte[] buffer = BitConverter.GetBytes((int)bytes.Length);
            
            var code = 3;
            var b = BitConverter.GetBytes(code);
            stream.Write(b, 0, 1);
            // Записываем код операции в поток.

            var userId = findUser.Id;
            var uI = BitConverter.GetBytes(userId);
            stream.Write(uI, 0, 1);
            // Записываем идентификатор пользователя в поток.
            
            var fName = uploadedFile.FileName;
            var fnb = System.Text.Encoding.UTF8.GetBytes(fName);
            // Преобразуем имя файла в байтовый массив.
            
            var c = BitConverter.GetBytes(fnb.Length);
            stream.Write(c, 0, c.Length);
            stream.Write(fnb, 0, fnb.Length);
            // Записываем размер имени файла и само имя файла в поток.
            
            stream.Write(buffer, 0, buffer.Length);
            stream.Write(bytes, 0, bytes.Length);
            // Записываем размер файла и его содержимое в поток.
            
            return RedirectToAction("Index"); // Перенаправляем на действие "Index" контроллера.
        }
    }
}