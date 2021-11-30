#region

using System;
using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Hunt_The_Wumpus {
    /// <summary>
    ///     The original Hunt the Wumpus was written in BASIC and can be found here:
    ///     http://web.archive.org/web/20100428041109/http://www.atariarchives.org/bcc1/showpage.php?page=247
    ///     This is the current game state.
    /// </summary>
    public class WumpusState : MonoBehaviour {
        /// <summary>
        ///     Number of rows (west to east) in grid
        /// </summary>
        [SerializeField] private int gridSizeX = 5;

        /// <summary>
        ///     Number of columns (south to north) in grid
        /// </summary>
        [SerializeField] private int gridSizeY = 5;

        /// <summary>
        ///     Represents an invalid location or direction
        /// </summary>
        private readonly Vector2Int _invalid = new(int.MinValue, int.MinValue);

        /// <summary>
        ///     Where the player is now
        /// </summary>
        private Vector2Int _playerLocation;

        /// <summary>
        ///     Number of times exited with gold minus number of times killed
        /// </summary>
        public int RawScore { get; set; }

        /// <summary>
        ///     Total number of times killed or exited (with or without gold).
        /// </summary>
        public int NumGames { get; set; }

        /// <summary>
        ///     Current score
        /// </summary>
        public int Score => Mathf.CeilToInt((float)RawScore/NumGames*1000);

        /// <summary>
        ///     Wumpus location this game
        /// </summary>
        public Vector2Int WumpusLocation { get; private set; }

        /// <summary>
        ///     Gold location this game; if invalid, player has it
        /// </summary>
        public Vector2Int GoldLocation { get; private set; }

        /// <summary>
        ///     Locations of the two pits this game
        /// </summary>
        public Vector2Int[] PitLocations { get; } = new Vector2Int[2];

        /// <summary>
        ///     Where the player is now; invalid if not in the caves or dead
        /// </summary>
        public Vector2Int PlayerLocation => _playerLocation;

        /// <summary>
        ///     Which way the player is facing; invalid if dead
        /// </summary>
        public Vector2Int PlayerFacing { get; private set; }

        /// <summary>
        ///     True if player still has an arrow
        /// </summary>
        public bool DoesPlayerHaveArrow { get; private set; }

        /// <summary>
        ///     True if wumpus is still alive and hungry.
        /// </summary>
        public bool IsWumpusAlive { get; private set; }

        /// <summary>
        ///     True if player is in the room with the gold (but doesn't have it)
        /// </summary>
        public bool IsGoldInRoom =>
            GoldLocation == PlayerLocation && PlayerLocation != _invalid;

        /// <summary>
        ///     True if player has the gold
        /// </summary>
        public bool DoesPlayerHaveGold => GoldLocation == _invalid;

        /// <summary>
        ///     True if player is in a room with a pit
        /// </summary>
        public bool IsPitInRoom =>
            PlayerLocation == PitLocations[0] || PlayerLocation == PitLocations[1];

        /// <summary>
        ///     True if player is in a room with a wumpus, dead or alive
        /// </summary>
        public bool IsWumpusInRoom => PlayerLocation == WumpusLocation;

        /// <summary>
        ///     True if player is in a room with a live wumpus
        /// </summary>
        public bool IsLiveWumpusInRoom => IsWumpusAlive && IsWumpusInRoom;

        /// <summary>
        ///     True if player is in a room with a dead wumpus
        /// </summary>
        public bool IsDeadWumpusInRoom => !IsWumpusAlive && IsWumpusInRoom;

        /// <summary>
        ///     True if player is in the cave system
        /// </summary>
        public bool IsPlayerInCaves => PlayerLocation != _invalid;

        /// <summary>
        ///     True if player is dead
        /// </summary>
        public bool IsPlayerDead => PlayerFacing == _invalid;

        /// <summary>
        ///     Number of rows (west to east) in grid
        /// </summary>
        public int GridSizeX => gridSizeX;

        /// <summary>
        ///     Number of columns (south to north) in grid
        /// </summary>
        public int GridSizeY => gridSizeY;

        /// <summary>
        ///     Called automatically when the game begins
        /// </summary>
        private void Start(){
            Init();
        }

        /// <summary>
        ///     Turn player to face north
        /// </summary>
        public void FaceNorth(){
            PlayerFacing = Vector2Int.up;
        }

        /// <summary>
        ///     Turn player to face south
        /// </summary>
        public void FaceSouth(){
            PlayerFacing = Vector2Int.down;
        }

        /// <summary>
        ///     Turn player to face east
        /// </summary>
        public void FaceEast(){
            PlayerFacing = Vector2Int.right;
        }

        /// <summary>
        ///     Turn player to face west
        /// </summary>
        public void FaceWest(){
            PlayerFacing = Vector2Int.left;
        }

        /// <summary>
        ///     Move in direction facing
        /// </summary>
        public void Move(){
            _playerLocation = ComputeMotion();
        }

        /// <summary>
        ///     Face north then move north one room
        /// </summary>
        public void MoveNorth(){
            FaceNorth();
            Move();
        }

        /// <summary>
        ///     Face south then move south one room
        /// </summary>
        public void MoveSouth(){
            FaceSouth();
            Move();
        }

        /// <summary>
        ///     Face east then move east one room
        /// </summary>
        public void MoveEast(){
            FaceEast();
            Move();
        }

        /// <summary>
        ///     Face west then move west one room
        /// </summary>
        public void MoveWest(){
            FaceWest();
            Move();
        }

        /// <summary>
        ///     Shoot if possible
        /// </summary>
        /// <returns>true if successful</returns>
        public bool Shoot(){
            if(!DoesPlayerHaveArrow) return false;
            DoesPlayerHaveArrow = false;
            int maxNumSteps = GridSizeX/2;
            if(PlayerFacing.y != 0) maxNumSteps = GridSizeY/2;
            for(int numSteps = 1; numSteps <= maxNumSteps; numSteps++){
                if(ComputeMotion(numSteps) != WumpusLocation) continue;
                IsWumpusAlive = false;
                break;
            }

            return true;
        }

        /// <summary>
        ///     Take gold if possible
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TakeGold(){
            if(!PlayerLocation.Equals(GoldLocation)) return false;
            GoldLocation = _invalid;
            return true;
        }

        /// <summary>
        ///     Cause player to die
        /// </summary>
        public void Die(){
            RawScore--;
            _playerLocation = _invalid;
            PlayerFacing = _invalid;
        }

        /// <summary>
        ///     Try to exit cave system
        /// </summary>
        /// <returns>true if successful</returns>
        public bool Exit(){
            if(!PlayerLocation.Equals(Vector2Int.zero)) return false;
            _playerLocation = _invalid;
            if(DoesPlayerHaveGold) RawScore++;
            return true;
        }

        /// <summary>
        ///     Initialize the game state for a new game (without affecting the raw score)
        /// </summary>
        public void Init(){
            NumGames++;
            DoesPlayerHaveArrow = true; // start with one arrow
            IsWumpusAlive = true; // new live wumpus every game
            PlayerFacing = new Vector2Int(1, 0); // start facing east
            _playerLocation = Vector2Int.zero; // start in the cave with the exit
            WumpusLocation = Random(PlayerLocation); // put a wumpus somewhere
            GoldLocation = Random(PlayerLocation, WumpusLocation); // put the gold somewhere

            //Put two pits somewhere
            PitLocations[0] = Random(PlayerLocation, WumpusLocation, GoldLocation);
            PitLocations[1] = Random(PlayerLocation, WumpusLocation, GoldLocation,
                PitLocations[0]);
        }

        /// <summary>
        ///     Determine if wumpus is in an adjacent room (but not the same room) as the player
        /// </summary>
        /// <returns>true if wumpus is near</returns>
        public bool IsWumpusNearby(){
            foreach(Vector2Int direction in new[]
                { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                if(ComputeMotion(direction) == WumpusLocation)
                    return true;

            return false;
        }

        /// <summary>
        ///     Determine if there is a pit in an adjacent room (but not the same room) as the player
        /// </summary>
        /// <returns>true if a pit is near</returns>
        public bool IsPitNearby(){
            foreach(Vector2Int direction in new[]
                { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right }){
                if(ComputeMotion(direction) == PitLocations[0]) return true;
                if(ComputeMotion(direction) == PitLocations[1]) return true;
            }

            return false;
        }

        /// <summary>
        ///     Compute the resulting location of moving the specified number of steps
        ///     in the specified direction from the player's current location
        /// </summary>
        /// <param name="direction">Direction to move</param>
        /// <param name="numSteps">Number of rooms to move through</param>
        /// <returns></returns>
        private Vector2Int ComputeMotion(Vector2Int direction, int numSteps = 1){
            int x = ((_playerLocation.x + direction.x*numSteps)%GridSizeX + GridSizeX)%
                    GridSizeX;
            int y = ((_playerLocation.y + direction.y*numSteps)%GridSizeY + GridSizeY)%
                    GridSizeY;
            return new Vector2Int(x, y);
        }

        /// <summary>
        ///     Compute motion for the specified number of steps in the direction
        ///     the player is facing
        /// </summary>
        /// <param name="numSteps">Number of rooms to move through</param>
        /// <returns></returns>
        private Vector2Int ComputeMotion(int numSteps = 1){
            return ComputeMotion(PlayerFacing, numSteps);
        }

        /// <summary>
        ///     Compute a random location, avoiding "taken" locations
        /// </summary>
        /// <param name="avoid">Locations to avoid</param>
        /// <returns></returns>
        private Vector2Int Random(params Vector2Int[] avoid){
            Vector2Int result;
            do{
                result = new Vector2Int(
                    UnityEngine.Random.Range(0, GridSizeX), UnityEngine.Random.Range(0,
                        GridSizeY));
            } while(Array.IndexOf(avoid, result) >= 0);

            return result;
        }
    }
}
