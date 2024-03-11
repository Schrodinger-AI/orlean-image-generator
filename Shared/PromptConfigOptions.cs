namespace Shared;

public class PromptConfigOptions
{
    public string? ScriptContent { get; set; }
    public ConfigText? ConfigText { get; set; }
}

public class ConfigText
{
    public string Prefix { get; set; }
}