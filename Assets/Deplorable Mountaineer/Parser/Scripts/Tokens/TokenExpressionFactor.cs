#region

using System.Collections.Generic;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Parser.Tokens {
    /// <summary>
    ///     A portion of a <see cref="TokenExpressionTerm" /> that is concatenated with
    ///     other portions, and is made up of the "and" of
    ///     <see cref="PrimaryTokenExpression" />s.
    /// </summary>
    public class TokenExpressionFactor {
        /// <summary>
        ///     The conjunction of primaries that makes up this factor
        /// </summary>
        private readonly List<PrimaryTokenExpression> _primaries = new();

        /// <summary>
        ///     The conjunction of primaries that makes up this factor
        /// </summary>
        [PublicAPI]
        public IReadOnlyList<PrimaryTokenExpression> Primaries => _primaries;

        /// <summary>
        ///     Compile this factor recursively by compiling the component primaries using
        ///     the regex options.
        /// </summary>
        public void Compile(RegexOptions options){
            foreach(PrimaryTokenExpression primary in Primaries) primary.Compile(options);
        }

        /// <summary>
        ///     Add a new primary to the factor's conjunction.
        /// </summary>
        /// <param name="primary">The primary token expression</param>
        public void Add(PrimaryTokenExpression primary){
            _primaries.Add(primary);
        }

        /// <summary>
        ///     Try to match the factor at the location in the text.
        /// </summary>
        /// <param name="name">
        ///     The compiled TDL name that referenced the expression
        ///     this factor belongs to, if it is a named token.
        /// </param>
        /// <param name="text">The text being scanned</param>
        /// <param name="startingAt">Location within text being scanned</param>
        /// <param name="parent">The compiled TDL parent for referencing named tokens</param>
        /// <param name="verbose">Show debug info if running in Unity editor</param>
        /// <returns>
        ///     A TokenMatch structure, either <see cref="TokenMatch.Failure" /> or
        ///     a new <see cref="TokenMatch" /> object populated with match information.
        /// </returns>
        public TokenMatch Match(string name, string text, int startingAt,
            TokenDefinitionLanguage parent, bool verbose = false){
            int index = startingAt;
            int length = text.Length - startingAt;
            foreach(PrimaryTokenExpression primary in Primaries){
                if(verbose) Debug.Log($"Trying to match primary {primary}");
                if(!primary.GetMatch(text, ref index, ref length, parent, verbose)){
                    if(verbose) Debug.Log("failed to match primary {primary}");
                    return TokenMatch.Failure;
                }

                if(verbose)
                    Debug.Log(
                        $"Partial match of primary {primary} against {text.Substring(index, length)}");
            }

            TokenMatch match = new(true, index, length,
                text.Substring(index, length), name);

            if(verbose) Debug.Log($"Match {match} of all primaries");
            return match;
        }

        public override string ToString(){
            return string.Join(" & ", _primaries);
        }
    }
}