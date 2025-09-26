namespace SparkPoint_Server.Enums
{
    public enum UserOperationStatus
    {
        Success,
        UserNotFound,
        EVOwnerNotFound,
        UsernameExists,
        EmailExists,
        PhoneExists,
        NICExists,
        ValidationFailed,
        AccountDeactivated,
        AccountAlreadyDeactivated,
        AccountAlreadyActive,
        NotAuthorized,
        ChargingStationNotFound,
        NotStationUser,
        Failed
    }

    public enum EVOwnerOperationStatus
    {
        Success,
        UserNotFound,
        EVOwnerNotFound,
        UsernameExists,
        EmailExists,
        PhoneExists,
        NICExists,
        ValidationFailed,
        AccountDeactivated,
        AccountAlreadyDeactivated,
        AccountAlreadyActive,
        NotAuthorized,
        Failed
    }

    public enum UserValidationError
    {
        None,
        UsernameRequired,
        UsernameTooShort,
        UsernameTooLong,
        EmailRequired,
        EmailInvalid,
        EmailTooLong,
        PasswordRequired,
        PasswordTooShort,
        PasswordTooLong,
        FirstNameTooLong,
        LastNameTooLong,
        PhoneTooShort,
        PhoneTooLong,
        PhoneInvalid,
        NICTooShort,
        NICTooLong,
        NICInvalid,
        ChargingStationIdRequired
    }

    public enum UserFilterType
    {
        Role,
        ActiveStatus,
        SearchTerm
    }

    public enum UserSortField
    {
        Username,
        Email,
        FirstName,
        LastName,
        RoleId,
        IsActive,
        CreatedAt,
        UpdatedAt
    }
}