using System;
using System.Text;

public static class HexadecimalEncoding
{
    public static string ToHexString(this string str)
    {
        var sb = new StringBuilder();

        var bytes = Encoding.Unicode.GetBytes(str);
        foreach (var t in bytes)
        {
            sb.Append(t.ToString("X2"));
        }

        return sb.ToString(); // returns: "48656C6C6F20776F726C64" for "Hello world"
    }

    public static string FromHexString(this string hexString)
    {
        var bytes = new byte[hexString.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
        }

        return Encoding.Unicode.GetString(bytes); // returns: "Hello world" for "48656C6C6F20776F726C64"
    }

    public static string MakeValidFileName( this string name )
    {
        string invalidChars = System.Text.RegularExpressions.Regex.Escape( new string( System.IO.Path.GetInvalidFileNameChars() ) );
        string invalidRegStr = string.Format( @"([{0}]*\.+$)|([{0}]+)", invalidChars );

        return System.Text.RegularExpressions.Regex.Replace( name, invalidRegStr, "_" );
    }
}
