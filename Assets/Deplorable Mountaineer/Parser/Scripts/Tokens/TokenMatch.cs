namespace Deplorable_Mountaineer.Parser.Tokens {
    /// <summary>
    ///     Returned by match functions when scanning text for tokens.
    /// </summary>
    public class TokenMatch {
        /// <summary>
        ///     An empty match structure returned when there is no match.
        ///     This is not the same as matching an empty string.
        /// </summary>
        public static readonly TokenMatch Failure = new(false);

        /// <summary>
        ///     Create a new TokenMatch structure
        /// </summary>
        /// <param name="success">
        ///     true if this is a successful match.
        ///     Should use <see cref="TokenMatch.Failure" /> for failed matches.
        /// </param>
        /// <param name="index">Location in the text where the match is found</param>
        /// <param name="length">Length of the substring matched</param>
        /// <param name="value">The substring that was matched</param>
        /// <param name="tokenName">The name of the token being matched</param>
        public TokenMatch(bool success = true, int index = -1, int length = 0,
            string value = null, string tokenName = null){
            Index = index;
            Length = length;
            Success = success;
            Value = value;
            TokenName = tokenName;
        }

        /// <summary>
        ///     The name of the token that was matched.  E.g. "&lt;STRING_LITERAL&gt;".
        /// </summary>
        public string TokenName { get; }

        /// <summary>
        ///     The location in the text where the match is found.
        /// </summary>
        public int Index { get; }

        /// <summary>
        ///     The length of the match found.
        /// </summary>
        public int Length { get; }

        /// <summary>
        ///     True if a successful match.  All other fields have unspecified values if not
        ///     a successful match.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        ///     The matched substring.  If <c>text</c> is the string being scanned, then
        ///     <code>Value == text.Substring(Index, Length)</code>
        /// </summary>
        public string Value { get; }

        public override string ToString(){
            if(!Success) return "<FAILED>";
            if(TokenName.StartsWith("\""))
                return TokenName;
            return $"{TokenName}({Value})";
        }
    }
}