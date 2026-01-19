using Microsoft.EntityFrameworkCore;
using MudBlazor;
using TaskManagementService.DAL;
using TaskManagementService.DAL.Enums;
using TaskManagementService.DAL.Models;
using TaskManagementService.Models.ViewModels;

namespace TaskManagementService.Services
{
    public interface IPermissionService
    {
        Task<GridData<UserPermissionViewModel>> LoadUserPermissionsAsync(
            GridState<UserPermissionViewModel> state,
            int currentUserId,
            string userSearchTerm = "",
            List<UserPermissionViewModel>? localPermissions = null,
            List<UserPermissionViewModel>? removedPermissions = null);

        Task<List<AppUser>> GetAvailableUsersAsync();
        Task<List<AppUser>> SearchUsersAsync(string searchText);
        Task SaveChangesAsync(List<UserPermissionViewModel> localPermissions,
                             List<UserPermissionViewModel> removedPermissions,
                             int currentUserId);
        Task<bool> CanManagePermissionsAsync(int userId);
        Task<bool> CanEditPermissionsAsync(int userId);
        Task<PermissionType> GetUserPermissionTypeAsync(int userId);
    }

    public class PermissionService : IPermissionService
    {
        private readonly IDbContextFactory<TaskManagementServiceDbContext> _dbContextFactory;

        public PermissionService(IDbContextFactory<TaskManagementServiceDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<GridData<UserPermissionViewModel>> LoadUserPermissionsAsync(
            GridState<UserPermissionViewModel> state,
            int currentUserId,
            string userSearchTerm = "",
            List<UserPermissionViewModel>? localPermissions = null,
            List<UserPermissionViewModel>? removedPermissions = null)
        {
            // Create a new DBContext for this operation
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            // Get current user's permission type
            var currentUserPermission = await GetUserPermissionTypeAsync(currentUserId);

            // Build the query
            var query = BuildPermissionQuery(dbContext, currentUserPermission, currentUserId, userSearchTerm);

            // Apply grid filters
            query = ApplyGridFilters(query, state);

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination and execute query
            var dbItems = await ApplyPagination(query, state)
                .Select(x => new UserPermissionViewModel
                {
                    UserPermission = x,
                    State = new UserPermissionState()
                })
                .ToListAsync();

            // Merge with local changes (only if user can edit)
            var canEdit = await CanEditPermissionsAsync(currentUserId);
            var allItems = MergeLocalChanges(dbItems, localPermissions, removedPermissions, canEdit);

            return new GridData<UserPermissionViewModel>
            {
                Items = allItems,
                TotalItems = CalculateTotalItems(totalCount, localPermissions, removedPermissions, canEdit)
            };
        }

        private IQueryable<UserPermission> BuildPermissionQuery(
            TaskManagementServiceDbContext dbContext,
            PermissionType currentUserPermission,
            int currentUserId,
            string userSearchTerm)
        {
            IQueryable<UserPermission> query = dbContext.UserPermissions
                .Include(up => up.AppUser)
                .Include(up => up.TaskItem)
                .AsQueryable();

            // If not SuperAdmin, only show their own permissions
            if (currentUserPermission != PermissionType.SuperAdmin)
            {
                query = query.Where(up => up.AppUserId == currentUserId);
            }

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(userSearchTerm))
            {
                query = query.Where(x =>
                    (x.AppUser.DisplayName != null && x.AppUser.DisplayName.Contains(userSearchTerm)) ||
                    (x.AppUser.Email != null && x.AppUser.Email.Contains(userSearchTerm)));
            }

            return query;
        }

        private IQueryable<UserPermission> ApplyGridFilters(
            IQueryable<UserPermission> query,
            GridState<UserPermissionViewModel> state)
        {
            foreach (var definition in state.FilterDefinitions)
            {
                if (definition.Column?.PropertyName == "UserPermission.PermissionType" && definition.Value is not null)
                {
                    query = query.Where(x => x.PermissionType == (PermissionType)definition.Value);
                }
            }

            return query;
        }

        private IQueryable<UserPermission> ApplyPagination(
            IQueryable<UserPermission> query,
            GridState<UserPermissionViewModel> state)
        {
            return query
                .Skip(state.Page * state.PageSize)
                .Take(state.PageSize);
        }

        private List<UserPermissionViewModel> MergeLocalChanges(
            List<UserPermissionViewModel> dbItems,
            List<UserPermissionViewModel>? localPermissions,
            List<UserPermissionViewModel>? removedPermissions,
            bool canEdit)
        {
            var allItems = dbItems.AsEnumerable();

            // Only merge local changes if user can edit
            if (canEdit)
            {
                if (localPermissions?.Any() == true)
                {
                    allItems = allItems.Concat(localPermissions);
                }

                if (removedPermissions?.Any() == true)
                {
                    allItems = allItems.Except(removedPermissions, new UserPermissionViewModelComparer());
                }
            }

            return allItems.ToList();
        }

        private int CalculateTotalItems(
            int dbTotalCount,
            List<UserPermissionViewModel>? localPermissions,
            List<UserPermissionViewModel>? removedPermissions,
            bool canEdit)
        {
            if (!canEdit)
            {
                return dbTotalCount;
            }

            return (dbTotalCount + (localPermissions?.Count ?? 0)) - (removedPermissions?.Count ?? 0);
        }

        public async Task<List<AppUser>> GetAvailableUsersAsync()
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            return await dbContext.AppUsers
                .AsNoTracking()
                .OrderBy(u => u.DisplayName)
                .ToListAsync();
        }

        public async Task<List<AppUser>> SearchUsersAsync(string searchText)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var query = dbContext.AppUsers.AsNoTracking();

            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(u =>
                    (u.DisplayName != null && u.DisplayName.Contains(searchText)) ||
                    (u.Email != null && u.Email.Contains(searchText)));
            }

            return await query
                .OrderBy(u => u.DisplayName)
                .Take(10)
                .ToListAsync();
        }

        public async Task SaveChangesAsync(
            List<UserPermissionViewModel> localPermissions,
            List<UserPermissionViewModel> removedPermissions,
            int currentUserId)
        {
            // Validate permissions first
            var canEdit = await CanEditPermissionsAsync(currentUserId);
            if (!canEdit)
            {
                throw new UnauthorizedAccessException("Only SuperAdmin can edit permissions");
            }

            // Create a new DBContext for the transaction
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            // Begin transaction for atomic operations
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            try
            {
                // Add new permissions
                foreach (var localPermission in localPermissions)
                {
                    // Attach or create new permission
                    var newPermission = new UserPermission
                    {
                        AppUserId = localPermission.UserPermission.AppUserId,
                        PermissionType = localPermission.UserPermission.PermissionType,
                        TaskItemId = localPermission.UserPermission.TaskItemId
                    };

                    // Check if user already has a permission
                    var existingPermission = await dbContext.UserPermissions
                        .FirstOrDefaultAsync(up => up.AppUserId == newPermission.AppUserId);

                    if (existingPermission != null)
                    {
                        // Update existing permission
                        existingPermission.PermissionType = newPermission.PermissionType;
                        existingPermission.TaskItemId = newPermission.TaskItemId;
                        dbContext.UserPermissions.Update(existingPermission);
                    }
                    else
                    {
                        // Add new permission
                        await dbContext.UserPermissions.AddAsync(newPermission);
                    }
                }

                // Remove permissions
                foreach (var removedPermission in removedPermissions)
                {
                    // Find the existing permission
                    var existing = await dbContext.UserPermissions
                        .FirstOrDefaultAsync(up => up.Id == removedPermission.UserPermission.Id);

                    if (existing != null)
                    {
                        // Don't allow removing own permission
                        if (existing.AppUserId == currentUserId)
                        {
                            throw new InvalidOperationException("Cannot remove your own permission");
                        }

                        dbContext.UserPermissions.Remove(existing);
                    }
                }

                // Save all changes
                await dbContext.SaveChangesAsync();

                // Commit transaction
                await transaction.CommitAsync();
            }
            catch
            {
                // Rollback on error
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> CanManagePermissionsAsync(int userId)
        {
            var permissionType = await GetUserPermissionTypeAsync(userId);

            // SuperAdmin, Admin, and User can manage (view) permissions
            // ReadOnly cannot manage anything
            return permissionType == PermissionType.SuperAdmin ||
                   permissionType == PermissionType.Admin ||
                   permissionType == PermissionType.User;
        }

        public async Task<bool> CanEditPermissionsAsync(int userId)
        {
            var permissionType = await GetUserPermissionTypeAsync(userId);

            // Only SuperAdmin can edit permissions
            return permissionType == PermissionType.SuperAdmin;
        }

        public async Task<PermissionType> GetUserPermissionTypeAsync(int userId)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var user = await dbContext.AppUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            return user?.PermissionType ?? PermissionType.User;
        }
    }
}