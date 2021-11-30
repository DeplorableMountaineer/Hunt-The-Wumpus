#region

using System;
using System.Collections;
using Deplorable_Mountaineer.Parser;
using Deplorable_Mountaineer.Parser.ParseTree;
using Deplorable_Mountaineer.Parser.Tokens;
using UnityEngine;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

#endregion

namespace Deplorable_Mountaineer.Hunt_The_Wumpus {
    /// <summary>
    ///     The original Hunt the Wumpus was written in BASIC and can be found here:
    ///     http://web.archive.org/web/20100428041109/http://www.atariarchives.org/bcc1/showpage.php?page=247
    ///     This is the command executor for Hunt the Wumpus.
    /// </summary>
    public class WumpusCommands : MonoBehaviour {
        /// <summary>
        ///     The tokens used by the parser
        /// </summary>
        public static readonly TokenDefinitionLanguage TokenDefinitionLanguage = new();

        /// <summary>
        ///     Maintains the state of the game (where the player is, is the wumpus dead,
        ///     does the player have an arrow, etc.)
        /// </summary>
        [SerializeField] private WumpusState wumpusState;

        /// <summary>
        ///     The parser used to interpret commands this class executes
        /// </summary>
        public static WumpusParser Parser { get; set; }

        private IEnumerator Start(){
            //wait for other things to init
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            //Start by describing the current room
            LookAround();
        }

        /// <summary>
        ///     Have the Console's "On Console Input" inspector property call this
        ///     to process commands typed in
        /// </summary>
        public void ProcessConsoleInput(){
            while(Console.Console.Instance.NumUnread > 0){
                string text = Console.Console.Instance.Get();
                ProcessInput(text);
            }
        }

        /// <summary>
        ///     Processes the actual console input
        /// </summary>
        /// <param name="text">The console input</param>
        public void ProcessInput(string text){
            string[] words = text.Trim().ToLowerInvariant()
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if(words.Length == 0) return;

            //in-console commands, not part of the game
            switch(words[0]){
                case "history" when words.Length == 1:
                    Console.Console.Instance.ShowHistory();
                    break;
                default:
                    //now, compile the command and try to interpret it as a game commaned
                    if(Parser.Compile(text)){
                        EvalCommand(Parser.ParseTree.Children[0]);
                        if(wumpusState.IsPitInRoom || wumpusState.IsLiveWumpusInRoom) Die();

                        Debug.Log("Parse succeeded.");
                        Debug.Log(Parser.ParseTree.Decompile());
                    }

                    break;
            }
        }

        /// <summary>
        ///     Evaluate a compiled command.  The compiled command is a parse
        ///     tree whose root node is a "Command" nonterminal.  See ConsoleGrammar.bnf.txt
        ///     for the structure of the grammar.
        /// </summary>
        /// <param name="compiledCommand">The command to evaluate</param>
        public void EvalCommand(ParseTreeNode compiledCommand){
            if(compiledCommand.IsNonterminal)
                switch(compiledCommand.Nonterminal){
                    case "Action":
                        ProcessAction(compiledCommand.Children[0]);
                        return;
                    case "LookCommand":
                        ProcessLookCommand(compiledCommand);
                        return;
                    case "Direction":
                        ProcessDirection(compiledCommand.Children[0]);
                        return;
                    default:
                        //This message means there is an error in the code
                        //itself, and should never appear.
                        Console.Console.Instance.SayHighlighted(
                            "Internal Error: invalid nonterminal for Command: " +
                            compiledCommand.Nonterminal);
                        return;
                }

            switch(compiledCommand.TokenName){
                case "<INVENTORY>":
                    Inventory();
                    return;
                case "<SCORE>":
                    ShowScore();
                    return;
                case "<QUIT>":
                    QuitGame();
                    return;
                case "<UP>":
                    Climb();
                    return;
                default:
                    //This message means there is an error in the code
                    //itself, and should never appear.
                    Console.Console.Instance.SayHighlighted(
                        "Internal Error: invalid token for Command: " +
                        compiledCommand.TokenName);
                    return;
            }
        }

        /// <summary>
        ///     Process an action that can change the state of the game
        /// </summary>
        /// <param name="node">The Action nonterminal parse tree node</param>
        private void ProcessAction(ParseTreeNode node){
            if(node.IsNonterminal)
                switch(node.Nonterminal){
                    case "Face":
                        Face(node);
                        return;
                    case "Walk":
                        Walk(node);
                        return;
                    case "Grab":
                        Grab();
                        return;
                    default:
                        //This message means there is an error in the code
                        //itself, and should never appear.
                        Console.Console.Instance.SayHighlighted(
                            "Internal Error: invalid nonterminal for Action: " +
                            node.Nonterminal);
                        return;
                }

            switch(node.TokenName){
                case "<KILL_WUMPUS>":
                    Shoot();
                    return;
                case "<CLIMB>":
                    Climb();
                    return;
                default:
                    //This message means there is an error in the code
                    //itself, and should never appear.
                    Console.Console.Instance.SayHighlighted(
                        "Internal Error: invalid token for Action: " +
                        node.TokenName);
                    return;
            }
        }

        /// <summary>
        ///     Try to shoot the wumpus.
        /// </summary>
        private void Shoot(){
            bool wasWumpusAlive = wumpusState.IsWumpusAlive;
            if(!wumpusState.Shoot()){
                if(!wumpusState.DoesPlayerHaveArrow){
                    Console.Console.Instance.Say(
                        "The bow makes a useless \"twang\" sound.  It would be better if it had an arrow.");
                    return;
                }

                Console.Console.Instance.Say("twang..g..g..g..g");
                return;
            }

            if(!wumpusState.IsWumpusAlive && wasWumpusAlive){
                Console.Console.Instance.Say(
                    "thhhwiipppppwangangangangang... and you hear the scream of a dying wumpus.");
                return;
            }

            Console.Console.Instance.Say(
                "The arrow wooshes through the air, straight and true, and nothing obstructs it.");
        }

        /// <summary>
        ///     Face the direction in the second child of the node
        /// </summary>
        /// <param name="node">The Face nonterminal node</param>
        private void Face(ParseTreeNode node){
            if(node.Children[1].Nonterminal != "Direction"){
                //This message means there is an error in the code
                //itself, and should never appear.
                Console.Console.Instance.SayHighlighted(
                    "Internal Error: invalid argument for Face: " +
                    node);
                return;
            }

            switch(node.Children[1].Children[0].TokenName){
                case "<NORTH>":
                    wumpusState.FaceNorth();
                    Console.Console.Instance.Say("You are now facing north.");
                    return;
                case "<SOUTH>":
                    wumpusState.FaceSouth();
                    Console.Console.Instance.Say("You are now facing south.");
                    return;
                case "<EAST>":
                    wumpusState.FaceEast();
                    Console.Console.Instance.Say("You are now facing east.");
                    return;
                case "<WEST>":
                    wumpusState.FaceWest();
                    Console.Console.Instance.Say("You are now facing west.");
                    return;
                default:
                    Console.Console.Instance.SayHighlighted(
                        //This message means there is an error in the code
                        //itself, and should never appear.
                        "Internal Error: invalid token for Direction: " +
                        node.TokenName);
                    return;
            }
        }

        /// <summary>
        ///     Walk in the direction in the second child of the node
        /// </summary>
        /// <param name="node">The Face nonterminal node</param>
        private void Walk(ParseTreeNode node){
            if(node.Children[1].Nonterminal != "Direction"){
                Console.Console.Instance.SayHighlighted(
                    //This message means there is an error in the code
                    //itself, and should never appear.
                    "Internal Error: invalid argument for Walk: " +
                    node);
                return;
            }

            ProcessDirection(node.Children[1].Children[0]);
        }

        /// <summary>
        ///     Grab the gold
        /// </summary>
        private void Grab(){
            if(wumpusState.TakeGold()){
                Console.Console.Instance.Say("You have the gold! Now find the exit.");
                return;
            }

            if(wumpusState.DoesPlayerHaveGold){
                Console.Console.Instance.Say(
                    "That would be a clever trick, since you already have it.");
                return;
            }

            Console.Console.Instance.Say("No treasures here....");
        }

        /// <summary>
        ///     Process the command to describe an object or the current room
        /// </summary>
        /// <param name="node">The look token node</param>
        private void ProcessLookCommand(ParseTreeNode node){
            switch(node.Children[0].TokenName){
                case "<LOOK>":
                    if(node.Children.Count == 1) LookAround();
                    else Describe(node.Children[1]);
                    return;
                case "<LOOK_AROUND>":
                    LookAround();
                    return;
                default:
                    Console.Console.Instance.SayHighlighted(
                        //This message means there is an error in the code
                        //itself, and should never appear.
                        "Internal Error: invalid token for Direction: " +
                        node.TokenName);
                    return;
            }
        }

        /// <summary>
        ///     Kill the player, show score, and start a new game.
        /// </summary>
        private void Die(){
            if(Random.value > .95f)
                Console.Console.Instance.Say(
                    "You find yourself falling toward a fiery pit and hear screams of the damned below you.");
            else
                Console.Console.Instance.Say(
                    "You see your life flashing before your eyes.  There is a long dark tunnel with a light at the end.");

            wumpusState.Die();
            ShowScore();
            StartNewGame();
        }

        /// <summary>
        ///     Describe the object in the node
        /// </summary>
        /// <param name="node">The Object nonterminal node</param>
        private void Describe(ParseTreeNode node){
            if(node.Nonterminal != "Object"){
                Console.Console.Instance.SayHighlighted(
                    //This message means there is an error in the code
                    //itself, and should never appear.
                    "Internal Error: invalid Object: " +
                    node);
                return;
            }

            if(node.Children[0].IsNonterminal)
                switch(node.Children[0].Nonterminal){
                    case "Item":
                        DescribeItem(node.Children[0].Children[0]);
                        return;
                    case "Actor":
                        DescribeActor(node.Children[0].Children[0]);
                        return;
                    default:
                        Console.Console.Instance.SayHighlighted(
                            //This message means there is an error in the code
                            //itself, and should never appear.
                            "Internal Error: invalid nonterminal for Object: " +
                            node.Children[0].Nonterminal);
                        return;
                }

            switch(node.Children[0].TokenName){
                case "<PIT>":
                    DescribePit();
                    return;
                case "<ROOM>":
                    LookAround();
                    return;
                default:
                    Console.Console.Instance.SayHighlighted(
                        //This message means there is an error in the code
                        //itself, and should never appear.
                        "Internal Error: invalid token for Object: " +
                        node.Children[0].TokenName);
                    return;
            }
        }


        /// <summary>
        ///     Describe the item token in the node
        /// </summary>
        /// <param name="node">A token node for an item</param>
        private void DescribeItem(ParseTreeNode node){
            switch(node.TokenName){
                case "<BOW>":
                    DescribeBow();
                    return;
                case "<ARROW>":
                    DescribeArrow();
                    return;
                case "<GOLD>":
                    DescribeGold();
                    return;
                default:
                    Console.Console.Instance.SayHighlighted(
                        //This message means there is an error in the code
                        //itself, and should never appear.
                        "Internal Error: invalid token for Item: " +
                        node.TokenName);
                    return;
            }
        }

        /// <summary>
        ///     Describe the bow
        /// </summary>
        private void DescribeBow(){
            if(wumpusState.DoesPlayerHaveArrow)
                Console.Console.Instance.Say(
                    "This is your trusty, deadly, wooden bow and arrow, suitable for shooting wumpuses.");
            else
                Console.Console.Instance.Say(
                    "This is your trusty, deadly, wooden bow, which would be suitable for shooting wumpuses if only you had an arrow as well.");
        }

        /// <summary>
        ///     describe the arrow
        /// </summary>
        private void DescribeArrow(){
            if(!wumpusState.DoesPlayerHaveArrow){
                Console.Console.Instance.Say("What arrow?");
                return;
            }

            Console.Console.Instance.Say(
                "This is your trusty, deadly, straight wooden arrow, which would be suitable for killing a wumpus if fired from a bow.");
        }

        /// <summary>
        ///     describe the gold
        /// </summary>
        private void DescribeGold(){
            if(!wumpusState.DoesPlayerHaveGold && !wumpusState.IsGoldInRoom){
                Console.Console.Instance.Say("No treasures to be found here...");
                return;
            }

            Console.Console.Instance.Say(
                "It is a large, massive wooden chest of bright, shiny, glittering, and very valuable gold.");
        }

        /// <summary>
        ///     Describe the actor token in the node
        /// </summary>
        /// <param name="node">A token node for an actor</param>
        private void DescribeActor(ParseTreeNode node){
            switch(node.TokenName){
                case "<PLAYER>":
                    DescribePlayer();
                    return;
                case "<WUMPUS>":
                    DescribeWumpus();
                    return;
                default:
                    Console.Console.Instance.SayHighlighted(
                        //This message means there is an error in the code
                        //itself, and should never appear.
                        "Internal Error: invalid token for Actor: " +
                        node.TokenName);
                    return;
            }
        }

        /// <summary>
        ///     Describe the player
        /// </summary>
        private void DescribePlayer(){
            if(wumpusState.IsPlayerDead){
                Console.Console.Instance.Say("You are a ghost.");
                return;
            }

            Console.Console.Instance.Say(
                "You are a cool, brave hero who kills wumpuses and collects gold.");
        }

        /// <summary>
        ///     Describe the wumpus
        /// </summary>
        private void DescribeWumpus(){
            if(!wumpusState.IsWumpusInRoom){
                Console.Console.Instance.Say("There is no wumpus in sight, though " +
                                             "you do know that they stink.");
                return;
            }

            Console.Console.Instance.Say(
                "The wumpus is an evil, smelly, fat, green, ugly, toothy, scary, " +
                "man-eating monster with suckers for feet and venomous fangs surrounding " +
                "its gaping mouth.  It fills most of the space available in this cavern.");
        }

        /// <summary>
        ///     Describe the pit
        /// </summary>
        private void DescribePit(){
            if(!wumpusState.IsPitInRoom){
                Console.Console.Instance.Say("There is no pit in sight, but you " +
                                             "do know it is bottomless and produces a damp breeze.  " +
                                             "You know you have reached the pit if suddenly there " +
                                             "is nothing beneath your feet.");
                return;
            }

            Console.Console.Instance.Say(
                "It is a treacherous, bottomless abyss with a damp breeze coming out of it. " +
                "But you cannot see this for yourself because of the lack of light.");
        }

        /// <summary>
        ///     Describe the current location
        /// </summary>
        private void LookAround(){
            //these first two cases probably will never happen in the game unless it is modified
            if(wumpusState.IsPlayerDead){
                Console.Console.Instance.Say("You see a bright light.  Go into the light.");
                return;
            }

            if(!wumpusState.IsPlayerInCaves){
                Console.Console.Instance.Say(
                    "You are standing at a cave entrance that appears to go down deep.");
                return;
            }

            //Describe cavern of the cave
            Console.Console.Instance.Say(
                $"You are in a system of caves.  If you didn't miscount, you are {GetUserFriendlyLocation()}.  You are facing {GetDirection()}.");

            //if gold in room
            if(wumpusState.IsGoldInRoom)
                Console.Console.Instance.Say(
                    "The brilliance of the shiny gold in the wooden chest is almost blinding after you have become used to the darkness.");

            //if you share a room with a very large, dead wumpus
            if(wumpusState.IsDeadWumpusInRoom)
                Console.Console.Instance.Say(
                    "There is a dead, stinky wumpus filling up most of the room.");

            //should never happen because the wumpus kills you immediately
            if(wumpusState.IsLiveWumpusInRoom)
                Console.Console.Instance.Say(
                    "There is a live, mean, smelly wumpus in the room with you!");

            //should never happen, because you fall immediately
            if(wumpusState.IsPitInRoom)
                Console.Console.Instance.Say(
                    "Your footing disappears from under you unexpectedly.");

            //describe the smell
            if(wumpusState.IsWumpusNearby())
                Console.Console.Instance.Say("You smell a strong stench.");

            //describe the feel
            if(wumpusState.IsPitNearby())
                Console.Console.Instance.Say("You feel a cool, damp breeze.");
        }

        /// <summary>
        ///     Get the name of the direction player is facing
        /// </summary>
        /// <returns></returns>
        private string GetDirection(){
            if(wumpusState.PlayerFacing == Vector2Int.up) return "north";
            if(wumpusState.PlayerFacing == Vector2Int.down) return "south";
            if(wumpusState.PlayerFacing == Vector2Int.right) return "east";
            if(wumpusState.PlayerFacing == Vector2Int.left) return "west";
            return "...I don't know, no sense of direction...";
        }

        /// <summary>
        ///     Get a string describing the player's location
        /// </summary>
        /// <returns></returns>
        private string GetUserFriendlyLocation(){
            string result = "";
            if(wumpusState.PlayerLocation.x <= wumpusState.GridSizeX/2)
                result += $"{wumpusState.PlayerLocation.x} caverns east of the exit and ";
            else
                result +=
                    $"{wumpusState.GridSizeX - wumpusState.PlayerLocation.x} caverns west of the exit and ";

            if(wumpusState.PlayerLocation.y <= wumpusState.GridSizeY/2)
                result += $"{wumpusState.PlayerLocation.y} caverns north of the exit";
            else
                result +=
                    $"{wumpusState.GridSizeY - wumpusState.PlayerLocation.y} caverns south of the exit";

            return result;
        }

        /// <summary>
        ///     Walk in the direction specified by the node's token
        /// </summary>
        /// <param name="node">A direction token node</param>
        private void ProcessDirection(ParseTreeNode node){
            switch(node.TokenName){
                case "<NORTH>":
                    GoNorth();
                    LookAround();
                    return;
                case "<SOUTH>":
                    GoSouth();
                    LookAround();
                    return;
                case "<EAST>":
                    GoEast();
                    LookAround();
                    return;
                case "<WEST>":
                    GoWest();
                    LookAround();
                    return;
                default:
                    Console.Console.Instance.SayHighlighted(
                        //This message means there is an error in the code
                        //itself, and should never appear.
                        "Internal Error: invalid token for Direction: " +
                        node.TokenName);
                    return;
            }
        }

        /// <summary>
        ///     walk one room north
        /// </summary>
        private void GoNorth(){
            wumpusState.MoveNorth();
        }

        /// <summary>
        ///     walk one room south
        /// </summary>
        private void GoSouth(){
            wumpusState.MoveSouth();
        }

        /// <summary>
        ///     walk one roof east
        /// </summary>
        private void GoEast(){
            wumpusState.MoveEast();
        }

        /// <summary>
        ///     walk one room west
        /// </summary>
        private void GoWest(){
            wumpusState.MoveWest();
        }

        /// <summary>
        ///     Display player's inventory.
        /// </summary>
        private void Inventory(){
            string result = "";
            if(wumpusState.DoesPlayerHaveGold) result += "    A chest of gold\n";
            if(wumpusState.DoesPlayerHaveArrow) result += "    A bow with arrow\n";
            else result += "    A bow with no arrow\n";
            Console.Console.Instance.Say("You have:\n" + result);
        }

        /// <summary>
        ///     Display the current score
        /// </summary>
        private void ShowScore(){
            Console.Console.Instance.Say($"Your current score is {wumpusState.Score} " +
                                         $"from {wumpusState.NumGames} games.");
        }

        /// <summary>
        ///     Quit the game
        /// </summary>
        private void QuitGame(){
            Application.Quit();
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#endif
        }

        /// <summary>
        ///     exit the caves if possible
        /// </summary>
        private void Climb(){
            if(!wumpusState.Exit()){
                Console.Console.Instance.Say("There is no exit from the caves here.");
                return;
            }

            ShowScore();
            StartNewGame();
        }

        /// <summary>
        ///     Start a new game (but don't erase the score)
        /// </summary>
        private void StartNewGame(){
            wumpusState.Init();
            Console.Console.Instance.SayHighlighted(
                "Welcome to Hunt the Wumpus!  Escape with the gold without dying! What now?");
            LookAround();
        }
    }
}
