using System.Collections.Generic;
using MongoDB.Driver;
using SparkPoint_Server.Models;
using SparkPoint_Server.Constants;
using SparkPoint_Server.Enums;

namespace SparkPoint_Server.Helpers
{
    public static class UserFilterHelper
    {
        public static FilterDefinition<User> BuildUserFilter(UserListFilterModel filter)
        {
            var filterBuilder = Builders<User>.Filter.Empty;

            if (filter == null)
                return filterBuilder;

            if (filter.RoleId.HasValue)
            {
                var roleFilter = Builders<User>.Filter.Eq(u => u.RoleId, filter.RoleId.Value);
                filterBuilder = Builders<User>.Filter.And(filterBuilder, roleFilter);
            }

            if (filter.IsActive.HasValue)
            {
                var activeFilter = Builders<User>.Filter.Eq(u => u.IsActive, filter.IsActive.Value);
                filterBuilder = Builders<User>.Filter.And(filterBuilder, activeFilter);
            }

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var searchFilter = BuildSearchFilter(filter.SearchTerm);
                filterBuilder = Builders<User>.Filter.And(filterBuilder, searchFilter);
            }

            return filterBuilder;
        }

        private static FilterDefinition<User> BuildSearchFilter(string searchTerm)
        {
            var usernameFilter = Builders<User>.Filter.Regex(
                u => u.Username,
                new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")
            );

            var emailFilter = Builders<User>.Filter.Regex(
                u => u.Email,
                new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")
            );

            var firstNameFilter = Builders<User>.Filter.Regex(
                u => u.FirstName,
                new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")
            );

            var lastNameFilter = Builders<User>.Filter.Regex(
                u => u.LastName,
                new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")
            );

            return Builders<User>.Filter.Or(usernameFilter, emailFilter, firstNameFilter, lastNameFilter);
        }

        public static SortDefinition<User> BuildUserSort(UserSortField sortField, SortOrder sortOrder)
        {
            var sortBuilder = Builders<User>.Sort;

            SortDefinition<User> sortDefinition;

            switch (sortField)
            {
                case UserSortField.Username:
                    sortDefinition = sortOrder == SortOrder.Ascending
                        ? sortBuilder.Ascending(u => u.Username)
                        : sortBuilder.Descending(u => u.Username);
                    break;
                case UserSortField.Email:
                    sortDefinition = sortOrder == SortOrder.Ascending
                        ? sortBuilder.Ascending(u => u.Email)
                        : sortBuilder.Descending(u => u.Email);
                    break;
                case UserSortField.FirstName:
                    sortDefinition = sortOrder == SortOrder.Ascending
                        ? sortBuilder.Ascending(u => u.FirstName)
                        : sortBuilder.Descending(u => u.FirstName);
                    break;
                case UserSortField.LastName:
                    sortDefinition = sortOrder == SortOrder.Ascending
                        ? sortBuilder.Ascending(u => u.LastName)
                        : sortBuilder.Descending(u => u.LastName);
                    break;
                case UserSortField.RoleId:
                    sortDefinition = sortOrder == SortOrder.Ascending
                        ? sortBuilder.Ascending(u => u.RoleId)
                        : sortBuilder.Descending(u => u.RoleId);
                    break;
                case UserSortField.IsActive:
                    sortDefinition = sortOrder == SortOrder.Ascending
                        ? sortBuilder.Ascending(u => u.IsActive)
                        : sortBuilder.Descending(u => u.IsActive);
                    break;
                case UserSortField.CreatedAt:
                    sortDefinition = sortOrder == SortOrder.Ascending
                        ? sortBuilder.Ascending(u => u.CreatedAt)
                        : sortBuilder.Descending(u => u.CreatedAt);
                    break;
                case UserSortField.UpdatedAt:
                    sortDefinition = sortOrder == SortOrder.Ascending
                        ? sortBuilder.Ascending(u => u.UpdatedAt)
                        : sortBuilder.Descending(u => u.UpdatedAt);
                    break;
                default:
                    sortDefinition = sortBuilder.Descending(u => u.CreatedAt);
                    break;
            }

            return sortDefinition;
        }
    }

    public static class UserUpdateHelper
    {
        public static UpdateDefinition<User> BuildUserUpdate(UserUpdateModel model)
        {
            var updateBuilder = Builders<User>.Update.Set(u => u.UpdatedAt, System.DateTime.UtcNow);

            if (!string.IsNullOrEmpty(model.FirstName))
            {
                var sanitizedFirstName = Utils.UserUtils.SanitizeString(model.FirstName);
                updateBuilder = updateBuilder.Set(u => u.FirstName, sanitizedFirstName);
            }

            if (!string.IsNullOrEmpty(model.LastName))
            {
                var sanitizedLastName = Utils.UserUtils.SanitizeString(model.LastName);
                updateBuilder = updateBuilder.Set(u => u.LastName, sanitizedLastName);
            }

            if (!string.IsNullOrEmpty(model.Email))
            {
                var sanitizedEmail = Utils.UserUtils.SanitizeString(model.Email);
                updateBuilder = updateBuilder.Set(u => u.Email, sanitizedEmail);
            }

            if (!string.IsNullOrEmpty(model.Username))
            {
                var sanitizedUsername = Utils.UserUtils.SanitizeString(model.Username);
                updateBuilder = updateBuilder.Set(u => u.Username, sanitizedUsername);
            }

            if (!string.IsNullOrEmpty(model.Password))
            {
                var hashedPassword = Utils.PasswordUtils.HashPassword(model.Password);
                updateBuilder = updateBuilder.Set(u => u.PasswordHash, hashedPassword);
            }

            return updateBuilder;
        }

        public static UpdateDefinition<EVOwner> BuildEVOwnerUpdate(EVOwnerUpdateModel model)
        {
            var updateBuilder = Builders<EVOwner>.Update.Set(o => o.UpdatedAt, System.DateTime.UtcNow);

            if (!string.IsNullOrEmpty(model.Phone))
            {
                var sanitizedPhone = Utils.UserUtils.SanitizeString(model.Phone);
                updateBuilder = updateBuilder.Set(o => o.Phone, sanitizedPhone);
            }

            return updateBuilder;
        }

        public static UpdateDefinition<User> BuildActivationUpdate(bool isActive)
        {
            return Builders<User>.Update
                .Set(u => u.IsActive, isActive)
                .Set(u => u.UpdatedAt, System.DateTime.UtcNow);
        }
    }
}