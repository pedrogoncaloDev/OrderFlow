using System.Globalization;

namespace OrderFlow.Web.Services;

/// <summary>Formatação no padrão brasileiro, sem depender da cultura instalada no servidor (contêiner).</summary>
public static class BrFormat
{
    /// <summary>1234.5 → "R$ 1.234,50".</summary>
    public static string Money(decimal value)
    {
        var text = value.ToString("#,##0.00", CultureInfo.InvariantCulture); // 1,234.50

        return "R$ " + text.Replace(',', '\u0001').Replace('.', ',').Replace('\u0001', '.');
    }

    /// <summary>1234 → "1.234".</summary>
    public static string Integer(int value) =>
        value.ToString("#,##0", CultureInfo.InvariantCulture).Replace(',', '.');
}
