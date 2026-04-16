namespace Redis.CommandDocs;

public static class CommandDocRespAdapter
{
    public static Dictionary<string, string> ToLegacyFieldMap(CommandDocumentation doc)
    {
        var fields = new Dictionary<string, string>();

        // Keep the RESP payload shape stable while normalizing ownership into a
        // typed model. Commands that have not moved yet still flow through their
        // legacy `Docs()` methods during the staged migration.
        if (!string.IsNullOrWhiteSpace(doc.Summary))
        {
            fields["summary"] = doc.Summary;
        }

        if (!string.IsNullOrWhiteSpace(doc.Syntax))
        {
            fields["syntax"] = doc.Syntax;
        }

        if (!string.IsNullOrWhiteSpace(doc.Since))
        {
            fields["since"] = doc.Since;
        }

        if (!string.IsNullOrWhiteSpace(doc.Group))
        {
            fields["group"] = doc.Group;
        }

        if (!string.IsNullOrWhiteSpace(doc.Complexity))
        {
            fields["complexity"] = doc.Complexity;
        }

        if (!string.IsNullOrWhiteSpace(doc.Documentation))
        {
            fields["documentation"] = doc.Documentation;
        }

        if (!string.IsNullOrWhiteSpace(doc.Arity))
        {
            fields["arity"] = doc.Arity;
        }

        if (doc.ContainerCommands.HasValue)
        {
            fields["container_commands"] = doc.ContainerCommands.Value
                ? "true"
                : "false";
        }

        if (!string.IsNullOrWhiteSpace(doc.History))
        {
            fields["history"] = doc.History;
        }

        if (!string.IsNullOrWhiteSpace(doc.Notes))
        {
            fields["notes"] = doc.Notes;
        }

        if (doc.Usage == null)
        {
            return fields;
        }

        for (var i = 0; i < doc.Usage.Count; i++)
        {
            var fieldName = doc.Usage.Count == 1
                ? "usage"
                : $"usage #{i + 1}";

            fields[fieldName] = doc.Usage[i];
        }

        return fields;
    }
}
