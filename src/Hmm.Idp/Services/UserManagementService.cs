using Duende.IdentityServer.Test;
using Hmm.Idp.Pages.Admin.User;

namespace Hmm.Idp.Services
{
    public class UserManagementService(ApplicationUserRepository userRepository)
    {
        private readonly ApplicationUserRepository _userStore = userRepository ?? throw new ArgumentNullException(nameof(userRepository));

        public async Task<IEnumerable<ApplicationUser>> GetAllUsers()
        {
            var users = await userRepository.GetUsersAsync();
            return new List<ApplicationUser>();
        }

        public async Task<ApplicationUser> GetUserBySubjectId(string subjectId)
        {
            return await _userStore.FindBySubjectIdAsync(subjectId);
        }

        public async Task<ApplicationUser> GetUserByUserName(string userName)
        {
            return await _userStore.FindByUserNameAsync(userName);
        }

        public async Task<ApplicationUser> CreateUser(string userName, string password, string name = null, string email = null)
        {
            return await _userStore.CreateUserAsync(userName, password, name, email);
        }

        public async Task<bool> UpdateUser(ApplicationUser user)
        {
            var existingUser = await _userStore.FindBySubjectIdAsync(user.SubjectId);
            if (existingUser == null)
                return false;

            // Remove the existing user and add the updated one
            var allUsers = await GetAllUsers();
            var users = allUsers.ToList();
            users.Remove(existingUser);
            users.Add(user);

            // Use reflection to update the private list
            var usersField = typeof(TestUserStore).GetField("_users",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (usersField != null)
            {
                usersField.SetValue(_userStore, users);
                return true;
            }
            return false;
        }

        public async Task<bool> DeleteUser(string subjectId)
        {
            var user = await _userStore.FindBySubjectIdAsync(subjectId);
            if (user == null)
                return false;

            var allUsers = await GetAllUsers();
            var users = allUsers.ToList();
            var result = users.Remove(user);

            // Use reflection to update the private list
            var usersField = typeof(TestUserStore).GetField("_users",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (usersField != null && result)
            {
                usersField.SetValue(_userStore, users);
                return true;
            }
            return false;
        }

        public async Task<bool> ValidateCredentials(string userName, string password)
        {
            return await _userStore.ValidateCredentialsAsync(userName, password);
        }
    }
}