using System;
using CommandSystem;

namespace CursedSoundBox.Commands;

public class SoundBoxRestartCommand : ICommand
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
        
        SoundBox.SoundBoxesByIds[value].Restart();
        response = "Restarted the SoundBox.";
        return true;
    }

    public string Command { get; } = "restart";
    public string[] Aliases { get; } = { "r" };
    public string Description { get; } = "Restarts the playing sound of a SoundBox";
}