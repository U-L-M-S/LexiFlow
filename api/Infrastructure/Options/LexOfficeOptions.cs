namespace LexiFlow.Api.Infrastructure.Options;

public class LexOfficeOptions
{
    public const string SectionName = "LexOffice";

    public string ApiBase { get; set; } = "http://lexmock";
    public string ApiKey { get; set; } = "demo-lexoffice-key";
}
