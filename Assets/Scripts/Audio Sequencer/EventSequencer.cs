#region Author

/************************************************************************************************************
Author: Nidre (Erdin Kacan)
Website: http://erdinkacan.tumblr.com/
GitHub: https://github.com/Nidre
Behance : https://www.behance.net/erdinkacan
************************************************************************************************************/

#endregion

#region Copyright

/************************************************************************************************************
The MIT License (MIT)
Copyright (c) 2015 Erdin
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
************************************************************************************************************/

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;

#endif

[RequireComponent(typeof(AudioSource))]
internal class EventSequencer : SequencerBase
{


  #region FadeTarget enum

  #region Enumerations

  [Flags]
  public enum FadeTarget
  {
    Play = (1 << 0),
    Stop = (1 << 1),
    Mute = (1 << 2),
    UnMute = (1 << 3),
    Pause = (1 << 4),
    UnPause = (1 << 5)
  }

  #endregion

  #endregion

  #region Fields

  /// <summary>
  /// Event to be fired on every step.
  /// </summary>
  public EventSequencerListener onAnyStep;// = new EventSequencerListenerContainer();

  /// <summary>
  /// Event to be fired on non-empty steps.
  /// </summary>  

  public EventSequencerListener onBeat;// = new EventSequencerListenerContainer();

  #endregion

  #region Properties

  /// <summary>
  /// True if clip data is loaded.
  /// </summary>
  public override bool IsReady
  {
    get { return true; }
  }

  /// <summary>
  /// Signature Lenght
  /// </summary>
  public int NumberOfSteps
  {
    get { return _sequence.Length; }
  }

  #endregion



  #region Variables

  /// <summary>
  /// Queues events to be fired make sure we are not missing any of them. Only created if the event is used.
  /// </summary>
  private Queue<Action> _onBeatEventQueue;

  /// <summary>
  /// Queues events to be fired make sure we are not missing any of them. Only created if the event is used.
  /// </summary>
  private Queue<Action> _onAnyStepEventQueue;

  [Range(0, 1f)]
  [SerializeField]
  private float baseVolume;


  /// <summary>
  /// Sequence of steps.
  /// True = Play
  /// False = Silent
  /// </summary>
  private Sequence _sequence;

  /// <summary>
  /// Fade in duration from muted to unmuted.
  /// </summary>
  [Range(0, 60)]
  public float fadeInDuration;

  /// <summary>
  /// Fade in duration from unmuted to muted.
  /// </summary>
  [Range(0, 60)]
  public float fadeOutDuration;

  /// <summary>
  /// When to trigger fade.
  /// </summary>
  [BitMask]
  public FadeTarget fadeWhen;


  /// <summary>
  /// Time of next tick.
  /// </summary>
  private double _nextTick;

  private float _volume;


  /// <summary>
  /// Sample rate.
  /// </summary>
  private double _sampleRate;


  /// <summary>
  /// Remaining beat events to be fired.
  /// </summary>
  private int _fireBeatEvent;

  /// <summary>
  /// Remaining any step events to be fired.
  /// </summary>
  private int _fireAnyStepEvent;


  /// <summary>
  /// Initial volume value to fade in.
  /// </summary>
  private float _initialVolumeValue;

  /// <summary>
  /// Volume of audio source just before fading in or out
  /// </summary>
  private float _volumeBeforeFade;

  /// <summary>
  /// Target volume when fade in/or finishes.
  /// </summary>
  private float _volumeAfterFade;

  /// <summary>
  /// Curernt percentage of fade progress.
  /// </summary>
  private float _fadeProgress = 1;

  /// <summary>
  /// Current fade speed;
  /// </summary>
  private float _fadeSpeed;

  /// <summary>
  /// What are we fading into.
  /// </summary>
  private FadeTarget _fadeTarget;

  /// <summary>
  /// Attached audio source.
  /// </summary>
  private AudioSource _audioSource;

  #endregion

  #region Methods

  public override void OnAwake()
  {
#if UNITY_EDITOR
    _isMutedOld = isMuted;
    _oldBpm = bpm;
#endif
    StartCoroutine(Init());
  }

  /// <summary>
  /// Wait until sequencer is ready.
  /// </summary>
  /// <returns></returns>
  private IEnumerator Init()
  {
    _audioSource = GetComponent<AudioSource>();
    _initialVolumeValue = baseVolume;
    _volumeAfterFade = _initialVolumeValue;
    _sampleRate = AudioSettings.outputSampleRate;
    _volume = 0;
    _sequence = GetComponent<Sequence>();

    if (playWhenReady)
    {
      Play();
    }
    OnReady();
    yield break;
  }


  /// <summary>
  /// Set mute state.
  /// </summary>
  /// <param name="isMuted"></param>
  public override void Mute(bool isMuted)
  {
    Mute(isMuted, isMuted ? fadeOutDuration : fadeInDuration);
  }

  /// <summary>
  ///  Toggle mute state.
  /// </summary>
  /// <param name="isMuted"></param>
  /// <param name="fadeDuration"></param>
  public override void Mute(bool isMuted, float fadeDuration)
  {
    if (isMuted && fadeDuration > 0 && fadeWhen.IsFlagSet(FadeTarget.Mute))
    {
      _fadeTarget = FadeTarget.Mute;
      FadeOut(fadeDuration);
    }
    else if (!isMuted && fadeDuration > 0 && fadeWhen.IsFlagSet(FadeTarget.UnMute))
    {
      _fadeTarget = FadeTarget.UnMute;
      FadeIn(fadeDuration);
    }
    else
    {
      _volume = isMuted ? 0 : _initialVolumeValue;
      _fadeProgress = 1;
      MuteInternal(isMuted);
    }
  }

  /// <summary>
  /// Changes default fade in and fade out durations.
  /// </summary>
  /// <param name="fadeIn"></param>
  /// <param name="fadeOut"></param>
  public override void SetFadeDurations(float fadeIn, float fadeOut)
  {
    fadeInDuration = fadeIn;
    fadeOutDuration = fadeOut;
  }

  private void MuteInternal(bool isMuted)
  {
    this.isMuted = isMuted;
#if UNITY_EDITOR
    _isMutedOld = this.isMuted;
#endif
  }

  /// <summary>
  /// Start playing.
  /// </summary>
  public override void Play()
  {
    Play(fadeInDuration);
  }

  /// <summary>
  /// Start playing.
  /// </summary>
  /// <param name="fadeDuration"></param>
  public override void Play(float fadeDuration)
  {
    if (!IsPlaying && fadeDuration > 0 && fadeWhen.IsFlagSet(FadeTarget.Play))
    {
      _fadeTarget = FadeTarget.Play;
      PlayInternal();
      FadeIn(fadeDuration);
    }
    else
    {
      _volume = isMuted ? 0 : _initialVolumeValue;
      _fadeProgress = 1;
      PlayInternal();
    }
  }

  private void PlayInternal()
  {
    _nextTick = AudioSettings.dspTime * _sampleRate;
    _audioSource.Play();
    _isPlaying = true;
  }

  /// <summary>
  /// Stop playing.
  /// </summary>
  public override void Stop()
  {
    Stop(fadeOutDuration);
  }

  /// <summary>
  /// Stop playing.
  /// </summary>
  /// <param name="fadeDuration"></param>
  public override void Stop(float fadeDuration)
  {
    if (IsPlaying && fadeDuration > 0 && fadeWhen.IsFlagSet(FadeTarget.Stop))
    {
      _fadeTarget = FadeTarget.Stop;
      FadeOut(fadeDuration);
    }
    else
    {
      _volume = isMuted ? 0 : _initialVolumeValue;
      _fadeProgress = 1;
      StopInternal();
    }
  }

  private void StopInternal()
  {
    _isPlaying = false;
    _audioSource.Stop();
    _sequence.Reset();
  }

  /// <summary>
  /// Pause/Unpause.
  /// </summary>
  /// <param name="isPaused"></param>
  public override void Pause(bool isPaused)
  {
    Pause(isPaused, isPaused ? fadeOutDuration : fadeInDuration);
  }

  /// <summary>
  /// Pause/Unpause.
  /// </summary>
  /// <param name="isPaused"></param>
  /// <param name="fadeDuration"></param>
  public override void Pause(bool isPaused, float fadeDuration)
  {
    if (isPaused && fadeDuration > 0 && fadeWhen.IsFlagSet(FadeTarget.Pause))
    {
      _fadeTarget = FadeTarget.Pause;
      FadeOut(fadeDuration);
    }
    else if (!isPaused && fadeDuration > 0 && fadeWhen.IsFlagSet(FadeTarget.UnPause))
    {
      _fadeTarget = FadeTarget.UnPause;
      PauseInternal(false);
      FadeIn(fadeDuration);
    }
    else
    {
      _volume = isMuted ? 0 : _initialVolumeValue;
      _fadeProgress = 1;
      PauseInternal(isPaused);
    }
  }

  private void PauseInternal(bool isPaused)
  {
    if (isPaused)
    {
      _audioSource.Pause();
      _isPlaying = false;
    }
    else
    {
      _audioSource.UnPause();
      _isPlaying = true;
    }
  }

  /// <summary>
  /// Toggle mute state.
  /// </summary>
  public override void ToggleMute()
  {
    isMuted = !isMuted;
  }

  private void FadeIn(float duration)
  {
    _fadeSpeed = 1f / duration;
    _fadeProgress = 0;
    MuteInternal(false);
    _volumeBeforeFade = _volume;
    _volumeAfterFade = _initialVolumeValue;
  }

  private void FadeOut(float duration)
  {
    _fadeSpeed = 1f / duration;
    _fadeProgress = 0;
    _volumeBeforeFade = _volume;
    _volumeAfterFade = 0;
  }



  private void Update()
  {
    if (_onAnyStepEventQueue != null)
    {
      while (_onAnyStepEventQueue.Count > 0)
      {
        _onAnyStepEventQueue.Dequeue().Invoke();
      }
    }
    if (_onBeatEventQueue != null)
    {
      while (_onBeatEventQueue.Count > 0)
      {
        _onBeatEventQueue.Dequeue().Invoke();
      }
    }
    if (_fadeProgress < 1)
    {
      _fadeProgress += Time.deltaTime * _fadeSpeed;
      if (_fadeProgress > 1) _fadeProgress = 1;
      _volume = Mathf.Lerp(_volumeBeforeFade, _volumeAfterFade, _fadeProgress);
      if (_fadeProgress == 1)
      {
        switch (_fadeTarget)
        {
          case FadeTarget.Play:
          case FadeTarget.UnPause:
          case FadeTarget.UnMute:
            //Done on start of Fade.
            break;
          case FadeTarget.Stop:
            StopInternal();
            break;
          case FadeTarget.Mute:
            MuteInternal(true);
            break;
          case FadeTarget.Pause:
            PauseInternal(true);
            break;
          default:
            throw new ArgumentOutOfRangeException();
        }
      }
    }
  }

  /// <summary>
  /// Set Bpm.
  /// </summary>
  /// <param name="newBpm">Beats per minute.</param>
  public override void SetBpm(int newBpm)
  {
    if (newBpm < 10) newBpm = 10;
    bpm = newBpm;
  }

  void OnAudioFilterRead(float[] bufferData, int bufferChannels)
  {
    if (!IsReady || !_isPlaying) return;
    double samplesPerTick = _sampleRate * 60.0F / bpm * 4.0F / NumberOfSteps;
    double sample = AudioSettings.dspTime * _sampleRate;
    int dataLeft = bufferData.Length;
    while (dataLeft > 0)
    {
      double newSample = sample + dataLeft;
      if (_nextTick < newSample)
      {
        dataLeft = (int)(newSample - _nextTick);
        _nextTick += samplesPerTick;
        _sequence.IncrementStep();

        if (_sequence.ShouldTrigger)
        {
          if (onBeat != null)
          {
            if (!isMuted)
            {
              if (_onBeatEventQueue == null) _onBeatEventQueue = new Queue<Action>();
              float volume = _volume;
              _onBeatEventQueue.Enqueue(() =>
              {
                onBeat.Invoke(_sequence);
              });
            }
            else
            {
              _fireBeatEvent++;
            }
          }
        }


        if (onAnyStep != null)
        {
          if (!isMuted)
          {
            if (_onAnyStepEventQueue == null) _onAnyStepEventQueue = new Queue<Action>();
            float volume = _volume;
            _onAnyStepEventQueue.Enqueue(() =>
            {
              onAnyStep.Invoke(_sequence);
            });
          }
          else
          {
            _fireAnyStepEvent++;
          }
        }

      }
      else
      {
        break;
      }
    }
  }


#if UNITY_EDITOR

  private bool _isMutedOld;
  private int _oldBpm;
  private int _oldBeatsPerBar;

  /// <summary>
  /// Check and update when options are changed from editor.
  /// </summary>
  void LateUpdate()
  {
    if (IsReady)
    {
      if (_isMutedOld != isMuted)
      {
        _isMutedOld = isMuted;
        Mute(isMuted);
      }
      if (_oldBpm != bpm)
      {
        _oldBpm = bpm;
        SetBpm(bpm);
      }
    }
  }

  [MenuItem("GameObject/Sequencer/Event Sequencer", false, 10)]
  static void CreateSequencerController(MenuCommand menuCommand)
  {
    // Create a custom game object
    GameObject go = new GameObject("EventSequencer");
    go.AddComponent<AudioSource>().playOnAwake = false;
    go.AddComponent<Sequence>();
    go.AddComponent<EventSequencer>();
    GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
    // Register the creation in the undo system
    Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
    Selection.activeObject = go;
  }


#endif

  #endregion

  #region Classes

  #endregion
}