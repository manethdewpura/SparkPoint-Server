using SparkPoint_Server.Constants;
using SparkPoint_Server.Enums;

namespace SparkPoint_Server.Utils
{
    /// <summary>
    /// Utility methods for authentication operations
    /// </summary>
    public static class AuthUtils
    {
        /// <summary>
        /// Gets the role name from role ID
        /// </summary>
        /// <param name="roleId">The role ID</param>
        /// <returns>The role name</returns>
        public static string GetRoleName(int roleId)
        {
            switch (roleId)
            {
                case ApplicationConstants.AdminRoleId:
                    return ApplicationConstants.AdminRoleName;
                case ApplicationConstants.StationUserRoleId:
                    return ApplicationConstants.StationUserRoleName;
                case ApplicationConstants.EVOwnerRoleId:
                    return ApplicationConstants.EVOwnerRoleName;
                default:
                    return "Unknown Role";
            }
        }

        /// <summary>
        /// Gets the UserRole enum from role ID
        /// </summary>
        /// <param name="roleId">The role ID</param>
        /// <returns>The UserRole enum value</returns>
        public static UserRole GetUserRoleEnum(int roleId)
        {
            switch (roleId)
            {
                case ApplicationConstants.AdminRoleId:
                    return UserRole.Admin;
                case ApplicationConstants.StationUserRoleId:
                    return UserRole.StationUser;
                case ApplicationConstants.EVOwnerRoleId:
                    return UserRole.EVOwner;
                default:
                    return UserRole.EVOwner; // Default fallback
            }
        }

        /// <summary>
        /// Checks if a role ID is valid
        /// </summary>
        /// <param name="roleId">The role ID to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidRoleId(int roleId)
        {
            return roleId == ApplicationConstants.AdminRoleId ||
                   roleId == ApplicationConstants.StationUserRoleId ||
                   roleId == ApplicationConstants.EVOwnerRoleId;
        }

        /// <summary>
        /// Checks if a user has admin privileges
        /// </summary>
        /// <param name="roleId">The user's role ID</param>
        /// <returns>True if admin, false otherwise</returns>
        public static bool IsAdmin(int roleId)
        {
            return roleId == ApplicationConstants.AdminRoleId;
        }

        /// <summary>
        /// Checks if a user is an EV Owner
        /// </summary>
        /// <param name="roleId">The user's role ID</param>
        /// <returns>True if EV Owner, false otherwise</returns>
        public static bool IsEVOwner(int roleId)
        {
            return roleId == ApplicationConstants.EVOwnerRoleId;
        }

        /// <summary>
        /// Checks if a user is a Station User
        /// </summary>
        /// <param name="roleId">The user's role ID</param>
        /// <returns>True if Station User, false otherwise</returns>
        public static bool IsStationUser(int roleId)
        {
            return roleId == ApplicationConstants.StationUserRoleId;
        }
    }
}