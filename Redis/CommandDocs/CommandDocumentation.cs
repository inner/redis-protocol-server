namespace Redis.CommandDocs;

public record CommandDocumentation(
    string Name,
    string? Summary = null,
    string? Syntax = null,
    string? Since = null,
    string? Group = null,
    string? Complexity = null,
    string? Documentation = null,
    string? Arity = null,
    bool? ContainerCommands = null,
    string? History = null,
    string? Notes = null,
    IReadOnlyList<string>? Usage = null
);
