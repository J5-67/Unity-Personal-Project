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
        public string portraitKey; // 이미지 파일 이름 (Key)
        public string text;
    }

    public static class DialogueParser
    {
        public static List<DialogueData> Parse(string csvText)
        {
            List<DialogueData> list = new List<DialogueData>();
            string[] lines = csvText.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

            // 첫 줄(헤더)은 건너뛰고 1부터 시작
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                // 쉼표(,)로 분리 (간단한 파싱)
                // 주의: 대사 안에 쉼표가 있다면 따옴표 처리 로직이 추가로 필요함.
                // 지금은 간단하게 구현함.
                string[] data = line.Split(',');

                if (data.Length < 5) continue; // 데이터 부족하면 스킵

                DialogueData entry = new DialogueData();

                // 0: ID
                int.TryParse(data[0], out entry.id);

                // 1: Name
                entry.name = data[1].Trim();

                // 2: Side (Left / Right)
                if (System.Enum.TryParse(data[2].Trim(), true, out SpeakerSide sideEnum))
                {
                    entry.side = sideEnum;
                }
                else
                {
                    entry.side = SpeakerSide.Left; // 기본값
                }

                // 3: Portrait Key
                entry.portraitKey = data[3].Trim();

                // 4: Text (쉼표가 포함된 대사 처리: 나머지 부분을 다 합침)
                string content = data[4];
                for (int j = 5; j < data.Length; j++)
                {
                    content += "," + data[j];
                }
                // 따옴표 제거 (혹시 CSV가 따옴표로 감싸져 있다면)
                content = content.Trim().Trim('"');
                content = content.Replace("\"\"", "\""); // 이중 따옴표 이스케이프 처리

                entry.text = content;

                list.Add(entry);
            }

            return list;
        }
    }
}
