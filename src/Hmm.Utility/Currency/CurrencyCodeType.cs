using System.ComponentModel.DataAnnotations;

namespace Hmm.Utility.Currency
{
    /// <summary>
    /// Enumeration of ISO 4217 currency codes, indexed with their respective ISO 4217 numeric currency codes.
    /// Only codes support in .Net with RegionInfo objects are listed.
    /// Uses Display attribute for descriptive names - see ENUM_MIGRATION_GUIDE.md for usage.
    /// </summary>
    public enum CurrencyCodeType
    {
        [Display(Name = "None")]
        None = 0,

        [Display(Name = "United Arab Emirates dirham")]
        Aed = 784,

        [Display(Name = "Afghan afghani")]
        Afn = 971,

        [Display(Name = "Albanian lek")]
        All = 8,

        [Display(Name = "Armenian dram")]
        Amd = 51,

        [Display(Name ="Argentine peso")]
        Ars = 32,

        [Display(Name ="Australian dollar")]
        Aud = 36,

        [Display(Name ="Azerbaijani manat")]
        Azn = 944,

        [Display(Name ="Bosnia and Herzegovina convertible mark")]
        Bam = 977,

        [Display(Name ="Bangladeshi taka")]
        Bdt = 50,

        [Display(Name ="Bulgarian lev")]
        Bgn = 975,

        [Display(Name ="Bahraini dinar")]
        Bhd = 48,

        [Display(Name ="Brunei dollar")]
        Bnd = 96,

        [Display(Name ="Bolivian boliviano")]
        Bob = 68,

        [Display(Name ="Brazilian real")]
        Brl = 986,

        [Display(Name ="Belarusian ruble")]
        Byr = 974,

        [Display(Name ="Belize dollar")]
        Bzd = 84,

        [Display(Name ="Canadian dollar")]
        Cad = 124,

        [Display(Name ="Swiss franc")]
        Chf = 756,

        [Display(Name ="Chilean peso")]
        Clp = 152,

        [Display(Name ="Chinese yuan")]
        Cny = 156,

        [Display(Name ="Colombian peso")]
        Cop = 170,

        [Display(Name ="Costa Rican colon")]
        Crc = 188,

        [Display(Name ="Czech koruna")]
        Czk = 203,

        [Display(Name ="Danish krone")]
        Dkk = 208,

        [Display(Name ="Dominican peso")]
        Dop = 214,

        [Display(Name ="Algerian dinar")]
        Dzd = 12,

        [Display(Name ="Egyptian pound")]
        Egp = 818,

        [Display(Name ="Ethiopian birr")]
        Etb = 230,

        [Display(Name ="Euro")]
        Eur = 978,

        [Display(Name ="British pound sterling")]
        Gbp = 826,

        [Display(Name ="Georgian lari")]
        Gel = 981,

        [Display(Name ="Guatemalan quetzal")]
        Gtq = 320,

        [Display(Name ="Hong Kong dollar")]
        Hkd = 344,

        [Display(Name ="Honduran lempira")]
        Hnl = 340,

        [Display(Name ="Croatian kuna")]
        Hrk = 191,

        [Display(Name ="Hungarian forint")]
        Huf = 348,

        [Display(Name ="Indonesian rupiah")]
        Idr = 360,

        [Display(Name ="Israeli new shekel")]
        Ils = 376,

        [Display(Name ="Indian rupee")]
        Inr = 356,

        [Display(Name ="Iraqi dinar")]
        Iqd = 368,

        [Display(Name ="Iranian rial")]
        Irr = 364,

        [Display(Name ="Icelandic krona")]
        Isk = 352,

        [Display(Name ="Jamaican dollar")]
        Jmd = 388,

        [Display(Name ="Jordanian dinar")]
        Jod = 400,

        [Display(Name ="Japanese yen")]
        Jpy = 392,

        [Display(Name ="Kenyan shilling")]
        Kes = 404,

        [Display(Name ="Kyrgyzstani som")]
        Kgs = 417,

        [Display(Name ="Cambodian riel")]
        Khr = 116,

        [Display(Name ="South Korean won")]
        Krw = 410,

        [Display(Name ="Kuwaiti dinar")]
        Kwd = 414,

        [Display(Name ="Kazakhstani tenge")]
        Kzt = 398,

        [Display(Name ="Lao kip")]
        Lak = 418,

        [Display(Name ="Lebanese pound")]
        Lbp = 422,

        [Display(Name ="Sri Lankan rupee")]
        Lkr = 144,

        [Display(Name ="Lithuanian litas")]
        Ltl = 440,

        [Display(Name ="Latvian lats")]
        Lvl = 428,

        [Display(Name ="Libyan dinar")]
        Lyd = 434,

        [Display(Name ="Moroccan dirham")]
        Mad = 504,

        [Display(Name ="Macedonian denar")]
        Mkd = 807,

        [Display(Name ="Mongolian tugrik")]
        Mnt = 496,

        [Display(Name ="Macanese pataca")]
        Mop = 446,

        [Display(Name ="Maldivian rufiyaa")]
        Mvr = 462,

        [Display(Name ="Mexican peso")]
        Mxn = 484,

        [Display(Name ="Malaysian ringgit")]
        Myr = 458,

        [Display(Name ="Nicaraguan cordoba")]
        Nio = 558,

        [Display(Name ="Norwegian krone")]
        Nok = 578,

        [Display(Name ="Nepalese rupee")]
        Npr = 524,

        [Display(Name ="New Zealand dollar")]
        Nzd = 554,

        [Display(Name ="Omani rial")]
        Omr = 512,

        [Display(Name ="Panamanian balboa")]
        Pab = 590,

        [Display(Name ="Peruvian sol")]
        Pen = 604,

        [Display(Name ="Philippine peso")]
        Php = 608,

        [Display(Name ="Pakistani rupee")]
        Pkr = 586,

        [Display(Name ="Polish zloty")]
        Pln = 985,

        [Display(Name ="Paraguayan guarani")]
        Pyg = 600,

        [Display(Name ="Qatari riyal")]
        Qar = 634,

        [Display(Name ="Romanian leu")]
        Ron = 946,

        [Display(Name ="Serbian dinar")]
        Rsd = 941,

        [Display(Name ="Russian ruble")]
        Rub = 643,

        [Display(Name ="Rwandan franc")]
        Rwf = 646,

        [Display(Name ="Saudi riyal")]
        Sar = 682,

        [Display(Name ="Swedish krona")]
        Sek = 752,

        [Display(Name ="Singapore dollar")]
        Sgd = 702,

        [Display(Name ="Syrian pound")]
        Syp = 760,

        [Display(Name ="Thai baht")]
        Thb = 764,

        [Display(Name ="Tajikistani somoni")]
        Tjs = 972,

        [Display(Name ="Tunisian dinar")]
        Tnd = 788,

        [Display(Name ="Turkish lira")]
        Try = 949,

        [Display(Name ="Trinidad and Tobago dollar")]
        Ttd = 780,

        [Display(Name ="New Taiwan dollar")]
        Twd = 901,

        [Display(Name ="Ukrainian hryvnia")]
        Uah = 980,

        [Display(Name ="United States dollar")]
        Usd = 840,

        [Display(Name ="Uruguayan peso")]
        Uyu = 858,

        [Display(Name ="Uzbekistani som")]
        Uzs = 860,

        [Display(Name ="Venezuelan bolivar")]
        Vef = 937,

        [Display(Name ="Vietnamese dong")]
        Vnd = 704,

        [Display(Name ="West African CFA franc")]
        Xof = 952,

        [Display(Name ="Yemeni rial")]
        Yer = 886,

        [Display(Name ="South African rand")]
        Zar = 710,

        [Display(Name ="Zimbabwean dollar")]
        Zwl = 932
    }
}