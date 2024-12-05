using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UPP.Utils.State;

partial class MicrophoneStreamer
{

    public enum MicrophoneState
    {
        SILENT,
        SPEAKING
    }

    public class MicrophoneStateMachine : AbstractStateMachine<MicrophoneState> { }

    public class MicState : AbstractState
    {

        protected MicState(MicrophoneStreamer streamer) { this.streamer = streamer; }
        protected MicrophoneStreamer streamer;
        protected MicrophoneStateMachine machine => streamer.stateMachine;
        protected bool isVoiceActive => streamer.isVoiceActive;
        protected const int maxDuration = 7; // number of frames that the voice is in a state before considered active / inactive
        protected int activationDuration;
        protected bool previousActivationState;

    }

    public class Silent : MicState
    {
        public Silent(MicrophoneStreamer streamer) : base(streamer) { }


        public override void Update()
        {
            if (isVoiceActive)
                activationDuration++;
            else
                activationDuration = 0;

            if (activationDuration > maxDuration)
                machine.TransitionTo(MicrophoneState.SPEAKING);
        }
    }

    public class Speaking : MicState
    {
        public Speaking(MicrophoneStreamer streamer) : base(streamer) { }

        public override void Enter()
        {
            UnityEngine.Debug.Log("Started Speaking");
            streamer.OnStartedSpeaking?.Invoke();

            //start coroutine
            Dispatcher.ExecuteOnMainThread.Enqueue(() => streamer.StartCoroutine(resampleRoutine()));
        }

        public override void Update()
        {
            if (!isVoiceActive)
                activationDuration++;
            else
                activationDuration = 0;

            if (activationDuration > maxDuration)
                machine.TransitionTo(MicrophoneState.SILENT);

        }

        private IEnumerator resampleRoutine()
        {
            for (; ; )
            {
                //while speaking, resample audio stream every 50ms
                byte[] resampled_audio = AudioProcessHelpers.Resample(Instance.sharedBuffer);
                UnityEngine.Debug.Log(AudioProcessHelpers.Encode(resampled_audio));

                yield return new WaitForSecondsRealtime(0.075f);
            }
        }

        public override void Exit()
        {
            UnityEngine.Debug.Log("Stopped Speaking");
            Dispatcher.ExecuteOnMainThread.Enqueue(() => streamer.StopCoroutine(resampleRoutine()));
            streamer.OnStoppedSpeaking?.Invoke();
        }
    }
}