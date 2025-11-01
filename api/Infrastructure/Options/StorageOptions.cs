namespace LexiFlow.Api.Infrastructure.Options;

public class StorageOptions
{
    public const string SectionName = "Storage";

    public string UploadsPath { get; set; } = "/app/uploads";
}
