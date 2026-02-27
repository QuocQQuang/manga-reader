using System;
using System.Threading.Tasks;
using Mangareading.Models;
using Mangareading.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mangareading.Services
{
    public class UserService : IUserService
    {
        private readonly YourDbContext _dbContext;
        private readonly ILogger<UserService> _logger;

        public UserService(YourDbContext dbContext, ILogger<UserService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<User> AuthenticateUserAsync(string username, string password)
        {
            try
            {
                try
                {
                    bool canConnect = await _dbContext.Database.CanConnectAsync();
                    _logger.LogInformation($"Database connection check: {canConnect}");

                    // Kiểm tra hiện trạng database
                    var connStr = _dbContext.Database.GetConnectionString();
                    _logger.LogInformation($"Using connection: {connStr?.Substring(0, Math.Min(connStr.Length, 30))}...");
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Database connection test failed");
                }
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                {
                    _logger.LogWarning($"Authentication failed: User '{username}' not found.");
                    return null;
                }

                // Kiểm tra nếu tài khoản bị vô hiệu hóa
                if (!user.IsActive)
                {
                    _logger.LogWarning($"Authentication failed: User '{username}' account is disabled.");
                    return null;
                }

                // Sửa lại cách xác thực mật khẩu - sử dụng Verify thay vì HashPassword
                bool passwordMatch = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

                if (!passwordMatch)
                {
                    _logger.LogWarning($"Authentication failed: Invalid password for user '{username}'.");
                    return null;
                }
                
                // Thêm log để xác nhận nếu user có quyền admin
                if (user.IsAdmin)
                {
                    _logger.LogInformation($"User '{username}' logged in with ADMIN privileges");
                }
                else
                {
                    _logger.LogInformation($"User '{username}' logged in with regular privileges");
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error authenticating user '{username}'.");
                return null;
            }
        }

        public async Task<User> RegisterUserAsync(RegisterViewModel model)
        {
            try
            {
                // **IMPORTANT: Replace this with SECURE PASSWORD HASHING!**
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

                var newUser = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = passwordHash,
                    CreatedAt = DateTime.UtcNow,
                    // Automatically generate avatar URL
                    AvatarUrl = $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(model.Username)}&background=random&color=fff" 
                };

                _dbContext.Users.Add(newUser);
                await _dbContext.SaveChangesAsync();

                return newUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error registering user '{model.Username}'.");
                return null;
            }
        }

        public async Task<bool> IsUsernameExistsAsync(string username)
        {
            try
            {
                return await _dbContext.Users.AnyAsync(u => u.Username == username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if username '{username}' exists.");
                return false;
            }
        }

        public async Task<bool> IsEmailExistsAsync(string email)
        {
            try
            {
                return await _dbContext.Users.AnyAsync(u => u.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if email '{email}' exists.");
                return false;
            }
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            try
            {
                return await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user by username '{username}'.");
                return null;
            }
        }

        public async Task<User> GetUserByIdAsync(int userId)
        {
            try
            {
                return await _dbContext.Users.FindAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user by ID '{userId}'.");
                return null;
            }
        }

        public async Task<bool> UpdateProfileAsync(int userId, string email, string avatarUrl)
        {
            try
            {
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null) 
                {
                    _logger.LogWarning($"UpdateProfile failed: User with ID '{userId}' not found.");
                    return false;
                }

                // Kiểm tra xem email mới có khác email cũ và đã tồn tại chưa
                if (user.Email != email && await IsEmailExistsAsync(email))
                {
                    _logger.LogWarning($"UpdateProfile failed: Email '{email}' already exists for user ID '{userId}'.");
                    // Có thể muốn trả về lỗi cụ thể hơn ở đây thay vì chỉ false
                    return false; 
                }

                user.Email = email;
                // Chỉ cập nhật AvatarUrl nếu nó không phải là null hoặc chuỗi rỗng
                // Nếu muốn cho phép xóa avatar thì cần logic khác
                if (!string.IsNullOrEmpty(avatarUrl))
                {
                     user.AvatarUrl = avatarUrl; 
                }
               
                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"User profile updated successfully for user ID '{userId}'.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating profile for user ID '{userId}'.");
                return false;
            }
        }

        public async Task<(bool Success, string ErrorMessage)> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null)
                {
                     _logger.LogWarning($"ChangePassword failed: User with ID '{userId}' not found.");
                    return (false, "Người dùng không tồn tại.");
                }

                // Xác thực mật khẩu hiện tại
                if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                {
                    _logger.LogWarning($"ChangePassword failed: Incorrect current password for user ID '{userId}'.");
                    return (false, "Mật khẩu hiện tại không đúng.");
                }

                // Hash mật khẩu mới
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"Password changed successfully for user ID '{userId}'.");
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error changing password for user ID '{userId}'.");
                return (false, "Đã xảy ra lỗi trong quá trình đổi mật khẩu.");
            }
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            try
            {
                return await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user by email '{email}'.");
                return null;
            }
        }

        public async Task<bool> UpdateEmailOnlyAsync(int userId, string newEmail)
        {
            try
            {
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("UpdateEmailOnly failed: User with ID '{UserId}' not found.", userId);
                    return false;
                }

                // Check if email actually changed and if the new one exists for another user
                if (user.Email.Equals(newEmail, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("UpdateEmailOnly skipped for user {UserId}: Email hasn't changed.", userId);
                    return true; // No change needed, consider it a success
                }

                if (await _dbContext.Users.AnyAsync(u => u.Email == newEmail && u.UserId != userId))
                {
                     _logger.LogWarning("UpdateEmailOnly failed for user {UserId}: New email '{NewEmail}' already exists.", userId, newEmail);
                     return false; // Email conflict
                }

                user.Email = newEmail;
                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync();
                 _logger.LogInformation("User {UserId} email updated successfully to {NewEmail}.", userId, newEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating email only for user ID '{UserId}'.", userId);
                return false;
            }
        }

        public async Task<bool> UpdateAvatarOnlyAsync(int userId, string? newAvatarUrl)
        {
             try
            {
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("UpdateAvatarOnly failed: User with ID '{UserId}' not found.", userId);
                    return false;
                }

                // Allow setting to null or empty string to clear the avatar
                user.AvatarUrl = string.IsNullOrWhiteSpace(newAvatarUrl) ? null : newAvatarUrl.Trim();

                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync();
                 _logger.LogInformation("User {UserId} avatar updated successfully. New URL: {Url}", userId, user.AvatarUrl ?? "Cleared");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating avatar only for user ID '{UserId}'.", userId);
                return false;
            }
        }
    }
}