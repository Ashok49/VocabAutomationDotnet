using PdfSharpCore.Fonts;
using System.IO;

namespace VocabAutomation.Fonts{
public class CustomFontResolver : IFontResolver
{
    private readonly byte[] _fontData;

    public CustomFontResolver()
    {
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "NotoSans-Regular.ttf");
        _fontData = File.ReadAllBytes(fontPath);
    }

    public string DefaultFontName => "NotoSans#";

    public byte[] GetFont(string faceName) => _fontData;

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        return new FontResolverInfo("NotoSans#");
    }
}
}