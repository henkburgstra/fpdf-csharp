using FpdfCsharp.Attachments;
using FpdfCsharp.Errors;
using FpdfCsharp.Layers;
using System;
using System.Collections.Generic;
using System.IO;

namespace FpdfCsharp
{
    public class Fpdf
    {
		private static readonly string cnFpdfVersion = "1.7";
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
		private BufferedStream[] pages;       //pages[]*bytes.Buffer            // slice[page] of page content; 1-based
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
		private string[] diffs;                                                 // array of encoding differences
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
		private LinkType[][] pageLinks;                                         // pageLinks[page][link], both 1-based
		private IntLinkType[] links;                                            // array of internal links
		private Attachment[] attachments;                                       // slice of content to embed globally
		private AnnotationAttach[] pageAttachments;                             // 1-based array of annotation for file attachments (per page)
		private OutlineType[] outlines;                                         // array of outlines
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
		struct color
		{
			// Composite values of colors
			ColorType draw;
			ColorType fill;
			ColorType text;
		}
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

	}
}
