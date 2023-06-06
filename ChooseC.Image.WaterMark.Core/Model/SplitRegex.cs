using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChooseC.Image.WaterMark.Core.Model
{
    public struct SplitRegex
    {
        public SplitRegex(int index, bool isMatch, string val)
        {
            Index = index;
            IsMatch = isMatch;
            Val = val;
        }

        public int Index { get; init; } = 0;
        public bool IsMatch { get; init; } = false;
        public string Val { get; init; } = string.Empty;
    }
}
