using Hmm.Idp.Pages.Admin.User;

namespace Hmm.Idp.Services;

public interface IUserManagementService
{
    Task<IEnumerable<ApplicationUser>> GetAllUsers();
    Task<ApplicationUser> GetUserBySubjectId(string subjectId);
    Task<ApplicationUser> GetUserByUserName(string userName);
    Task<ApplicationUser> CreateUser(string userName, string password, string name = null, string email = null);
    Task<bool> UpdateUser(ApplicationUser user);
    Task<bool> DeleteUser(string subjectId);
    Task<bool> ValidateCredentials(string userName, string password);
}
