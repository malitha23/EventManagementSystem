// Create a new file UserService.cs in Services folder
using EventManagementSystem.Data;
using EventManagementSystem.Models;
using EventManagementSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

public interface IUserService
{
    Task<UserListViewModel> GetUsers(UserFilterModel filter, int page = 1, int pageSize = 10);
    Task<User> GetUserById(int id);
    Task<UserViewModel> GetUserViewModel(string id);
    Task<bool> CreateUser(UserCreateViewModel model);
    Task<bool> UpdateUser(UserEditViewModel model);
    Task<bool> DeleteUser(string id);
    Task<bool> ToggleUserStatus(string id);
    Task<int> GetUsersCount();
    Task<List<string>> GetAllRoles();
}

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;

    public UserService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserListViewModel> GetUsers(UserFilterModel filter, int page = 1, int pageSize = 10)
    {
        var query = _context.Users.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            query = query.Where(u =>
                u.Name.Contains(filter.SearchTerm) ||
                u.Email.Contains(filter.SearchTerm) ||
                u.Phone.Contains(filter.SearchTerm));
        }

        if (!string.IsNullOrEmpty(filter.Role))
        {
            query = query.Where(u => u.Role == filter.Role);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == filter.IsActive.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(u => u.CreatedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(u => u.CreatedAt <= filter.ToDate.Value);
        }

        // Apply sorting
        query = ApplySorting(query, filter.SortBy, filter.SortOrder);

        // Get total count for pagination
        var totalUsers = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalUsers / pageSize);

        // Apply pagination
        var users = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserViewModel
            {
                Id = u.Id.ToString(),
                Name = u.Name,
                Email = u.Email,
                Phone = u.Phone,
                Role = u.Role,
                CreatedAt = u.CreatedAt,
                IsActive = u.IsActive,
                TotalBookings = _context.Bookings.Count(b => b.CustomerId == u.Id),
                TotalSpent = _context.Bookings
                    .Where(b => b.CustomerId == u.Id && b.PaymentStatus == "paid")
                    .Sum(b => (decimal?)b.FinalAmount) ?? 0,
                EventsCreated = u.Role == "organizer" ?
                    _context.Events.Count(e => e.OrganizerId == u.Id) : 0
            })
            .ToListAsync();

        return new UserListViewModel
        {
            Users = users,
            Filter = filter,
            TotalUsers = totalUsers,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    private IQueryable<User> ApplySorting(IQueryable<User> query, string sortBy, string sortOrder)
    {
        sortBy ??= "CreatedAt";
        sortOrder ??= "desc";

        return sortBy.ToLower() switch
        {
            "name" => sortOrder == "asc" ? query.OrderBy(u => u.Name) : query.OrderByDescending(u => u.Name),
            "email" => sortOrder == "asc" ? query.OrderBy(u => u.Email) : query.OrderByDescending(u => u.Email),
            "role" => sortOrder == "asc" ? query.OrderBy(u => u.Role) : query.OrderByDescending(u => u.Role),
            "createdat" => sortOrder == "asc" ? query.OrderBy(u => u.CreatedAt) : query.OrderByDescending(u => u.CreatedAt),
            _ => query.OrderByDescending(u => u.CreatedAt)
        };
    }

    public async Task<User?> GetUserById(int id)
    {
        return await _context.Users.FindAsync(id);
    }


    public async Task<UserViewModel> GetUserViewModel(string id)
    {
        return await _context.Users
            .Where(u => u.Id.ToString() == id)
            .Select(u => new UserViewModel
            {
                Id = u.Id.ToString(),
                Name = u.Name,
                Email = u.Email,
                Phone = u.Phone,
                Role = u.Role,
                CreatedAt = u.CreatedAt,
                IsActive = u.IsActive,
                TotalBookings = _context.Bookings.Count(b => b.CustomerId == u.Id),
                TotalSpent = _context.Bookings
                    .Where(b => b.CustomerId == u.Id && b.PaymentStatus == "paid")
                    .Sum(b => (decimal?)b.FinalAmount) ?? 0,
                EventsCreated = u.Role == "organizer" ?
                    _context.Events.Count(e => e.OrganizerId == u.Id) : 0
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> CreateUser(UserCreateViewModel model)
    {
        try
        {
            var user = new User
            {
                Name = model.Name,
                Email = model.Email,
                Phone = model.Phone,
                Role = model.Role,
                Password = HashPassword(model.Password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateUser(UserEditViewModel model)
    {
        try
        {
            if (!int.TryParse(model.Id, out int userId))
                return false;

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.Name = model.Name;
            user.Email = model.Email;
            user.Phone = model.Phone;
            user.Role = model.Role;
            user.IsActive = model.IsActive;

            // Update password if provided
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                user.Password = HashPassword(model.NewPassword);
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteUser(string id)
    {
        try
        {
            if (!int.TryParse(id, out int userId))
                return false;

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            // Check if user has bookings or events
            if (user.Role == "organizer")
            {
                var hasEvents = await _context.Events.AnyAsync(e => e.OrganizerId.ToString() == id);
                if (hasEvents)
                {
                    // Instead of deleting, deactivate
                    user.IsActive = false;
                    _context.Users.Update(user);
                }
                else
                {
                    _context.Users.Remove(user);
                }
            }
            else
            {
                var hasBookings = await _context.Bookings.AnyAsync(b => b.CustomerId.ToString() == id);
                if (hasBookings)
                {
                    // Instead of deleting, deactivate
                    user.IsActive = false;
                    _context.Users.Update(user);
                }
                else
                {
                    _context.Users.Remove(user);
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ToggleUserStatus(string id)
    {
        try
        {
            if (!int.TryParse(id, out int userId))
                return false;

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.IsActive = !user.IsActive;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<int> GetUsersCount()
    {
        return await _context.Users.CountAsync();
    }

    public async Task<List<string>> GetAllRoles()
    {
        return await _context.Users
            .Select(u => u.Role)
            .Distinct()
            .ToListAsync();
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}