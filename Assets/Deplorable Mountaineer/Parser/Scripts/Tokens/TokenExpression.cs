#region

using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Parser.Tokens {
    /// <summary>
    ///     A compiled generalized regular expression, including regular expressions and named tokens,
    ///     as well as concatenations, conjunctions, negations, and disjunctions, and grouping with
    ///     parentheses.
    /// </summary>
    public class TokenExpression {
        /// <summary>
        ///     The terms (conjunction terms, separated by vertical bars) of the expression
        /// </summary>
        private readonly List<TokenExpressionTerm> _terms = new();

        private List<TokenExpressionTerm> _discard;

        /// <summary>
        ///     Matching options for the regular expression
        /// </summary>
        public RegexOptions Options;

        /// <summary>
        ///     The terms (conjunction terms, separated by vertical bars) of the expression
        /// </summary>
        public IReadOnlyList<TokenExpressionTerm> Terms => _terms;

        public IReadOnlyList<TokenExpressionTerm> Discard => _discard;

        /// <summary>
        ///     Compile this expression recursively by compiling the component terms
        ///     using the regex options.
        /// </summary>
        public void Compile(){
            foreach(TokenExpressionTerm term in _terms) term.Compile(Options);
        }

        public void SetDiscard(TokenExpression discard){
            Debug.Assert(_discard == null, "Attempted to set discard twice");
            _discard = discard._terms;
        }

        /// <summary>
        ///     Add a new term to the expression.
        /// </summary>
        /// <param name="term">The term</param>
        public void Add(TokenExpressionTerm term){
            _terms.Add(term);
        }

        /// <summary>
        ///     Try to match the expression at the location in the text.
        /// </summary>
        /// <param name="name">
        ///     The compiled TDL name that referenced this expression,
        ///     if it is a named token.
        /// </param>
        /// <param name="text">The text being scanned</param>
        /// <param name="startingAt">Location within text being scanned</param>
        /// <param name="parent">The compiled TDL parent for referencing named tokens</param>
        /// <param name="verbose">Show debug info if running in Unity editor</param>
        /// <param name="verboseDiscard">
        ///     Show debug info for the discard portion
        ///     if running in Unity editor
        /// </param>
        /// <returns>
        ///     A TokenMatch structure, either <see cref="TokenMatch.Failure" /> or
        ///     a new <see cref="TokenMatch" /> object populated with match information.
        /// </returns>
        public TokenMatch Match(string name, string text, int startingAt,
            TokenDefinitionLanguage parent, bool verbose = false, bool verboseDiscard = false){
            TokenMatch match = MatchWithoutDiscard(name, text, startingAt, parent, verbose);
            if(!match.Success || _discard == null || _discard.Count == 0) return match;

            foreach(TokenExpressionTerm term in _discard){
                TokenMatch discardMatch =
                    term.Match(name, text, match.Index + match.Length, parent,
                        verboseDiscard || verbose);
                if(discardMatch.Success){
                    Debug.Log("Discard match: " + discardMatch + " : " + term);
                    return match;
                }
            }

            return TokenMatch.Failure;
        }

        public override string ToString(){
            string result = string.Join(" | ", _terms);
            if(_discard != null && _discard.Count > 0)
                result += " [DISCARD] " + string.Join(" | ", _discard);

            return result;
        }

        /// <summary>
        ///     Try to match the expression at the location in the text, ignoring a discard clause
        /// </summary>
        /// <param name="name">
        ///     The compiled TDL name that referenced this expression,
        ///     if it is a named token.
        /// </param>
        /// <param name="text">The text being scanned</param>
        /// <param name="startingAt">Location within text being scanned</param>
        /// <param name="parent">The compiled TDL parent for referencing named tokens</param>
        /// <param name="verbose">Show debug info if running in Unity editor</param>
        /// <returns>
        ///     A TokenMatch structure, either <see cref="TokenMatch.Failure" /> or
        ///     a new <see cref="TokenMatch" /> object populated with match information.
        /// </returns>
        private TokenMatch MatchWithoutDiscard(string name, string text, int startingAt,
            TokenDefinitionLanguage parent, bool verbose = false){
            TokenMatch longestMatch = null;
            foreach(TokenExpressionTerm term in _terms){
                TokenMatch match = term.Match(name, text, startingAt, parent, verbose);
                if(!match.Success) continue;
                if(longestMatch == null || match.Length > longestMatch.Length)
                    longestMatch = match;
            }

            return longestMatch ?? TokenMatch.Failure;
        }
    }
}