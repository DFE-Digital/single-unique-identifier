namespace SUI.Find.Application.Models;

public interface IPepFilterableRecord : IPepFilterable
{
    string RecordUrl { get; }
}
