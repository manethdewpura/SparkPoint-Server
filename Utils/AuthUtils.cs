/*
 * AuthUtils.cs
 * 
 * This utility class provides authentication-related helper methods.
 * It includes role management functions, role validation, and user type
 * identification methods used throughout the authentication system.
 * 
 */

using SparkPoint_Server.Constants;
using SparkPoint_Server.Enums;

namespace SparkPoint_Server.Utils
{

    public static class AuthUtils
    {

        // Gets role name from role ID
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

        // Validates if role ID is valid
        public static bool IsValidRoleId(int roleId)
        {
            return roleId == ApplicationConstants.AdminRoleId ||
                   roleId == ApplicationConstants.StationUserRoleId ||
                   roleId == ApplicationConstants.EVOwnerRoleId;
        }

        // Checks if role ID is admin
        public static bool IsAdmin(int roleId)
        {
            return roleId == ApplicationConstants.AdminRoleId;
        }

        // Checks if role ID is EV owner
        public static bool IsEVOwner(int roleId)
        {
            return roleId == ApplicationConstants.EVOwnerRoleId;
        }

        // Checks if role ID is station user
        public static bool IsStationUser(int roleId)
        {
            return roleId == ApplicationConstants.StationUserRoleId;
        }
    }
}