namespace Deplorable_Mountaineer.Parser.Tokens {
    /// <summary>
    ///     The kind of generalized regex token
    /// </summary>
    public enum PrimaryTokenType {
        /// <summary>
        ///     Ordinary regular expression
        /// </summary>
        Regex,

        /// <summary>
        ///     Identifier referencing TDL named token
        /// </summary>
        Identifier,

        /// <summary>
        ///     A parenthesized subexpression
        /// </summary>
        Subexpression
    }
}