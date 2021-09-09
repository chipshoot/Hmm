using Hmm.Utility.StringEnumeration;

namespace Hmm.Utility.Currency

{
    /// <summary>
    /// Enumeration of ISO 4217 currency codes, indexed with their respective ISO 4217 numeric currency codes.
    /// Only codes support in .Net with RegionInfo objects are listed
    /// </summary>
    public enum CurrencyCodeType
    {
        [StringValue("None")]
        None = 0,

        [StringValue("United Arab Emirates dirham")]
        Aed = 784,

        [StringValue("Afghan afghani")]
        Afn = 971,

        [StringValue("Albanian lek")]
        All = 8,

        [StringValue("Armenian dram")]
        Amd = 51,


        Ars = 32,
        Aud = 36,
        Azn = 944,
        Bam = 977,
        Bdt = 50,
        Bgn = 975,
        Bhd = 48,
        Bnd = 96,
        Bob = 68,
        Brl = 986,
        Byr = 974,
        Bzd = 84,

        [StringValue("Canadian dollar")]
        Cad = 124,
        Chf = 756,
        Clp = 152,

        [StringValue("Chinese yuan")]
        Cny = 156,
        Cop = 170,
        Crc = 188,
        Czk = 203,
        Dkk = 208,
        Dop = 214,
        Dzd = 12,
        Egp = 818,
        Etb = 230,
        Eur = 978,
        Gbp = 826,
        Gel = 981,
        Gtq = 320,

        [StringValue("Hong Kong dollar")]
        Hkd = 344,
        Hnl = 340,
        Hrk = 191,
        Huf = 348,
        Idr = 360,
        Ils = 376,
        Inr = 356,
        Iqd = 368,
        Irr = 364,
        Isk = 352,
        Jmd = 388,
        Jod = 400,
        Jpy = 392,
        Kes = 404,
        Kgs = 417,
        Khr = 116,
        KrW = 410,
        Kwd = 414,
        Kzt = 398,
        Lak = 418,
        Lbp = 422,
        Lkr = 144,
        Ltl = 440,
        Lvl = 428,
        Lyd = 434,
        Mad = 504,
        Mkd = 807,
        Mnt = 496,
        Mop = 446,
        Mvr = 462,
        Mxn = 484,
        Myr = 458,
        Nio = 558,
        Nok = 578,
        Npr = 524,
        Nzd = 554,
        Omr = 512,
        Pab = 590,
        Pen = 604,
        Php = 608,
        Pkr = 586,
        Pln = 985,
        Pyg = 600,
        Qar = 634,
        Ron = 946,
        Rsd = 941,
        Rub = 643,
        Rwf = 646,
        Sar = 682,
        Sek = 752,
        Sgd = 702,
        Syp = 760,
        Thb = 764,
        Tjs = 972,
        Tnd = 788,
        Try = 949,
        Ttd = 780,
        Twd = 901,
        Uah = 980,

        [StringValue("United States dollar")]
        Usd = 840,

        Uyu = 858,
        Uzs = 860,
        Vef = 937,
        Vnd = 704,
        Xof = 952,
        Yer = 886,
        Zar = 710,
        Zwl = 932
    }
}