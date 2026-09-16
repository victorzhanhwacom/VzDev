using System;
using System.Globalization;

/// <summary>
/// Utility methods for formatting numbers with thousand separators,
/// similar to currency-style display (e.g. 1234567 -> "1,234,567").
/// </summary>
public static class NumberFormater
{
    /// <summary>
    /// Formats an integer value with thousand separators, no decimals.
    /// e.g. 1234567 -> "1,234,567"
    /// </summary>
    public static string ToThousands(long value)
    {
        return value.ToString("N0", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats a floating point value with thousand separators.
    /// decimalPlaces controls how many digits after the decimal point are shown.
    /// e.g. ToThousands(1234567.891, 2) -> "1,234,567.89"
    /// </summary>
    public static string ToThousands(double value, int decimalPlaces = 0)
    {
        string format = "N" + decimalPlaces;
        return value.ToString(format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats a numeric string with thousand separators.
    /// Returns the original string unchanged if parsing fails.
    /// e.g. "1234567" -> "1,234,567"
    /// </summary>
    public static string ToThousands(string numericString, int decimalPlaces = 0)
    {
        if (string.IsNullOrWhiteSpace(numericString))
        {
            return numericString;
        }

        if (double.TryParse(numericString, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed))
        {
            return ToThousands(parsed, decimalPlaces);
        }

        // Not a valid number; return input unchanged so callers can decide how to handle it.
        return numericString;
    }

    /// <summary>
    /// Formats a value as currency using the invariant culture's currency symbol rules
    /// combined with a custom symbol prefix (since InvariantCulture has no real currency symbol).
    /// e.g. ToCurrency(1234567.5, "$") -> "$1,234,567.50"
    /// </summary>
    public static string ToCurrency(double value, string symbol = "$", int decimalPlaces = 2)
    {
        string number = ToThousands(value, decimalPlaces);
        return symbol + number;
    }
}