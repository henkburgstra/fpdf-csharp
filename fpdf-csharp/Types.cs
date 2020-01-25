﻿using System;
using System.Collections.Generic;
using System.Text;

namespace FpdfCsharp
{

    enum ColorMode { colorModeRGB, colorModeSpot, colorModeCMYK };
    
    /// <summary>
    /// SizeType fields Wd and Ht specify the horizontal and vertical extents of a
    /// document element such as a page.
    /// </summary>
    public struct SizeType 
    {
        public double Wd;
        public double Ht;
    }

    /// <summary>
    /// PointType fields X and Y specify the horizontal and vertical coordinates of
    /// a point, typically used in drawing.
    /// </summary>
    public struct PointType 
    {
        public double X;
        public double Y;
    }

    /// <summary>
    /// PageBox defines the coordinates and extent of the various page box types
    /// </summary>
    public struct PageBox 
    {
        public SizeType Size;
        public PointType Point;
    }

    /// <summary>
    /// FontLoader is used to read fonts (JSON font specification and zlib compressed font binaries)
    /// from arbitrary locations (e.g. files, zip files, embedded font resources).
    ///
    /// Open provides an io.Reader for the specified font file (.json or .z). The file name
    /// never includes a path. Open returns an error if the specified file cannot be opened.
    /// </summary>
    public interface FontLoader
    {
        //  Open(name string) (io.Reader, error)
    }

    struct FontBoxType
    {
        int Xmin;
        int Ymin;
        int Xmax;
        int Ymax;
    }

    struct FontFileType
    {
        Int64 length1;
        Int64 length2;
        int n;
        bool embedded;
        byte[] content;
        string fontType;
    }

    // FontDescType (font descriptor) specifies metrics and other
    // attributes of a font, as distinct from the metrics of individual
    // glyphs (as defined in the pdf specification).
    struct FontDescType
    {
        // The maximum height above the baseline reached by glyphs in this
        // font (for example for "S"). The height of glyphs for accented
        // characters shall be excluded.
        int Ascent;
        // The maximum depth below the baseline reached by glyphs in this
        // font. The value shall be a negative number.
        int Descent;
        // The vertical coordinate of the top of flat capital letters,
        // measured from the baseline (for example "H").
        int CapHeight;
        // A collection of flags defining various characteristics of the
        // font. (See the FontFlag* constants.)
        int Flags;
        // A rectangle, expressed in the glyph coordinate system, that
        // shall specify the font bounding box. This should be the smallest
        // rectangle enclosing the shape that would result if all of the
        // glyphs of the font were placed with their origins coincident
        // and then filled.
        FontBoxType FontBBox;
        // The angle, expressed in degrees counterclockwise from the
        // vertical, of the dominant vertical strokes of the font. (The
        // 9-o’clock position is 90 degrees, and the 3-o’clock position
        // is –90 degrees.) The value shall be negative for fonts that
        // slope to the right, as almost all italic fonts do.
        int ItalicAngle;
        // The thickness, measured horizontally, of the dominant vertical
        // stems of glyphs in the font.
        int StemV;
        // The width to use for character codes whose widths are not
        // specified in a font dictionary’s Widths array. This shall have
        // a predictable effect only if all such codes map to glyphs whose
        // actual widths are the same as the value of the MissingWidth
        // entry. (Default value: 0.)
        int MissingWidth;
    }

    struct FontDefType
    {
        string Tp;                        // "Core", "TrueType", ...
        string Name;                      // "Courier-Bold", ...
        FontDescType Desc;                // Font descriptor
        int Up;                           // Underline position
        int Ut;                           // Underline thickness
        int[] Cw;                         // Character width by ordinal
        string Enc;                       // "cp1252", ...
        string Diff;                      // Differences from reference encoding
        string File;                      // "Redressed.z"
        int Size1;                        // Type1 values
        int Size2;                        // Type1 values
        int OriginalSize;                 // Size of uncompressed font file
        int N;                            // Set by font loader
        int DiffN;                        // Position of diff in app array, set by font loader
        string i;                         // 1-based position in font list, set by font loader, not this program
        //utf8File* utf8FontFile // UTF-8 font
        Dictionary<int, int> usedRunes;   // Array of used runes
    }

    /// <summary>
    /// ImageInfoType contains size, color and other information about an image.
    /// Changes to this structure should be reflected in its GobEncode and GobDecode
    /// methods.
    /// </summary>
    struct ImageInfoType
    {
        byte[] data;       // Raw image data
        byte[] smask;      // Soft Mask, an 8bit per-pixel transparency mask
        int n;             // Image object number
        double w;          // Width
        double h;          // Height
        string cs;         // Color space
        byte[] pal;        // Image color palette
        int bpc;           // Bits Per Component
        string f;          // Image filter
        string dp;         // DecodeParms
        int[] trns;        // Transparency mask
        double scale;      // Document scale factor
        double dpi;        // Dots-per-inch found from image file (png only)
        string i;          // SHA-1 checksum of the above values.
    }

    struct LinkType 
    {
        double x;
        double y;
        double wd;
        double ht;
        int link;          // Auto-generated internal link ID or...
        string linkStr;    // ...application-provided external link string
    }

    struct IntLinkType
    {
        int page;
        double y;
    }

    /// <summary>
    /// outlineType is used for a sidebar outline of bookmarks
    /// </summary>
    struct OutlineType
    {
        string text;
        int level;
        int parent;
        int first;
        int last;
        int next;
        int prev;
        double y;
        int p;
    }
    
    struct BlendModeType
    {
        string strokeStr;
        string fillStr;
        string modeStr;
        int objNum;
    }
    
    struct GradientType
    {
        int tp;   // 2: linear, 3: radial
        string clr1Str;
        string clr2Str;
        double x1;
        double y1;
        double x2;
        double y2;
        double r;
        int objNum;
    }
    
    struct ProtectType
    {
        bool encrypted;
        byte[] uValue;
        byte[] oValue;
        int pValue;
        byte[] padding;
        byte[] encryptionKey;
        int objNum;
        //rc4cipher* rc4.Cipher
        UInt32 rc4n; // Object number associated with rc4 cipher
    }

    struct ColorType
    {
        double r;
        double g;
        double b;
        int ir;
        int ig;
        int ib;
        ColorMode mode;
        string spotStr;     // name of current spot color
        bool gray;
        string str;
    }

    // SpotColorType specifies a named spot color value
    struct SpotColorType
    {
        int id;
        int objID;
        CmykColorType val;
    }
    // CMYKColorType specifies an ink-based CMYK color value
    struct CmykColorType
    {
        byte c;
        byte m;
        byte y;
        byte k; // 0% to 100%
    }


}