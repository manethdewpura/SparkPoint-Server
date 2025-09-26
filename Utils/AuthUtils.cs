using SparkPoint_Server.Constants;
using SparkPoint_Server.Enums;

namespace SparkPoint_Server.Utils
{

    public static class AuthUtils
    {

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
                    return UserRole.EVOwner;
            }
        }

        public static bool IsValidRoleId(int roleId)
        {
            return roleId == ApplicationConstants.AdminRoleId ||
                   roleId == ApplicationConstants.StationUserRoleId ||
                   roleId == ApplicationConstants.EVOwnerRoleId;
        }

        public static bool IsAdmin(int roleId)
        {
            return roleId == ApplicationConstants.AdminRoleId;
        }

        public static bool IsEVOwner(int roleId)
        {
            return roleId == ApplicationConstants.EVOwnerRoleId;
        }

        public static bool IsStationUser(int roleId)
        {
            return roleId == ApplicationConstants.StationUserRoleId;
        }
    }
}