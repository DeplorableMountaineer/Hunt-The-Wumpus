#region

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Parser.Tokens {
    /// <summary>
    ///     Utilities for working with tokens and scanning
    /// </summary>
    public static class TokenUtils {
        /// <summary>
        ///     Matches a C-style identifier
        /// </summary>
        private static readonly Regex IdentifierRegex =
            new(@"^[A-Za-z_][A-Za-z0-9_]*$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        ///     When displaying context, show up to this many characters before the caret
        /// </summary>
        [PublicAPI]
        public static int ContextBefore { get; set; } = 40;

        /// <summary>
        ///     When displaying context, show up to this many characters after the caret
        /// </summary>
        [PublicAPI]
        public static int ContextAfter { get; set; } = 20;

        /// <summary>
        ///     When displaying context, use this string to show the dcaret
        /// </summary>
        [PublicAPI]
        public static string ContextCaret { get; set; } = "<b><color=#00ffff>^</color></b>";

        /// <summary>
        ///     Given a name, guess what token type it represents, and compute a normalized name.
        ///     Angle brackets around an identifier signify a named token (or possibly the empty token
        ///     or end of text token).  A quoted string is a string literal.  A quoted string
        ///     beginning with an At sign is an explicit regular expression.
        /// </summary>
        /// <param name="name">The name to test</param>
        /// <param name="normalizedName">The normalization of the name</param>
        /// <returns>The token type</returns>
        /// <exception cref="NotSupportedException">Name corresponds to no supported token type</exception>
        public static TokenType GuessTokenType(string name, out string normalizedName){
            string n = name.Trim().ToUpper();
            switch(n){
                case "<EMPTY>" or "\"\"":
                    normalizedName = "<EMPTY>";
                    return TokenType.Empty;
                case "<END_OF_TEXT>":
                    normalizedName = "<END_OF_TEXT>";
                    return TokenType.EndOfText;
            }

            int len = TryParseStringLiteral(name.Trim(), 0, out string t);
            if(len > 0){
                normalizedName = MakeStringLiteral(t);
                return TokenType.StringLiteral;
            }

            if(n.StartsWith("<") && n.EndsWith(">") && n.Length > 2){
                string id = n.Substring(1, n.Length - 2).Trim();
                if(id.Length == 0 || !IsIdentifier(id))
                    throw new NotSupportedException(
                        $"Token name {name} is not valid");

                normalizedName = $"<{id}>";
                return TokenType.NamedToken;
            }

            if(!n.StartsWith('@'))
                throw new NotSupportedException($"Token name {name} is not valid");

            string sl = n[1..].Trim();
            len = TryParseStringLiteral(sl, 0, out t);
            if(len <= 0) throw new NotSupportedException($"Token name {name} is not valid");

            normalizedName = $"@{t}";
            return TokenType.ExplicitRegex;
        }

        /// <summary>
        ///     The opposite of <see cref="TryParseStringLiteral" />.
        ///     Turn text into a string literal by escaping characters as needed,
        ///     and possibly surrounding with quotes.
        /// </summary>
        /// <param name="text">The text to convert</param>
        /// <param name="useQuotes">Surround the result with quotes</param>
        /// <returns>The string literal</returns>
        public static string MakeStringLiteral(string text, bool useQuotes = true){
            string result = useQuotes ? "\"" : "";
            int cursor = 0;
            while(cursor < text.Length){
                result += text[cursor] switch {
                    '\n' => "\\n",
                    '\t' => "\\t",
                    '\r' => "\\r",
                    '\a' => "\\a",
                    '\b' => "\\b",
                    '\f' => "\\f",
                    '\v' => "\\v",
                    '\0' => "\\0",
                    '\"' => "\\\"",
                    '\\' => "\\\\",
                    _ => text[cursor]
                };
                cursor++;
            }

            result += useQuotes ? "\"" : "";
            return result;
        }

        /// <summary>
        ///     The opposite of <see cref="MakeStringLiteral" />.
        ///     Turn the quoted string literal into the text it represents
        ///     by removing the quotes and unescaping the escaped characters.  Can
        ///     extract a string literal from a larger body of text.
        /// </summary>
        /// <param name="text">The text having the string literal</param>
        /// <param name="startingAt">Start at this location</param>
        /// <param name="stringValue">The resulting string value</param>
        /// <returns>The number of characters of the text that was converted</returns>
        public static int TryParseStringLiteral(string text,
            int startingAt, out string stringValue){
            stringValue = null;
            int cursor = startingAt;
            if(cursor >= text.Length || text[cursor] != '"') return -1;

            cursor++;

            string result = "";
            while(cursor < text.Length && text[cursor] != '"'){
                char c = text[cursor];
                if(c == '\\'){
                    cursor++;
                    if(cursor >= text.Length) return -1;
                    c = text[cursor];
                    result += c switch {
                        'n' => "\n",
                        't' => "\t",
                        'r' => "\r",
                        'a' => "\a",
                        'b' => "\b",
                        'f' => "\f",
                        'v' => "\v",
                        '0' => "\0",
                        _ => c
                    };
                    cursor++;
                    continue;
                }

                result += c;
                cursor++;
            }

            if(cursor >= text.Length || text[cursor] != '"') return -1;

            cursor++;
            stringValue = result;
            return cursor - startingAt;
        }

        /// <summary>
        ///     Return true if the text is a C-style identifier
        /// </summary>
        /// <param name="text">The text to test</param>
        /// <returns>true or false</returns>
        [PublicAPI]
        public static bool IsIdentifier(string text){
            Match match = IdentifierRegex.Match(text);
            return match.Success;
        }

        /// <summary>
        ///     Show the location in the text at the cursor with surrounding context.  Uses
        ///     <see cref="ContextBefore" />, <see cref="ContextAfter" />, and <see cref="ContextCaret" />
        ///     .
        /// </summary>
        /// <param name="text">The body of text</param>
        /// <param name="cursor">The location of the caret in the text</param>
        /// <returns></returns>
        [PublicAPI]
        public static string Context(string text, int cursor){
            return MakeStringLiteral(
                       Scanner.SafeSubstring(text, cursor - ContextBefore, cursor - 1),
                       false) +
                   ContextCaret +
                   MakeStringLiteral(
                       Scanner.SafeSubstring(text, cursor, cursor + ContextAfter),
                       false);
        }

        /// <summary>
        ///     Show a user-friendly string, only when playing in the game engine, using
        ///     the context of the text at the specified cursor, and using the specified
        ///     filename in the message
        /// </summary>
        /// <param name="text">The body of text being scanned</param>
        /// <param name="cursor">The location of the caret</param>
        /// <param name="filename">
        ///     The filename the text came from, \
        ///     or null to not show any filename.
        /// </param>
        public static void DebugShowCurrentScanLocation(string text, int cursor,
            string filename = null){
            ComputeLocation(text, cursor, out int line, out int charNum);
            string file = filename == null ? "" : $"{filename}:";
            Debug.Log($"{file}{line}[{charNum}]: {Context(text, cursor)}");
        }

        /// <summary>
        ///     Compute the line number and character number of a cursor location
        ///     in a body of text.
        /// </summary>
        /// <param name="text">The body of text</param>
        /// <param name="cursor">The location of the caret</param>
        /// <param name="line">The line number</param>
        /// <param name="charNum">The character number in the line</param>
        [PublicAPI]
        public static void ComputeLocation(string text, int cursor, out int line,
            out int charNum){
            line = 1;
            int lastEol = -1;
            for(int i = 0; i <= cursor && i < text.Length; i++)
                if(text[i] == '\n'){
                    lastEol = i;
                    line++;
                }

            charNum = cursor - lastEol + 2;
        }

        public static bool IsOneOf(string key, params string[] choices){
            foreach(string choice in choices)
                if(choice == key)
                    return true;

            return false;
        }

        public static bool IsOneOf(string key, IEnumerable<string> choices){
            foreach(string choice in choices)
                if(choice == key)
                    return true;

            return false;
        }
    }
}
