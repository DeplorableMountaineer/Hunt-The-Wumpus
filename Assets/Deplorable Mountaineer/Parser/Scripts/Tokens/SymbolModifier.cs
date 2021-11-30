namespace Deplorable_Mountaineer.Parser.Tokens {
    /// <summary>
    ///     Modify a symbol for repetition or optionality.
    /// </summary>
    public enum SymbolModifier {
        /// <summary>
        ///     Unmodified symbol.  Matches exactly once.
        /// </summary>
        None,

        /// <summary>
        ///     Repeated symbol: matches zero or more times.
        /// </summary>
        Star,

        /// <summary>
        ///     Optional symbol: matches zero or one times.
        /// </summary>
        Question,

        /// <summary>
        ///     Mandatory repeated symbol: matches one or more times.
        /// </summary>
        Plus
    }
}
