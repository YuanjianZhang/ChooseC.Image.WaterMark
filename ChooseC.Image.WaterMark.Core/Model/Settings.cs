using System.Drawing;
using System.Drawing.Imaging;

namespace ChooseC.Image.WaterMark.Core.Model
{
    public sealed class Settings
    {
        private long _exportquality = 100L;
        /// <summary>
        /// 导出文件类型
        /// </summary>
        public ImageFormat exporttype { get; set; }
        /// <summary>
        /// 导出质量
        /// </summary>
        public long exportquality
        {
            get
            {
                if (_exportquality > 100L) return 100L;
                return _exportquality;
            }
            set
            {
                _exportquality = value;
            }
        }
        /// <summary>
        /// 导出文件夹
        /// </summary>
        public string exportfolder { get; set; }
        /// <summary>
        /// 导出尺寸
        /// 默认保持原图缩放比 
        /// </summary>
        /// <remarks>
        /// <list type="table">
        /// <item><see cref="Size.Empty" langword="不进行缩放"></see></item>
        /// <item>只设置 Width 或 Height ，将根据原图比例进行计算缩放。</item>
        /// <item> 
        /// 16:9
        /// <list type="bullet">
        /// <item>HD：1920 * 1080</item>
        /// <item>4k: 3840 * 2160</item>
        /// <item>8k: 7680 * 4320</item>
        /// </list>
        /// </item>
        /// <item> 
        /// 4:3
        /// <list type="bullet">
        /// <item>2048 * 1536</item>
        /// <item>3200 * 2400</item>
        /// <item>4096 * 3072</item>
        /// <item>6400 * 4800</item>
        /// </list>
        /// </item>
        /// </list>
        /// </remarks>
        public Size exportsize { get; set; }
        /// <summary>
        /// 个签
        /// </summary>
        public string sign { get; set; }

        public ConfigSection topbottom { get; set; }

        public ConfigSection bottom { get; set; }
        public ConfigSection surround { get; set; }
        public ConfigSection onlyfill { get; set; }
        /// <summary>
        /// 底部信息和图像的间隔
        /// </summary>
        public int bottommargin { get; set; }

    }

    public class ConfigSection
    {
        /// <summary>
        /// 上方logo
        /// </summary>
        public string? toplogo { get; set; }
        /// <summary>
        /// 下方信息
        /// </summary>
        public string? bottominfo { get; set; }
        /// <summary>
        /// 左边logo
        /// </summary>
        public string? leftlogo { get; set; }
        /// <summary>
        /// 左边信息
        /// </summary>
        public string? leftinfo { get; set; }
        /// <summary>
        /// 右边logo
        /// </summary>
        public string? rightlogo { get; set; }
        /// <summary>
        /// 右边信息
        /// </summary>
        public string? rightinfo { get; set; }
        /// <summary>
        /// 背景填充颜色
        /// </summary>
        public string fillcolor { get; set; }
        /// <summary>
        /// 背景填充空白占比率
        /// </summary>
        public double fillratio { get; set; }
        /// <summary>
        /// 文字logo字体配置
        /// </summary>
        public FontConfig wordlogofont { get; set; }
        /// <summary>
        /// 信息字体配置
        /// </summary>
        public FontConfig infofont { get; set; }

        /// <summary>
        /// Logo图像最大高度
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item> 1>=number>0：相对原图的占比</item>
        /// <item> number>1：Logo图像最大像素高度</item>
        /// </list>
        /// </remarks>
        public double logmaxheight { get; set; } = 0;

    }

    public class FontConfig
    {
        /// <summary>
        /// 绘制字体
        /// </summary>
        public string fontname { get; set; }
        /// <summary>
        /// 字体颜色
        /// </summary>
        public string fontcolor { get; set; }
        /// <summary>
        /// 字体样式
        /// </summary>
        public FontStyle fontstyle { get; set; }
        /// <summary>
        /// 字体大小
        /// </summary>
        public int fontsize { get; set; }
        /// <summary>
        /// 字体单位
        /// </summary>
        public GraphicsUnit fontunit { get; set; }
    }
}
