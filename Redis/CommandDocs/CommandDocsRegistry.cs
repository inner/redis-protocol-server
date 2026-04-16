using Redis.CommandDocs.Commands;

namespace Redis.CommandDocs;

public static class CommandDocsRegistry
{
    private static readonly IReadOnlyDictionary<string, CommandDocumentation> Docs =
        new Dictionary<string, CommandDocumentation>(StringComparer.OrdinalIgnoreCase)
        {
            [AclDoc.Instance.Name] = AclDoc.Instance,
            [AuthDoc.Instance.Name] = AuthDoc.Instance,
            [BlpopDoc.Instance.Name] = BlpopDoc.Instance,
            [ClientDoc.Instance.Name] = ClientDoc.Instance,
            [ConfigDoc.Instance.Name] = ConfigDoc.Instance,
            [DelDoc.Instance.Name] = DelDoc.Instance,
            [DiscardDoc.Instance.Name] = DiscardDoc.Instance,
            [EchoDoc.Instance.Name] = EchoDoc.Instance,
            [ExecDoc.Instance.Name] = ExecDoc.Instance,
            [ExistsDoc.Instance.Name] = ExistsDoc.Instance,
            [GeoaddDoc.Instance.Name] = GeoaddDoc.Instance,
            [GeodistDoc.Instance.Name] = GeodistDoc.Instance,
            [GeoposDoc.Instance.Name] = GeoposDoc.Instance,
            [GeosearchDoc.Instance.Name] = GeosearchDoc.Instance,
            [GetDoc.Instance.Name] = GetDoc.Instance,
            [IncrDoc.Instance.Name] = IncrDoc.Instance,
            [InfoDoc.Instance.Name] = InfoDoc.Instance,
            [KeysDoc.Instance.Name] = KeysDoc.Instance,
            [LlenDoc.Instance.Name] = LlenDoc.Instance,
            [LpopDoc.Instance.Name] = LpopDoc.Instance,
            [LpushDoc.Instance.Name] = LpushDoc.Instance,
            [LrangeDoc.Instance.Name] = LrangeDoc.Instance,
            [MultiDoc.Instance.Name] = MultiDoc.Instance,
            [PingDoc.Instance.Name] = PingDoc.Instance,
            [PsyncDoc.Instance.Name] = PsyncDoc.Instance,
            [PublishDoc.Instance.Name] = PublishDoc.Instance,
            [QuitDoc.Instance.Name] = QuitDoc.Instance,
            [ReplconfDoc.Instance.Name] = ReplconfDoc.Instance,
            [RpushDoc.Instance.Name] = RpushDoc.Instance,
            [SetDoc.Instance.Name] = SetDoc.Instance,
            [SubscribeDoc.Instance.Name] = SubscribeDoc.Instance,
            [TypeDoc.Instance.Name] = TypeDoc.Instance,
            [UnsubscribeDoc.Instance.Name] = UnsubscribeDoc.Instance,
            [WaitDoc.Instance.Name] = WaitDoc.Instance,
            [XaddDoc.Instance.Name] = XaddDoc.Instance,
            [XrangeDoc.Instance.Name] = XrangeDoc.Instance,
            [XreadDoc.Instance.Name] = XreadDoc.Instance,
            [ZaddDoc.Instance.Name] = ZaddDoc.Instance,
            [ZcardDoc.Instance.Name] = ZcardDoc.Instance,
            [ZrangeDoc.Instance.Name] = ZrangeDoc.Instance,
            [ZrankDoc.Instance.Name] = ZrankDoc.Instance,
            [ZremDoc.Instance.Name] = ZremDoc.Instance,
            [ZscoreDoc.Instance.Name] = ZscoreDoc.Instance
        };

    public static Dictionary<string, Dictionary<string, string>> AllDocs()
    {
        return Docs.Values.ToDictionary(
            doc => doc.Name,
            CommandDocRespAdapter.ToLegacyFieldMap,
            StringComparer.OrdinalIgnoreCase);
    }
}
