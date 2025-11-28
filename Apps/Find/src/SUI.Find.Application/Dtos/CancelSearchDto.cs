using SUI.Find.Application.Services;

namespace SUI.Find.Application.Dtos;

public record CancelSearchDto(CancelSearchResult Result, string ErrorMessage);
