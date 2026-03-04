using UnityEngine;
using UnityEngine.Playables;

public class DialogueBehaviour : PlayableBehaviour
{
    public TextAsset targetCSV;
    public int startId;
    public int endId;
    public bool pauseTimeline;
    public PlayableDirector director;

    private bool _hasPlayed;

    public override void OnPlayableCreate(Playable playable)
    {
        _hasPlayed = false;
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (_hasPlayed || !Application.isPlaying) return;
        _hasPlayed = true;

        if (UI.DialogueTester.Instance == null) return;

        if (targetCSV != null)
        {
            UI.DialogueTester.Instance.LoadDialogueData(targetCSV);
        }

        if (pauseTimeline && director != null)
        {
            director.Pause();
            UI.DialogueTester.Instance.OnDialogueEnded += ResumeTimeline;
        }

        UI.DialogueTester.Instance.PlayDialogueRange(startId, endId);
    }

    private void ResumeTimeline()
    {
        if (UI.DialogueTester.Instance == null) return;

        UI.DialogueTester.Instance.OnDialogueEnded -= ResumeTimeline;

        if (director != null)
        {
            director.Play();
        }
    }
}
