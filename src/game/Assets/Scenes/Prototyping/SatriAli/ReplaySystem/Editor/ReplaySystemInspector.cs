using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Replay
{
    [CustomEditor(typeof(ReplaySystem))]
    public class ReplaySystemInspector : Editor
    {
        public override bool RequiresConstantRepaint()
        {
            return true;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            ReplaySystem rs = (ReplaySystem)this.target;
            if (rs.Data == null)
                return;

            ReplayData.Metrics m = rs.Data.GetMetrics();
            string infoText = $"Objects: {m.objectCount}\nStreams: {m.streamCount} ({m.streamBytes} bytes)\nEvents: {m.eventListCount} ({m.eventListBytes} bytes)\nSize: {(m.totalBytes/1024f):F2} KiB";
            EditorGUILayout.HelpBox(infoText, MessageType.None);
        }
    }
}
