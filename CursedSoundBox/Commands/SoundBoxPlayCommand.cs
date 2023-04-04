using System;
using System.IO;
using CommandSystem;

namespace CursedSoundBox.Commands;

public class SoundBoxPlayCommand : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (arguments.Count == 0)
        {
            response = "Specify the audio file path.";
            return false;
        }
        
        if (uint.TryParse(arguments.At(0), out uint value))
        {
            if (!SoundBox.SoundBoxesByIds.ContainsKey(value))
            {
                response = "SoundBox not found.";
                return false;
            }
        
            SoundBox.SoundBoxesByIds[value].Play();
        }

        string path = string.Join(" ", arguments);

        if (!File.Exists(path))
        {
            response = "File not found!";
            return false;
        }

        SoundBox soundBox = SoundBox.CreateWithDummy();
        soundBox.Play(path);
        response = $"Created a SoundBox with the id: {soundBox.Id}";
        return true;
    }

    public string Command { get; } = "play";
    public string[] Aliases { get; } = { "p" };
    public string Description { get; } = "Plays an audio file at the desired path.";
}