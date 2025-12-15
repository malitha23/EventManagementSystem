using Microsoft.EntityFrameworkCore;
using EventManagementSystem.Data;
using EventManagementSystem.Models;
using EventManagementSystem.Utilities;
using EventManagementSystem.Models.ViewModels;

namespace EventManagementSystem.Services
{
    public interface IAuthService
    {
        Task<User?> RegisterAsync(RegisterViewModel model);
        Task<User?> LoginAsync(string email, string password);
        Task<User?> GetUserByIdAsync(int id);
        Task<bool> EmailExistsAsync(string email);
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> RegisterAsync(RegisterViewModel model)
        {
            if (await EmailExistsAsync(model.Email))
            {
                return null;
            }

            var user = new User
            {
                Name = model.Name,
                Email = model.Email,
                Password = PasswordHasher.HashPassword(model.Password),
                Phone = model.Phone,
                Role = model.Role,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User?> LoginAsync(string email, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !PasswordHasher.VerifyPassword(password, user.Password))
            {
                return null;
            }

            return user;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }
    }
}