using UPP.Utils.State;

partial class MicrophoneStreamerWithLocalVAD
{

    public enum MicrophoneState
    {
        SILENT,
        SPEAKING
    }

    public class MicrophoneStateMachine : AbstractStateMachine<MicrophoneState> { }

    public class MicState : AbstractState
    {

        protected MicState(MicrophoneStreamerWithLocalVAD streamer) { this.streamer = streamer; }
        protected MicrophoneStreamerWithLocalVAD streamer;
        protected MicrophoneStateMachine machine => streamer.stateMachine;
        protected bool isVoiceActive => streamer.isVoiceActive;
        protected const int maxDuration = 7; // number of frames that the voice is in a state before considered active / inactive
        protected int activationDuration;
        protected bool previousActivationState;

    }

    public class Silent : MicState
    {
        public Silent(MicrophoneStreamerWithLocalVAD streamer) : base(streamer) { }


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
        public Speaking(MicrophoneStreamerWithLocalVAD streamer) : base(streamer) { }

        public override void Enter()
        {
            UnityEngine.Debug.Log("Started Speaking");
            streamer.OnStartedSpeaking?.Invoke();
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

        public override void Exit()
        {
            UnityEngine.Debug.Log("Stopped Speaking");
            streamer.OnStoppedSpeaking?.Invoke();
        }
    }
}