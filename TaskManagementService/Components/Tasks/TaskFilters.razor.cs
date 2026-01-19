using Microsoft.AspNetCore.Components;
using MudBlazor;
using TaskManagementService.DAL.Enums;
using TaskManagementService.Models.ViewModels;
using TaskStatus = TaskManagementService.DAL.Enums.TaskStatus;

namespace TaskManagementService.Components.Tasks
{
    public partial class TaskFilters
    {
        [Parameter]
        public TaskFiltersModel Filters { get; set; } = new();

        [Parameter]
        public EventCallback<TaskFiltersModel> OnFiltersChanged { get; set; }

        private string? _searchTerm;
        private TaskStatus? _statusFilter;
        private DateTime? _startDate;
        private DateTime? _endDate;

        protected override void OnParametersSet()
        {
            _searchTerm = Filters.SearchTerm;
            _statusFilter = Filters.Status;
            _startDate = Filters.StartDate;
            _endDate = Filters.EndDate;
        }

        private Task OnSearchTermChanged(string? value)
        {
            _searchTerm = value;
            StateHasChanged();
            return Task.CompletedTask;
        }

        private Task OnStatusFilterChanged(TaskStatus? value)
        {
            _statusFilter = value;
            StateHasChanged();
            return Task.CompletedTask;
        }

        private Task OnStartDateChanged(DateTime? value)
        {
            _startDate = value;
            StateHasChanged();
            return Task.CompletedTask;
        }

        private Task OnEndDateChanged(DateTime? value)
        {
            _endDate = value;
            StateHasChanged();
            return Task.CompletedTask;
        }

        private async Task OnSearchChanged(string searchTerm)
        {
            _searchTerm = searchTerm;
            await ApplyFilters();
        }

        private async Task ApplyFilters()
        {
            Filters.SearchTerm = _searchTerm;
            Filters.Status = _statusFilter;
            Filters.StartDate = _startDate;
            Filters.EndDate = _endDate;
            await OnFiltersChanged.InvokeAsync(Filters);
        }

        private async Task ResetFilters()
        {
            _searchTerm = null;
            _statusFilter = null;
            _startDate = null;
            _endDate = null;
            Filters = new TaskFiltersModel();
            await OnFiltersChanged.InvokeAsync(Filters);
        }
    }

    public class TaskFiltersModel
    {
        public string? SearchTerm { get; set; }
        public TaskStatus? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}