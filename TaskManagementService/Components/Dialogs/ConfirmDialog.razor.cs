using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace TaskManagementService.Components.Dialogs
{
    public partial class ConfirmDialog
    {
        [CascadingParameter]
        private IMudDialogInstance DialogInstance { get; set; } = default!;

        [Parameter]
        public string ContentText { get; set; } = "Are you sure?";

        [Parameter]
        public string ButtonText { get; set; } = "Confirm";

        [Parameter]
        public Color Color { get; set; } = Color.Primary;

        private void Cancel()
        {
            DialogInstance.Cancel();
        }

        private void Confirm()
        {
            DialogInstance.Close(DialogResult.Ok(true));
        }
    }
}