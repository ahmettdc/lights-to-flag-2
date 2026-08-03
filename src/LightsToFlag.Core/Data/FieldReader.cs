using System.Globalization;

namespace LightsToFlag.Core.Data;

/// <summary>
/// A forward-only cursor over the underscore-separated fields of one legacy
/// carset record. Every read trims the fixed-width padding the authoring tool
/// leaves behind and parses numbers with the invariant culture, so records like
/// <c>"7 "</c> or <c>"84.125 "</c> read cleanly. Failures carry the record
/// context so a bad carset points at the offending field.
/// </summary>
internal sealed class FieldReader
{
    private readonly string[] _fields;
    private readonly string _context;
    private int _cursor;

    public FieldReader(string[] fields, string context)
    {
        _fields = fields;
        _context = context;
    }

    /// <summary>Number of fields still unread.</summary>
    public int Remaining => _fields.Length - _cursor;

    public string ReadString()
    {
        if (_cursor >= _fields.Length)
        {
            throw new CarsetValidationException(
                $"{_context}: expected another field at index {_cursor} but the record only has {_fields.Length}.");
        }

        return _fields[_cursor++].Trim();
    }

    public int ReadInt()
    {
        var raw = ReadString();
        if (raw.Length == 0)
        {
            return 0;
        }

        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            // Some authored fields carry a decimal where an int is expected; round it.
            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var asDouble))
            {
                return (int)Math.Round(asDouble);
            }

            throw new CarsetValidationException(
                $"{_context}: field {_cursor} '{raw}' is not an integer.");
        }

        return value;
    }

    public double ReadDouble()
    {
        var raw = ReadString();
        if (raw.Length == 0)
        {
            return 0d;
        }

        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            throw new CarsetValidationException(
                $"{_context}: field {_cursor} '{raw}' is not a number.");
        }

        return value;
    }

    public bool ReadBool()
    {
        var raw = ReadString();
        return raw.Equals("True", StringComparison.OrdinalIgnoreCase)
            || raw == "1";
    }

    public IReadOnlyList<string> ReadStrings(int count)
    {
        var list = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            list.Add(ReadString());
        }

        return list;
    }

    public IReadOnlyList<int> ReadInts(int count)
    {
        var list = new List<int>(count);
        for (var i = 0; i < count; i++)
        {
            list.Add(ReadInt());
        }

        return list;
    }

    public IReadOnlyList<double> ReadDoubles(int count)
    {
        var list = new List<double>(count);
        for (var i = 0; i < count; i++)
        {
            list.Add(ReadDouble());
        }

        return list;
    }
}
