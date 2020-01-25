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
		Error err = null;                                                       // Set if error occurs during life cycle of instance
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
