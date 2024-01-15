# SPY

SPY is a learning game focused on CT. The game principle is to select from a list of actions, those that will allow a robot to get out of a maze. These actions are represented as blocks and the player has to build a sequence of actions that will be sent to the robot for execution.

To get out of the maze, the player has to program the robot to avoid obstacles. These obstacles have been built for educational purposes:
 - Sentinels end the level if they spot the robot (if the robot passes through their detection area). Players can select every sentinel to see the sequence of actions assigned to it. Players must know how to read programs, to understand it and to anticipate the sentinels' movements in order to create a proper sequence to reach the objective with the robot.
 - Doors can be activated with terminals. This feature was designed to engage a step-by-step resolution process. Players have to understand that to reach the exit, it is necessary to solve the level in sub-steps (Objective 1: Activate the terminal to open the door. Objective 2: Reach the exit). This also allows players to manipulate the states of an object (open or closed).
 - Program several robots with a unique program. Players need to find a generic solution that enables two robots to reach their exit in different mazes.
 
In sum players have to observe and model the simulation (abstraction), decompose their strategy in sub-steps (decomposition), determine the best solution (evaluation), plan the actions to perform (algorithmic thinking) and reuse and adapt previous solutions on new problems (generalization).

You can play the game at: [https://spy.lip6.fr/](https://spy.lip6.fr/)

SPY is developed with Unity and the [FYFY](https://github.com/Mocahteam/FYFY) library.




# Additions by Loona, Jules and Nassim
## Variables
The notion of variable is now represented by the weight of the player. Some levels may need the player to be heavier, and weigh a specific number of kg to open doors. Now, doors can also be opened by activating floor switches and not only consoles. The floor switches are activated when the player is stepping on them with the right weight. The total weight of the player is shown on the screen. The player has to be the exact same weight as the floor switch indicates: either way the switch will not be activated.

## Functions
The notion of function is represented by the action of picking up and dropping batteries. 
The player can encounter piles of batteries in some levels, and choose the amount of batteries (in kg) to carry/drop (the parameter of the function). When executed, the function adds/substracts the weight of the batteries to the current weight of the robot. 

## Things we did not have the time to do
* Fix the animations
* Display the player's current weight only when relevant, i.e in levels requiring to activate a switch
* Try with 2 robots: adapt the UI to have the weight of the robot and its name
* Better UI display: attach the weight to the robot prefab (when relevant) so that it appears above its head like the switch weight. Also make it face the camera at all times
