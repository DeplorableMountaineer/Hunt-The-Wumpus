#region

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Parser.Tokens {
    /// <summary>
    ///     Process tokens following a generalized regular expression spec in a Token Definition
    ///     Language.
    /// </summary>
    public class Scanner {
        /// <summary>
        ///     A stack for saving and restoring the cursor, facilitating a backtracking parser.
        /// </summary>
        private readonly Stack<int> _cursorStack = new();

        /// <summary>
        ///     Current location in the text
        /// </summary>
        private int _cursor;

        /// <summary>
        ///     The identifier (with angle brackets) of an expression for anything to be skipped
        ///     (typically whitespace and comments).
        /// </summary>
        private string _skipKey;

        /// <summary>
        ///     Create a new scanner given compiled TDL named tokens, the text to be scanned, and
        ///     a skip key
        /// </summary>
        /// <param name="tokens">The compiled TDL named tokens</param>
        /// <param name="text">the text to be scanned</param>
        /// <param name="skipKey">
        ///     The angle-bracketed identifier referencing a generalized
        ///     regular expression matching anything to be skipped (e.g. whitespace, comments)
        /// </param>
        public Scanner(TokenDefinitionLanguage tokens, string text, string skipKey = "<SKIP>"){
            Tokens = tokens;
            Text = text;
            _skipKey = skipKey;
        }

        /// <summary>
        ///     Current location in the text
        /// </summary>
        [PublicAPI]
        public int Cursor {
            get => _cursor;
            set => _cursor = Mathf.Clamp(value, 0, Text.Length);
        }

        /// <summary>
        ///     The compiled TDL named tokens
        /// </summary>
        public TokenDefinitionLanguage Tokens { get; private set; }

        /// <summary>
        ///     The text being scanned
        /// </summary>
        [PublicAPI]
        public string Text { get; private set; }

        /// <summary>
        ///     True when cursor reaches the end of text
        /// </summary>
        [PublicAPI]
        public bool IsEndOfText => Cursor >= Text.Length;

        public void Reset(string text, string skipKey = null){
            Text = text;
            Cursor = 0;
            if(skipKey != null) _skipKey = skipKey;
        }

        public void Reset(TokenDefinitionLanguage tokens, string text, string skipKey = null){
            Tokens = tokens;
            Text = text;
            Cursor = 0;
            if(skipKey != null) _skipKey = skipKey;
        }

        /// <summary>
        ///     Only for play-in-browser: show a user-friendly "current location" message
        /// </summary>
        /// <param name="filename">The filename being scanned, to be put into the message</param>
        public void DebugShowCurrentScanLocation(string filename = null){
            TokenUtils.DebugShowCurrentScanLocation(Text, Cursor, filename);
        }

        /// <summary>
        ///     Scan the text at the current location and attempt to match one of the names.
        /// </summary>
        /// <param name="names">The names to match</param>
        /// <returns>
        ///     The match structure, whose "Success" field tells where there
        ///     was a match.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Indicates an error in the
        ///     code and should never happen.
        /// </exception>
        public TokenMatch Scan(params string[] names){
            Skip();
            TokenMatch bestMatch = null;
            foreach(string name in names){
                TokenType type = TokenUtils.GuessTokenType(name, out string normalizedName);
                switch(type){
                    case TokenType.StringLiteral:
                        bestMatch = GetStringLiteral(normalizedName, bestMatch);
                        break;
                    case TokenType.NamedToken:
                        bestMatch = GetNamedToken(normalizedName, bestMatch);
                        break;
                    case TokenType.ExplicitRegex:
                        bestMatch = GetExplicitRegex(normalizedName, bestMatch);
                        break;
                    case TokenType.Empty:
                        bestMatch = GetEmpty(bestMatch, normalizedName);
                        break;
                    case TokenType.EndOfText:
                        bestMatch = GetEndOfText(bestMatch, normalizedName);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return bestMatch ?? TokenMatch.Failure;
        }

        public TokenMatch Scan(IEnumerable<string> names){
            Skip();
            TokenMatch bestMatch = null;
            foreach(string name in names){
                TokenType type = TokenUtils.GuessTokenType(name, out string normalizedName);
                switch(type){
                    case TokenType.StringLiteral:
                        bestMatch = GetStringLiteral(normalizedName, bestMatch);
                        break;
                    case TokenType.NamedToken:
                        bestMatch = GetNamedToken(normalizedName, bestMatch);
                        break;
                    case TokenType.ExplicitRegex:
                        bestMatch = GetExplicitRegex(normalizedName, bestMatch);
                        break;
                    case TokenType.Empty:
                        bestMatch = GetEmpty(bestMatch, normalizedName);
                        break;
                    case TokenType.EndOfText:
                        bestMatch = GetEndOfText(bestMatch, normalizedName);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return bestMatch ?? TokenMatch.Failure;
        }

        /// <summary>
        ///     Push the current cursor location
        /// </summary>
        public void Push(){
            _cursorStack.Push(Cursor);
        }

        /// <summary>
        ///     Restore the cursor location from the top of the stack
        /// </summary>
        public void Pop(){
            Cursor = _cursorStack.Pop();
        }

        /// <summary>
        ///     Pop the cursor stack without changing the cursor
        /// </summary>
        public void Discard(){
            _cursorStack.Pop();
        }

        /// <summary>
        ///     Return (without popping or changing the cursor) the cursor location at the top
        ///     of the stack.
        /// </summary>
        /// <returns>An integer location in text</returns>
        public int Peek(){
            return _cursorStack.Peek();
        }

        /// <summary>
        ///     Return the current depth of the stack.  Zero means it cannot be popped,
        ///     peeked, or discarded.
        /// </summary>
        /// <returns>The number of entries in the stack</returns>
        public int Depth(){
            return _cursorStack.Count;
        }

        /// <summary>
        ///     Consume the matched token, advancing the cursor beyond it.
        /// </summary>
        /// <param name="tokenMatch">The token just scanned at the current position</param>
        public void Consume(TokenMatch tokenMatch){
            Debug.Assert(
                tokenMatch is { Success: true } && tokenMatch.Index == Cursor,
                "tokenMatch is { Success: true } && tokenMatch.Index == Cursor");
            Cursor += tokenMatch.Length;
        }

        /// <summary>
        ///     Combine the <see cref="Scan(string[])" /> and <see cref="Consume" /> operations into one
        ///     call.
        ///     Consume is not called if the scan does not succeed.
        /// </summary>
        /// <param name="names">The names to match</param>
        /// <returns>
        ///     The match structure, whose "Success" field tells where there
        ///     was a match.
        /// </returns>
        public TokenMatch ScanConsume(params string[] names){
            TokenMatch tm = Scan(names);
            if(tm.Success) Consume(tm);
            return tm;
        }

        /// <summary>
        ///     Called by <see cref="Scan(string[])" />. Skip anything matching the skip key.
        /// </summary>
        public void Skip(){
            if(!Tokens.NamedTokens.ContainsKey(_skipKey)) return;
            TokenMatch tm = Tokens.Match(_skipKey, Text, Cursor);
            if(!tm.Success) return;
            Cursor += tm.Length;
        }


        /// <summary>
        ///     Called by <see cref="Scan(string[])" />.  If the empty match is better than the current
        ///     best
        ///     match (that is, the current best match is null or itself empty), return
        ///     an empty token match.
        /// </summary>
        /// <param name="currentBestMatch">The best match so far</param>
        /// <param name="normalizedName">
        ///     The name of the empty token to be put into the
        ///     token match result.
        /// </param>
        /// <returns>the currentBestMatch or a better match</returns>
        private TokenMatch GetEmpty(TokenMatch currentBestMatch, string normalizedName){
            if(currentBestMatch == null || currentBestMatch.Length == 0)
                return new TokenMatch(true, Cursor, 0, "", normalizedName);
            return currentBestMatch;
        }

        /// <summary>
        ///     Called by <see cref="Scan(string[])" />.  If the end-of-text match is better than the
        ///     current best
        ///     match (that is, the current best match is null or itself empty and
        ///     cursor is at the end of text), return an end of text match.
        /// </summary>
        /// <param name="currentBestMatch">The best match so far</param>
        /// <param name="normalizedName">
        ///     The name of the end of text token to be put into the
        ///     token match result.
        /// </param>
        /// <returns>the currentBestMatch or a better match</returns>
        private TokenMatch GetEndOfText(TokenMatch currentBestMatch, string normalizedName){
            if(!IsEndOfText) return currentBestMatch;
            if(currentBestMatch == null || currentBestMatch.Length == 0)
                return new TokenMatch(true, Cursor, 0, "", normalizedName);
            return currentBestMatch;
        }

        /// <summary>
        ///     Called by <see cref="Scan(string[])" />.  If the regular expression match is better than
        ///     the current best
        ///     match (that is, the current best match is null or is shorter), return
        ///     the better match.
        /// </summary>
        /// <param name="currentBestMatch">The best match so far</param>
        /// <param name="normalizedName">
        ///     The name of the regex token to be put into the
        ///     token match result.
        /// </param>
        /// <returns>the currentBestMatch or a better match</returns>
        private TokenMatch
            GetExplicitRegex(string normalizedName, TokenMatch currentBestMatch){
            int len = TokenUtils.TryParseStringLiteral(normalizedName, 1,
                out string text);
            Debug.Assert(len > 0);
            Regex regex = new(text,
                RegexOptions.Compiled | RegexOptions.CultureInvariant);
            Match match = regex.Match(Text, Cursor);
            if(!match.Success || match.Index != Cursor) return currentBestMatch;
            if(currentBestMatch == null || match.Length > currentBestMatch.Length)
                return new TokenMatch(true, Cursor, match.Length,
                    match.Value,
                    normalizedName);
            return currentBestMatch;
        }

        /// <summary>
        ///     Called by <see cref="Scan(string[])" />.  If the named token match is better than the
        ///     current best
        ///     match (that is, the current best match is null or is shorter), return the
        ///     better match.
        /// </summary>
        /// <param name="currentBestMatch">The best match so far</param>
        /// <param name="normalizedName">
        ///     The name of the token to be put into the
        ///     token match result.
        /// </param>
        /// <returns>the currentBestMatch or a better match</returns>
        private TokenMatch GetNamedToken(string normalizedName, TokenMatch currentBestMatch){
            TokenMatch tm = Tokens.Match(normalizedName, Text, Cursor);
            if(!tm.Success) return currentBestMatch;
            if(currentBestMatch == null || tm.Length > currentBestMatch.Length)
                return tm;
            return currentBestMatch;
        }

        /// <summary>
        ///     Called by <see cref="Scan(string[])" />.  If the string literal match is better than the
        ///     current best
        ///     match (that is, the current best match is null or is shorter), return the
        ///     better match.
        /// </summary>
        /// <param name="currentBestMatch">The best match so far</param>
        /// <param name="normalizedName">
        ///     The name of the string literal token to be put into the
        ///     token match result.
        /// </param>
        /// <returns>the currentBestMatch or a better match</returns>
        private TokenMatch
            GetStringLiteral(string normalizedName, TokenMatch currentBestMatch){
            int len = TokenUtils.TryParseStringLiteral(normalizedName, 0,
                out string text);
            Debug.Assert(len > 0);

            if(currentBestMatch != null && text.Length < currentBestMatch.Length)
                return currentBestMatch;

            if(IsSubstringAt(Text, text, Cursor))
                return new TokenMatch(true, Cursor, text.Length, text,
                    normalizedName);

            return currentBestMatch;
        }


        /// <summary>
        ///     Return true if the substring is to be found in the text at the specified location
        /// </summary>
        /// <param name="text">The text to scan</param>
        /// <param name="substring">The substring to match</param>
        /// <param name="textStart">The locaiton in text</param>
        /// <returns>true or false</returns>
        private static bool IsSubstringAt(string text, string substring, int textStart){
            int l = substring.Length;
            for(int i = 0; i < l; i++){
                if(textStart + i >= text.Length)
                    return false;
                if(text[textStart + i] != substring[i]) return false;
            }

            return true;
        }

        /// <summary>
        ///     Return the substring from start to end, zero-up, inclusive.  This works
        ///     even for out of range values by returning what is available.
        /// </summary>
        /// <param name="text">The text to scan</param>
        /// <param name="start">The index of the substring's first character </param>
        /// <param name="end">The index of the substring's last character</param>
        /// <returns>The substring, possibly empty</returns>
        public static string SafeSubstring(string text, int start, int end){
            if(start > text.Length || end < 0 || end < start || text.Length == 0) return "";
            int s = Mathf.Max(start, 0);
            int e = Mathf.Min(end, text.Length - 1);
            return text.Substring(s, e - s + 1);
        }
    }
}
