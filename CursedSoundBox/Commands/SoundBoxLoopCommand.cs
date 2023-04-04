using System;
using CommandSystem;

namespace CursedSoundBox.Commands;

public class SoundBoxLoopCommand : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (arguments.Count == 0)
        {
            response = "Please specify the SoundBox Id";
            return false;
        }

        if (!uint.TryParse(arguments.At(0), out uint value))
        {
            response = "Couldn't parse the id";
            return false;
        }

        if (!SoundBox.SoundBoxesByIds.ContainsKey(value))
        {
            response = "SoundBox not found.";
            return false;
        }
        
        SoundBox.SoundBoxesByIds[value].Loop = !SoundBox.SoundBoxesByIds[value].Loop;
        response = "Toggled loops in the SoundBox.";
        return true;
    }

    public string Command { get; } = "loop";
    public string[] Aliases { get; } = { "l" };
    public string Description { get; } = "Loops the playing sound of a SoundBox";
}