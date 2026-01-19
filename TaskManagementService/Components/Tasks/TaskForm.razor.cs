using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using TaskManagementService.DAL.Enums;
using TaskManagementService.Models.ViewModels;
using TaskStatusEnum = TaskManagementService.DAL.Enums.TaskStatus;

namespace TaskManagementService.Components.Tasks
{
    public partial class TaskForm
    {
        [CascadingParameter]
        private IMudDialogInstance DialogInstance { get; set; } = default!;

        [Parameter]
        public bool IsCreating { get; set; } = true;

        [Parameter]
        public UpdateTaskModel? ExistingTask { get; set; }

        private CreateTaskModel CreateModel { get; set; } = new();
        private UpdateTaskModel EditModel { get; set; } = new();

        private EditContext _editContext = null!;
        private bool IsSubmitting { get; set; }

        protected override void OnInitialized()
        {
            if (!IsCreating && ExistingTask != null)
            {
                EditModel = new UpdateTaskModel
                {
                    Title = ExistingTask.Title,
                    Description = ExistingTask.Description,
                    Status = ExistingTask.Status
                };
                _editContext = new EditContext(EditModel);
            }
            else
            {
                _editContext = new EditContext(CreateModel);
            }
        }

        private object CurrentModel => IsCreating ? (object)CreateModel : EditModel;

        private async Task HandleValidSubmit()
        {
            IsSubmitting = true;
            await InvokeAsync(StateHasChanged);

            // Simulate async operation
            await Task.Delay(100);

            if (IsCreating)
            {
                DialogInstance.Close(DialogResult.Ok(CreateModel));
            }
            else
            {
                DialogInstance.Close(DialogResult.Ok(EditModel));
            }

            IsSubmitting = false;
        }

        private void Cancel()
        {
            DialogInstance.Cancel();
        }
    }
}