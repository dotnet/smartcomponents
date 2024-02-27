namespace SmartComponents.Infrastructure;

#if SMART_COMPONENTS_COMPONENTS
internal
#else
public
#endif
struct SmartTextAreaConfig
{
    public string? UserRole { get; set; }
    public string[]? UserPhrases { get; set; }
}
