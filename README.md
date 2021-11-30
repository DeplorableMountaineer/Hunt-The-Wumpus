# Hunt-The-Wumpus
A text-based adventure based on the game of the same name written by Gregory Yob in 1974

<h1>Hunt the Wumpus </h1>
<p><img src="https://img.itch.zone/aW1nLzc1NDM1ODYucG5n/original/wHI5sE.png" alt="A Wumpus (concept)" title="A Wumpus (concept)"></p>
<figcaption>The wumpus has sucker feet and a huge mouth with venomous mandibles</figcaption>
<p></p>
<p>A text-based adventure based on the game of the same name written by Gregory Yob in 1974. &nbsp; <br><br>See <a href="http://web.archive.org/web/20100428041109/http://www.atariarchives.org/bcc1/showpage.php?page=247">http://web.archive.org/web/20100428041109/http://www.atariarchives.org/bcc1/show...</a> for the original game, written in BASIC. &nbsp;
Hunt the Wumpus was originally played on the vertices of a dodecahedron.&nbsp;&nbsp; Thus the cave system had 20
caverns, and each cavern was adjacent to three other caverns.&nbsp;&nbsp;</p>
<h2>The Game </h2>
<p>
This version is more similar to what is described in <em>Artificial Intelligence: A Modern Approach</em>, 4th US ed. (http://aima.cs.berkeley.edu/) by Stuart Russell and Peter Norvig.&nbsp; The game is played
on a 5x5 grid, and thus there are 25 caverns, each one adjacent to four other caverns.&nbsp; The grid
wraps around like a torus, similar to the classic arcade game Asteroids.&nbsp;  Thus, if you move 5 rooms in any direction, you return to your starting point.
&nbsp; <br></p>
<p>
In the game, you can move East, West, North, or South.&nbsp; If you are in the starting room, you can also climb up to exit the cave, ending the game.  The game can be played multiple times and the score
is based on the average score of games.  If you exit the game without the the gold, you get zero points.
If you exit with the gold, you get 1000 points (with the total points divided by the number of games played).  If you get killed somewhere in the caves, you lose 1000 points. <br></p>
<p>Each time the game is played, a chest of gold is to be found in a random grid square, but not at the starting point.&nbsp;  A wumpus (the evil smelly
deadly man-eating monster) is in a random square, but not the same as the gold or the starting point.&nbsp; Two additional squares (not the ones with the wumpus or gold or starting point) have bottomless
pits in them.&nbsp; <br></p>
<p>You have a bow with one arrow.&nbsp; You can face East, West, North, or South, and shoot the arrow.&nbsp;  It will
fly up to two squares in the direction you face.&nbsp;  If it hits the wumpus, it screams audibly and dies.&nbsp; If not, you have lost the arrow forever and the wumpus lives.&nbsp;&nbsp; Note that if you walk in any direction,
you will be facing that direction when you stop walking.</p>
<p>
If you are in the room with the gold, it will glitter so brightly you can see it in the dark cave.&nbsp; You can then pick up the chest of gold and make for the exit to get your 1000 points.&nbsp; If you enter the room with a live wumpus, it will eat you and, of course, you will die, ending the game.&nbsp;
If the wumpus is dead, you will not die, but it will stink in the room.&nbsp; If you enter either room with a pit, you will fall into the pit (since you cannot see it) and
die of boredom on the way down.&nbsp; You cannot see much, but you do get some warning. &nbsp; <br></p>
<p>When you are in a square adjacent to a wumpus,
you smell his stench (whether it is alive or dead).&nbsp; You then know it is in one of the four
adjacent squares, but not which one (though of course, you can eliminate the square you entered from).&nbsp; If you are in a square adjacent to a pit, you will feel a cool, damp breeze from air being sucked into
what is essentially a vacuum. <br></p>
<h2>Strategy </h2>
<p>To play the game effectively, you should draw a map. &nbsp; Just draw a big square, draw two lines to divide it vertically and horizontally, then draw Eight more lines: two each dividing the vertical halves into thirds, and two each dividing the horizontal halves into thirds.&nbsp; You now have a 5x5 grid.&nbsp; The start (top left is a good choice) can be marked with an X to indicate it is the exit.
As you explore, write B in any square in which you feel a breeze, and S in any square in which you smell a stench.  Write E in a square with neither.  It is sometimes possible to deduce the location
or direction of the wumpus or pits, or at least deduce that various squares in certain directions are
safe. <br></p>
<p>So, to the extent possible, explore safe squares, and if you know which way the wumpus is
and know you are within two squares of it, shoot it .  If you can do this, you will eventually find
the gold and take a safe path back to the start. <br></p>
<p>It doesn't always work so nicely!&nbsp;  Sometimes there are no safe unexplored spaces left.&nbsp; Then you must make a decision:  is one direction more likely to be safe than another?&nbsp; Is the probability of it being
deadly low enough you can risk going that way?&nbsp; If not, you might simply backtrack and exit the cave without the gold, scoring zero points, but also losing nothing.&nbsp; Now you see why the game was a subject in an artificial intelligence textbook: a useful AI exercise is
to devise the optional strategy for maximizing the score over several games. <br></p>
<h2>The User Interface </h2>
<p><img src="https://img.itch.zone/aW1nLzc1NDM2OTMucG5n/original/%2Fzah7X.png"></p>
<p>This is a text-based game.&nbsp; The parser is a bit more sophisticated than simple two-word command parsers that understand "take gold" or "look wumpus".&nbsp; You can type more complex commands just to test if the interpreter can figure it out.&nbsp; Some commands
to try: <br></p>
<ul><li>
Walk towards the east </li><li>Face northward </li><li>
Pick up the wooden chest of shiny gold </li><li>Describe the smelly wumpus&nbsp;</li><li>Climb up </li><li>Quit this game</li><li>&nbsp;Show inventory </li><li>I </li><li>Fire your arrow at that wumpus</li><li>&nbsp;Show the score </li><li>Turn west </li><li>Shoot the bow</li><li>&nbsp;Take the gold </li><li>Look around</li><li>&nbsp;Look at the gold </li><li>Attack the wumpus</li><li>&nbsp;Describe the dank cavern of the cave</li><li>&nbsp;Go up </li><li>South </li><li>S </li><li>L bow </li></ul>
<p>(note that capitalization does not matter). <br></p>
<p><br>In addition, you can use "history" &nbsp;to show the most recent commands, or the up and down arrows to select recent commands.<br><br></p>
