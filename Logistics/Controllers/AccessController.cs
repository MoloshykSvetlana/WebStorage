using System.Linq;
using System.Threading.Tasks;
using FileServer;
using FileServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Employee.Controllers
{
    public class AccessController : Controller
    {
        private readonly ApplicationContext _context;

        public AccessController(ApplicationContext context)
        {
            _context = context;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = HttpContext.User.Identity.Name;

            var findUser = _context.Users.FirstOrDefault(u => u.Login == user);

            if (findUser.RoleModelId == 1)
            {
                var res = _context.Accesses
                    .Include(x => x.FileModel)
                    .Include(x => x.UserModel)
                    .AsNoTracking()
                    .ToList();

                return View(res);
            }

            var query = _context.Accesses
                .Include(x => x.FileModel)
                .Include(x => x.UserModel)
                .Where(x => x.FileModel.UserModelId == findUser.Id)
                .AsNoTracking();

            var result = await query
                .ToListAsync();

            _context.DetachEntities(result);
            return View(result);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var user = HttpContext.User.Identity.Name;

            var findUser = _context.Users.FirstOrDefault(u => u.Login == user);

            var findAccess = _context.Accesses.FirstOrDefault(u => findUser.RoleModelId == 1 || u.AccessLevel == 2 || u.UserModelId == findUser.Id);

            if (findAccess is null)
            {
                return RedirectToAction("Index", "Access");
            }

            var itemToUpdate = await _context.Accesses
                .FirstAsync(t => t.Id == id);

            _context.DetachEntity(itemToUpdate);

            var radioTypes = new [] {
                new {Id = 1, Name = "Скачивание"},
                new {Id = 2, Name = "Владелец"}
            };

            ViewBag.Radio = radioTypes;

            var loaders = await _context.Users
                .Where(x => x.RoleModelId == 2)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Employee = loaders;

            var files = await _context.Files
                .Where(x => x.UserModelId == findUser.Id)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Files = files;

            if (findUser.RoleModelId == 1)
            {
                ViewBag.Files = await _context.Files
                    .AsNoTracking()
                    .ToListAsync();
            }

            return View(itemToUpdate);
        }

        [HttpPost]
        public async Task<IActionResult> Update(AccessModel item)
        {
            _context.Accesses.Update(item);
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

            var findAccess = _context.Accesses.FirstOrDefault(u => findUser.RoleModelId == 1 || u.AccessLevel == 2 || u.UserModelId == findUser.Id);

            if (findAccess is null)
            {
                return RedirectToAction("Index", "Access");
            }

            var itemToDelete = await _context.Accesses
                .FirstOrDefaultAsync(t => t.Id == id);

            if (itemToDelete is not null)
            {
                _context.Accesses.Remove(itemToDelete);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var user = HttpContext.User.Identity.Name;

            var findUser = _context.Users.FirstOrDefault(u => u.Login == user);

            var radioTypes = new [] {
                new {Id = 1, Name = "Скачивание"},
                new {Id = 2, Name = "Владелец"}
            };

            ViewBag.Radio = radioTypes;

            var loaders = await _context.Users
                .Where(x => x.RoleModelId == 2)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Employee = loaders;

            var files = await _context.Files
                .Where(x => x.UserModelId == findUser.Id)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Files = files;

            if (findUser.RoleModelId == 1)
            {
                ViewBag.Files = await _context.Files
                    .AsNoTracking()
                    .ToListAsync();
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(AccessModel item)
        {
            var a = _context.Accesses.FirstOrDefault(x => x.UserModelId == item.UserModelId
                                                  && x.FileModelId == item.FileModelId);

            if (a is not null)
            {
                return RedirectToAction("Index", "Access");
            }

            _context.Accesses.Add(item);
            await _context.SaveChangesAsync();

            _context.DetachEntity(item);

            return RedirectToAction(nameof(Index));
        }
    }
}
