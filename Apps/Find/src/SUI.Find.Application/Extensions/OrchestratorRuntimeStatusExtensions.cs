using Microsoft.DurableTask.Client;
using SUI.Find.Application.Enums;

namespace SUI.Find.Application.Extensions;

public static class OrchestratorRuntimeStatusExtensions
{
    public static SearchStatus ToSuiSearchStatus(this OrchestrationRuntimeStatus status)
    {
        return status switch
        {
            OrchestrationRuntimeStatus.Running => SearchStatus.Running,
            OrchestrationRuntimeStatus.Completed => SearchStatus.Completed,
            OrchestrationRuntimeStatus.Failed => SearchStatus.Failed,
            OrchestrationRuntimeStatus.Terminated => SearchStatus.Cancelled,
            OrchestrationRuntimeStatus.Pending => SearchStatus.Queued,
            _ => SearchStatus.None,
        };
    }
}
