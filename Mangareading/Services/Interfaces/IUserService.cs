using System;
using System.Threading.Tasks;
using Mangareading.Models;



namespace Mangareading.Services.Interfaces
{
    public interface IUserService
    {
        Task<User> AuthenticateUserAsync(string username, string password);
        Task<User> RegisterUserAsync(RegisterViewModel model);
        Task<bool> IsUsernameExistsAsync(string username);
        Task<bool> IsEmailExistsAsync(string email);
        Task<User> GetUserByUsernameAsync(string username);
        Task<User> GetUserByIdAsync(int userId);
        Task<bool> UpdateProfileAsync(int userId, string email, string avatarUrl);
        Task<(bool Success, string ErrorMessage)> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<User> GetUserByEmailAsync(string email);
        Task<bool> UpdateEmailOnlyAsync(int userId, string newEmail);
        Task<bool> UpdateAvatarOnlyAsync(int userId, string? newAvatarUrl);
    }
}