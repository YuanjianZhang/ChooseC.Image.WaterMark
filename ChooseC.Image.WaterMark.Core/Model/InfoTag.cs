namespace ChooseC.Image.WaterMark.Core.Model
{
    /// <summary>
    /// 水印信息标签
    /// </summary>
    public class InfoTag
    {
        /// <summary>
        /// 个签
        /// </summary>
        public static readonly string sign = "Self Sign";
        /// <summary>
        /// 相机厂商
        /// </summary>
        public static readonly int camermake = 0x010F;
        /// <summary>
        /// 相机型号
        /// </summary>
        public static readonly int camermodel = 0x0110;
        /// <summary>
        /// 焦距
        /// </summary>
        public static readonly int focallenth = 0x920A;
        /// <summary>
        /// 光圈
        /// </summary>
        public static readonly int aperture = 0x829D;
        /// <summary>
        /// 快门/曝光
        /// </summary>
        public static readonly int exposure = 0x829A;
        /// <summary>
        /// ISO
        /// </summary>
        public static readonly int iso = 0x8827;
        /// <summary>
        /// 源拍摄时间
        /// </summary>
        public static readonly int origintime = 0x9003;
        /// <summary>
        /// 文件创建时间
        /// </summary>
        public static readonly int createtime = 0x0132;
        /// <summary>
        /// 地点
        /// </summary>
        public static readonly int location = 0xA214;
        /// <summary>
        /// 镜头厂商
        /// </summary>
        public static readonly int lensmake = 0xA433;
        /// <summary>
        /// 镜头型号
        /// </summary>
        public static readonly int lensmodel = 0xA434;
        /// <summary>
        /// 镜头规格
        /// </summary>
        public static readonly int lensspec = 0xA432;
    }
}
