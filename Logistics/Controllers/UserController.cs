using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FileServer;
using FileServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OclUdp.Sockets;

namespace Employee.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationContext _context;

        public UserController(ApplicationContext context)
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
                var res = _context.Users
                    .AsNoTracking()
                    .ToList();

                return View(res);
            }

            var result = _context.Users.Where(u => u.Login == user).AsNoTracking().ToList();

            return View(result);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var client = new OclUdpClient();

            await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 1488));

            var a = client.GetStream();

            var itemToUpdate = await _context.Users
                .FirstAsync(t => t.Id == id);

            _context.DetachEntity(itemToUpdate);

            return View(itemToUpdate);
        }

        [HttpPost]
        public async Task<IActionResult> Update(UserModel item)
        {
            var existingRole = await _context.Users
                .Where(u => u.Id == item.Id)
                .Select(u => u.RoleModelId)
                .FirstAsync();

            item.RoleModelId = existingRole;
            _context.Users.Update(item);
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

            var itemToDelete = await _context.Users
                .FirstOrDefaultAsync(t => t.Id == id);

            if (findUser.Id == id)
            {
                return RedirectToAction(nameof(Index));
            }

            _context.Users.Remove(itemToDelete);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}