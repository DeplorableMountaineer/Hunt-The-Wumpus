#region

using System.Collections.Generic;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Parser.Tokens {
    /// <summary>
    ///     A portion of a <see cref="TokenExpression" /> that is concatenated with
    ///     other portions, and is made up of the concatenation of
    ///     <see cref="TokenExpressionFactor" />s.
    /// </summary>
    public class TokenExpressionTerm {
        /// <summary>
        ///     The disjunction of factors that makes up this term
        /// </summary>
        private readonly List<TokenExpressionFactor> _factors = new();

        /// <summary>
        ///     The disjunction of factors that makes up this term
        /// </summary>
        [PublicAPI]
        public IReadOnlyList<TokenExpressionFactor> Factors => _factors;

        /// <summary>
        ///     Compile this term recursively by compiling the component factors using
        ///     the regex options.
        /// </summary>
        public void Compile(RegexOptions options){
            foreach(TokenExpressionFactor factor in Factors) factor.Compile(options);
        }

        /// <summary>
        ///     Add a new factor to the term's conjunction.
        /// </summary>
        /// <param name="factor">The primary token expression</param>
        public void Add(TokenExpressionFactor factor){
            _factors.Add(factor);
        }

        /// <summary>
        ///     Try to match the term at the location in the text.
        /// </summary>
        /// <param name="name">
        ///     The compiled TDL name that referenced the expression
        ///     this term belongs to, if it is a named token.
        /// </param>
        /// <param name="text">The text being scanned</param>
        /// <param name="startingAt">Location within text being scanned</param>
        /// <param name="parent">The compiled TDL parent for referencing named tokens</param>
        /// <param name="verbose">Show debug info if running in Unity editor</param>
        /// <returns>
        ///     A TokenMatch structure, either <see cref="TokenMatch.Failure" /> or
        ///     a new <see cref="TokenMatch" /> bject populated with match information.
        /// </returns>
        public TokenMatch Match(string name, string text, int startingAt,
            TokenDefinitionLanguage parent, bool verbose = false){
            int index = startingAt;
            foreach(TokenExpressionFactor factor in _factors){
                TokenMatch match = factor.Match(name, text, index, parent, verbose);
                if(!match.Success) return TokenMatch.Failure;
                if(verbose) Debug.Log($"Match {match} of factor {factor}");
                index += match.Length;
            }

            return new TokenMatch(true, startingAt, index - startingAt,
                text.Substring(startingAt, index - startingAt), name);
        }

        public override string ToString(){
            return string.Join(" ", _factors);
        }
    }
}