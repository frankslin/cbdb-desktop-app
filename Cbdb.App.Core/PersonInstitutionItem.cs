namespace Cbdb.App.Core;

public sealed record PersonInstitutionItem(
    string? InstitutionNameChn,
    string? InstitutionName,
    string? Role,
    int? BeginYear,
    string? BeginNianhao,
    int? BeginNianhaoYear,
    string? BeginRange,
    int? EndYear,
    string? EndNianhao,
    int? EndNianhaoYear,
    string? EndRange,
    string? PlaceNameChn,
    string? PlaceName,
    string? PlaceType,
    string? Source,
    string? Pages,
    string? Notes,
    double? XCoord,
    double? YCoord
);
