using FpdfCsharp.Attachments;
using FpdfCsharp.Embedding;
using FpdfCsharp.Errors;
using FpdfCsharp.Layers;
using FpdfCsharp.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace FpdfCsharp
{
    public class Fpdf
    {
		private static readonly string cnFpdfVersion = "1.7";
		private static Gl gl = new Gl();
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
		private Utils.Buffer buffer = new Utils.Buffer();                       // buffer holding in-memory PDF
		private Utils.Buffer[] pages;       //pages[]*bytes.Buffer            // slice[page] of page content; 1-based
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
		private double lMargin;                                                 // left margin
		private double tMargin;                                                 // top margin
		private double rMargin;                                                 // right margin
		private double bMargin;                                                 // page break margin
		private double cMargin;                                                 // cell margin
		private double x;                                                       // current position in user unit
		private double y;                                                       // current position in user unit
		private double lasth;                                                   // height of last printed cell
		private double lineWidth;                                               // line width in user unit
		private string fontpath;                                                // path containing fonts
		private FontLoader fontLoader;                                          // used to load font files from arbitrary locations
		private Dictionary<string, bool> coreFonts;                             // array of core font names
		private Dictionary<string, FontDefType> fonts;                          // array of used fonts
		private Dictionary<string, FontFileType> fontFiles;                     // array of font files
		private List<string> diffs = new List<string>();                        // array of encoding differences
		private string fontFamily;                                              // current font family
		private string fontStyle;                                               // current font style
		private bool underline;                                                 // underlining flag
		private bool strikeout;                                                 // strike out flag
		private FontDefType currentFont;                                        // current font info
		private double fontSizePt;                                              // current font size in points
		private double fontSize;                                                // current font size in user unit
		private double ws;                                                      // word spacing
		private Dictionary<string, ImageInfoType> images;                       // array of used images
		private Dictionary<string, string> aliasMap;                            // map of alias->replacement
		private List<List<LinkType>> pageLinks = new List<List<LinkType>>();    // pageLinks[page][link], both 1-based
		private List<IntLinkType> links = new List<IntLinkType>();              // list of internal links
		private Attachment[] attachments;                                       // slice of content to embed globally
		private AnnotationAttach[] pageAttachments;                             // 1-based array of annotation for file attachments (per page)
		private List<OutlineType> outlines = new List<OutlineType>();           // array of outlines
		private int outlineRoot;                                                // root of outlines
		private bool autoPageBreak;                                             // automatic page breaking
		private Func<bool> acceptPageBreak;                                     // returns true to accept page break
		private double pageBreakTrigger;                                        // threshold used to trigger page breaks
		private bool inHeader;                                                  // flag set when processing header
		private Action headerFnc;                                               // function provided by app and called to write header
		private bool headerHomeMode;                                            // set position to home after headerFnc is called
		private bool inFooter;                                                  // flag set when processing footer
		private Action footerFnc;                                               // function provided by app and called to write footer
		private Action<bool> footerFncLpi;                                      // function provided by app and called to write footer with last page flag
		private string zoomMode;                                                // zoom display mode
		private string layoutMode;                                              // layout display mode
		private byte[] xmp;                                                     // XMP metadata
		private string producer;                                                // producer
		private string title;                                                   // title
		private string subject;                                                 // subject
		private string author;                                                  // author
		private string keywords;                                                // keywords
		private string creator;                                                 // creator
		private DateTime creationDate; //     time.Time                  // override for document CreationDate value
		private DateTime modDate; //          time.Time                  // override for document ModDate value
		private string aliasNbPagesStr;                                         // alias for total number of pages
		private string pdfVersion;                                              // PDF version number
		private string fontDirStr;                                              // location of font definition files
		private int capStyle;                                                   // line cap style: butt 0, round 1, square 2
		private int joinStyle;                                                  // line segment join style: miter 0, round 1, bevel 2
		private double[] dashArray;                                             // dash array
		private double dashPhase;                                               // dash phase
		private BlendModeType[] blendList;                                      // slice[idx] of alpha transparency modes, 1-based
		private Dictionary<string, int> blendMap;                               // map into blendList
		private string blendMode;                                               // current blend mode
		private double alpha;                                                   // current transpacency
		private GradientType[] gradientList;                                    // slice[idx] of gradient records
		private int clipNest;                                                   // Number of active clipping contexts
		private int transformNest;                                              // Number of active transformation contexts
		private Error err = null;                                               // Set if error occurs during life cycle of instance
		private ProtectType protect;                                            // document protection structure
		private LayerRecType layer;                                             // manages optional layers in document
		private bool catalogSort;                                               // sort resource catalogs in document
		private int nJs;                                                        // JavaScript object number
		private string javascript; // javascript* string                   // JavaScript code to include in the PDF
		private bool colorFlag;                                                 // indicates whether fill and text colors are different
		private Color color = new Color();
		private Dictionary<string, SpotColorType> spotColorMap;                 // Map of named ink-based colors
		private double userUnderlineThickness;                                  // A custom user underline thickness multiplier.

		/// <summary>
		/// Ok returns true if no processing errors have occurred.
		/// </summary>
		public bool Ok()
		{
			return err == null;
		}
		/// <summary>
		/// Err returns true if a processing error has occurred.
		/// </summary>
		public bool Err()
		{
			return err != null;
		}
		/// <summary>
		/// ClearError unsets the internal Fpdf error. This method should be used with
		/// care, as an internal error condition usually indicates an unrecoverable
		/// problem with the generation of a document. It is intended to deal with cases
		/// in which an error is used to select an alternate form of the document.
		/// </summary>
		public void ClearError()
		{
			err = null;
		}
		/// <summary>
		/// SetErrorf sets the internal Fpdf error with formatted text to halt PDF
		/// generation; this may facilitate error handling by application. If an error
		/// condition is already set, this call is ignored.
		///
		/// See the documentation for String.Format for details
		/// about fmtStr and args.
		/// </summary>
		/// <param name="fmtStr">a composite format string</param>
		/// <param name="args">an object array that contains zero or more objects to format</param>
		public void SetErrorf(string fmtStr, params object[] args)
		{
			if (err == null)
			{
				err = new PdfError(String.Format(fmtStr, args));
			}
		}
		/// <summary>
		/// ToString overrides base ToString
		/// </summary>
		/// <returns>a summary of the Fpdf instance</returns>
		public override string ToString()
		{
			return $"Fpdf {cnFpdfVersion}";
		}
		/// <summary>
		/// SetError sets an error to halt PDF generation. This may facilitate error
		/// handling by application. See also Ok(), Err() and Error().
		/// </summary>
		public void SetError(Error err)
		{
			if (this.err == null && err != null)
			{
				this.err = err;
			}
		}

		/// <summary>
		/// Error returns the internal Fpdf error; this will be null if no error has occurred.
		/// </summary>
		public Error Error()
		{
			return err;
		}
		/// <summary>
		/// GetPageSize returns the current page's width and height. This is the paper's
		/// size. To compute the size of the area being used, subtract the margins (see
		/// GetMargins()).
		/// </summary>
		/// <returns>(width, height)</returns>
		public (double, double) GetPageSize()
		{
			return (w, h);
		}
		/// <summary>
		/// GetMargins returns the left, top, right, and bottom margins. The first three
		/// are set with the SetMargins() method. The bottom margin is set with the
		/// SetAutoPageBreak() method.
		/// </summary>
		/// <returns>(left margin, top margin, right margin, bottom margin)</returns>
		public (double, double, double, double) GetMargins()
		{
			return (lMargin, tMargin, rMargin, bMargin);
		}
		/// <summary>
		/// SetMargins defines the left, top and right margins. By default, they equal 1 cm.
		/// Call this method to change them. If the value of the right margin is
		/// less than zero, it is set to the same as the left margin.
		/// </summary>
		/// <param name="left">left margin</param>
		/// <param name="top">top margin</param>
		/// <param name="right">right margin</param>
		public void SetMargins(double left, double top, double right)
		{
			lMargin = left;
			tMargin = top;
			if (right < 0)
			{
				right = left;
			}
			rMargin = right;
		}
		/// <summary>
		/// SetLeftMargin defines the left margin. The method can be called before
		/// creating the first page. If the current abscissa gets out of page, it is
		/// brought back to the margin.
		/// </summary>
		/// <param name="margin">left margin</param>
		public void SetLeftMargin(double margin)
		{
			lMargin = margin;
			if (page > 0 && x < margin)
			{
				x = margin;
			}
		}

		/// <summary>
		/// SetPageBoxRec sets the page box for the current page, and any following
		/// pages. Allowable types are trim, trimbox, crop, cropbox, bleed, bleedbox,
		/// art and artbox box types are case insensitive. See SetPageBox() for a method
		/// that specifies the coordinates and extent of the page box individually.
		/// </summary>
		/// <param name="t">type name</param>
		/// <param name="pb"></param>
		public void SetPageBoxRec(string t, PageBox pb)
		{
			switch (t.ToLower()) {
				case "trim":
				case "trimbox":
					t = "TrimBox";
					break;
				case "crop":
				case "cropbox":
					t = "CropBox";
					break;
				case "bleed":
				case "bleedbox":
					t = "BleedBox";
					break;
				case "art":
				case "artbox":
					t = "ArtBox";
					break;
				default:
					err = new PdfError($"{t} is not a valid page box type");
					return;
			}
			pb.Point.X *= k;
			pb.Point.Y *= k;
			pb.Size.Wd = (pb.Size.Wd * k) + pb.Point.X;
			pb.Size.Ht = (pb.Size.Ht * k) + pb.Point.Y;

			if (page > 0) 
			{
				pageBoxes[page][t] = pb;
			}

			// always override. page defaults are supplied in addPage function
			defPageBoxes[t] = pb;

		}
		/// <summary>
		/// SetPageBox sets the page box for the current page, and any following pages.
		/// Allowable types are trim, trimbox, crop, cropbox, bleed, bleedbox, art and
		/// artbox box types are case insensitive.
		//// </summary>
		public void SetPageBox(string t, double x, double y, double wd, double ht)
		{
			SetPageBoxRec(t, new PageBox
			{
				Point = new PointType { X = x, Y = y },
				Size = new SizeType { Wd = wd, Ht = ht }
			});
		}
		/// <summary>
		/// SetPage sets the current page to that of a valid page in the PDF document.
		/// pageNum is one-based. The SetPage() example demonstrates this method.
		/// </summary>
		/// <param name="pageNum">one-based</param>
		public void SetPage(int pageNum)
		{
			if ((pageNum > 0) && (pageNum < pages.Length))
			{
				page = pageNum;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public int PageCount()
		{
			return pages.Length - 1;
		}
		/// <summary>
		/// SetFontLocation sets the location in the file system of the font and font
		/// definition files.
		/// </summary>
		public void SetFontLocation(string fontDirStr)
		{
			fontpath = fontDirStr;
		}
		/// <summary>
		/// SetFontLoader sets a loader used to read font files (.json and .z) from an
		/// arbitrary source. If a font loader has been specified, it is used to load
		/// the named font resources when AddFont() is called. If this operation fails,
		/// an attempt is made to load the resources from the configured font directory
		/// (see SetFontLocation()).
		/// </summary>
		public void SetFontLoader(FontLoader loader)
		{
			fontLoader = loader;
		}
		/// <summary>
		/// SetHeaderFuncMode sets the function that lets the application render the
		/// page header. See SetHeaderFunc() for more details. The value for homeMode
		/// should be set to true to have the current position set to the left and top
		/// margin after the header function is called.
		/// </summary>
		public void SetHeaderFuncMode(Action fnc, bool homeMode)
		{
			headerFnc = fnc;
			headerHomeMode = homeMode;
		}
		/// <summary>
		/// SetHeaderFunc sets the function that lets the application render the page
		/// header. The specified function is automatically called by AddPage() and
		/// should not be called directly by the application. The implementation in Fpdf
		/// is empty, so you have to provide an appropriate function if you want page
		/// headers. fnc will typically be a closure that has access to the Fpdf
		/// instance and other document generation variables.
		///
		/// A header is a convenient place to put background content that repeats on
		/// each page such as a watermark. When this is done, remember to reset the X
		/// and Y values so the normal content begins where expected. Including a
		/// watermark on each page is demonstrated in the example for TransformRotate.
		///
		/// This method is demonstrated in the example for AddPage().
		/// </summary>
		public void SetHeaderFunc(Action fnc)
		{
			headerFnc = fnc;
		}
		/// <summary>
		/// SetFooterFunc sets the function that lets the application render the page
		/// footer. The specified function is automatically called by AddPage() and
		/// Close() and should not be called directly by the application. The
		/// implementation in Fpdf is empty, so you have to provide an appropriate
		/// function if you want page footers. fnc will typically be a closure that has
		/// access to the Fpdf instance and other document generation variables. See
		/// SetFooterFuncLpi for a similar function that passes a last page indicator.
		///
		/// This method is demonstrated in the example for AddPage().
		/// </summary>
		public void SetFooterFunc(Action fnc)
		{
			footerFnc = fnc;
		}
		/// <summary>
		/// SetFooterFuncLpi sets the function that lets the application render the page
		/// footer. The specified function is automatically called by AddPage() and
		/// Close() and should not be called directly by the application. It is passed a
		/// boolean that is true if the last page of the document is being rendered. The
		/// implementation in Fpdf is empty, so you have to provide an appropriate
		/// function if you want page footers. fnc will typically be a closure that has
		/// access to the Fpdf instance and other document generation variables.
		/// </summary>
		public void SetFooterFuncLpi(Action<bool> fnc)
		{
			footerFncLpi = fnc;
			footerFnc = null;
		}
		/// <summary>
		/// SetTopMargin defines the top margin. The method can be called before
		/// creating the first page.
		/// </summary>
		public void SetTopMargin(double margin)
		{
			tMargin = margin;
		}
		/// <summary>
		/// SetRightMargin defines the right margin. The method can be called before
		/// creating the first page.
		/// </summary>
		public void SetRightMargin(double margin)
		{
			rMargin = margin;
		}
		/// <summary>
		/// GetAutoPageBreak returns true if automatic pages breaks are enabled, false
		/// otherwise. This is followed by the triggering limit from the bottom of the
		/// page. This value applies only if automatic page breaks are enabled.
		/// </summary>
		/// <returns>(bool autoPageBreak, double bottomMargin)</returns>
		public (bool, double) GetAutoPageBreak()
		{
			return (autoPageBreak, bMargin);
		}
		/// <summary>
		/// SetAutoPageBreak enables or disables the automatic page breaking mode. When
		/// enabling, the second parameter is the distance from the bottom of the page
		/// that defines the triggering limit. By default, the mode is on and the margin
		/// is 2 cm.
		/// </summary>
		public void SetAutoPageBreak(bool auto, double margin)
		{
			autoPageBreak = auto;
			bMargin = margin;
			pageBreakTrigger = h - margin;
		}
		/// <summary>
		/// SetDisplayMode sets advisory display directives for the document viewer.
		/// Pages can be displayed entirely on screen, occupy the full width of the
		/// window, use real size, be scaled by a specific zooming factor or use viewer
		/// default (configured in the Preferences menu of Adobe Reader). The page
		/// layout can be specified so that pages are displayed individually or in
		/// pairs.
		///
		/// zoomStr can be "fullpage" to display the entire page on screen, "fullwidth"
		/// to use maximum width of window, "real" to use real size (equivalent to 100%
		/// zoom) or "default" to use viewer default mode.
		///
		/// layoutStr can be "single" (or "SinglePage") to display one page at once,
		/// "continuous" (or "OneColumn") to display pages continuously, "two" (or
		/// "TwoColumnLeft") to display two pages on two columns with odd-numbered pages
		/// on the left, or "TwoColumnRight" to display two pages on two columns with
		/// odd-numbered pages on the right, or "TwoPageLeft" to display pages two at a
		/// time with odd-numbered pages on the left, or "TwoPageRight" to display pages
		/// two at a time with odd-numbered pages on the right, or "default" to use
		/// viewer default mode.
		/// </summary>
		/// <param name="zoomStr"></param>
		/// <param name="layoutStr"></param>
		public void SetDisplayMode(string zoomStr, string layoutStr)
		{
			if (err != null)
			{
				return;
			}
			switch (zoomStr) 
			{
				case "fullpage":
				case "fullwidth":
				case "real":
				case "default":
					zoomMode = zoomStr;
					break;
				default:
					err = new PdfError($"incorrect zoom display mode: {zoomStr}");
					return;
			}
			switch (layoutStr)
			{
				case "single":
				case "continuous":
				case "two":
				case "default":
				case "SinglePage":
				case "OneColumn":
				case "TwoColumnLeft":
				case "TwoColumnRight":
				case "TwoPageLeft":
				case "TwoPageRight":
					layoutMode = layoutStr;
					break;
				default:
					err = new PdfError($"incorrect layout display mode: {layoutStr}");
					return;
			}
		}
		/// <summary>
		/// SetDefaultCompression controls the default setting of the internal
		/// compression flag. See SetCompression() for more details. Compression is on
		/// by default.
		/// </summary>
		public void SetDefaultCompression(bool compress)
		{
			gl.noCompress = !compress;
		}
		/// <summary>
		/// SetCompression activates or deactivates page compression with zlib. When
		/// activated, the internal representation of each page is compressed, which
		/// leads to a compression ratio of about 2 for the resulting document.
		/// Compression is on by default.
		/// </summary>
		public void SetCompression(bool compress)
		{
			this.compress = compress;
		}
		/// <summary>
		/// SetProducer defines the producer of the document. isUTF8 indicates if the string
		/// is encoded in ISO-8859-1 (false) or UTF-8 (true).
		/// </summary>
		public void SetProducer(string producerStr, bool isUTF8)
		{
			if (isUTF8)
			{
				producerStr = Util.Utf8ToUtf16(producerStr);
			}
			this.producer = producerStr;
		}

		/// <summary>
		/// SetTitle defines the title of the document. isUTF8 indicates if the string
		/// is encoded in ISO-8859-1 (false) or UTF-8 (true).
		/// </summary>
		public void SetTitle(string titleStr, bool isUTF8)
		{
			if (isUTF8) 
			{
				titleStr = Util.Utf8ToUtf16(titleStr);
			}
			this.title = titleStr;
		}
		/// <summary>
		/// SetSubject defines the subject of the document. isUTF8 indicates if the
		/// string is encoded in ISO-8859-1 (false) or UTF-8 (true).
		/// </summary>
		public void SetSubject(string subjectStr, bool isUTF8)
		{
			if (isUTF8) 
			{
				subjectStr = Util.Utf8ToUtf16(subjectStr);
			}
			this.subject = subjectStr;
		}
		/// <summary>
		/// SetAuthor defines the author of the document. isUTF8 indicates if the string
		/// is encoded in ISO-8859-1 (false) or UTF-8 (true).
		/// </summary>
		public void SetAuthor(string authorStr, bool isUTF8)
		{
			if (isUTF8) 
			{
				authorStr = Util.Utf8ToUtf16(authorStr);
			}
			this.author = authorStr;
		}
		/// <summary>
		/// SetKeywords defines the keywords of the document. keywordStr is a
		/// space-delimited string, for example "invoice August". isUTF8 indicates if
		/// the string is encoded
		/// </summary>
		public void SetKeywords(string keywordsStr, bool isUTF8)
		{
			if (isUTF8) 
			{
				keywordsStr = Util.Utf8ToUtf16(keywordsStr);
			}
			this.keywords = keywordsStr;
		}
		/// <summary>
		/// SetCreator defines the creator of the document. isUTF8 indicates if the
		/// string is encoded in ISO-8859-1 (false) or UTF-8 (true).
		/// </summary>
		public void SetCreator(string creatorStr, bool isUTF8)
		{
			if (isUTF8) 
			{
				creatorStr = Util.Utf8ToUtf16(creatorStr);
			}
			this.creator = creatorStr;
		}
		/// <summary>
		/// SetXmpMetadata defines XMP metadata that will be embedded with the document.
		/// </summary>
		public void SetXmpMetadata(byte[] xmpStream)
		{
			this.xmp = xmpStream;
		}
		/// <summary>
		/// AliasNbPages defines an alias for the total number of pages. It will be
		/// substituted as the document is closed. An empty string is replaced with the
		/// string "{nb}".
		///
		/// See the example for AddPage() for a demonstration of this method.
		/// </summary>
		public void AliasNbPages(string aliasStr)
		{
			if (string.IsNullOrEmpty(aliasStr)) 
			{
				aliasStr = "{nb}";
			}
			this.aliasNbPagesStr = aliasStr;
		}
		/// <summary>
		/// RTL enables right-to-left mode
		/// </summary>
		public void RTL()
		{
			this.isRTL = true;
		}
		/// <summary>
		/// LTR disables right-to-left mode
		/// </summary>
		public void LTR()
		{
			this.isRTL = false;
		}
		/// <summary>
		/// open begins a document
		/// </summary>
		private void open()
		{
			this.state = 1;
		}
		/// <summary>
		/// Close terminates the PDF document. It is not necessary to call this method
		/// explicitly because Output(), OutputAndClose() and OutputFileAndClose() do it
		/// automatically. If the document contains no page, AddPage() is called to
		/// prevent the generation of an invalid document.
		/// </summary>
		public void Close()
		{
			if (this.err == null) 
			{
				if (this.clipNest > 0) 
				{
					this.err = new PdfError("clip procedure must be explicitly ended");
				}
				else if (this.transformNest > 0) 
				{
					this.err = new PdfError("transformation procedure must be explicitly ended");
	  			}
			}
			if (this.err != null) 
			{
				return;
			}
			if (this.state == 3) 
			{
				return;
			}
			if (this.page == 0) 
			{
				AddPage();
				if (this.err != null) 
				{
					return;
				}
			}
			// Page footer
			this.inFooter = true;
	   		if (this.footerFnc != null) 
			{
				this.footerFnc();
			}
			else if (this.footerFncLpi != null) 
			{
				this.footerFncLpi(true);
	  		}
			this.inFooter = false;

			// Close page
			this.endpage();
			// Close document
			this.enddoc();
			return;
		}
		/// <summary>
		/// PageSize returns the width and height of the specified page in the units
		/// established in New(). These return values are followed by the unit of
		/// measure itself. If pageNum is zero or otherwise out of bounds, it returns
		/// the default page size, that is, the size of the page that would be added by
		/// AddPage().
		/// </summary>
		/// <returns>double wd, double ht, string unitStr</returns>
		public (double wd, double ht, string unitStr) PageSize(int pageNum) 
		{
			if (this.pageSizes.TryGetValue(pageNum, out SizeType sz))
			{
				sz.Wd /= this.k;
				sz.Ht /= this.k;
			}
			else
			{
				sz = this.defPageSize;
			}
			return (sz.Wd, sz.Ht, this.unitStr);
		}

		// AddPageFormat adds a new page with non-default orientation or size. See
		// AddPage() for more details.
		//
		// See New() for a description of orientationStr.
		//
		// size specifies the size of the new page in the units established in New().
		//
		// The PageSize() example demonstrates this method.
		public void AddPageFormat(string orientationStr, SizeType size)
		{
			if (this.err != null) 
			{
				return;
			}
			if (this.page != this.pages.Length - 1) 
			{
				this.page = this.pages.Length - 1;
			}
			if (this.state == 0) 
			{
				this.open();
			}
			var familyStr = this.fontFamily;
			var style = this.fontStyle;
	   		if (this.underline) 
			{
				style += "U";
			}
			if (this.strikeout) 
			{
				style += "S";
			}
			var fontsize = this.fontSizePt;
			var lw = this.lineWidth;
			var dc = this.color.draw;
			var fc = this.color.fill;
			var tc = this.color.text;
			var cf = this.colorFlag;
	   
			if (this.page > 0) 
			{
				this.inFooter = true;
				// Page footer avoid double call on footer.
				if (this.footerFnc != null) 
				{
					this.footerFnc();

				}
				else
				{
					this.footerFncLpi?.Invoke(false);  // not last page.
				}
				this.inFooter = false;
				// Close page
				this.endpage();
			}
			// Start new page
			this.beginpage(orientationStr, size);
			// 	Set line cap style to current value
			this.outf("{0:D} J", this.capStyle);
			// 	Set line join style to current value
			this.outf("{0:D} j", this.joinStyle);
			// Set line width
			this.lineWidth = lw;
			this.outf("{0:F2} w", lw * this.k);
			// Set dash pattern
			if (this.dashArray.Length > 0) 
			{
				this.outputDashPattern();
			}
			// 	Set font
			if (familyStr != "") 
			{
				this.SetFont(familyStr, style, fontsize);
				if (this.err != null) 
				{
					return;
				}
			}
			// 	Set colors
			this.color.draw = dc;
	   		if (dc.str != "0 G") 
			{
				this._out(dc.str);
			}
			this.color.fill = fc;
	   		if (fc.str != "0 g") 
			{
				this._out(fc.str);
			}
			this.color.text = tc;
			this.colorFlag = cf;
			// 	Page header
			if (this.headerFnc != null)
			{
				this.inHeader = true;
			}
			this.headerFnc();
			this.inHeader = false;
			if (this.headerHomeMode) 
			{
				this.SetHomeXY();
			}
			// 	Restore line width
			if (this.lineWidth != lw) 
			{
				this.lineWidth = lw;
				this.outf("{0:F2} w", lw * this.k);
			}
			// Restore font
			if (familyStr != "") 
			{
				this.SetFont(familyStr, style, fontsize);
				if (this.err != null) 
				{
					return;
				}
			}
			// Restore colors
			if (this.color.draw.str != dc.str) 
			{
				this.color.draw = dc;
				this._out(dc.str);
			}
			if (this.color.fill.str != fc.str) 
			{
				this.color.fill = fc;
				this._out(fc.str);
			}
			this.color.text = tc;
			this.colorFlag = cf;
			return;
		}
		/// <summary>
		/// GetXY returns the abscissa and ordinate of the current position.
		///
		/// Note: the value returned for the abscissa will be affected by the current
		/// cell margin. To account for this, you may need to either add the value
		/// returned by GetCellMargin() to it or call SetCellMargin(0) to remove the
		/// cell margin.
		/// </summary>
		/// <returns>(x, y)</returns>
		public (double x, double y) GetXY()
		{
			return (this.x, this.y);
		}
		/// <summary>
		/// GetX returns the abscissa of the current position.
		///
		/// Note: the value returned will be affected by the current cell margin. To
		/// account for this, you may need to either add the value returned by
		/// GetCellMargin() to it or call SetCellMargin(0) to remove the cell margin.
		/// </summary>
		public double GetX()
		{
			return this.x;
		}
		/// <summary>
		/// SetX defines the abscissa of the current position. If the passed value is
		/// negative, it is relative to the right of the page.
		/// </summary>
		public void SetX(double x)
		{
			if (x >= 0) 
			{
				this.x = x;
			}
			else
			{
				this.x = this.w + x;
			}
		}
		/// <summary>
		/// GetY returns the ordinate of the current position.
		/// </summary>
		public double GetY()
		{
			return this.y;
		}
		/// <summary>
		/// SetY moves the current abscissa back to the left margin and sets the
		/// ordinate. If the passed value is negative, it is relative to the bottom of
		/// the page.
		/// </summary>
		public void SetY(double y)
		{
			// dbg("SetY x %.2f, lMargin %.2f", f.x, f.lMargin)
			this.x = this.lMargin;
			if (y >= 0) 
			{
				this.y = y;
			}
			else
			{
				this.y = this.h + y;
			}
		}
		/// <summary>
		/// SetHomeXY is a convenience method that sets the current position to the left
		/// and top margins.
		/// </summary>
		public void SetHomeXY()
		{
			this.SetY(this.tMargin);
			this.SetX(this.lMargin);
		}
		/// <summary>
		/// SetXY defines the abscissa and ordinate of the current position. If the
		/// passed values are negative, they are relative respectively to the right and
		/// bottom of the page.
		/// </summary>
		public void SetXY(double x, double y)
		{
			this.SetY(y);
			this.SetX(x);
		}
		/// <summary>
		/// Condition font family string to PDF name compliance. See section 5.3 (Names)
		/// in https://resources.infosecinstitute.com/pdf-file-format-basic-structure/
		/// </summary>
		private string fontFamilyEscape(string familyStr) 
		{
			var escStr = familyStr.Replace(" ", "#20");
			// Additional replacements can take place here
			return escStr;
		}


		/// <summary>
		/// SetFont sets the font used to print character strings. It is mandatory to
		/// call this method at least once before printing text or the resulting
		/// document will not be valid.
		///
		/// The font can be either a standard one or a font added via the AddFont()
		/// method or AddFontFromReader() method. Standard fonts use the Windows
		/// encoding cp1252 (Western Europe).
		///
		/// The method can be called before the first page is created and the font is
		/// kept from page to page. If you just wish to change the current font size, it
		/// is simpler to call SetFontSize().
		///
		/// Note: the font definition file must be accessible. An error is set if the
		/// file cannot be read.
		/// </summary>
		/// <param name="familyStr">
		/// familyStr specifies the font family. It can be either a name defined by
		/// AddFont(), AddFontFromReader() or one of the standard families (case
		/// insensitive): "Courier" for fixed-width, "Helvetica" or "Arial" for sans
		/// serif, "Times" for serif, "Symbol" or "ZapfDingbats" for symbolic.
		/// </param>
		/// <param name="styleStr">
		/// styleStr can be "B" (bold), "I" (italic), "U" (underscore), "S" (strike-out)
		/// or any combination. The default value (specified with an empty string) is
		/// regular. Bold and italic styles do not apply to Symbol and ZapfDingbats.
		/// </param>
		/// <param name="size">
		/// size is the font size measured in points. The default value is the current
		/// size. If no size has been specified since the beginning of the document, the
		/// value taken is 12.
		/// </param>
		public void SetFont(string familyStr, string styleStr, double size)
		{
			// dbg("SetFont x %.2f, lMargin %.2f", f.x, f.lMargin)
			if (this.err != null) 
			{
				return;
			}
			// dbg("SetFont")
			familyStr = fontFamilyEscape(familyStr);
	   		
			if (familyStr == "") 
			{
				familyStr = this.fontFamily;
			}
			else
			{
				familyStr = familyStr.ToLower();
	  		}
			styleStr = styleStr.ToUpper();
			this.underline = styleStr.Contains("U");
	   		if (this.underline) 
			{
				styleStr = styleStr.Replace("U", "");
			}
			this.strikeout = styleStr.Contains("S");
			if (this.strikeout) 
			{
				styleStr = styleStr.Replace("S", "");
			}
			if (styleStr == "IB") 
			{
				styleStr = "BI";
			}
			if (size == 0.0) 
			{
				size = this.fontSizePt;
			}

			// Test if font is already loaded
			var fontKey = familyStr + styleStr;
			if (!this.fonts.ContainsKey(fontKey))
			{ 
				// Test if one of the core fonts
				if (familyStr == "arial") 
				{
					familyStr = "helvetica";
				}
				if (this.coreFonts.ContainsKey(familyStr))
				{
					if (familyStr == "symbol") 
					{
						familyStr = "zapfdingbats";
					}
					if (familyStr == "zapfdingbats") 
					{
						styleStr = "";
					}
					fontKey = familyStr + styleStr;
					if (!this.fonts.ContainsKey(fontKey))
					{
						var rdr = this.coreFontReader(familyStr, styleStr);
						if (this.err == null) 
						{
							this.AddFontFromReader(familyStr, styleStr, rdr);
						}
						if (this.err != null) {
							return;
						}
					}
				}
				else
				{
					this.err = new PdfError($"undefined font: {familyStr} {styleStr}");
					return;
	  			}
			}
			// Select it
			this.fontFamily = familyStr;
			this.fontStyle = styleStr;
			this.fontSizePt = size;
			this.fontSize = size / this.k;
			this.currentFont = this.fonts[fontKey];
	   		if (this.currentFont.Tp == "UTF8") 
			{
				this.isCurrentUTF8 = true;
			}
			else
			{
				this.isCurrentUTF8 = false;
	  		}
			if (this.page > 0) 
			{
				this.outf("BT /F{0} {1:F2} Tf ET", this.currentFont.i, this.fontSizePt);
			}
			return;
		}

		/// <summary>
		/// SetFontStyle sets the style of the current font. See also SetFont()
		/// </summary>
		public void SetFontStyle(string styleStr)
		{
			this.SetFont(this.fontFamily, styleStr, this.fontSizePt);
		}

		/// <summary>
		/// SetFontSize defines the size of the current font.
		/// </summary>
		/// <param name="size">
		/// Size is specified in points (1/ 72 inch). See also SetFontUnitSize().
		/// </param>
		public void SetFontSize(double size)
		{
			this.fontSizePt = size;
			this.fontSize = size / this.k;
	   		if (this.page > 0) 
			{
				this.outf("BT /F%s %.2f Tf ET", this.currentFont.i, this.fontSizePt);
			}
		}

		// SetFontUnitSize defines the size of the current font. Size is specified in
		// the unit of measure specified in New(). See also SetFontSize().
		public void SetFontUnitSize(double size)
		{
			this.fontSizePt = size * this.k;
			this.fontSize = size;
	   		if (this.page > 0) 
			{
				this.outf("BT /F%s %.2f Tf ET", this.currentFont.i, this.fontSizePt);
			}
		}

		/// <summary>
		/// GetFontSize returns the size of the current font in points followed by the
		/// size in the unit of measure specified in New(). The second value can be used
		/// as a line height value in drawing operations.
		/// </summary>
		public (double ptSize, double unitSize) GetFontSize()
		{
			return (this.fontSizePt, this.fontSize);
		}

		/// <summary>
		/// AddLink creates a new internal link and returns its identifier. An internal
		/// link is a clickable area which directs to another place within the document.
		/// The destination is defined with SetLink().
		/// </summary>
		/// <returns>An identifier that can be passed to Cell(), Write(), Image() or Link()</returns>
		public int AddLink()
		{
			this.links.Add(new IntLinkType());
			return this.links.Count - 1;
		}

		/// <summary>
		/// SetLink defines the page and position a link points to. See AddLink().
		/// </summary>
		public void SetLink(int link, double y, int page)
		{
			if (y == -1) 
			{
				y = this.y;
			}
			if (page == -1) 
			{
				page = this.page;
			}
			this.links[link] = new IntLinkType { page = page, y = y };
		}

		/// <summary>
		/// newLink adds a new clickable link on current page
		/// </summary>
		private void newLink(double x, double y, double w, double h, int link, string linkStr)
		{
			// linkList, ok := f.pageLinks[f.page]
			// if !ok {
			// linkList = make([]linkType, 0, 8)
			// f.pageLinks[f.page] = linkList
			// }
			this.pageLinks[this.page].Add(new LinkType
			{
				x = x * this.k,
				y = this.hPt - y * this.k,
				wd = w * this.k,
				ht = h * this.k,
				link = link,
				linkStr = linkStr
			});
		}

		/// <summary>
		/// Link puts a link on a rectangular area of the page. Text or image links are
		/// generally put via Cell(), Write() or Image(), but this method can be useful
		/// for instance to define a clickable area inside an image. link is the value
		/// returned by AddLink().
		/// </summary>
		public void Link(double x, double y, double w, double h, int link)
		{
			this.newLink(x, y, w, h, link, "");
		}

		/// <summary>
		/// LinkString puts a link on a rectangular area of the page. Text or image
		/// links are generally put via Cell(), Write() or Image(), but this method can
		/// be useful for instance to define a clickable area inside an image. linkStr
		/// is the target URL.
		/// </summary>
		public void LinkString(double x, double y, double w, double h, string linkStr)
		{
			this.newLink(x, y, w, h, 0, linkStr);
		}

		/// <summary>
		/// Bookmark sets a bookmark that will be displayed in a sidebar outline. txtStr
		/// is the title of the bookmark. level specifies the level of the bookmark in
		/// the outline; 0 is the top level, 1 is just below, and so on. y specifies the
		/// vertical position of the bookmark destination in the current page; -1
		/// indicates the current position.
		/// </summary>
		public void Bookmark(string txtStr, int level, double y)
		{
			if (y == -1) 
			{
				y = this.y;
			}
			if (this.isCurrentUTF8) 
			{
				txtStr = Util.Utf8ToUtf16(txtStr);
			}
			this.outlines.Add(new OutlineType
			{
				text = txtStr,
				level = level,
				y = y,
				p = this.PageNo(),
				prev = -1,
				last = -1,
				next = -1,
				first = -1
			});
		}

		/// <summary>
		/// Text prints a character string. The origin (x, y) is on the left of the
		/// first character at the baseline. This method permits a string to be placed
		/// precisely on the page, but it is usually easier to use Cell(), MultiCell()
		/// or Write() which are the standard methods to print text.
		/// </summary>
		public void Text(double x, double y, string txtStr)
		{
			string txt2;
	   		if (this.isCurrentUTF8) 
			{
				if (this.isRTL) 
				{
					txtStr = reverseText(txtStr);
					x -= this.GetStringWidth(txtStr);
				}
				txt2 = this.escape(Util.Utf8ToUtf16(txtStr, false));
				foreach (var uni in txtStr)
				{
					this.currentFont.usedRunes[uni] = uni;
				}
			}
			else
			{
				txt2 = this.escape(txtStr);
	  		}
			var s = string.Format("BT {0:F2} {1:F2} Td ({2}) Tj ET", x * this.k, (this.h - y) * this.k, txt2);
	   		if (this.underline && txtStr != "") 
			{
				s += " " + this.dounderline(x, y, txtStr);
			}
			if (this.strikeout && txtStr != "") 
			{
				s += " " + this.dostrikeout(x, y, txtStr);
			}
			if (this.colorFlag) 
			{
				s = string.Format("q {0} {1} Q", this.color.text.str, s);
			}
			this._out(s);
		}

		/// <summary>
		/// SetUnderlineThickness accepts a multiplier for adjusting the text underline
		/// thickness, defaulting to 1. See SetUnderlineThickness example.
		/// </summary>
		public void SetUnderlineThickness(double thickness)
		{
			this.userUnderlineThickness = thickness;
		}

		private int blankCount(string str)
		{
			int count = 0;
			foreach (var c in str)
			{
				if (Char.IsWhiteSpace(c)) count++;
			}
			return count;
		}
		// Underline text
		private string dounderline(double x, double y, string txt)
		{
			var up = (double)(this.currentFont.Up);
			var ut = (double)(this.currentFont.Ut) * this.userUnderlineThickness;
			var w = this.GetStringWidth(txt) + this.ws * (double)(blankCount(txt));
			return string.Format("{0:F2} {1:F2} {2:F2} {3:F2} re f", x * this.k,
				(this.h - (y - up / 1000 * this.fontSize)) * this.k, w * this.k, -ut / 1000 * this.fontSizePt);
		}

		private string dostrikeout(double x, double y, string txt)
		{
			var up = (double)(this.currentFont.Up);
			var ut = (double)(this.currentFont.Ut);
			var w = this.GetStringWidth(txt) + this.ws * (double)(blankCount(txt));
			return string.Format("{0:F2} {1:F2} {2:F2} {3:F2} re f", x * this.k,
				(this.h - (y + 4 * up / 1000 * this.fontSize)) * this.k, w * this.k, -ut / 1000 * this.fontSizePt);
		}

		/// <summary>
		/// Escape special characters in strings
		/// </summary>
		private string escape(string s)
		{
			return s.Replace("\\", "\\\\")
				.Replace("(", "\\(")
				.Replace(")", "\\)")
				.Replace("\r", "\\r");
		}

		/// <summary>
		/// GetStringWidth returns the length of a string in user units. A font must be
		/// currently selected.
		/// </summary>
		double GetStringWidth(string s) 
		{
			if (this.err != null) 
			{
				return 0;
			}
			var w = this.GetStringSymbolWidth(s);
			return (double)(w) * this.fontSize / 1000;
		}


		/// <summary>
		/// GetStringSymbolWidth returns the length of a string in glyf units. A font must be
		/// currently selected.
		/// </summary>
		public int GetStringSymbolWidth(string s)
		{
			if (this.err != null) 
			{
				return 0;
			}
			int w = 0;
			if (this.isCurrentUTF8) 
			{
				foreach(var c in s)
				{
					if (this.currentFont.Cw.Length >= c && this.currentFont.Cw[c] > 0) 
					{
						if (this.currentFont.Cw[c] != 65535) 
						{
							w += this.currentFont.Cw[c];
						}
					} 
					else if (this.currentFont.Desc.MissingWidth != 0) 
					{
						w += this.currentFont.Desc.MissingWidth;
					} 
					else 
					{
						w += 500;
					}
				}
			} 
			else 
			{
				foreach (var c in s) 
				{
					if (c == 0) 
					{
						break;
					}
					w += this.currentFont.Cw[c];
				}
			}
			return w;
		}


		/// <summary>
		/// Revert string to use in RTL languages
		/// </summary>
		private string reverseText(string text)
		{
			char[] arr = text.ToCharArray();
			Array.Reverse(arr);
			return new string(arr);
		}


		/// <summary>
		/// PageNo returns the current page number.
		///
		/// See the example for AddPage() for a demonstration of this method.
		/// </summary>
		public int PageNo()
		{
			return this.page;
		}

		/// <summary>
		/// getFontKey is used by AddFontFromReader and GetFontDesc
		/// </summary>
		private string getFontKey(string familyStr, string styleStr)
		{
			familyStr = familyStr.ToLower();
			styleStr = styleStr.ToUpper();
			if (styleStr == "IB") 
			{
				styleStr = "BI";
			}
			return familyStr + styleStr;
		}
		/// <summary>
		/// AddFontFromReader imports a TrueType, OpenType or Type1 font and makes it
		/// available using a reader that satisifies the io.Reader interface. See
		/// AddFont for details about familyStr and styleStr.
		/// </summary>
		public void AddFontFromReader(string familyStr, string styleStr, TextReader r)
		{
			if (this.err != null) 
			{
				return;
			}
			// dbg("Adding family [%s], style [%s]", familyStr, styleStr)
			familyStr = fontFamilyEscape(familyStr);
			var fontkey = getFontKey(familyStr, styleStr);
			if (this.fonts.ContainsKey(fontkey)) 
			{
				return;
			}
			FontDefType info = this.loadfont(r);
			if (this.err != null) 
			{
				return;
			}
			if (info.Diff.Length > 0) 
			{
				// Search existing encodings
				var n = -1;
				for (var j = 0; j < this.diffs.Count; j++)
				{
					var str = this.diffs[j];
					if (str == info.Diff) 
					{
						n = j + 1;
						break;
					}
				}
				if (n < 0) 
				{
					this.diffs.Add(info.Diff);
					n = this.diffs.Count;
				}
				info.DiffN = n;
			}
			// dbg("font [%s], type [%s]", info.File, info.Tp)
			if (info.File.Length > 0) 
			{
				// Embedded font
				if (info.Tp == "TrueType") 
				{
					this.fontFiles[info.File] = new FontFileType { length1 = info.OriginalSize };
				}
				else
				{
					this.fontFiles[info.File] = new FontFileType { length1 = info.Size1, length2 = info.Size2 };
				}
			}
			this.fonts[fontkey] = info;
			return;
		}


		// Load a font definition file from the given Reader
		private FontDefType loadfont(TextReader r)
		{
			FontDefType fontDef;
			if (this.err != null)
			{
				return null;
			}
			// dbg("Loading font [%s]", fontStr)
			try
			{
				var fontDefStr = r.ReadToEnd();
				fontDef = JsonConvert.DeserializeObject<FontDefType>(fontDefStr);
			}
			catch (Exception e)
			{
				this.err = new PdfError(e.Message);
				return null;
			}
			fontDef.i = fontDef.GenerateFontID();
			return fontDef;
		}




		public StringReader coreFontReader(string familyStr, string styleStr)
		{
			var key = familyStr + styleStr;
			if (Embed.embeddedFontList.TryGetValue(key, out string embeddedFont)) 
			{
				return new StringReader(embeddedFont);
			}
			else
			{
				this.SetErrorf("could not locate '{0}' among embedded core font definition files", key);
			}
			return null;
		}

		private void outputDashPattern()
		{
			var buf = new Utils.Buffer();
			buf.WriteByte((byte)'[');
			for (var i = 0; i < this.dashArray.Length; i++)
			{
				var value = this.dashArray[i];
				if (i > 0) 
				{
					buf.WriteByte((byte)' ');
				}
				buf.WriteString(string.Format(CultureInfo.InvariantCulture, "{0:F2}", value));
			}
			buf.WriteString("] ");
			buf.WriteString(string.Format(CultureInfo.InvariantCulture, "{0:F2}", this.dashPhase));
			buf.WriteString(" d");
			this.outbuf(buf);
		}

		private void outbuf(Utils.Buffer buf)
		{
			Utils.Buffer targetBuf;
			if (this.state == 2)
			{
				targetBuf = this.pages[this.page];
			}
			else
			{
				targetBuf = this.buffer;
			}
			targetBuf.ReadFrom(buf);
			targetBuf.WriteString("\n");
		}

		private void outf(string fmtStr, params object[] args)
		{
			_out(String.Format(CultureInfo.InvariantCulture, fmtStr, args));
		}

		private void _out(string s)
		{
			Utils.Buffer buffer;
			if (this.state == 2)
			{
				buffer = this.pages[this.page];
			}
			else
			{
				buffer = this.buffer;
			}
			buffer.WriteString(s);
			buffer.WriteString("\n");
		}

		private void beginpage(string orientationStr, SizeType size)
		{
			throw new NotImplementedException();
		}

		private void endpage()
		{
			// TODO
		}
		private void enddoc()
		{
			// TODO
		}
		public void AddPage()
		{
			// TODO
		}

	}
}
