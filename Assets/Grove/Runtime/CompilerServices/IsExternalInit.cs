#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Unity 6.3 stays on C# 9 / .NET Standard 2.1, which does not ship this type.
    /// Record positional properties emit <c>init</c> setters that require it.
    /// Headless tests target net8 and already have the type, so this is compiled out there.
    /// </summary>
    internal static class IsExternalInit
    {
    }
}
#endif
