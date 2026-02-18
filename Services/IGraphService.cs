namespace dizparc_elevate.Services
{
    public interface IGraphService
    {
        /// <summary>
        /// Adds a user to an Entra ID group. No-op if already a member.
        /// </summary>
        Task<bool> AddUserToGroupAsync(string userPrincipalName, string groupId);

        /// <summary>
        /// Removes a user from an Entra ID group. No-op if not a member.
        /// </summary>
        Task<bool> RemoveUserFromGroupAsync(string userPrincipalName, string groupId);
    }
}
