using System;
using System.Collections.Generic;
using System.Text;

namespace FpdfCsharp.Attachments
{
    /// <summary>
    /// Attachment defines a content to be included in the pdf, in one
    /// of the following ways :
    /// 	- associated with the document as a whole : see SetAttachments()
    ///	- accessible via a link localized on a page : see AddAttachmentAnnotation()
    /// </summary>
    public class Attachment
    {
        byte[] Content;
        // Filename is the displayed name of the attachment
        string Filename;
        // Description is only displayed when using AddAttachmentAnnotation(),
        // and might be modified by the pdf reader.
        string Description;
        int objectNumber; // filled when content is included
    }
}
