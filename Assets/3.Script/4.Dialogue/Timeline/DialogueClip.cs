using UnityEngine;
using UnityEngine.Playables;

public class DialogueClip : PlayableAsset
{
    public TextAsset targetCSV;
    public int startId;
    public int endId;
    public bool pauseTimeline = true;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<DialogueBehaviour>.Create(graph);
        DialogueBehaviour behaviour = playable.GetBehaviour();

        behaviour.targetCSV = targetCSV;
        behaviour.startId = startId;
        behaviour.endId = endId;
        behaviour.pauseTimeline = pauseTimeline;

        if (owner != null)
        {
            behaviour.director = owner.GetComponent<PlayableDirector>();
        }

        return playable;
    }
}
