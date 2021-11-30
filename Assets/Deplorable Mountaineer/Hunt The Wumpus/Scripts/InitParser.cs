#region

using System.Collections;
using Deplorable_Mountaineer.Parser;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

#endregion

namespace Deplorable_Mountaineer.Hunt_The_Wumpus {
    /// <summary>
    ///     Set up the parser and compile the token file.
    /// </summary>
    public class InitParser : MonoBehaviour {
        [SerializeField] private TextAsset[] tokens;

        private IEnumerator Start(){
            yield return null;
            foreach(TextAsset t in tokens){
                Debug.Log($"Compiling TDL {t.name}");
                if(!WumpusCommands.TokenDefinitionLanguage.Compile(t.text, t.name)){
#if UNITY_EDITOR
                    EditorApplication.isPlaying = false;
                    //something went wrong...
#endif
                    yield break;
                }
            }

            yield return null;
            WumpusCommands.Parser = new WumpusParser(WumpusCommands.TokenDefinitionLanguage);
        }
    }
}
