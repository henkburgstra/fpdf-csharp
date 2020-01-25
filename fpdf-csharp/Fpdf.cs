using FpdfCsharp.Attachments;
using FpdfCsharp.Layers;
using System;
using System.Collections.Generic;

namespace FpdfCsharp
{
    public class Fpdf
    {
		private bool isCurrentUTF8;                                             // is current font used in utf-8 mode
		private bool isRTL;                                                     // is is right to left mode enabled
		private int page;                                                       // current page number
		private int n;                                                          // current object number
		private int[] offsets;                                                  // array of object offsets
		private Dictionary<string, Template> templates;                         // templates used in this document
		private Dictionary<string, int> templateObjects;                        // template object IDs within this document
		private Dictionary<string, byte[]> importedObjs;                        // imported template objects (gofpdi)
		private Dictionary<string, Dictionary<int, string>> importedObjPos;     // imported template objects hashes and their positions (gofpdi)
		private Dictionary<string, string> importedTplObjs;                     // imported template names and IDs (hashed) (gofpdi)
		private Dictionary<string, int> importedTplIDs;                         // imported template ids hash to object id int (gofpdi)
		private FmtBuffer buffer;                                               // buffer holding in-memory PDF
		//pages[]*bytes.Buffer            // slice[page] of page content; 1-based
		private int state;                                                      // current document state
		private bool compress;                                                  // compression flag
		private double k;                                                       // scale factor (number of points in user unit)
		private string defOrientation;                                          // default orientation
		private string curOrientation;                                          // current orientation
		private Dictionary<string, SizeType> stdPageSizes;                      // standard page sizes
		private SizeType defPageSize;                                           // default page size
		private Dictionary<string, PageBox> defPageBoxes;                       // default page size
		private SizeType curPageSize;                                           // current page size
		private Dictionary<int, SizeType> pageSizes;                            // used for pages with non default sizes or orientations
		private Dictionary<int, Dictionary<string, PageBox>> pageBoxes;         // used to define the crop, trim, bleed and art boxes
		private string unitStr;                                                 // unit of measure for all rendered objects except fonts
		private double wPt;                                                     // dimensions of current page in points
		private double hPt;                                                     // dimensions of current page in points
		private double w;                                                       // dimensions of current page in user unit
		private double h;                                                       // dimensions of current page in user unit
		double lMargin;                                                         // left margin
		double tMargin;                                                         // top margin
		double rMargin;                                                         // right margin
		double bMargin;                                                         // page break margin
		double cMargin;                                                         // cell margin
		double x;                                                               // current position in user unit
		double y;                                                               // current position in user unit
		double lasth;                                                           // height of last printed cell
		double lineWidth;                                                       // line width in user unit
		string fontpath;                                                        // path containing fonts
		FontLoader fontLoader;                                                  // used to load font files from arbitrary locations
		Dictionary<string, bool> coreFonts;                                     // array of core font names
		Dictionary<string, FontDefType> fonts;                                  // array of used fonts
		Dictionary<string, FontFileType> fontFiles;                             // array of font files
		string[] diffs;                                                         // array of encoding differences
		string fontFamily;                                                      // current font family
		string fontStyle;                                                       // current font style
		bool underline;                                                         // underlining flag
		bool strikeout;                                                         // strike out flag
		FontDefType currentFont;                                                // current font info
		double fontSizePt;                                                      // current font size in points
		double fontSize;                                                        // current font size in user unit
		double ws;                                                              // word spacing
		Dictionary<string, ImageInfoType> images;                               // array of used images
		Dictionary<string, string> aliasMap;                                    // map of alias->replacement
		LinkType[][] pageLinks;                                                 // pageLinks[page][link], both 1-based
		IntLinkType[] links;                                                    // array of internal links
		Attachment[] attachments;                                               // slice of content to embed globally
		AnnotationAttach[] pageAttachments;                                     // 1-based array of annotation for file attachments (per page)
		OutlineType[] outlines;                                                 // array of outlines
		int outlineRoot;                                                        // root of outlines
		bool autoPageBreak;                                                     // automatic page breaking
		//acceptPageBreak  func() bool                // returns true to accept page break
		double pageBreakTrigger;                                                // threshold used to trigger page breaks
		bool inHeader;                                                          // flag set when processing header
																				//headerFnc        func()                     // function provided by app and called to write header
		bool headerHomeMode;                                                    // set position to home after headerFnc is called
		bool inFooter;                                                          // flag set when processing footer
																				//footerFnc        func()                     // function provided by app and called to write footer
																				//footerFncLpi func(bool)                 // function provided by app and called to write footer with last page flag
		string zoomMode;                                                        // zoom display mode
		string layoutMode;                                                      // layout display mode
		byte[] xmp;                                                             // XMP metadata
		string producer;                                                        // producer
		string title;                                                           // title
		string subject;                                                         // subject
		string author;                                                          // author
		string keywords;                                                        // keywords
		string creator;                                                         // creator
		DateTime creationDate; //     time.Time                  // override for document CreationDate value
		DateTime modDate; //          time.Time                  // override for document ModDate value
		string aliasNbPagesStr;                                                 // alias for total number of pages
		string pdfVersion;                                                      // PDF version number
		string fontDirStr;                                                      // location of font definition files
		int capStyle;                                                           // line cap style: butt 0, round 1, square 2
		int joinStyle;                                                          // line segment join style: miter 0, round 1, bevel 2
		double[] dashArray;                                                     // dash array
		double dashPhase;                                                       // dash phase
		BlendModeType[] blendList;                                              // slice[idx] of alpha transparency modes, 1-based
		Dictionary<string, int> blendMap;                                       // map into blendList
		string blendMode;                                                       // current blend mode
		double alpha;                                                           // current transpacency
		GradientType[] gradientList;                                            // slice[idx] of gradient records
		int clipNest;                                                           // Number of active clipping contexts
		int transformNest;                                                      // Number of active transformation contexts
																				//err              error                      // Set if error occurs during life cycle of instance
		ProtectType protect;                                                    // document protection structure
		LayerRecType layer;                                                     // manages optional layers in document
		bool catalogSort;                                                       // sort resource catalogs in document
		int nJs;                                                                // JavaScript object number
		string javascript; // javascript* string                    // JavaScript code to include in the PDF
		bool colorFlag;                                                         // indicates whether fill and text colors are different
		struct color
		{
			// Composite values of colors
			ColorType draw;
			ColorType fill;
			ColorType text;
		}
		Dictionary<string, SpotColorType> spotColorMap;                         // Map of named ink-based colors
		double userUnderlineThickness;                                          // A custom user underline thickness multiplier.
	}
}
