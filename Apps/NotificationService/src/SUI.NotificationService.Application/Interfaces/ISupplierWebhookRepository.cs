using SUI.NotificationService.Application.Models;

namespace SUI.NotificationService.Application.Interfaces;

public interface ISupplierWebhookRepository
{
    Task AddAsync(SupplierWebhook webhook, CancellationToken cancellationToken = default);
    Task UpdateAsync(SupplierWebhook webhook, CancellationToken cancellationToken = default);
    Task DisableAsync(string supplierId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SupplierWebhook>> GetAllEnabledAsync(
        CancellationToken cancellationToken = default
    );
}
