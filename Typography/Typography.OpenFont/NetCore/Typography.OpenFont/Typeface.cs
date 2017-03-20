﻿//Apache2, 2017, WinterDev
//Apache2, 2014-2016, Samuel Carlsson, WinterDev
using System.Collections.Generic;
using Typography.OpenFont.Tables;

namespace Typography.OpenFont
{
    public class Typeface
    {
        readonly Bounds _bounds;
        readonly ushort _unitsPerEm;
        readonly Glyph[] _glyphs;
        readonly CharacterMap[] _cmaps;
        //TODO: implement vertical metrics
        readonly HorizontalMetrics _horizontalMetrics;
        readonly NameEntry _nameEntry;

        Kern _kern;

        internal Typeface(
            NameEntry nameEntry,
            Bounds bounds,
            ushort unitsPerEm,
            Glyph[] glyphs,
            CharacterMap[] cmaps,
            HorizontalMetrics horizontalMetrics,
            OS2Table os2Table)
        {
            _nameEntry = nameEntry;
            _bounds = bounds;
            _unitsPerEm = unitsPerEm;
            _glyphs = glyphs;
            _cmaps = cmaps;
            _horizontalMetrics = horizontalMetrics;
            OS2Table = os2Table;
        }

        /// <summary>
        /// control values in Font unit
        /// </summary>
        internal int[] ControlValues { get; set; }
        internal byte[] PrepProgramBuffer { get; set; }
        internal byte[] FpgmProgramBuffer { get; set; }
        internal MaxProfile MaxProfile { get; set; }

        public bool HasPrepProgramBuffer { get { return PrepProgramBuffer != null; } }
        internal Kern KernTable
        {
            get { return _kern; }
            set { this._kern = value; }
        }
        internal Gasp GaspTable
        {
            get;
            set;
        }
        internal OS2Table OS2Table
        {
            get;
            set;
        }
        /// <summary>
        /// actual font filename
        /// </summary>
        public string Filename { get; set; }
        /// <summary>
        /// OS2 sTypoAscender, in font designed unit
        /// </summary>
        public short Ascender
        {
            get
            {

                return OS2Table.sTypoAscender;
            }
        }
        /// <summary>
        /// OS2 sTypoDescender, in font designed unit
        /// </summary>
        public short Descender
        {
            get
            {
                return OS2Table.sTypoDescender;
            }
        }
        /// <summary>
        /// OS2 Linegap
        /// </summary>
        public short LineGap
        {
            get
            {
                return OS2Table.sTypoLineGap;
            }
        }
        /// <summary>
        /// overall calculated line spacing 
        /// </summary>
        public int LineSpacing
        {
            get
            {

                //from https://www.microsoft.com/typography/OTSpec/recom.htm#tad
                //sTypoAscender, sTypoDescender and sTypoLineGap
                //sTypoAscender is used to determine the optimum offset from the top of a text frame to the first baseline.
                //sTypoDescender is used to determine the optimum offset from the last baseline to the bottom of the text frame. 
                //The value of (sTypoAscender - sTypoDescender) is recommended to equal one em.
                //
                //While the OpenType specification allows for CJK (Chinese, Japanese, and Korean) fonts' sTypoDescender and sTypoAscender 
                //fields to specify metrics different from the HorizAxis.ideo and HorizAxis.idtp baselines in the 'BASE' table,
                //CJK font developers should be aware that existing applications may not read the 'BASE' table at all but simply use 
                //the sTypoDescender and sTypoAscender fields to describe the bottom and top edges of the ideographic em-box. 
                //If developers want their fonts to work correctly with such applications, 
                //they should ensure that any ideographic em-box values in the 'BASE' table describe the same bottom and top edges as the sTypoDescender and
                //sTypoAscender fields. 
                //See the sections “OpenType CJK Font Guidelines“ and ”Ideographic Em-Box“ for more details.

                //For Western fonts, the Ascender and Descender fields in Type 1 fonts' AFM files are a good source of sTypoAscender
                //and sTypoDescender, respectively. 
                //The Minion Pro font family (designed on a 1000-unit em), 
                //for example, sets sTypoAscender = 727 and sTypoDescender = -273.

                //sTypoAscender, sTypoDescender and sTypoLineGap specify the recommended line spacing for single-spaced horizontal text.
                //The baseline-to-baseline value is expressed by:
                //OS/2.sTypoAscender - OS/2.sTypoDescender + OS/2.sTypoLineGap

                //sTypoLineGap will usually be set by the font developer such that the value of the above expression is approximately 120% of the em.
                //The application can use this value as the default horizontal line spacing. 
                //The Minion Pro font family (designed on a 1000-unit em), for example, sets sTypoLineGap = 200.


                return Ascender - Descender + LineGap;
            }
        }



        public string Name
        {
            get { return _nameEntry.FontName; }
        }
        public string FontSubFamily
        {
            get { return _nameEntry.FontSubFamily; }
        }
        public int LookupIndex(char character)
        {
            // TODO: What if there are none or several tables?
            return _cmaps[0].CharacterToGlyphIndex(character);
        }
        //public void CollectGlyphIndexListFromSampleChar(char starAt, char endAt, GlyphIndexCollector collector)
        //{
        //    // TODO: What if there are none or several tables?
        //    _cmaps[0].CollectGlyphIndexListFromSampleChar(sampleChar, collector);
        //}
        public Glyph Lookup(char character)
        {
            return _glyphs[LookupIndex(character)];
        }
        public Glyph GetGlyphByIndex(int glyphIndex)
        {
            return _glyphs[glyphIndex];
        }

        public ushort GetAdvanceWidth(char character)
        {
            return _horizontalMetrics.GetAdvanceWidth(LookupIndex(character));
        }
        public ushort GetHAdvanceWidthFromGlyphIndex(int glyphIndex)
        {

            return _horizontalMetrics.GetAdvanceWidth(glyphIndex);
        }
        public short GetHFrontSideBearingFromGlyphIndex(int glyphIndex)
        {
            return _horizontalMetrics.GetLeftSideBearing(glyphIndex);
        }
        public short GetKernDistance(ushort leftGlyphIndex, ushort rightGlyphIndex)
        {
            return _kern.GetKerningDistance(leftGlyphIndex, rightGlyphIndex);
        }
        public Bounds Bounds { get { return _bounds; } }
        public ushort UnitsPerEm { get { return _unitsPerEm; } }
        public Glyph[] Glyphs { get { return _glyphs; } }


        const int pointsPerInch = 72;
        public float CalculateFromPointToPixelScale(float sizeInPointUnit, int resolution = 96)
        {
            return ((sizeInPointUnit * resolution) / (pointsPerInch * this.UnitsPerEm));
        }
        public static float ConvPointsToPixels(float pointsValue, int resolution = 96)
        {
            //http://stackoverflow.com/questions/139655/convert-pixels-to-points
            //points = pixels * 72 / 96
            //pixels = points * 96 /72
            //pixels = points * resolution / pointPerInch
            return pointsValue * resolution / pointsPerInch;
        }
        internal GDEF GDEFTable
        {
            get;
            set;
        }
        public GSUB GSUBTable
        {
            get;
            set;
        }
        public GPOS GPOSTable
        {
            get;
            set;
        }
        internal BASE BaseTable
        {
            get;
            set;
        }

        //-------------------------------------------------------

        public void Lookup(char[] buffer, List<int> output)
        {
            //do shaping here?
            //1. do look up and substitution 
            int j = buffer.Length;
            for (int i = 0; i < j; ++i)
            {
                output.Add(LookupIndex(buffer[i]));
            }
            //tmp disable here
            //check for glyph substitution
            //this.GSUBTable.CheckSubstitution(output[1]);
        }
        //-------------------------------------------------------
        //experiment
        internal void LoadOpenFontLayoutInfo(GDEF gdefTable, GSUB gsubTable, GPOS gposTable, BASE baseTable)
        {

            //***
            this.GDEFTable = gdefTable;
            this.GSUBTable = gsubTable;
            this.GPOSTable = gposTable;
            this.BaseTable = baseTable;
            //---------------------------
            //1. fill glyph definition            
            if (gdefTable != null)
            {
                gdefTable.FillGlyphData(this.Glyphs);
            }
        }
    }


    //------------------------------------------------------------------------------------------------------
    public class GlyphPos
    {
        public readonly ushort glyphIndex;
        public readonly ushort advWidth;
        public short xoffset;
        public short yoffset;
        public GlyphClassKind _classKind;
        public GlyphPos(ushort glyphIndex, GlyphClassKind classKind, ushort advWidth)
        {
            this.glyphIndex = glyphIndex;
            this.advWidth = advWidth;
            this._classKind = classKind;
        }

#if DEBUG
        public override string ToString()
        {
            return glyphIndex.ToString() + "(" + xoffset + "," + yoffset + ")";
        }
#endif
    }

    namespace Extensions
    {

        public static class TypefaceExtensions
        {
            public static bool DoseSupportUnicode(
                this Typeface typeface,
                UnicodeLangBits unicodeLangBits)
            {
                if (typeface.OS2Table == null)
                {
                    return false;
                }
                //-----------------------------
                long bits = (long)unicodeLangBits;
                int bitpos = (int)(bits >> 32);

                if (bitpos == 0)
                {
                    return true; //default
                }
                else if (bitpos < 32)
                {
                    //use range 1
                    return (typeface.OS2Table.ulUnicodeRange1 & (1 << bitpos)) != 0;
                }
                else if (bitpos < 64)
                {
                    return (typeface.OS2Table.ulUnicodeRange2 & (1 << (bitpos - 32))) != 0;
                }
                else if (bitpos < 96)
                {
                    return (typeface.OS2Table.ulUnicodeRange3 & (1 << (bitpos - 64))) != 0;
                }
                else if (bitpos < 128)
                {
                    return (typeface.OS2Table.ulUnicodeRange4 & (1 << (bitpos - 96))) != 0;
                }
                else
                {
                    throw new System.NotSupportedException();
                }
            }
        }
        public static class UnicodeLangBitsExtension
        {
            public static UnicodeRangeInfo ToUnicodeRangeInfo(this UnicodeLangBits unicodeLangBits)
            {
                long bits = (long)unicodeLangBits;
                int bitpos = (int)(bits >> 32);
                int lower32 = (int)(bits & 0xFFFFFFFF);
                return new UnicodeRangeInfo(bitpos,
                    lower32 >> 16,
                    lower32 & 0xFFFF);
            }
        }


    }
}
