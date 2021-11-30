#region

using System;
using System.Text.RegularExpressions;
using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Parser.Tokens {
    /// <summary>
    ///     A compiled primary expression (portion of a factor separated by an ampersand;
    ///     a basic expression: an identifier referencing another named token, a
    ///     regular expression, or a parenthesized subexpression, possibly followed
    ///     by an optional modifier (plus, star, or question mark).  It
    ///     may be negated as well.
    /// </summary>
    public class PrimaryTokenExpression {
        /// <summary>
        ///     If this is a regex primary, the compiled regular expression
        /// </summary>
        private Regex _compiledRegex;

        /// <summary>
        ///     If this is an identifier referencing another named token, the identifier string
        /// </summary>
        public string Identifier;

        /// <summary>
        ///     If this is a regex primary, the uncompiled regex string
        /// </summary>
        public string Regex;

        /// <summary>
        ///     If this is a parenthesized subexpression, the token expression representing it.
        /// </summary>
        public TokenExpression SubExpression;

        /// <summary>
        ///     Create a new primary (without the data filled in)
        /// </summary>
        /// <param name="tokenType">the type of the primary</param>
        /// <param name="negated">True if a negated primary</param>
        public PrimaryTokenExpression(PrimaryTokenType tokenType, bool negated = false){
            TokenType = tokenType;
            Negated = negated;
        }

        /// <summary>
        ///     True if the primary is negated
        /// </summary>
        public bool Negated { get; }

        /// <summary>
        ///     Which kind of primary this is (regex, subexpression, etc.)
        /// </summary>
        public PrimaryTokenType TokenType { get; }

        /// <summary>
        ///     Optional modifier for the primary (optional plus, star, or question mark)
        /// </summary>
        public SymbolModifier ExpressionModifier { get; set; } = SymbolModifier.None;

        /// <summary>
        ///     Compile this primary expression using the specified options
        /// </summary>
        /// <param name="options">The regex options to use</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     This should not be thrown; it would
        ///     indicate an error in this code
        /// </exception>
        public void Compile(RegexOptions options){
            switch(TokenType){
                case PrimaryTokenType.Regex:
                    _compiledRegex = new Regex(Regex, options);
                    break;
                case PrimaryTokenType.Identifier:
                    break;
                case PrimaryTokenType.Subexpression:
                    SubExpression.Options = options;
                    SubExpression.Compile();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///     Try to match the text to this primary expression
        /// </summary>
        /// <param name="text">The text being scanned</param>
        /// <param name="index">
        ///     Location being scanned; will be replaced with
        ///     a new location on success
        /// </param>
        /// <param name="length">
        ///     Max number of characters in portion to be scanned;
        ///     will be replaced with the actual number of characters matched on success
        /// </param>
        /// <param name="parent">
        ///     The TDL this expression is part of (needed if
        ///     the primary is an identifier referencing a named token)
        /// </param>
        /// <param name="verbose">Show debug info if running in Unity editor</param>
        /// <returns>true if this primary matches the text at the index</returns>
        public bool GetMatch(string text, ref int index, ref int length,
            TokenDefinitionLanguage parent, bool verbose = false){
            int newIndex = index;
            int newLength = length;
            bool result =
                GetUnNegatedMatch(text, ref newIndex, ref newLength, parent, verbose);
            if(!Negated){
                index = newIndex;
                length = newLength;
                return result;
            }

            if(result) return false;
            return true;
        }

        public override string ToString(){
            string prefix = Negated ? "!" : "";
            string suffix = ExpressionModifier switch {
                SymbolModifier.None => "",
                SymbolModifier.Star => "*",
                SymbolModifier.Question => "?",
                SymbolModifier.Plus => "+",
                _ => throw new ArgumentOutOfRangeException()
            };
            string regexChar = "/";
            string regexPrefix = "";
            if(Regex != null){
                if(Regex.Contains(regexChar)){
                    regexChar = "@";
                    regexPrefix = "c";
                }

                string trials = "|&!#$_=;:\"'";
                while(Regex.Contains(regexChar) && trials.Length > 0){
                    regexChar = trials[..1];
                    trials = trials[1..];
                }

                int ord = 32;
                while(!(char.IsSeparator(regexChar[0]) || char.IsSymbol(regexChar[0]) ||
                        char.IsPunctuation(regexChar[0])) || regexChar == "_" ||
                      Regex.Contains(regexChar)){
                    regexChar = char.ConvertFromUtf32(ord);
                    ord++;
                    if(ord <= Mathf.Max(Regex.Length, 16384,
                        Regex.Length*10))
                        continue;
                    regexChar = "/";
                    break;
                }
            }

            return TokenType switch {
                PrimaryTokenType.Regex => prefix + regexPrefix + regexChar + _compiledRegex +
                                          regexChar + suffix,
                PrimaryTokenType.Identifier => prefix + Identifier + suffix,
                PrimaryTokenType.Subexpression => prefix + "(" + SubExpression + ")" + suffix,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        /// <summary>
        ///     Try to match the text to this primary expression, ignoring the negation
        /// </summary>
        /// <param name="text">The text being scanned</param>
        /// <param name="index">
        ///     Location being scanned; will be replaced with
        ///     a new location on success
        /// </param>
        /// <param name="length">
        ///     Max number of characters in portion to be scanned;
        ///     will be replaced with the actual number of characters matched on success
        /// </param>
        /// <param name="parent">
        ///     The TDL this expression is part of (needed if
        ///     the primary is an identifier referencing a named token)
        /// </param>
        /// <param name="verbose">Show debug info if running in Unity editor</param>
        /// <returns>true if this primary matches the text at the index</returns>
        private bool GetUnNegatedMatch(string text, ref int index, ref int length,
            TokenDefinitionLanguage parent, bool verbose = false){
            int len;
            switch(ExpressionModifier){
                case SymbolModifier.None:
                    len = GetSingleMatchByType(text, index, length, out int newIndex, parent,
                        verbose);
                    if(len < 0) return false;
                    if(verbose) Debug.Log($"Match primary {this} because it has no modifier");
                    index = newIndex;
                    length = len;
                    return true;
                case SymbolModifier.Star:
                    len = GetSingleMatchByType(text, index, length, out newIndex, parent);
                    if(len < 0){
                        length = 0;
                        if(verbose)
                            Debug.Log($"Match primary {this} because it ends in a star");
                        return true;
                    }

                    index = newIndex;
                    int currentLength = len;
                    int currentIndex = index + len;
                    length -= len;
                    while(length > 0){
                        len = GetSingleMatchByType(text, currentIndex, length, out newIndex,
                            parent);
                        if(len < 0 || newIndex > currentIndex){
                            length = currentLength;
                            return true;
                        }

                        currentIndex = index + len;
                        currentLength += len;
                    }

                    length = currentLength;
                    return true;
                case SymbolModifier.Question:
                    len = GetSingleMatchByType(text, index, length, out newIndex, parent);
                    if(len < 0){
                        length = 0;
                        if(verbose)
                            Debug.Log(
                                $"Match primary {this} because it ends in a question mark");
                        return true;
                    }

                    index = newIndex;
                    length = len;
                    return true;
                case SymbolModifier.Plus:
                    len = GetSingleMatchByType(text, index, length, out newIndex, parent);
                    if(len < 0) return false;

                    if(verbose)
                        Debug.Log(
                            $"Match primary {this} because it ends in a plus and has at least one match");

                    index = newIndex;
                    currentLength = len;
                    currentIndex = index + len;
                    length -= len;
                    while(length > 0){
                        len = GetSingleMatchByType(text, currentIndex, length, out newIndex,
                            parent);
                        if(len < 0 || newIndex > currentIndex){
                            length = currentLength;
                            return true;
                        }

                        currentIndex = index + len;
                        currentLength += len;
                    }

                    length = currentLength;
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///     Try to match the text to this primary expression
        /// </summary>
        /// <param name="text">The text being scanned</param>
        /// <param name="index">Location being scanned;</param>
        /// <param name="length">Max number of characters in portion to be scanned</param>
        /// <param name="newIndex">The new location to scan the next portion, if successful</param>
        /// <param name="parent">
        ///     The TDL this expression is part of (needed if
        ///     the primary is an identifier referencing a named token)
        /// </param>
        /// <param name="verbose">Show debug info if running in Unity editor</param>
        /// <returns>Number of characters matched, or -1 if match failed</returns>
        private int GetSingleMatchByType(string text, int index,
            int length, out int newIndex, TokenDefinitionLanguage parent,
            bool verbose = false){
            switch(TokenType){
                case PrimaryTokenType.Regex:
                    Match match = _compiledRegex.Match(text, index, length);

                    if(!match.Success){
                        newIndex = -1;
                        return -1;
                    }

                    if(parent.ForceMatchAtCursor && match.Index != index){
                        newIndex = -1;
                        return -1;
                    }

                    if(verbose) Debug.Log($"Regex {_compiledRegex} matched {match.Value}");
                    newIndex = match.Index;
                    return match.Length;
                case PrimaryTokenType.Identifier:
                    string n = $"<{Identifier.ToUpper()}>";
                    if(n == "<EMPTY>"){
                        if(verbose) Debug.Log("<EMPTY> always matches");
                        newIndex = index;
                        return 0;
                    }

                    if(n == "<END_OF_TEXT>"){
                        if(index != text.Length){
                            newIndex = -1;
                            return -1;
                        }

                        if(verbose) Debug.Log("<END_OF_TEXT> matched");
                        newIndex = index;
                        return 0;
                    }

                    TokenExpression expression =
                        parent.NamedTokens[n];
                    TokenMatch tmm = expression.Match(n, text, index, parent, verbose);
                    if(!tmm.Success){
                        newIndex = -1;
                        return -1;
                    }

                    if(verbose) Debug.Log("Named token matched");
                    newIndex = tmm.Index;
                    return tmm.Length;
                case PrimaryTokenType.Subexpression:
                    tmm = SubExpression.Match("(subexpression)", text, index, parent, verbose);
                    if(!tmm.Success){
                        newIndex = -1;
                        return -1;
                    }

                    newIndex = tmm.Index;
                    return tmm.Length;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
