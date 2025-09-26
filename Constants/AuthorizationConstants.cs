using SparkPoint_Server.Constants;

namespace SparkPoint_Server.Constants
{
    public static class AuthorizationConstants
    {
        // Role authorization constants for attributes
        public static class Roles
        {
            public const string Admin = ApplicationConstants.AdminRoleString;
            public const string StationUser = ApplicationConstants.StationUserRoleString;  
            public const string EVOwner = ApplicationConstants.EVOwnerRoleString;
        }

        // Combined role groups for common scenarios
        public static class RoleGroups
        {
            // Admin only
            public static readonly string[] AdminOnly = { Roles.Admin };
            
            // Admin and Station User
            public static readonly string[] AdminAndStationUser = { Roles.Admin, Roles.StationUser };
            
            // Admin and EV Owner
            public static readonly string[] AdminAndEVOwner = { Roles.Admin, Roles.EVOwner };
            
            // EV Owner only
            public static readonly string[] EVOwnerOnly = { Roles.EVOwner };
            
            // Station User only
            public static readonly string[] StationUserOnly = { Roles.StationUser };
            
            // All roles
            public static readonly string[] AllRoles = { Roles.Admin, Roles.StationUser, Roles.EVOwner };
        }
    }
}