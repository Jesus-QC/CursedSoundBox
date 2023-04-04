using System;
using System.Collections.Generic;
using System.IO;
using CursedMod.Features.Logger;
using CursedMod.Features.Wrappers.Player;
using CursedMod.Features.Wrappers.Player.Dummies;
using CursedMod.Features.Wrappers.Server;
using MEC;
using Mirror;
using NLayer;
using UnityEngine;
using VoiceChat;
using VoiceChat.Codec;
using VoiceChat.Codec.Enums;
using VoiceChat.Networking;

namespace CursedSoundBox;

public class SoundBox
{
    public static readonly Dictionary<uint, SoundBox> SoundBoxesByIds = new ();

    private SoundBox(bool destroyOnFinish)
    {
        _coroutine = Timing.RunCoroutine(PlayAudio());
        DestroyOnFinish = destroyOnFinish;
        SoundBoxesByIds.Add(Id, this);
    }

    /// <summary>
    /// Creates an instance of a SoundBox.
    /// </summary>
    /// <param name="destroyOnFinish">Whether or not the SoundBox should be destroyed once it finishes.</param>
    /// <returns>The created instance of the SoundBox.</returns>

    public static SoundBox Create(bool destroyOnFinish = true) => new (destroyOnFinish);

    /// <summary>
    /// Creates an instance of a SoundBox with a dummy as a speaker.
    /// </summary>
    /// <param name="dummyName">The name of the speaker.</param>
    /// <returns>The created instance of the SoundBox with the dummy as its speaker.</returns>
    public static SoundBox CreateWithDummy(string dummyName = "speaker")
    {
        CursedPlayer dummy = CursedDummy.Create(dummyName);
        SoundBox soundBox = Create();
        soundBox.Speaker = dummy;
        soundBox.DestroySpeakerOnFinish = true;
        return soundBox;
    }

    public static OpusEncoder OpusEncoder { get; } = new (OpusApplicationType.Voip);

    private static uint _idGenerator;

    public uint Id { get; } = _idGenerator++;
    
    public CursedPlayer Speaker { get; set; } = CursedServer.LocalPlayer;

    public VoiceChatChannel Channel { get; set; } = VoiceChatChannel.Mimicry;

    public bool Loop { get; set; }

    public bool DestroyOnFinish { get; set; }
    
    public bool DestroySpeakerOnFinish { get; set; } = true;

    public bool IsPlaying { get; set; }

    public Queue<MpegFile> Queue { get; } = new ();

    private MpegFile _mpegFile;

    private readonly CoroutineHandle _coroutine;
    
    private readonly float[] _buffer = new float[480]; 
    private readonly byte[] _encodedBuffer = new byte[512];

    public void Destroy()
    {
        _mpegFile?.Dispose();
        Timing.KillCoroutines(_coroutine);

        SoundBoxesByIds.Remove(Id);
        
        if (!DestroySpeakerOnFinish)
            return;
        
        NetworkServer.Destroy(Speaker.GameObject);
    }
    
    /// <summary>
    /// Plays the MPEG file on the provided path.
    /// </summary>
    /// <param name="filePath">The path of the MPEG file.</param>
    public void Play(string filePath) => Play(File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read));
    
    /// <summary>
    /// Plays a MPEG stream in the desired channel.
    /// </summary>
    /// <param name="stream">The desired stream.</param>
    public void Play(Stream stream)
    {
        try
        {
            _mpegFile?.Dispose();
            _mpegFile = new MpegFile(stream);
            _mpegFile.StereoMode = StereoMode.DownmixToMono;
        }
        catch (InvalidDataException)
        {
            CursedLogger.LogError("The file stream provided to play is not a valid MPEG file!");
        }
        catch (Exception e)
        {
            CursedLogger.LogError("There was an exception while trying to play a file.");
            CursedLogger.LogError(e);
        }

        Play();
    }

    /// <summary>
    /// Resumes the audio if it was paused.
    /// </summary>
    public void Play() => IsPlaying = true;

    public void Enqueue(string filePath) => Enqueue(File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read));
    
    public void Enqueue(Stream stream)
    {
        try
        {
            MpegFile mpegFile = new (stream);
            _mpegFile.StereoMode = StereoMode.DownmixToMono;
            Queue.Enqueue(mpegFile);
        }
        catch (InvalidDataException)
        {
            CursedLogger.LogError("The file stream provided to enqueue is not a valid MPEG file!");
        }
        catch (Exception e)
        {
            CursedLogger.LogError("There was an exception while trying to enqueue a file.");
            CursedLogger.LogError(e);
        }
    }

    /// <summary>
    /// Stops the audio if it was playing.
    /// </summary>
    public void Stop() => IsPlaying = false;

    /// <summary>
    /// Restarts the audio to its beginning.
    /// </summary>
    public void Restart() 
    {
        _mpegFile.Position = 0;
        Play();
    }

    private IEnumerator<float> PlayAudio()
    {
        const float samplesPerSecond = VoiceChatSettings.SampleRate * VoiceChatSettings.Channels;

        int samplesPerFrame = 0;
        while (true)
        {
            if (IsPlaying)
            {
                if (_mpegFile.Position == _mpegFile.Length)
                {
                    if (Loop)
                    {
                        Restart();
                    }
                    else
                    {
                        if (Queue.Count != 0)
                        {
                            _mpegFile.Dispose();
                            _mpegFile = Queue.Dequeue();
                            continue;
                        }

                        if (DestroyOnFinish)
                        {
                            Destroy();
                            break;
                        }
                        
                        Stop();
                        continue;
                    }    
                }
            
                samplesPerFrame += Mathf.RoundToInt(samplesPerSecond * Time.deltaTime);

                while (samplesPerFrame >= 480) // we can't encode packages of less than 480 or opus crashes :(
                {
                    _mpegFile.ReadSamples(_buffer, 0, 480);

                    int lenght = OpusEncoder.Encode(_buffer, _encodedBuffer);

                    foreach (CursedPlayer player in CursedPlayer.Collection)
                    {
                        player.NetworkConnection.Send(new VoiceMessage(Speaker.ReferenceHub, Channel, _encodedBuffer, lenght, false));
                    }
                
                    samplesPerFrame -= 480;
                }
            }
            
            yield return Timing.WaitForOneFrame;
        }
    }
}