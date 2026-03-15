using System.ComponentModel;
using JetBrains.Annotations;

namespace Witcher3StringEditor.Contracts;

/// <summary>
///     Represents the supported languages for The Witcher 3 string operations
///     This enumeration defines the different languages that can be used for translating or processing The Witcher 3
///     string items
///     Each language is associated with a specific culture code via the Description attribute
/// </summary>
public enum W3Language
{
    /// <summary>
    ///     Arabic language
    ///     Culture code: ar
    /// </summary>
    [Description("ar")] Ar = 0,

    /// <summary>
    ///     Brazilian Portuguese language
    ///     Culture code: pt
    /// </summary>
    [Description("pt")] Br = 1,

    /// <summary>
    ///     Simplified Chinese language
    ///     Culture code: zh-Hans
    /// </summary>
    [Description("zh-Hans")] Cn = 2,

    /// <summary>
    ///     Czech language
    ///     Culture code: cs
    /// </summary>
    [UsedImplicitly] [Description("cs")] Cz = 3,

    /// <summary>
    ///     German language
    ///     Culture code: de
    /// </summary>
    [Description("de")] [UsedImplicitly] De = 4,

    /// <summary>
    ///     English language
    ///     Culture code: en
    /// </summary>
    [Description("en")] [UsedImplicitly] En = 5,

    /// <summary>
    ///     Spanish language
    ///     Culture code: es
    /// </summary>
    [Description("es")] [UsedImplicitly] Es = 6,

    /// <summary>
    ///     Mexican Spanish language
    ///     Culture code: es-MX
    /// </summary>
    [Description("es-MX")] Esmx = 7,

    /// <summary>
    ///     French language
    ///     Culture code: fr
    /// </summary>
    [Description("fr")] [UsedImplicitly] Fr = 8,

    /// <summary>
    ///     Hungarian language
    ///     Culture code: hu
    /// </summary>
    [Description("hu")] [UsedImplicitly] Hu = 9,

    /// <summary>
    ///     Italian language
    ///     Culture code: it
    /// </summary>
    [Description("it")] [UsedImplicitly] It = 10,

    /// <summary>
    ///     Japanese language
    ///     Culture code: ja
    /// </summary>
    [UsedImplicitly] [Description("ja")] Jp = 11,

    /// <summary>
    ///     Korean language
    ///     Culture code: ko
    /// </summary>
    [Description("ko")] Kr = 12,

    /// <summary>
    ///     Polish language
    ///     Culture code: pl
    /// </summary>
    [Description("pl")] [UsedImplicitly] Pl = 13,

    /// <summary>
    ///     Russian language
    ///     Culture code: ru
    /// </summary>
    [Description("ru")] [UsedImplicitly] Ru = 14,

    /// <summary>
    ///     Traditional Chinese language
    ///     Culture code: zh-Hant
    /// </summary>
    [UsedImplicitly] [Description("zh-Hant")]
    Zh = 15,

    /// <summary>
    ///     Turkish language
    ///     Culture code: tr
    /// </summary>
    [Description("tr")] Tr = 16
}