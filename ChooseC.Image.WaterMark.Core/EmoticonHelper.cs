using ChooseC.Image.WaterMark.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChooseC.Image.WaterMark.Core
{
    /// <summary>
    /// 表情符号帮助类
    /// </summary>
    public class EmoticonHelper
    {
        private static readonly string EmojiPattern = CoreResource.EmojiRegex;        
        public static bool CheckEmojiChar(string input)
        {
            //一个或多个表情符号
            var rx = $@"^(?:{EmojiPattern})+$";
            return Regex.IsMatch(input, rx);
        }

        public static void SplitEmoji(string input,out List<SplitRegex> splitList)
        {
            int idx = 0;
            splitList = new List<SplitRegex>();
            while (CheckEmojiChar(input))
            {
                var match = Regex.Match(input, EmojiPattern);
                var matchVal = match.Value;
                var matchIdx = match.Index;

                var previousStr = input.Substring(0, matchIdx);
                if (previousStr !=string.Empty)
                {
                    splitList.Add(new SplitRegex(index: idx, isMatch: false, val: previousStr) );
                    input = input.Remove(0, matchIdx);
                    idx += matchIdx;
                }

                splitList.Add(new SplitRegex(index: idx, isMatch: true, val: matchVal));
                input = input.Remove(0, matchVal.Length);
                idx += matchVal.Length;
            }

        }

    }
}
