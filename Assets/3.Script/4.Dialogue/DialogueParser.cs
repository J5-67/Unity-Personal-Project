using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    [System.Serializable]
    public class DialogueData
    {
        public int id;
        public string name;
        public SpeakerSide side;
        public string portraitKey;
        public string text;
    }

    public static class DialogueParser
    {
        public static List<DialogueData> Parse(string csvText)
        {
            List<DialogueData> list = new List<DialogueData>();
            string[] lines = csvText.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] data = line.Split(',');

                if (data.Length < 5) continue;

                DialogueData entry = new DialogueData();

                int.TryParse(data[0], out entry.id);

                entry.name = data[1].Trim();

                if (System.Enum.TryParse(data[2].Trim(), true, out SpeakerSide sideEnum))
                {
                    entry.side = sideEnum;
                }
                else
                {
                    entry.side = SpeakerSide.Left;
                }

                entry.portraitKey = data[3].Trim();

                string content = data[4];
                for (int j = 5; j < data.Length; j++)
                {
                    content += "," + data[j];
                }
                content = content.Trim().Trim('"');
                content = content.Replace("\"\"", "\"");

                entry.text = content;

                list.Add(entry);
            }

            return list;
        }
    }
}
