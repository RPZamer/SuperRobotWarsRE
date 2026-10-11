using System;
using UnityEngine;

// WEEK 6: Stores one line of dialogue for a story cutscene.
[Serializable]
public class CutsceneLine
{
    [Header("Speaker")]
    public PilotBase pilot;
    public PilotEmotion emotion;

    [Header("Dialogue")]
    [TextArea(2, 5)]
    public string dialogue;

    // WEEK 6: Determines whether the speaker's portrait appears.
    public bool showPortrait = true;
}

// WEEK 6: Creates reusable cutscene assets for each level.
[CreateAssetMenu(
    fileName = "New Cutscene",
    menuName = "SRW/Cutscene")]
public class CutsceneData : ScriptableObject
{
    [Header("Cutscene Information")]
    public string cutsceneName;

    [Header("Dialogue")]
    public CutsceneLine[] lines;

    [Header("What Happens After This Cutscene")]
    public bool transitionToScene;

    // WEEK 6: Scene to load after the dialogue finishes.
    public string destinationScene;
}