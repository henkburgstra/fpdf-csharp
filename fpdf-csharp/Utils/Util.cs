using System;
using System.Collections.Generic;
using System.Text;

namespace FpdfCsharp.Utils
{
    public static class Util
    {
        public static string Utf8ToUtf16(string input, bool withBOM = false)
        {
            // TODO: support BOM
            return Encoding.Unicode.GetString(Encoding.Convert(Encoding.UTF8, Encoding.Unicode, Encoding.UTF8.GetBytes(input)));
        }
    }
}
