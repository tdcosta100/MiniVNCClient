namespace MiniVNCClient.Data
{
    /// <summary>
    /// Key codes for the <see cref="ClientToServerMessageType.KeyEvent"/>, as defined in <c>&lt;X11/keysymdef.h&gt;</c>
    /// </summary>
    public enum KeyDefinitions : uint
    {
        /// <summary>
        /// " "
        /// </summary>
        Space                = 0x0020,

        /// <summary>
        /// !
        /// </summary>
        Exclamation          = 0x0021,

        /// <summary>
        /// "
        /// </summary>
        DoubleQuote          = 0x0022,

        /// <summary>
        /// #
        /// </summary>
        NumberSignHash       = 0x0023,

        /// <summary>
        /// $
        /// </summary>
        DollarSign           = 0x0024,

        /// <summary>
        /// %
        /// </summary>
        Percent              = 0x0025,

        /// <summary>
        /// &amp;
        /// </summary>
        Ampersand            = 0x0026,

        /// <summary>
        /// '
        /// </summary>
        Apostrophe           = 0x0027,

        /// <summary>
        /// (
        /// </summary>
        LeftParenthesis      = 0x0028,

        /// <summary>
        /// )
        /// </summary>
        RightParenthesis     = 0x0029,

        /// <summary>
        /// *
        /// </summary>
        Asterisk             = 0x002a,

        /// <summary>
        /// +
        /// </summary>
        Plus                 = 0x002b,

        /// <summary>
        /// ,
        /// </summary>
        Comma                = 0x002c,

        /// <summary>
        /// -
        /// </summary>
        Minus                = 0x002d,

        /// <summary>
        /// .
        /// </summary>
        Period               = 0x002e,

        /// <summary>
        /// /
        /// </summary>
        Slash                = 0x002f,

        /// <summary>
        /// 0
        /// </summary>
        Number0              = 0x0030,

        /// <summary>
        /// 1
        /// </summary>
        Number1              = 0x0031,

        /// <summary>
        /// 2
        /// </summary>
        Number2              = 0x0032,

        /// <summary>
        /// 3
        /// </summary>
        Number3              = 0x0033,

        /// <summary>
        /// 4
        /// </summary>
        Number4              = 0x0034,

        /// <summary>
        /// 5
        /// </summary>
        Number5              = 0x0035,

        /// <summary>
        /// 6
        /// </summary>
        Number6              = 0x0036,

        /// <summary>
        /// 7
        /// </summary>
        Number7              = 0x0037,

        /// <summary>
        /// 8
        /// </summary>
        Number8              = 0x0038,

        /// <summary>
        /// 9
        /// </summary>
        Number9              = 0x0039,

        /// <summary>
        /// :
        /// </summary>
        Colon                = 0x003a,

        /// <summary>
        /// ;
        /// </summary>
        Semicolon            = 0x003b,

        /// <summary>
        /// &lt;
        /// </summary>
        Less                 = 0x003c,

        /// <summary>
        /// =
        /// </summary>
        Equal                = 0x003d,

        /// <summary>
        /// &gt;
        /// </summary>
        Greater              = 0x003e,

        /// <summary>
        /// ?
        /// </summary>
        Question             = 0x003f,

        /// <summary>
        /// @
        /// </summary>
        At                   = 0x0040,

        /// <summary>
        /// A
        /// </summary>
        CapitalA             = 0x0041,

        /// <summary>
        /// B
        /// </summary>
        CapitalB             = 0x0042,

        /// <summary>
        /// C
        /// </summary>
        CapitalC             = 0x0043,

        /// <summary>
        /// D
        /// </summary>
        CapitalD             = 0x0044,

        /// <summary>
        /// E
        /// </summary>
        CapitalE             = 0x0045,

        /// <summary>
        /// F
        /// </summary>
        CapitalF             = 0x0046,

        /// <summary>
        /// G
        /// </summary>
        CapitalG             = 0x0047,

        /// <summary>
        /// H
        /// </summary>
        CapitalH             = 0x0048,

        /// <summary>
        /// I
        /// </summary>
        CapitalI             = 0x0049,

        /// <summary>
        /// J
        /// </summary>
        CapitalJ             = 0x004a,

        /// <summary>
        /// K
        /// </summary>
        CapitalK             = 0x004b,

        /// <summary>
        /// L
        /// </summary>
        CapitalL             = 0x004c,

        /// <summary>
        /// M
        /// </summary>
        CapitalM             = 0x004d,

        /// <summary>
        /// N
        /// </summary>
        CapitalN             = 0x004e,

        /// <summary>
        /// O
        /// </summary>
        CapitalO             = 0x004f,

        /// <summary>
        /// P
        /// </summary>
        CapitalP             = 0x0050,

        /// <summary>
        /// Q
        /// </summary>
        CapitalQ             = 0x0051,

        /// <summary>
        /// R
        /// </summary>
        CapitalR             = 0x0052,

        /// <summary>
        /// S
        /// </summary>
        CapitalS             = 0x0053,

        /// <summary>
        /// T
        /// </summary>
        CapitalT             = 0x0054,

        /// <summary>
        /// U
        /// </summary>
        CapitalU             = 0x0055,

        /// <summary>
        /// V
        /// </summary>
        CapitalV             = 0x0056,

        /// <summary>
        /// W
        /// </summary>
        CapitalW             = 0x0057,

        /// <summary>
        /// X
        /// </summary>
        CapitalX             = 0x0058,

        /// <summary>
        /// Y
        /// </summary>
        CapitalY             = 0x0059,

        /// <summary>
        /// Z
        /// </summary>
        CapitalZ             = 0x005a,

        /// <summary>
        /// [
        /// </summary>
        LeftBracket          = 0x005b,

        /// <summary>
        /// \
        /// </summary>
        Backslash            = 0x005c,

        /// <summary>
        /// ]
        /// </summary>
        RightBracket         = 0x005d,

        /// <summary>
        /// ^
        /// </summary>
        CircumflexAccent     = 0x005e,

        /// <summary>
        /// _
        /// </summary>
        Underscore           = 0x005f,

        /// <summary>
        /// `
        /// </summary>
        GraveAccent          = 0x0060,

        /// <summary>
        /// a
        /// </summary>
        SmallA               = 0x0061,

        /// <summary>
        /// b
        /// </summary>
        SmallB               = 0x0062,

        /// <summary>
        /// c
        /// </summary>
        SmallC               = 0x0063,

        /// <summary>
        /// d
        /// </summary>
        SmallD               = 0x0064,

        /// <summary>
        /// e
        /// </summary>
        SmallE               = 0x0065,

        /// <summary>
        /// f
        /// </summary>
        SmallF               = 0x0066,

        /// <summary>
        /// g
        /// </summary>
        SmallG               = 0x0067,

        /// <summary>
        /// h
        /// </summary>
        SmallH               = 0x0068,

        /// <summary>
        /// i
        /// </summary>
        SmallI               = 0x0069,

        /// <summary>
        /// j
        /// </summary>
        SmallJ               = 0x006a,

        /// <summary>
        /// k
        /// </summary>
        SmallK               = 0x006b,

        /// <summary>
        /// l
        /// </summary>
        SmallL               = 0x006c,

        /// <summary>
        /// m
        /// </summary>
        SmallM               = 0x006d,

        /// <summary>
        /// n
        /// </summary>
        SmallN               = 0x006e,

        /// <summary>
        /// o
        /// </summary>
        SmallO               = 0x006f,

        /// <summary>
        /// p
        /// </summary>
        SmallP               = 0x0070,

        /// <summary>
        /// q
        /// </summary>
        SmallQ               = 0x0071,

        /// <summary>
        /// r
        /// </summary>
        SmallR               = 0x0072,

        /// <summary>
        /// s
        /// </summary>
        SmallS               = 0x0073,

        /// <summary>
        /// t
        /// </summary>
        SmallT               = 0x0074,

        /// <summary>
        /// u
        /// </summary>
        SmallU               = 0x0075,

        /// <summary>
        /// v
        /// </summary>
        SmallV               = 0x0076,

        /// <summary>
        /// w
        /// </summary>
        SmallW               = 0x0077,

        /// <summary>
        /// x
        /// </summary>
        SmallX               = 0x0078,

        /// <summary>
        /// y
        /// </summary>
        SmallY               = 0x0079,

        /// <summary>
        /// z
        /// </summary>
        SmallZ               = 0x007a,

        /// <summary>
        /// {
        /// </summary>
        LeftBrace            = 0x007b,

        /// <summary>
        /// |
        /// </summary>
        VerticalBar          = 0x007c,

        /// <summary>
        /// }
        /// </summary>
        RightBrace           = 0x007d,

        /// <summary>
        /// ~
        /// </summary>
        Tilde                = 0x007e,


        /// <summary>
        /// " "
        /// </summary>
        NoBreakSpace         = 0x00a0,

        /// <summary>
        /// ¡
        /// </summary>
        InvertedExclamation  = 0x00a1,

        /// <summary>
        /// ¢
        /// </summary>
        Cent                 = 0x00a2,

        /// <summary>
        /// £
        /// </summary>
        Sterling             = 0x00a3,

        /// <summary>
        /// ¤
        /// </summary>
        Currency             = 0x00a4,

        /// <summary>
        /// ¥
        /// </summary>
        Yen                  = 0x00a5,

        /// <summary>
        /// ¦
        /// </summary>
        Brokenbar            = 0x00a6,

        /// <summary>
        /// §
        /// </summary>
        Section              = 0x00a7,

        /// <summary>
        /// ¨
        /// </summary>
        Diaeresis            = 0x00a8,

        /// <summary>
        /// ©
        /// </summary>
        Copyright            = 0x00a9,

        /// <summary>
        /// ª
        /// </summary>
        FeminineOrdinal      = 0x00aa,

        /// <summary>
        /// «
        /// </summary>
        LeftGuillemot        = 0x00ab,

        /// <summary>
        /// ¬
        /// </summary>
        Notsign              = 0x00ac,

        /// <summary>
        /// <see href="https://en.wikipedia.org/wiki/Soft_hyphen">Soft hyphen</see>
        /// </summary>
        SoftHyphen           = 0x00ad,

        /// <summary>
        /// ®
        /// </summary>
        Registered           = 0x00ae,

        /// <summary>
        /// ¯
        /// </summary>
        Macron               = 0x00af,

        /// <summary>
        /// °
        /// </summary>
        Degree               = 0x00b0,

        /// <summary>
        /// ±
        /// </summary>
        PlusMinus            = 0x00b1,

        /// <summary>
        /// ²
        /// </summary>
        TwoSuperior          = 0x00b2,

        /// <summary>
        /// ³
        /// </summary>
        ThreeSuperior        = 0x00b3,

        /// <summary>
        /// ´
        /// </summary>
        Acute                = 0x00b4,

        /// <summary>
        /// µ
        /// </summary>
        Mu                   = 0x00b5,

        /// <summary>
        /// ¶
        /// </summary>
        Paragraph            = 0x00b6,

        /// <summary>
        /// ·
        /// </summary>
        PeriodCentered       = 0x00b7,

        /// <summary>
        /// ¸
        /// </summary>
        Cedilla              = 0x00b8,

        /// <summary>
        /// ¹
        /// </summary>
        OneSuperior          = 0x00b9,

        /// <summary>
        /// º
        /// </summary>
        MasculineOrdinal     = 0x00ba,

        /// <summary>
        /// »
        /// </summary>
        RightGuillemot       = 0x00bb,

        /// <summary>
        /// ¼
        /// </summary>
        OneQuarter           = 0x00bc,

        /// <summary>
        /// ½
        /// </summary>
        OneHalf              = 0x00bd,

        /// <summary>
        /// ¾
        /// </summary>
        ThreeQuarters        = 0x00be,

        /// <summary>
        /// ¿
        /// </summary>
        InvertedQuestion     = 0x00bf,

        /// <summary>
        /// À
        /// </summary>
        CapitalAGrave        = 0x00c0,

        /// <summary>
        /// Á
        /// </summary>
        CapitalAAcute        = 0x00c1,

        /// <summary>
        /// Â
        /// </summary>
        CapitalACircumflex   = 0x00c2,

        /// <summary>
        /// Ã
        /// </summary>
        CapitalATilde        = 0x00c3,

        /// <summary>
        /// Ä
        /// </summary>
        CapitalADiaeresis    = 0x00c4,

        /// <summary>
        /// Å
        /// </summary>
        CapitalARing         = 0x00c5,

        /// <summary>
        /// Æ
        /// </summary>
        CapitalAE            = 0x00c6,

        /// <summary>
        /// Ç
        /// </summary>
        CapitalCCedilla      = 0x00c7,

        /// <summary>
        /// È
        /// </summary>
        CapitalEGrave        = 0x00c8,

        /// <summary>
        /// É
        /// </summary>
        CapitalEAcute        = 0x00c9,

        /// <summary>
        /// Ê
        /// </summary>
        CapitalECircumflex   = 0x00ca,

        /// <summary>
        /// Ë
        /// </summary>
        CapitalEDiaeresis    = 0x00cb,

        /// <summary>
        /// Ì
        /// </summary>
        CapitalIGrave        = 0x00cc,

        /// <summary>
        /// Í
        /// </summary>
        CapitalIAcute        = 0x00cd,

        /// <summary>
        /// Î
        /// </summary>
        CapitalICircumflex   = 0x00ce,

        /// <summary>
        /// Ï
        /// </summary>
        CapitalIDiaeresis    = 0x00cf,

        /// <summary>
        /// Ð
        /// </summary>
        CapitalEth           = 0x00d0,

        /// <summary>
        /// Ñ
        /// </summary>
        CapitalNTilde        = 0x00d1,

        /// <summary>
        /// Ò
        /// </summary>
        CapitalOGrave        = 0x00d2,

        /// <summary>
        /// Ó
        /// </summary>
        CapitalOAcute        = 0x00d3,

        /// <summary>
        /// Ô
        /// </summary>
        CapitalOCircumflex   = 0x00d4,

        /// <summary>
        /// Õ
        /// </summary>
        CapitalOTilde        = 0x00d5,

        /// <summary>
        /// Ö
        /// </summary>
        CapitalODiaeresis    = 0x00d6,

        /// <summary>
        /// ×
        /// </summary>
        Multiply             = 0x00d7,

        /// <summary>
        /// Ø
        /// </summary>
        CapitalOSlash        = 0x00d8,

#pragma warning disable CA1069 // Enums values should not be duplicated
        /// <summary>
        /// Ø
        /// </summary>
        CapitalOOblique      = 0x00d8,
#pragma warning restore CA1069 // Enums values should not be duplicated

        /// <summary>
        /// Ù
        /// </summary>
        CapitalUGrave        = 0x00d9,

        /// <summary>
        /// Ú
        /// </summary>
        CapitalUAcute        = 0x00da,

        /// <summary>
        /// Û
        /// </summary>
        CapitalUCircumflex   = 0x00db,

        /// <summary>
        /// Ü
        /// </summary>
        CapitalUDiaeresis    = 0x00dc,

        /// <summary>
        /// Ý
        /// </summary>
        CapitalYAcute        = 0x00dd,

        /// <summary>
        /// Þ
        /// </summary>
        CapitalThorn         = 0x00de,

        /// <summary>
        /// ß
        /// </summary>
        CapitalSSharp        = 0x00df,

        /// <summary>
        /// à
        /// </summary>
        SmallAGrave          = 0x00e0,

        /// <summary>
        /// á
        /// </summary>
        SmallAAcute          = 0x00e1,

        /// <summary>
        /// â
        /// </summary>
        SmallACircumflex     = 0x00e2,

        /// <summary>
        /// ã
        /// </summary>
        SmallATilde          = 0x00e3,

        /// <summary>
        /// ä
        /// </summary>
        SmallADiaeresis      = 0x00e4,

        /// <summary>
        /// å
        /// </summary>
        SmallARing           = 0x00e5,

        /// <summary>
        /// æ
        /// </summary>
        SmallAE              = 0x00e6,

        /// <summary>
        /// ç
        /// </summary>
        SmallCCedilla        = 0x00e7,

        /// <summary>
        /// è
        /// </summary>
        SmallEGrave          = 0x00e8,

        /// <summary>
        /// é
        /// </summary>
        SmallEAcute          = 0x00e9,

        /// <summary>
        /// ê
        /// </summary>
        SmallECircumflex     = 0x00ea,

        /// <summary>
        /// ë
        /// </summary>
        SmallEDiaeresis      = 0x00eb,

        /// <summary>
        /// ì
        /// </summary>
        SmallIGrave          = 0x00ec,

        /// <summary>
        /// í
        /// </summary>
        SmallIAcute          = 0x00ed,

        /// <summary>
        /// î
        /// </summary>
        SmallICircumflex     = 0x00ee,

        /// <summary>
        /// ï
        /// </summary>
        SmallIDiaeresis      = 0x00ef,

        /// <summary>
        /// ð
        /// </summary>
        SmallEth             = 0x00f0,

        /// <summary>
        /// ñ
        /// </summary>
        SmallNtilde          = 0x00f1,

        /// <summary>
        /// ò
        /// </summary>
        SmallOgrave          = 0x00f2,

        /// <summary>
        /// ó
        /// </summary>
        SmallOacute          = 0x00f3,

        /// <summary>
        /// ô
        /// </summary>
        SmallOcircumflex     = 0x00f4,

        /// <summary>
        /// õ
        /// </summary>
        SmallOtilde          = 0x00f5,

        /// <summary>
        /// ö
        /// </summary>
        SmallOdiaeresis      = 0x00f6,

        /// <summary>
        /// ÷
        /// </summary>
        Division             = 0x00f7,

        /// <summary>
        /// ø
        /// </summary>
        SmallOslash          = 0x00f8,

#pragma warning disable CA1069 // Enums values should not be duplicated
        /// <summary>
        /// ø
        /// </summary>
        SmallOOblique        = 0x00f8,
#pragma warning restore CA1069 // Enums values should not be duplicated

        /// <summary>
        /// ù
        /// </summary>
        SmallUGrave          = 0x00f9,

        /// <summary>
        /// ú
        /// </summary>
        SmallUAcute          = 0x00fa,

        /// <summary>
        /// û
        /// </summary>
        SmallUCircumflex     = 0x00fb,

        /// <summary>
        /// ü
        /// </summary>
        SmallUDiaeresis      = 0x00fc,

        /// <summary>
        /// ý
        /// </summary>
        SmallYAcute          = 0x00fd,

        /// <summary>
        /// þ
        /// </summary>
        SmallThorn           = 0x00fe,

        /// <summary>
        /// ÿ
        /// </summary>
        SmallYDiaeresis      = 0x00ff,

        /* Control Keys */
        /// <summary>
        /// Backspace
        /// </summary>
        BackSpace            = 0xff08,

        /// <summary>
        /// Tab
        /// </summary>
        Tab                  = 0xff09,

        /// <summary>
        /// Enter
        /// </summary>
        Enter                = 0xff0d,

#pragma warning disable CA1069 // Enums values should not be duplicated
        /// <summary>
        /// Return
        /// </summary>
        Return               = 0xff0d,
#pragma warning restore CA1069 // Enums values should not be duplicated

        /// <summary>
        /// Pause
        /// </summary>
        Pause                = 0xff13,

        /// <summary>
        /// Scroll Lock
        /// </summary>
        ScrollLock           = 0xff14,

        /// <summary>
        /// Escape
        /// </summary>
        Escape               = 0xff1b,

        /// <summary>
        /// Insert
        /// </summary>
        Insert               = 0xff63,

        /// <summary>
        /// Delete
        /// </summary>
        Delete               = 0xffff,

        /// <summary>
        /// Home
        /// </summary>
        Home                 = 0xff50,

        /// <summary>
        /// End
        /// </summary>
        End                  = 0xff57,

        /// <summary>
        /// Page Up
        /// </summary>
        PageUp               = 0xff55,

        /// <summary>
        /// Page Down
        /// </summary>
        PageDown             = 0xff56,

        /// <summary>
        /// Left
        /// </summary>
        Left                 = 0xff51,

        /// <summary>
        /// Up
        /// </summary>
        Up                   = 0xff52,

        /// <summary>
        /// Right
        /// </summary>
        Right                = 0xff53,

        /// <summary>
        /// Down
        /// </summary>
        Down                 = 0xff54,

        /// <summary>
        /// F1
        /// </summary>
        F1                   = 0xffbe,

        /// <summary>
        /// F2
        /// </summary>
        F2                   = 0xffbf,

        /// <summary>
        /// F3
        /// </summary>
        F3                   = 0xffc0,

        /// <summary>
        /// F4
        /// </summary>
        F4                   = 0xffc1,

        /// <summary>
        /// F5
        /// </summary>
        F5                   = 0xffc2,

        /// <summary>
        /// F6
        /// </summary>
        F6                   = 0xffc3,

        /// <summary>
        /// F7
        /// </summary>
        F7                   = 0xffc4,

        /// <summary>
        /// F8
        /// </summary>
        F8                   = 0xffc5,

        /// <summary>
        /// F9
        /// </summary>
        F9                   = 0xffc6,

        /// <summary>
        /// F10
        /// </summary>
        F10                  = 0xffc7,

        /// <summary>
        /// F11
        /// </summary>
        F11                  = 0xffc8,

        /// <summary>
        /// F12
        /// </summary>
        F12                  = 0xffc9,

        /// <summary>
        /// F13
        /// </summary>
        F13                  = 0xffca,

        /// <summary>
        /// F14
        /// </summary>
        F14                  = 0xffcb,

        /// <summary>
        /// F15
        /// </summary>
        F15                  = 0xffcc,

        /// <summary>
        /// F16
        /// </summary>
        F16                  = 0xffcd,

        /// <summary>
        /// F17
        /// </summary>
        F17                  = 0xffce,

        /// <summary>
        /// F18
        /// </summary>
        F18                  = 0xffcf,

        /// <summary>
        /// F19
        /// </summary>
        F19                  = 0xffd0,

        /// <summary>
        /// F20
        /// </summary>
        F20                  = 0xffd1,

        /// <summary>
        /// Left Shift
        /// </summary>
        LeftShift            = 0xffe1,

        /// <summary>
        /// Right Shift
        /// </summary>
        RightShift           = 0xffe2,

        /// <summary>
        /// Left Control
        /// </summary>
        LeftControl          = 0xffe3,

        /// <summary>
        /// Right Control
        /// </summary>
        RightControl         = 0xffe4,

        /// <summary>
        /// Left Meta
        /// </summary>
        LeftMeta             = 0xffe7,

        /// <summary>
        /// Right Meta
        /// </summary>
        RightMeta            = 0xffe8,

        /// <summary>
        /// Left Alt
        /// </summary>
        LeftAlt              = 0xffe9,

        /// <summary>
        /// Right Alt
        /// </summary>
        RightAlt             = 0xffea,

        /// <summary>
        /// Menu
        /// </summary>
        Menu                 = 0xff67
    }
}
