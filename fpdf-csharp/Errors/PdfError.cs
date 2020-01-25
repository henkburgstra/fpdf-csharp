using System;
using System.Collections.Generic;
using System.Text;

namespace FpdfCsharp.Errors
{
    public class PdfError : Error
    {
        private string _err = "";
        public PdfError(string err)
        {
            _err = err;
        }
        public string Error()
        {
            return _err;
        }
    }
}
