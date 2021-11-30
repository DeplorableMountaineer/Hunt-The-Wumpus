namespace Deplorable_Mountaineer.Parser.Tokens {
    /// <summary>
    ///     Possible tokens that can be parsed (not a token of the TDL but of the parsed text)
    /// </summary>
    public enum TokenType {
        /// <summary>
        ///     A quoted string literal with some characters escaped as needed
        /// </summary>
        StringLiteral,

        /// <summary>
        ///     A named token (identifier in angle brackets) referencing an expression in the TDL
        /// </summary>
        NamedToken,

        /// <summary>
        ///     An explicit regular expression
        /// </summary>
        ExplicitRegex,

        /// <summary>
        ///     Matches the empty string
        /// </summary>
        Empty,

        /// <summary>
        ///     Matches the end of text
        /// </summary>
        EndOfText,

        /// <summary>
        ///     Used when the symbol turns out to be a nonterminal symbol.
        /// </summary>
        NotAToken
    }
}