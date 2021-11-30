#region

using System.Collections.Generic;
using Deplorable_Mountaineer.Parser.ParseTree;
using Deplorable_Mountaineer.Parser.Tokens;
using JetBrains.Annotations;
using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Parser {
    /// <summary>
    ///     The original Hunt the Wumpus was written in BASIC and can be found here:
    ///     http://web.archive.org/web/20100428041109/http://www.atariarchives.org/bcc1/showpage.php?page=247
    ///     Parse the console command language, using tokens from the file ConsoleTokens.tdl.txt
    /// </summary>
    public class WumpusParser {
        /// <summary>
        ///     Tokens that can be the first thing seen when the named symbol is encountered
        /// </summary>
        private readonly Dictionary<string, HashSet<string>> _first = new();

        /// <summary>
        ///     Tokens that may follow the named symbol.  In this game, currently,
        ///     &lt;END_OF_TEXT&gt; is all there is.
        /// </summary>
        private readonly Dictionary<string, HashSet<string>> _follows = new();

        /// <summary>
        ///     The parse tree holding the compiled command
        /// </summary>
        public readonly ParseTreeNode ParseTree;

        /// <summary>
        ///     Holds the most recent error message, to be displayed if the
        ///     parsing ultimately fails.
        /// </summary>
        private string _error;

        /// <summary>
        ///     Create a new parser.
        /// </summary>
        /// <param name="tokens">The compiled token defs to scan</param>
        /// <param name="skipKey">
        ///     Whitespace or anything else automatically
        ///     skipped before processing any token.
        /// </param>
        public WumpusParser(TokenDefinitionLanguage tokens, string skipKey = "<SKIP>"){
            ParseTree = new ParseTreeNode("Command");
            Scanner = new Scanner(tokens, "", skipKey);
            InitFirstSets();
            InitFollowsSet();
        }

        /// <summary>
        ///     The token scanner
        /// </summary>
        [PublicAPI]
        public Scanner Scanner { get; }

        /// <summary>
        ///     Set to true to get in-unity-editor debug messages
        /// </summary>
        [PublicAPI]
        public bool DebugMode { get; set; }

        /// <summary>
        ///     Compile a command line, displaying a game-friendly error on failure
        /// </summary>
        /// <param name="text">The command line</param>
        /// <returns>True if it parsed successfully and completely</returns>
        public bool Compile(string text){
            Scanner.Reset(text);
            ParseTree.Clear();

            //fallback error if no other error message gets applied
            //should be rare
            _error = "I don't understand.";

            if(!Command(ParseTree)){
                if(!string.IsNullOrEmpty(_error)) Console.Console.Instance.Say(_error);
                return false;
            }

            //Make sure there is no text left over
            Scanner.Skip();
            if(Scanner.IsEndOfText) return true;
            string rest = Scanner.Text[Scanner.Cursor..];
            Console.Console.Instance.Say($"This made sense up to \"{rest}\"...");
            return false;
        }

        /// <summary>
        ///     Thus, the Face nonterminal symbol must start with a &lt;FACE&gt; token, and so on
        /// </summary>
        private void InitFirstSets(){
            _first["Face"] = new HashSet<string> { "<FACE>" };
            _first["Walk"] = new HashSet<string> { "<WALK>" };
            _first["Grab"] = new HashSet<string> { "<GRAB>" };
            _first["Action"] = new HashSet<string> { "<KILL_WUMPUS>", "<CLIMB>" };
            _first["Action"].UnionWith(_first["Face"]);
            _first["Action"].UnionWith(_first["Walk"]);
            _first["Action"].UnionWith(_first["Grab"]);
            _first["LookCommand"] = new HashSet<string> { "<LOOK>", "<LOOK_AROUND>" };
            _first["Direction"] = new HashSet<string>
                { "<NORTH>", "<SOUTH>", "<EAST>", "<WEST>" };
            _first["Command"] = new HashSet<string> {
                "<INVENTORY>", "<SCORE>", "<QUIT>", "<UP>"
            };
            _first["Command"].UnionWith(_first["Action"]);
            _first["Command"].UnionWith(_first["LookCommand"]);
            _first["Command"].UnionWith(_first["Direction"]);
            _first["Item"] = new HashSet<string> { "<BOW>", "<ARROW>", "<GOLD>" };
            _first["Actor"] = new HashSet<string> { "<PLAYER>", "<WUMPUS>" };
            _first["Object"] = new HashSet<string> { "<PIT>", "<ROOM>" };
            _first["Object"].UnionWith(_first["Item"]);
            _first["Object"].UnionWith(_first["Actor"]);
        }

        /// <summary>
        ///     In this game, all nonterminals are followed only by &lt;END_OF_TEXT&gt;
        /// </summary>
        private void InitFollowsSet(){
            _follows["Command"] = new HashSet<string> { "<END_OF_TEXT>" };
            _follows["Actor"] = new HashSet<string> { "<END_OF_TEXT>" };
            _follows["Direction"] = new HashSet<string> { "<END_OF_TEXT>" };
            _follows["Item"] = new HashSet<string> { "<END_OF_TEXT>" };
            _follows["Action"] = new HashSet<string> { "<END_OF_TEXT>" };
            _follows["Face"] = new HashSet<string> { "<END_OF_TEXT>" };
            _follows["Walk"] = new HashSet<string> { "<END_OF_TEXT>" };
            _follows["Grab"] = new HashSet<string> { "<END_OF_TEXT>" };
            _follows["LookCommand"] = new HashSet<string> { "<END_OF_TEXT>" };
            _follows["Object"] = new HashSet<string> { "<END_OF_TEXT>" };
        }

        /// <summary>
        ///     Parse the Command nonterminal start symbol
        /// </summary>
        /// <param name="parseTree">
        ///     the root node of the parse tree, filled in with
        ///     the nonterminal "Command", initially with no children.  This
        ///     method will add the needed children.
        /// </param>
        /// <returns>True if successful</returns>
        private bool Command(ParseTreeNode parseTree){
            if(DebugMode) Debug.Log("Attempting to parse Command");

            //The general pattern is check that the next token to be scanned
            //is in the nonterminal's "first" set.  If not, exit early.
            TokenMatch token = Scanner.Scan(_first["Command"]);
            if(!token.Success){
                if(DebugMode) Debug.Log("Failed to parse Command");
                token = Scanner.Scan("<WORD>");
                if(token.Success) _error = $"I don't know how to \"{token.Value}\"";
                return false;
            }

            //so far, so good.  But it still might be a non-command depending on later tokens
            if(DebugMode) Debug.Log("While parsing Command: found first token " + token);

            //save cursor state.  Doesn't matter with command because "sub commands" have
            //not been added yet, but in general, if a nonterminal fails to parse,
            //the cursor is restored and another nonterminal is tried.  Thus,
            //the parser has a chance of working even if the grammar is not an LL(1)
            //grammar, though less efficiently.
            Scanner.Push();

            //Now, check all alternatives for the nonterminal.  For this one,
            //a command can be an action
            if(TokenUtils.IsOneOf(token.TokenName, _first["Action"])){
                //the next token is an appropriate first token for an action, so try to
                //parse an action
                ParseTreeNode child = new("Action");
                if(Action(child)){
                    //If it succeeds, see if what follows makes sense.
                    //If not, something is wrong (such as junk after a
                    //valid command.
                    if(Scanner.Scan(_follows["Action"]).Success){
                        //add the action as the first child of the command
                        parseTree.AddChild(child);
                        //discard the saved cursor: we are moving on
                        Scanner.Discard();
                        //nothing more need for an action: success!
                        return true;
                    }

                    //Something followed Action that shouldn't have.
                    //give the player a snarky message.
                    string rest = Scanner.Text[Scanner.Cursor..];
                    _error = $"This made sense up to \"{rest}\"...";
                }
            }

            //if action fails (if Action was called, it would save the
            // cursor, and upon failure, restore the cursor)
            // try the second alternative, in this case, LookCommand.
            if(TokenUtils.IsOneOf(token.TokenName, _first["LookCommand"])){
                ParseTreeNode child = new("LookCommand");
                if(LookCommand(child)){
                    if(Scanner.Scan(_follows["LookCommand"]).Success){
                        parseTree.AddChild(child);
                        Scanner.Discard();
                        return true;
                    }

                    string rest = Scanner.Text[Scanner.Cursor..];
                    _error = $"This made sense up to \"{rest}\"...";
                }
            }

            //Alternative number 3
            if(TokenUtils.IsOneOf(token.TokenName, _first["Direction"])){
                ParseTreeNode child = new("Direction");
                if(Direction(child)){
                    if(Scanner.Scan(_follows["Direction"]).Success){
                        parseTree.AddChild(child);
                        Scanner.Discard();
                        return true;
                    }

                    string rest = Scanner.Text[Scanner.Cursor..];
                    _error = $"This made sense up to \"{rest}\"...";
                }
            }

            //The only remaining alternatives are tokens, not nonterminals.  That can
            //be handled easily.
            if(TokenUtils.IsOneOf(token.TokenName, "<INVENTORY>", "<SCORE>", "<QUIT>",
                "<UP>")){
                //consume the token
                Scanner.Consume(token);
                //add the token to the Command's children
                ParseTreeNode child = new(token.TokenName, token.Value);
                parseTree.AddChild(child);
                //discard the saved cursor and move on
                Scanner.Discard();
                //success!
                return true;
            }

            //None of the alternatives worked.  Restore the cursor to try again.
            //(doesn't apply to command unless subcommands are added so that the
            //Command nonterminal is part of one alternative of several of
            //some other production
            Scanner.Pop();

            //Return false because of failure
            return false;
        }

        //Try to parse an action
        private bool Action(ParseTreeNode node){
            if(DebugMode) Debug.Log("Attempting to parse Action");
            TokenMatch token = Scanner.Scan(_first["Action"]);
            if(!token.Success){
                if(DebugMode) Debug.Log("Failed to parse Action");
                return false;
            }

            if(DebugMode) Debug.Log("While parsing Action: found first token " + token);

            Scanner.Push();
            if(TokenUtils.IsOneOf(token.TokenName, _first["Face"])){
                ParseTreeNode child = new("Face");
                if(Face(child)){
                    if(Scanner.Scan(_follows["Face"]).Success){
                        node.AddChild(child);
                        Scanner.Discard();
                        return true;
                    }

                    string rest = Scanner.Text[Scanner.Cursor..];
                    _error = $"This made sense up to \"{rest}\"...";
                }
            }

            if(TokenUtils.IsOneOf(token.TokenName, _first["Walk"])){
                ParseTreeNode child = new("Walk");
                if(Walk(child)){
                    if(Scanner.Scan(_follows["Walk"]).Success){
                        node.AddChild(child);
                        Scanner.Discard();
                        return true;
                    }

                    string rest = Scanner.Text[Scanner.Cursor..];
                    _error = $"This made sense up to \"{rest}\"...";
                }
            }

            if(TokenUtils.IsOneOf(token.TokenName, _first["Grab"])){
                ParseTreeNode child = new("Grab");
                if(Grab(child)){
                    if(Scanner.Scan(_follows["Grab"]).Success){
                        node.AddChild(child);
                        Scanner.Discard();
                        return true;
                    }

                    string rest = Scanner.Text[Scanner.Cursor..];
                    _error = $"This made sense up to \"{rest}\"...";
                }
            }

            if(TokenUtils.IsOneOf(token.TokenName, "<KILL_WUMPUS>", "<CLIMB>")){
                Scanner.Consume(token);
                ParseTreeNode child = new(token.TokenName, token.Value);
                node.AddChild(child);
                Scanner.Discard();
                return true;
            }

            Scanner.Pop();
            return false;
        }

        //Try to parse look around or describe
        private bool LookCommand(ParseTreeNode node){
            if(DebugMode) Debug.Log("Attempting to parse LookCommand");
            TokenMatch token = Scanner.Scan(_first["LookCommand"]);
            if(!token.Success){
                if(DebugMode) Debug.Log("Failed to parse LookCommand");
                return false;
            }

            if(DebugMode) Debug.Log("While parsing LookCommand: found first token " + token);
            Scanner.Push();
            if(token.TokenName == "<LOOK>"){
                Scanner.Push();
                Scanner.Consume(token);
                TokenMatch token2 = Scanner.Scan(_first["Object"]);
                if(token2.Success){
                    ParseTreeNode child = new("Object");
                    if(Object(child)){
                        if(Scanner.Scan(_follows["Object"]).Success){
                            node.AddChild(new ParseTreeNode(token.TokenName, token.Value));
                            node.AddChild(child);
                            Scanner.Discard();
                            Scanner.Discard();
                            return true;
                        }

                        string rest = Scanner.Text[Scanner.Cursor..];
                        _error = $"This made sense up to \"{rest}\"...";
                    }
                }
                else{
                    token2 = Scanner.Scan("<WORD>");
                    _error = token2.Success
                        ? $"I do not recognize an object called \"{token2.Value}\""
                        : "Look at what?";
                }

                Scanner.Pop();
            }

            if(token.TokenName == "<LOOK_AROUND>" || token.Value.ToLower() == "look" ||
               token.Value.ToLower() == "l"){
                Scanner.Consume(token);
                ParseTreeNode child = new(token.TokenName, token.Value);
                node.AddChild(child);
                Scanner.Discard();
                return true;
            }

            Scanner.Pop();
            return false;
        }

        //Try to parse an object that is an argument to a command
        private bool Object(ParseTreeNode node){
            if(DebugMode) Debug.Log("Attempting to parse Object");
            TokenMatch token = Scanner.Scan(_first["Object"]);
            if(!token.Success) return false;
            Scanner.Push();
            if(TokenUtils.IsOneOf(token.TokenName, _first["Item"])){
                ParseTreeNode child = new("Item");
                if(Item(child)){
                    if(Scanner.Scan(_follows["Action"]).Success){
                        node.AddChild(child);
                        Scanner.Discard();
                        return true;
                    }

                    string rest = Scanner.Text[Scanner.Cursor..];
                    _error = $"This made sense up to \"{rest}\"...";
                }
            }

            if(TokenUtils.IsOneOf(token.TokenName, _first["Actor"])){
                ParseTreeNode child = new("Actor");
                if(Actor(child)){
                    if(Scanner.Scan(_follows["Action"]).Success){
                        node.AddChild(child);
                        Scanner.Discard();
                        return true;
                    }

                    string rest = Scanner.Text[Scanner.Cursor..];
                    _error = $"This made sense up to \"{rest}\"...";
                }
            }

            if(TokenUtils.IsOneOf(token.TokenName, "<PIT>", "<ROOM>")){
                Scanner.Consume(token);
                ParseTreeNode child = new(token.TokenName, token.Value);
                node.AddChild(child);
                Scanner.Discard();
                return true;
            }

            Scanner.Pop();
            return false;
        }

        //Try to parse a face direction command
        private bool Face(ParseTreeNode node){
            if(DebugMode) Debug.Log("Attempting to parse Face");
            TokenMatch token = Scanner.Scan(_first["Face"]);
            if(!token.Success){
                if(DebugMode) Debug.Log("Failed to parse Face");
                return false;
            }

            if(DebugMode) Debug.Log("While parsing Face: found first token " + token);

            Scanner.Push();
            if(token.TokenName == "<FACE>"){
                Scanner.Consume(token);
                TokenMatch token2 = Scanner.Scan(_first["Direction"]);
                if(token2.Success){
                    ParseTreeNode child = new("Direction");
                    if(Direction(child)){
                        node.AddChild(new ParseTreeNode(token.TokenName, token.Value));
                        node.AddChild(child);
                        Scanner.Discard();
                        return true;
                    }
                }
                else{
                    token2 = Scanner.Scan("<WORD>");
                    _error = token2.Success
                        ? $"I don't think \"{token2.Value}\" is a direction"
                        : "face which way?";
                }
            }

            Scanner.Pop();
            return false;
        }

        //Try to parse a walk direction command
        private bool Walk(ParseTreeNode node){
            if(DebugMode) Debug.Log("Attempting to parse Walk");
            TokenMatch token = Scanner.Scan(_first["Walk"]);
            if(!token.Success){
                if(DebugMode) Debug.Log("Failed to parse Walk");
                return false;
            }

            if(DebugMode) Debug.Log("While parsing Walk: found first token " + token);

            Scanner.Push();
            if(token.TokenName == "<WALK>"){
                Scanner.Consume(token);
                TokenMatch token2 = Scanner.Scan(_first["Direction"]);
                if(DebugMode && !token2.Success){
                    Debug.Log("While parsing Walk: failed to find direction first token " +
                              string.Join(", ", _first["Direction"]));
                    Scanner.DebugShowCurrentScanLocation();
                }

                if(token2.Success){
                    ParseTreeNode child = new("Direction");
                    if(Direction(child)){
                        node.AddChild(new ParseTreeNode(token.TokenName, token.Value));
                        node.AddChild(child);
                        Scanner.Discard();
                        return true;
                    }
                }
                else{
                    token2 = Scanner.Scan("<WORD>");
                    _error = token2.Success
                        ? $"I don't think \"{token2.Value}\" is a direction"
                        : "Walk which way?";
                }
            }

            Scanner.Pop();
            return false;
        }

        //Try to parse a grab gold command
        private bool Grab(ParseTreeNode node){
            if(DebugMode) Debug.Log("Attempting to parse Grab");
            TokenMatch token = Scanner.Scan(_first["Grab"]);
            if(!token.Success){
                if(DebugMode) Debug.Log("Failed to parse Grab");
                return false;
            }

            if(DebugMode) Debug.Log("While parsing Grab: found first token " + token);
            Scanner.Push();
            Scanner.Consume(token);
            if(DebugMode) Debug.Log("Attempting to scan Gold");
            TokenMatch token2 = Scanner.Scan("<GOLD>");
            if(token2.Success){
                Scanner.Consume(token2);
                if(DebugMode) Debug.Log("Found Gold token " + token2);
                node.AddChild(new ParseTreeNode(token.TokenName, token.Value));
                node.AddChild(new ParseTreeNode(token2.TokenName, token2.Value));
                Scanner.Discard();
                return true;
            }

            Scanner.Push();
            if(Object(node)){
                Scanner.Pop();
                token2 = Scanner.Scan("<WORD>");
                _error = token2.Success
                    ? $"I do not see any \"{token2.Value}\", but then it is very dark."
                    : "Pick up what? Gold?";
            }
            else{
                Scanner.Pop();
                token2 = Scanner.Scan("<WORD>");
                _error = token2.Success
                    ? $"I don't know how to pick up \"{token2.Value}\""
                    : "Pick up what? Gold?";
            }

            if(DebugMode) Debug.Log("Failed to scan Gold");
            Scanner.Pop();
            return false;
        }

        //Try to parse a direction
        private bool Direction(ParseTreeNode node){
            if(DebugMode) Debug.Log("Attempting to parse Direction");
            TokenMatch token = Scanner.Scan(_first["Direction"]);
            if(!token.Success){
                if(DebugMode) Debug.Log("Failed to parse direction.");
                return false;
            }

            Scanner.Consume(token);
            node.AddChild(new ParseTreeNode(token.TokenName, token.Value));
            return true;
        }

        //Try to parse an item (bow, arrow, gold)
        private bool Item(ParseTreeNode node){
            if(DebugMode) Debug.Log("Attempting to parse Item");
            TokenMatch token = Scanner.Scan(_first["Item"]);
            if(!token.Success){
                if(DebugMode) Debug.Log("Failed to parse Item.");
                return false;
            }

            Scanner.Consume(token);

            node.AddChild(new ParseTreeNode(token.TokenName, token.Value));
            return true;
        }

        //Try to parse an actor (player, wumpus)
        private bool Actor(ParseTreeNode node){
            if(DebugMode) Debug.Log("Attempting to parse Actor");
            TokenMatch token = Scanner.Scan(_first["Actor"]);
            if(!token.Success) return false;
            Scanner.Consume(token);
            node.AddChild(new ParseTreeNode(token.TokenName, token.Value));
            return true;
        }
    }
}
