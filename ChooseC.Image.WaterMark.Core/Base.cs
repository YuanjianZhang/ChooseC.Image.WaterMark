using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChooseC.Image.WaterMark.Core
{
    public class Base
    {
        protected static readonly string SourceFolder = Path.Combine(AppContext.BaseDirectory, "Source");
        protected static readonly string FontsFolder = Path.Combine(SourceFolder, "Fonts");
        protected static readonly string LogosFolder = Path.Combine(SourceFolder, "Logos");

        /// <summary>
        /// 获取字体
        /// </summary>
        /// <param name="font"></param>
        /// <param name="fontStyle"></param>
        /// <param name="size"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        protected virtual Font GetFont(string fontFamilyName, FontStyle fontStyle, int size, GraphicsUnit unit)
        {
            try
            {
                var font = FontHelper.GetFont(fontFamilyName, size, fontStyle, unit);
                if (font is null)
                {
                    var fontPath = string.Empty;
                    var fontFileName = $"{fontFamilyName}.*";
                    var fontStyleFileName = $"{fontFamilyName}-{System.Enum.GetName(typeof(FontStyle), fontStyle)}.*";

                    if (new DirectoryInfo(FontsFolder).Exists)
                    {
                        var FontFiles = new DirectoryInfo(FontsFolder).GetFileSystemInfos(fontStyleFileName);
                        if (FontFiles.Length > 0)
                        {
                            fontPath = FontFiles.First().FullName;
                        }
                        else
                        {
                            FontFiles = new DirectoryInfo(FontsFolder).GetFileSystemInfos(fontFileName);
                            if (FontFiles.Length > 0)
                            {
                                fontPath = FontFiles.First().FullName;
                            }
                        }
                    }
                    font = FontHelper.GetFontByPath(fontPath, size, fontStyle, unit);
                }
                return font;

            }
            catch { throw; }
        }
    }
}
