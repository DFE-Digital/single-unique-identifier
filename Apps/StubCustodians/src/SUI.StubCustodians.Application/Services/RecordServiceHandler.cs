using Microsoft.Extensions.Logging;
using SUI.StubCustodians.Application.Common;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Services;

public class RecordServiceHandler<T> : IRecordServiceHandler<T>
    where T : class
{
    private readonly IRecordProvider<T> _recordProvider;
    private readonly ILogger<RecordServiceHandler<T>> _logger;

    public RecordServiceHandler(
        IRecordProvider<T> recordProvider,
        ILogger<RecordServiceHandler<T>> logger
    )
    {
        _recordProvider = recordProvider;
        _logger = logger;
    }

    public Task<HandlerResult<RecordEnvelope<T>>> GetRecord(string sui, string providerSystemId)
    {
        var errors = RecordRequestValidator.Validate(sui, providerSystemId);

        if (errors.Count > 0)
        {
            return Task.FromResult(HandlerResult<RecordEnvelope<T>>.ValidationFailure(errors));
        }

        var result = _recordProvider.GetRecordForSui(sui, providerSystemId);

        if (result == null)
        {
            _logger.LogError(
                "{RecordType} not found for SUI:'{Sui}', SystemId:'{ProviderSystemId}'",
                nameof(T),
                sui,
                providerSystemId
            );

            return Task.FromResult(
                HandlerResult<RecordEnvelope<T>>.NotFound(
                    $"Record of type {nameof(T)} not found for SUI:'{sui}'"
                )
            );
        }

        return Task.FromResult(HandlerResult<RecordEnvelope<T>>.Success(result));
    }
}
