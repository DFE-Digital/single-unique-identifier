using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Models.AuditPayloads;
using SUI.Find.Application.Models.Pep;
using SUI.Find.Domain.Events.Audit;

namespace SUI.Find.Application.Services;

public class FetchRecordService(
    ILogger<FetchRecordService> logger,
    IMaskUrlService maskUrlService,
    ICustodianService custodianService,
    IProviderHttpClient providerClient,
    IOutboundAuthService outboundAuthService,
    IPolicyEnforcementService policyEnforcementService,
    IAuditQueueClient auditClient,
    TimeProvider timeProvider
) : IFetchRecordService
{
    public async Task<OneOf<CustodianRecord, NotFound, Unauthorized, Error>> FetchRecordAsync(
        string fetchId,
        string requestingOrgId,
        CancellationToken cancellationToken
    )
    {
        ResolvedFetchMapping? mapping = null;
        var outcome = FetchOutcome.NetworkError;
        PolicyDecisionResult? pepDecision = null;
        OneOf<CustodianRecord, NotFound, Unauthorized, Error> result;
        var starTime = timeProvider.GetUtcNow();
        var receivedByteCount = 0;

        try
        {
            var resolvedMapping = await maskUrlService.ResolveAsync(
                requestingOrgId,
                fetchId,
                cancellationToken
            );

            if (resolvedMapping.Value is ResolvedFetchMapping successDto)
            {
                var fetchResult = await GetCustodianDataAsync(successDto, cancellationToken);
                mapping = fetchResult.Mapping;
                outcome = fetchResult.Outcome;
                pepDecision = fetchResult.PepDecision;
                result = fetchResult.Result;

                // If we received a record, compute the byte count from the body we fetched
                if (result.IsT0 && fetchResult.Record is not null)
                {
                    // Serialize back to JSON to measure the exact payload size that was fetched from the provider
                    var json = JsonSerializer.Serialize(
                        fetchResult.Record,
                        JsonSerializerOptions.Web
                    );
                    receivedByteCount = Encoding.UTF8.GetByteCount(json);
                }
            }
            else
            {
                result = resolvedMapping.Value switch
                {
                    NotFound notFound => SetOutcome(
                        out outcome,
                        FetchOutcome.JobNotFound,
                        notFound
                    ),
                    Unauthorized unauthorized => SetOutcome(
                        out outcome,
                        FetchOutcome.AuthorizationFailure,
                        unauthorized
                    ),
                    Error error => SetOutcome(out outcome, FetchOutcome.NetworkError, error),
                    _ => SetOutcome(out outcome, FetchOutcome.NetworkError, new Error()),
                };
            }
        }
        finally
        {
            var endTime = timeProvider.GetUtcNow();
            var status =
                outcome == FetchOutcome.Success ? RequestStatus.Success : RequestStatus.Failure;
            var statusMessage = outcome switch
            {
                FetchOutcome.Success => "Record fetched successfully.",
                FetchOutcome.RecordNotFound => "Record not found.",
                FetchOutcome.PolicyDenial => "Access denied by policy enforcement point.",
                FetchOutcome.JobNotFound => "Fetch job not found.",
                FetchOutcome.AuthorizationFailure => "Authorization failure.",
                FetchOutcome.NetworkError => "Network or service error occurred.",
                _ => "Unknown outcome.",
            };
            var auditPayload = BuildAuditPayload(
                requestingOrgId,
                mapping,
                status,
                statusMessage,
                pepDecision,
                starTime,
                endTime,
                receivedByteCount
            );
            await auditClient.SendAuditEventAsync(auditPayload, cancellationToken);
        }

        return result;
    }

    private static T SetOutcome<T>(out FetchOutcome outcome, FetchOutcome value, T result)
    {
        outcome = value;
        return result;
    }

    private sealed record FetchResult(
        ResolvedFetchMapping? Mapping,
        CustodianRecord? Record,
        FetchOutcome Outcome,
        PolicyDecisionResult? PepDecision,
        OneOf<CustodianRecord, NotFound, Unauthorized, Error> Result
    );

    private async Task<FetchResult> GetCustodianDataAsync(
        ResolvedFetchMapping resolvedMapping,
        CancellationToken cancellationToken
    )
    {
        var provider = await custodianService.GetCustodianAsync(resolvedMapping.TargetOrgId);

        if (!provider.Success || provider.Value is null)
        {
            logger.LogError(
                "Failed to retrieve custodian organisation for Org ID {OrgId}",
                resolvedMapping.TargetOrgId
            );
            return new FetchResult(
                resolvedMapping,
                null,
                FetchOutcome.NetworkError,
                null,
                new Error()
            );
        }

        var requestingOrg = await custodianService.GetCustodianAsync(
            resolvedMapping.RequestingOrgId
        );

        if (!requestingOrg.Success || requestingOrg.Value is null)
        {
            logger.LogError(
                "Failed to retrieve requesting organisation for Org ID {OrgId}",
                resolvedMapping.RequestingOrgId
            );
            return new FetchResult(
                resolvedMapping,
                null,
                FetchOutcome.NetworkError,
                null,
                new Error()
            );
        }

        // PEP check for CONTENT mode access
        var pepDecision = await policyEnforcementService.EvaluateAsync(
            new PolicyDecisionRequest(
                SourceOrgId: resolvedMapping.TargetOrgId,
                DestinationOrgId: resolvedMapping.RequestingOrgId,
                RecordType: resolvedMapping.RecordType,
                Mode: ShareMode.Content,
                Purpose: "SAFEGUARDING" // TODO: Default for now for purpose of Fetch operations
            ),
            provider.Value.DsaPolicy,
            requestingOrg.Value.OrgType
        );

        if (!pepDecision.IsAllowed)
        {
            logger.LogWarning(
                "PEP denied CONTENT access for {RequestingOrg} to {TargetOrg} record type {RecordType}. Reason: {Reason}",
                resolvedMapping.RequestingOrgId,
                resolvedMapping.TargetOrgId,
                resolvedMapping.RecordType,
                pepDecision.Reason
            );
            return new FetchResult(
                resolvedMapping,
                null,
                FetchOutcome.PolicyDenial,
                pepDecision,
                new Unauthorized()
            );
        }

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "PEP allowed CONTENT access for {RequestingOrg} to {TargetOrg} record type {RecordType}",
                resolvedMapping.RequestingOrgId,
                resolvedMapping.TargetOrgId,
                resolvedMapping.RecordType
            );

        var tokenResult = await outboundAuthService.GetAccessTokenAsync(
            provider.Value,
            CancellationToken.None
        );

        if (!tokenResult.Success || string.IsNullOrWhiteSpace(tokenResult.Value))
        {
            return new FetchResult(
                resolvedMapping,
                null,
                FetchOutcome.NetworkError,
                pepDecision,
                new Error()
            );
        }

        var response = await providerClient.GetAsync(
            resolvedMapping.TargetUrl,
            tokenResult.Value,
            cancellationToken
        );

        if (!response.Success)
        {
            return new FetchResult(
                resolvedMapping,
                null,
                FetchOutcome.RecordNotFound,
                pepDecision,
                new Error()
            );
        }

        if (string.IsNullOrWhiteSpace(response.Value))
        {
            logger.LogInformation("Fetch Record returned empty response");
            return new FetchResult(
                resolvedMapping,
                null,
                FetchOutcome.RecordNotFound,
                pepDecision,
                new Error()
            );
        }

        var recordContent = JsonSerializer.Deserialize<CustodianRecord>(
            response.Value,
            JsonSerializerOptions.Web
        );

        if (recordContent is not null)
        {
            return new FetchResult(
                resolvedMapping,
                recordContent,
                FetchOutcome.Success,
                pepDecision,
                recordContent
            );
        }

        return new FetchResult(
            resolvedMapping,
            null,
            FetchOutcome.NetworkError,
            pepDecision,
            new Error()
        );
    }

    private AuditEvent BuildAuditPayload(
        string requestingOrgId,
        ResolvedFetchMapping? mapping,
        RequestStatus requestStatus,
        string requestStatusMessage,
        PolicyDecisionResult? pepDecision,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        int receivedByteCount
    )
    {
        var payload = new PepFetchPayload
        {
            DestinationOrgId = requestingOrgId,
            Purpose = "SAFEGUARDING", // Hard coded for now for Fetch operations
            RequestStatus = requestStatus,
            RequestStatusMessage = requestStatusMessage,
            RequestStartedAt = startTime,
            RequestFinishedAt = endTime,
            ReceivedByteCount = receivedByteCount,
            PolicySnapshot =
                mapping is null || pepDecision is null
                    ? null
                    : new PepFindRecordDetail
                    {
                        SourceOrgId = mapping.TargetOrgId,
                        RecordUrl = mapping.TargetUrl,
                        RecordType = mapping.RecordType,
                        IsSharedAllowed = pepDecision.IsAllowed,
                        RuleType = pepDecision.RuleType ?? "unknown",
                        RuleEffect = pepDecision.RuleEffect ?? "unknown",
                        RuleValidFrom = pepDecision.ValidFrom,
                        RuleValidUntil = pepDecision.ValidUntil,
                        DecisionReason = pepDecision.Reason,
                    },
        };

        return new AuditEvent
        {
            EventId = Guid.NewGuid().ToString(),
            EventName = ApplicationConstants.Audit.PolicyEnforcementPoint.FetchRequestEventName,
            ServiceName = "PolicyEnforcementPoint",
            Actor = new AuditActor { ActorId = requestingOrgId, ActorRole = "Organisation" },
            Payload = JsonSerializer.SerializeToElement(payload),
            Timestamp = timeProvider.GetUtcNow().DateTime,
            CorrelationId = Guid.NewGuid().ToString(),
        };
    }
}
