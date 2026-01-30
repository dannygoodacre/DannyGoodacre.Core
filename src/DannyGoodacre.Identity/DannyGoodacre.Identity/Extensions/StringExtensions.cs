using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DannyGoodacre.Identity.Extensions;

public static class StringExtensions
{
    extension(string errorCode)
    {
        public string Property
            => errorCode switch
            {
                "PasswordTooShort"
                or "PasswordRequiresDigit"
                or "PasswordRequiresLower"
                or "PasswordRequiresUpper"
                or "PasswordRequiresNonAlphanumeric"
                or "PasswordRequiresUniqueChars"
                or "PasswordMismatch"
                or "UserAlreadyHasPassword" => "Password",

                "InvalidUserName"
                or "DuplicateUserName"
                or "UserNameNotFound" => "Username",

                "InvalidEmail"
                or "DuplicateEmail" => "Email",

                "InvalidRoleName"
                or "DuplicateRoleName"
                or "RoleNotFound"
                or "UserAlreadyInRole"
                or "UserNotInRole" => "Role",

                "InvalidToken"
                or "RecoveryCodeRedemptionFailed"
                or "LoginAlreadyAssociated" => "Token",

                _ => "General"
            };
    }
}
