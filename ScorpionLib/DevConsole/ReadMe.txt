/*************************************************
DevConsole v1.0 by CobsTech
**************************************************/

/*************************************************
HOW TO
**************************************************/
//****CREATING THE CONSOLE****
- To add the Console to your project, just add the "Console" script to one of your gameobjects.
- It is recommended to use a single purpouse gameobject as a Console, and there should only be one per scene.
- You can make the console persistent throughout scenes setting the "dontDestroyOnLoad" variable to true.

//****USING THE CONSOLE****
- Open/Close Console: "consoleKey" KeyCode button.
- When a command is entered, a function linked to the command is executed.
- Clear the Console: "DC_CLEAR" command.
- Some commands accept a string parameter. Everything typed after the command (and a space) will be passed to the function.
- Use the Console for Debug: "DC_SHOW_DEBUGLOG" command with a value of "1", "true", or "yes".
- Show help for a specific command: Add a '?' at the end of the command. e.g. "DC_CLEAR?".
- Show all commands available: Type the "HELP" command.

//Completion Window
- A Completion Window appears when the text matches one or more commands.
- Navigate: UP and DOWN arrow keys.
- Close: ESC key.
- Show: F1 key.
- Autocomplete: TAB key.

//History
- When there is no text in the input field, you can access the history by pressing UP or DOWN arrow keys.
- Navigate: UP and DOWN arrow keys.

//Use Console through Code
- Calling the Console "Log" method logs a message to the Console.
- You can also specify a color for the text.
- Alternatively, calling "LogWarning", "LogInfo" and "LogError" will log a light blue, yellow and red text, respectively.

//****MAKING CUSTOM COMMANDS****
Create new commands by calling the "Console.AddCommand" at anytime, as it is done in the "Example" script.
1) The first parameter of the constructor is a string representing the command name.
2) The second parameter is either a delegate to a "NoArgs" or "OneStringArg" function.
	2.1) Both must return a void value and are usually static.
	2.2) The function must have no arguments at all (NoArgs) or only one string (OneStringArg).
	2.3) It is up to the programmer to parse the string received properly.
3) The third parameter, if added, represents the help or info shown with '?'. e.g. "DC_CLEAR?".
	3.1) Usually, you just want to shown some invariable string, so you should use the string overload.
	3.2) Sometimes, though, you want to run a function when asking for help. To do that, pass a reference to a NoArgs function.
4) To remove a command, call "Console.RemoveCommand" with the command name as parameter.
5) There are some examples in the "Example" script.
