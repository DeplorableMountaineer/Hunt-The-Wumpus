#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Parser.Tokens {
    /// <summary>
    ///     Compiles the text of a token definition file and maintains named tokens
    ///     for use by <see cref="Scanner" />
    /// </summary>
    /// <remarks>
    ///     <code>
    /// TokenDef : &lt;IDENTIFIER&gt; ":" TokenExpression Discard? &lt;OPTIONS&gt;? ";";
    /// TokenExpression : TokenExpressionTerm ("|" TokenExpressionTerm)*;
    /// TokenExpressionTerm : TokenExpressionFactor+
    /// TokenExpressionFactor : PrimaryTokenExpression ("&amp;" PrimaryTokenExpression)*;
    /// PrimaryTokenExpression : "!"? (
    ///                 &lt;REGULAR_EXPRESSION&gt;
    ///                 | &lt;IDENTIFIER&gt;
    ///                 | "(" TokenExpression ")"
    ///                 ) ("?"|"*"|"+")?;
    /// Discard : "[DISCARD]" TokenExpression
    /// </code>
    ///     A regular expression is delimited at the beginning and end by "/"s, as in "/ab*c|d+/"
    ///     or by any character (other than a letter or whitespace or a digit or underscore)
    ///     if it begins with "c", as in "c@ab*c|d+@".
    ///     A token def can end with options: one or more of [I], [M], [S], or [X],
    ///     for I = ignore case, M = multiline, S = single line, or X = ignore pattern whitespace.
    ///     See https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-options
    /// </remarks>
    public class TokenDefinitionLanguage {
        /// <summary>
        ///     Matches a C-style identifier
        /// </summary>
        private static readonly Regex IdentifierRegex =
            new(@"^[A-Za-z_][A-Za-z0-9_]*",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        ///     Matches bracketed options at the end of a token def
        /// </summary>
        private static readonly Regex OptionsRegex =
            new(@"^([[](i|s|x|m)[]]\s*)*",
                RegexOptions.Compiled | RegexOptions.CultureInvariant |
                RegexOptions.IgnoreCase);

        /// <summary>
        ///     Dictionary of named tokens that have been compiled.
        /// </summary>
        private readonly Dictionary<string, TokenExpression> _namedTokens = new();

        /// <summary>
        ///     Dictionary of named tokens that have been compiled.
        /// </summary>
        public IReadOnlyDictionary<string, TokenExpression> NamedTokens => _namedTokens;

        /// <summary>
        ///     If true, tokens can only match at the cursor.  If false,
        ///     will match anywhere after the cursor as well.  For most parsing,
        ///     you will want this to be true.  False simulates the meaning of grep.
        ///     Even if true, token defs should have the "match beginning of string"
        ///     character at the beginning of each alternative of a regular expression
        ///     to make regular expression matching more efficient.
        /// </summary>
        public bool ForceMatchAtCursor { get; set; } = true;

        /// <summary>
        ///     Compile a token definition file's text.
        /// </summary>
        /// <param name="text">The text to compile</param>
        /// <param name="filename">The filename to use in error messages</param>
        /// <returns>True if successful</returns>
        public bool Compile(string text, string filename = null){
            string remaining = text;
            string error;
            while(GetTokenDef(remaining, out remaining, out error)){
            }

            if(string.IsNullOrWhiteSpace(remaining)) return true;

            Debug.LogError($"Failed.  {error}");
            TokenUtils.DebugShowCurrentScanLocation(text, text.Length - remaining.Length,
                filename);
            return false;
        }

        /// <summary>
        ///     Try to match the named token at the location in the text.
        /// </summary>
        /// <param name="tokenName">
        ///     The name of the token to match.
        ///     Should be of the form "&lt;" IDENTIFIER "&gt;"
        ///     (with no spaces, identifier an all caps C-style identifier).
        /// </param>
        /// <param name="text">The text being scanned</param>
        /// <param name="startingAt">Location within text being scanned</param>
        /// <returns>
        ///     A TokenMatch structure, either <see cref="TokenMatch.Failure" /> or
        ///     a new <see cref="TokenMatch" /> object populated with match information.
        /// </returns>
        public TokenMatch Match(string tokenName, string text, int startingAt){
            TokenExpression expression = NamedTokens[tokenName];
            return expression.Match(tokenName, text, startingAt, this);
        }

        /// <summary>
        ///     Parse and compile a single token def
        /// </summary>
        /// <param name="text">The text of the token def language file</param>
        /// <param name="remaining">The remainder of the unparsed text</param>
        /// <param name="error">An error message if it failed</param>
        /// <returns>true if no errors</returns>
        private bool GetTokenDef(string text, out string remaining, out string error){
            string current = text.TrimStart();
            error = "";

            if(!GetIdentifier(current, out string id, out remaining, out error)){
                error = $"While scanning a token definition: {error}";
                return false;
            }

            current = remaining.TrimStart();

            if(!current.StartsWith(":")){
                error = "While scanning a token definition: " +
                        "expected a colon after the identifier.";
                return false;
            }

            current = current[1..].TrimStart();
            if(!GetExpression(current, out TokenExpression expression, out remaining,
                out error)){
                error = $"While scanning a token definition: expected an expression.  {error}";
                return false;
            }

            current = remaining.TrimStart();

            TokenExpression discard = null;
            if(current.StartsWith("[DISCARD]", StringComparison.InvariantCultureIgnoreCase)){
                current = current[9..].TrimStart();
                if(!GetExpression(current, out discard, out remaining,
                    out error)){
                    error =
                        "While scanning a token definition: expected an " +
                        $"expression after the discard token.  {error}";
                    return false;
                }

                current = remaining.TrimStart();
            }

            if(!GetOptions(current, out RegexOptions options, out remaining, out error)){
                error = "While scanning a token definition: " +
                        "expected options or a semicolon after the expression.  ";
                return false;
            }

            current = remaining.TrimStart();

            if(!current.StartsWith(";")){
                error = "While scanning a token definition: " +
                        "expected a semicolon after the expression and options.";
                return false;
            }

            remaining = current[1..].TrimStart();
            expression.Options = options;
            expression.Compile();
            if(discard != null){
                discard.Options = options;
                discard.Compile();
                expression.SetDiscard(discard);
            }

            _namedTokens[$"<{id.ToUpper(CultureInfo.InvariantCulture)}>"] = expression;
            //Debug.Log($"<{id.ToUpper(CultureInfo.InvariantCulture)}> : {expression};");
            return true;
        }

        /// <summary>
        ///     Parse and compile a Token Expression (generalized regular expression).
        /// </summary>
        /// <param name="text">The text being parsed</param>
        /// <param name="result">The compiled expression</param>
        /// <param name="remaining">The remaining unparsed text</param>
        /// <param name="error">Any error message that gets returned</param>
        /// <returns>true if no errors</returns>
        private bool GetExpression(string text, out TokenExpression result,
            out string remaining, out string error){
            result = new TokenExpression();

            if(!GetTerm(text, out TokenExpressionTerm term, out remaining, out error))
                return false;

            result.Add(term);
            string current = remaining.TrimStart();
            while(current.StartsWith("|")){
                current = current[1..].TrimStart();
                if(!GetTerm(current, out term, out remaining, out error)){
                    remaining = current;
                    error = $"Expected a term after the \"|\".  {error}";
                    return false;
                }

                result.Add(term);
                current = remaining.TrimStart();
            }

            return true;
        }

        /// <summary>
        ///     Parse and compile a term (portion separated by vertical bar, contains
        ///     the concatenation of factors) of a token expression.
        /// </summary>
        /// <param name="text">The text being parsed</param>
        /// <param name="result">The compiled term</param>
        /// <param name="remaining">The remaining unparsed text</param>
        /// <param name="error">Any error message that gets returned</param>
        /// <returns>true if no errors</returns>
        private bool GetTerm(string text, out TokenExpressionTerm result, out string remaining,
            out string error){
            result = new TokenExpressionTerm();

            if(!GetFactor(text, out TokenExpressionFactor factor, out remaining,
                out error))
                return false;

            result.Add(factor);
            string current = remaining.TrimStart();
            while(GetFactor(current, out factor, out remaining, out error)){
                result.Add(factor);
                current = remaining.TrimStart();
            }

            return true;
        }

        /// <summary>
        ///     Parse and compile a factor (portion of a term that is concatenated with other portions,
        ///     contains the "and" of primaries)
        /// </summary>
        /// <param name="text">The text being parsed</param>
        /// <param name="result">The compiled factor</param>
        /// <param name="remaining">The remaining unparsed text</param>
        /// <param name="error">Any error message that gets returned</param>
        /// <returns>true if no errors</returns>
        private bool GetFactor(string text, out TokenExpressionFactor result,
            out string remaining,
            out string error){
            result = new TokenExpressionFactor();
            if(!GetPrimary(text, out PrimaryTokenExpression primary, out remaining,
                out error))
                return false;


            result.Add(primary);
            string current = remaining.TrimStart();
            while(current.StartsWith("&")){
                current = current[1..].TrimStart();
                if(!GetPrimary(current, out primary, out remaining, out error)) return false;

                result.Add(primary);
                current = remaining.TrimStart();
            }

            return true;
        }


        /// <summary>
        ///     Parse and compile a primary (portion of a factor separated by an ampersand;
        ///     a basic expression: an identifier referencing another named token, a
        ///     regular expression, or a parenthesized subexpression, possibly followed
        ///     by an optional modifier (plus, star, or question mark).
        /// </summary>
        /// <param name="text">The text being parsed</param>
        /// <param name="result">The compiled primary</param>
        /// <param name="remaining">The remaining unparsed text</param>
        /// <param name="error">Any error message that gets returned</param>
        /// <returns>true if no errors</returns>
        private bool GetPrimary(string text, out PrimaryTokenExpression result,
            out string remaining,
            out string error){
            result = null;
            bool negated = false;
            string current = text.TrimStart();
            if(current.StartsWith("!")){
                negated = true;
                current = current[1..].TrimStart();
            }

            if(current.Length == 0){
                remaining = current;
                error = "Expected an identifier, a regex, or a parenthesized expression.";
                return false;
            }

            if(current.StartsWith("(")){
                current = current[1..].TrimStart();
                result = new PrimaryTokenExpression(PrimaryTokenType.Subexpression, negated);
                if(!GetExpression(current, out result.SubExpression, out remaining,
                    out error))
                    return false;

                current = remaining.TrimStart();
                if(!current.StartsWith(")")){
                    error = "Expected a closing parenthesis after the subexpression.";
                    return false;
                }

                Debug.Assert(current[0] == ')');
                current = current[1..].TrimStart();

                if(current.StartsWith("?")){
                    result.ExpressionModifier = SymbolModifier.Question;
                    current = current[1..].TrimStart();
                }
                else if(current.StartsWith("+")){
                    result.ExpressionModifier = SymbolModifier.Plus;
                    current = current[1..].TrimStart();
                }
                else if(current.StartsWith("*")){
                    result.ExpressionModifier = SymbolModifier.Star;
                    current = current[1..].TrimStart();
                }

                remaining = current;
                return true;
            }

            result = new PrimaryTokenExpression(PrimaryTokenType.Regex, negated);
            if(GetRegex(current, out result.Regex, out remaining, out error)) return true;

            result = new PrimaryTokenExpression(PrimaryTokenType.Identifier, negated);
            if(GetIdentifier(current, out result.Identifier, out remaining, out error))
                return true;

            error = "Expected an identifier, a regex, or a parenthesized expression.";
            return false;
        }

        /// <summary>
        ///     Get a regular expression token
        /// </summary>
        /// <param name="text">The text being parsed</param>
        /// <param name="result">The regex</param>
        /// <param name="remaining">The remaining unparsed text</param>
        /// <param name="error">Any error message that gets returned</param>
        /// <returns>true if no errors</returns>
        private bool GetRegex(string text, out string result, out string remaining,
            out string error){
            error = "";
            result = "";
            remaining = text.TrimStart();
            char delim = '/';
            if(remaining.StartsWith("c", StringComparison.CurrentCultureIgnoreCase) &&
               remaining.Length > 2 && !char.IsLetterOrDigit(remaining[1]) &&
               remaining[1] != '_' && !char.IsWhiteSpace(remaining[1])){
                remaining = remaining[1..];
                if(remaining.Length == 0){
                    error = "When scanning regex, expected a delimiter after 'c'";
                    return false;
                }

                delim = remaining[0];
            }

            if(!remaining.StartsWith(delim)){
                error = $"When scanning regex, expected an initial delimiter '{delim}'";
                return false;
            }

            remaining = remaining[1..];
            while(remaining.Length > 0 && remaining[0] != delim){
                result += remaining[0];
                remaining = remaining[1..];
            }

            if(!remaining.StartsWith(delim)){
                error = $"When scanning regex, expected a final delimiter '{delim}'";
                return false;
            }

            remaining = remaining[1..];
            return true;
        }

        /// <summary>
        ///     Get an identifier token
        /// </summary>
        /// <param name="text">The text being parsed</param>
        /// <param name="result">The identifier</param>
        /// <param name="remaining">The remaining unparsed text</param>
        /// <param name="error">Any error message that gets returned</param>
        /// <returns>true if no errors</returns>
        private bool GetIdentifier(string text, out string result, out string remaining,
            out string error){
            error = "";
            result = "";
            remaining = text.TrimStart();
            Match match = IdentifierRegex.Match(remaining);
            if(!match.Success){
                error = "Expected an identifier beginning with a letter or underscore.";
                return false;
            }

            result = match.Value;
            remaining = remaining[match.Length..];
            return true;
        }

        /// <summary>
        ///     Get and interpret token def options
        /// </summary>
        /// <param name="text">The text being parsed</param>
        /// <param name="options">The regex options</param>
        /// <param name="remaining">The remaining unparsed text</param>
        /// <param name="error">Any error message that gets returned</param>
        /// <returns>true if no errors</returns>
        private bool GetOptions(string text, out RegexOptions options, out string remaining,
            out string error){
            error = "";
            options = RegexOptions.Compiled | RegexOptions.CultureInvariant;
            remaining = text.TrimStart();
            Match match = OptionsRegex.Match(remaining);
            if(!match.Success){
                error = "Expected options, \"[I]\",  \"[M]\", \"[S]\", and/or \"[X]\".";
                return false;
            }

            if(match.Value.Contains("[I]", StringComparison.InvariantCultureIgnoreCase))
                options |= RegexOptions.IgnoreCase;
            if(match.Value.Contains("[M]", StringComparison.InvariantCultureIgnoreCase))
                options |= RegexOptions.Multiline;
            if(match.Value.Contains("[S]", StringComparison.InvariantCultureIgnoreCase))
                options |= RegexOptions.Singleline;
            if(match.Value.Contains("[X]", StringComparison.InvariantCultureIgnoreCase))
                options |= RegexOptions.IgnorePatternWhitespace;

            remaining = remaining[match.Length..];
            return true;
        }
    }
}