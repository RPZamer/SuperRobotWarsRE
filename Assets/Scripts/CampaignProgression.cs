using UnityEngine;
using UnityEngine.SceneManagement;

public static class CampaignProgression 
{
    
   private const string NextSceneKey = "NextScene";
    public static void AdvanceAfterVictory()
    {
        string completedScene = SceneManager.GetActiveScene().name;
        Debug.Log("CAMPAIGN DEBUG - Completed scene: " + completedScene);

        string[] levels =
        {
            "Level 1 - Strange Robot Gathering 1",
            "Level 2 - Launch to space, Super Robot Army 1",
            "Level 3 - Space Battle, Keep strong",
            "Level 4 - Moon battle before decisive conflict",
            "Final Stage - Char Vs Super Robot Army"
        };  

        for (int i = 0; i < levels.Length; i++)
        {
            if (completedScene != levels[i])
                  
            {
                continue;
            }
            if (i + 1 < levels.Length)
            {
                PlayerPrefs.SetString(NextSceneKey, levels[i + 1]);
                PlayerPrefs.Save();

                Debug.Log("Next campaign mission: " + levels[i + 1]);

            }
            else
            {
                Debug.Log("Campaign complete.");
            }
            return;

        }
        Debug.LogWarning("Completed scene not found in campaign progression list: " + completedScene);
    }


}
        
 

