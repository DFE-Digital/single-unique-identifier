using System.Collections;

namespace SUI.Find.Application.Models;

public record RecordBase(
    string DataType,
    string RecordId,
    string ProviderSystem,
    string Suid
);