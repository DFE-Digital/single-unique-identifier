namespace SUI.Find.Infrastructure.Utility;

public class PolicyKeyFactory
{

    public static string CreateKey(string src, string dest, string mode, string type, string purpose)
    {
        // 
        return $"{src}|{dest}|{mode}|{type}|{purpose}".ToUpperInvariant();
    }
}