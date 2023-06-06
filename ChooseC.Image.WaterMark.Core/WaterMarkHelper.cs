using ChooseC.Image.WaterMark.Core.Enum;
using ChooseC.Image.WaterMark.Core.Model;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;


namespace ChooseC.Image.WaterMark.Core
{
    public class WaterMarkHelper : Base
    {
        /// <summary>
        /// 创建水印图片在底部，布局为上下
        /// </summary>
        /// <param name="fileinfo"></param>
        /// <param name="settings"></param>
        /// <param name="direction"></param>
        /// <param name="exportfolder"></param>
        public static Task CreateWaterMark_TopBottom(FileInfo fileinfo, Settings settings, DrawDirectionEnum direction, DirectoryInfo exportfolder)
        {
            System.Drawing.Image topBitmap = null;
            System.Drawing.Image bottomBitmap = null;
            System.Drawing.Image originBitmap = null;
            System.Drawing.Image exportBitmap = null;
            try
            {
                var config = settings.topbottom;
                using (FileStream stream = fileinfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    #region EXIF 信息数据
                    var fields = typeof(InfoTag).GetFields(BindingFlags.Public | BindingFlags.Static);
                    var infoDict = ImageMetaDataHelper.GetMetaData(
                        stream
                        , new[] {
                            new KeyValuePair<string, string>("sign", settings.sign)
                        }
                        , fields.Select(p => p.Name).ToArray());
                    //string format
                    var strFormatter = GetStringFormat(direction);
                    #endregion

                    #region get top logo Bitmap
                    var strCamermake = infoDict[nameof(InfoTag.camermake)];
                    switch (config.toplogo)
                    {
                        case nameof(LogoTypeEnum.imglogo):
                            var logofile = GetLogos(LogosFolder, strCamermake)?.FullName;
                            if (logofile is not null) topBitmap = Bitmap.FromFile(logofile);
                            break;
                        case nameof(LogoTypeEnum.wordlogo):
                            var logoFont = new WaterMarkHelper().GetFont(config.wordlogofont.fontname,
                                config.wordlogofont.fontstyle,
                                config.wordlogofont.fontsize,
                                config.wordlogofont.fontunit);

                            topBitmap = CreateWordImage(
                                strCamermake,
                                strFormatter,
                                logoFont,
                                GetColor(config.fillcolor),
                                GetColor(config.wordlogofont.fontcolor),
                                Size.Empty,
                                10,
                                10);
                            break;
                        case nameof(LogoTypeEnum.none):
                            break;
                        default:
                            break;
                    }
                    #endregion

                    #region origin Bitmap 
                    originBitmap = FillImage(
                        stream,
                        GetColor(config.fillcolor),
                        settings.exportsize,
                        0);
                    if (originBitmap is null) throw new Exception("无法解析原图对象！");
                    #endregion

                    #region get bottom info Bitmap
                    //font
                    using (var font = new WaterMarkHelper().GetFont(
                        config.infofont.fontname,
                        config.infofont.fontstyle,
                        config.infofont.fontsize,
                        config.infofont.fontunit)
                    )
                    {
                        //info
                        var bottominfo = FormatterInfo(config.bottominfo, infoDict);
                        //size
                        var bottomSize = new Size(originBitmap.Width, 0);

                        bottomBitmap = CreateWordImage(
                            bottominfo, strFormatter, font,
                            GetColor(config.fillcolor),
                            GetColor(config.infofont.fontcolor),
                            bottomSize, 10, 10);
                    }
                    #endregion

                    #region 缩放logo
                    if (config.logmaxheight > 0)
                    {
                        var maxheight = config.logmaxheight <= 1 ?
                            (int)Math.Round(originBitmap.Height * config.logmaxheight, 0)
                            :
                            (int)config.logmaxheight;
                        if (topBitmap != null && topBitmap.Height > maxheight)
                        {
                            var logoRatio = (double)topBitmap.Width / topBitmap.Height;
                            var resize = new Size(
                                (int)(Math.Round(logoRatio * maxheight, 0)),
                                maxheight);
                            topBitmap = new Bitmap(topBitmap, resize);
                        }
                    }
                    #endregion

                    #region 计算绘制的图片大小并创建Bitmap

                    var fillW = 0;
                    var fillH = 0;
                    var fillratio = config.fillratio;
                    if (fillratio > 0 && fillratio <= 1)
                    {
                        fillW = (int)Math.Round(originBitmap.Width * fillratio, 0);
                        fillH = (int)Math.Round(originBitmap.Height * fillratio, 0);
                    }
                    else
                    {
                        fillW = (int)fillratio * 2;
                        fillH = (int)fillratio * 2;
                    }

                    var bottomH = 0;
                    var bottomMargin = 0;
                    if (topBitmap is not null || bottomBitmap is not null)
                    {
                        bottomH += (topBitmap == null ? 0 : topBitmap.Height)
                        + (bottomBitmap == null ? 0 : bottomBitmap.Height);

                        if (config.bottommargin > 0) bottomMargin = config.bottommargin + fillH / 2;
                    }

                    //总高度 = 原图缩放后的高度 + fillratio * 2 +  bottomBitmap + bottommargin * 2
                    //总宽度 = 原图缩放后的宽度 + fillratio * 2 

                    var totalHeight = originBitmap.Height + fillH + bottomH + bottomMargin + fillH / 2;
                    var totalWidth = originBitmap.Width + fillW;

                    exportBitmap = new Bitmap(totalWidth, totalHeight);
                    //绘制起点
                    var x = fillW / 2;
                    var y = fillH / 2;
                    #endregion

                    #region graphics 属性初始化
                    using Graphics graphics = Graphics.FromImage(exportBitmap);
                    #region 图像质量
                    //字体质量
                    graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                    graphics.TextContrast = 4;//高对比度,default : 4 
                    //图像质量
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    #endregion
                    //背景颜色设置
                    graphics.Clear(GetColor(config.fillcolor));
                    #endregion

                    #region 绘制

                    graphics.DrawImage(originBitmap, x, y, originBitmap.Width, originBitmap.Height);
                    if (topBitmap is not null)
                    {
                        x = fillW / 2 + (originBitmap.Width - topBitmap.Width) / 2;
                        y = originBitmap.Height + fillH + bottomMargin;
                        graphics.DrawImage(topBitmap, x, y, topBitmap.Width, topBitmap.Height);
                    }
                    if (bottomBitmap is not null)
                    {
                        x = fillW / 2 + (originBitmap.Width - bottomBitmap.Width) / 2;
                        y = originBitmap.Height
                            + fillH
                            + (topBitmap == null ? 0 : topBitmap.Height)
                            + bottomMargin;
                        graphics.DrawImage(bottomBitmap, x, y, bottomBitmap.Width, bottomBitmap.Height);
                    }
                    graphics.Flush();
                    graphics.Dispose();
                    #endregion

                    #region 导出保存
                    if (exportfolder is null)
                    {
                        var outPath = Path.Combine(AppContext.BaseDirectory, settings.exportfolder);
                        if (!Directory.Exists(outPath)) Directory.CreateDirectory(outPath);
                        exportfolder = new DirectoryInfo(outPath);
                    }
                    SaveImageFile(
                        exportBitmap,
                        fileinfo.Name.Replace(fileinfo.Extension, ""),
                        settings.exporttype,
                        exportfolder.FullName,
                        settings.exportquality
                        );
                    #endregion
                }
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                topBitmap.Dispose();
                bottomBitmap.Dispose();
                originBitmap.Dispose();
                exportBitmap.Dispose();
            }
        }
        /// <summary>
        /// 创建水印图片在底部，布局为左右
        /// </summary>
        /// <param name="fileinfo"></param>
        /// <param name="settings"></param>
        /// <param name="direction"></param>
        /// <param name="exportfolder"></param>
        public static Task CreateWaterMark_Bottom(FileInfo fileinfo, Settings settings, DrawDirectionEnum direction, DirectoryInfo exportfolder)
        {
            System.Drawing.Image leftBitmap = null, leftinfoBitmap = null,
                        rightBitmap = null, rightinfoBitmap = null,
                        originBitmap = null, exportBitmap = null;
            try
            {
                var config = settings.bottom;
                using (FileStream stream = fileinfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    #region EXIF 信息
                    //exif info data
                    var fields = typeof(InfoTag).GetFields(BindingFlags.Public | BindingFlags.Static);
                    var infoDict = ImageMetaDataHelper.GetMetaData(
                        stream
                        , new[] {
                            new KeyValuePair<string, string>("sign", settings.sign)
                        }
                        , fields.Select(p => p.Name).ToArray());
                    //string format
                    var sf = GetStringFormat(direction);
                    #endregion

                    /** 布局思路
                     * 计算底部可绘制的宽度 = 图片宽度
                     * 左右对半分，计算绘制文字图片，
                     * 优先info 模块，以info模块的高度作为logo的最大高度
                     * logo宽高比 1：1,如果高度超过剩余宽度，则不显示logo
                     * */

                    #region logo Font

                    #endregion

                    #region origin bitmap 
                    originBitmap = FillImage(
                        stream,
                        GetColor(config.fillcolor),
                        settings.exportsize,
                        0);
                    if (originBitmap is null) throw new Exception("无法解析原图对象！");
                    #endregion

                    #region 计算底部宽度分配
                    var originW = originBitmap.Width;
                    var leftW = (int)Math.Round(originW * config.bottomratio.left.value);
                    var rightW = (int)Math.Round(originW * config.bottomratio.right.value);

                    var leftpadding = 10;
                    var toppadding = 10;
                    var splitPadding = settings.splitpadding;

                    if (splitPadding > 0)
                        leftpadding = 0; toppadding = splitPadding;

                    //info
                    var leftinfo = FormatterInfo(config.leftinfo, infoDict);
                    var rightinfo = FormatterInfo(config.rightinfo, infoDict);
                    //size
                    var leftinfoSize = new Size(leftW, config.bottommaxsize.Height);
                    var rightinfoSize = new Size(rightW, config.bottommaxsize.Height);

                    #endregion

                    #region    left  info Bitmap 
                    if (!string.IsNullOrWhiteSpace(leftinfo))
                    {
                        using (var font = new WaterMarkHelper().GetFont(
                        config.infofont.fontname, config.infofont.fontstyle,
                        config.infofont.fontsize, config.infofont.fontunit))
                        {
                            leftinfoBitmap = CreateWordImage(
                            leftinfo,
                            sf,
                            font,
                            GetColor(config.fillcolor),
                            GetColor(config.infofont.fontcolor),
                            leftinfoSize,
                            leftpadding,
                            toppadding,
                            settings.lineheight);
                        }
                    }
                    #endregion

                    #region  right info Bitmap
                    if (!string.IsNullOrWhiteSpace(rightinfo))
                    {
                        using (var font = new WaterMarkHelper().GetFont(
                        config.infofont.fontname, config.infofont.fontstyle,
                        config.infofont.fontsize, config.infofont.fontunit))
                        {
                            rightinfoBitmap = CreateWordImage(
                            rightinfo,
                            sf,
                            font,
                            GetColor(config.fillcolor),
                            GetColor(config.infofont.fontcolor),
                            rightinfoSize,
                            leftpadding,
                            toppadding,
                            settings.lineheight);
                        }
                    }
                    #endregion

                    #region 判断是否显示logo,并计算logo的宽高

                    var leftinfoW = leftinfoBitmap == null ? 0 : leftinfoBitmap.Width;
                    var rightinfoW = rightinfoBitmap == null ? 0 : rightinfoBitmap.Width;
                    //底部最大高度
                    var maxheight = Math.Max(leftinfoBitmap.Height, rightinfoBitmap.Height);

                    //Left 剩余可绘制LOGO空间
                    var leftlogoW = originW - leftinfoW - rightinfoW;
                    //logo宽高比 1：1
                    var leftMaxHeight = maxheight;
                    //如果可绘制空间不足，不绘制logo
                    if (leftlogoW < leftMaxHeight) leftMaxHeight = 0;

                    //Right 剩余可绘制空间
                    var rightlogoW = originW - leftMaxHeight - leftinfoW - rightinfoW;
                    //logo宽高比 1：1
                    //默认绘制logo高和信息高度一致
                    var rightMaxHeight = maxheight;
                    //如果可绘制空间不足，不绘制logo
                    if (rightlogoW < rightMaxHeight) rightMaxHeight = 0;

                    #endregion

                    #region get left logo Bitmap

                    if (leftMaxHeight > 0)
                    {
                        var camermake = infoDict[nameof(InfoTag.camermake)];
                        switch (config.leftlogo)
                        {
                            case nameof(LogoTypeEnum.imglogo):
                                var logofile = GetLogos(LogosFolder, camermake)?.FullName;
                                if (logofile is not null) leftBitmap = GetLogo(logofile, 0, leftMaxHeight);
                                break;
                            case nameof(LogoTypeEnum.wordlogo):
                                //默认字体大小 = 绘制高度 - toppadding * 2
                                //计算绘制宽度 = 字体大小 * 字体数量
                                var fsize = leftMaxHeight - (2 * toppadding);
                                var drawWidth = fsize * camermake.Length;
                                if (drawWidth > leftlogoW)
                                {
                                    fsize = leftlogoW / camermake.Length;
                                    drawWidth = leftlogoW;
                                }
                                using (var logoFont = new WaterMarkHelper().GetFont(config.wordlogofont.fontname,
                                        config.wordlogofont.fontstyle, fsize, GraphicsUnit.Pixel)
                                    )
                                {
                                    leftBitmap = CreateWordImage(
                                        camermake,
                                        sf,
                                        logoFont,
                                        GetColor(config.fillcolor),
                                        GetColor(config.wordlogofont.fontcolor),
                                        new Size(drawWidth, 0), leftpadding, toppadding, 0);
                                }
                                break;
                            case nameof(LogoTypeEnum.none):
                                break;
                            default:
                                break;
                        }
                    }

                    #endregion

                    #region get right logo Bitmap
                    if (rightMaxHeight > 0)
                    {

                        var camermake = infoDict[nameof(InfoTag.camermake)];
                        var lensmake = infoDict[nameof(InfoTag.lensmake)];
                        switch (config.rightlogo)
                        {
                            case nameof(LogoTypeEnum.imglogo):
                                var logofile = GetLogos(LogosFolder, lensmake)?.FullName;
                                if (logofile is not null) rightBitmap = GetLogo(logofile, 0, rightMaxHeight);
                                break;
                            case nameof(LogoTypeEnum.wordlogo):
                                //默认字体大小 = 绘制高度 - toppadding * 2
                                //计算绘制宽度 = 字体大小 * 字体数量
                                var fsize = rightMaxHeight - (2 * toppadding);
                                var drawWidth = fsize * camermake.Length;
                                if (drawWidth > rightlogoW)
                                {
                                    fsize = rightlogoW / camermake.Length;
                                    drawWidth = rightlogoW;
                                }

                                using (var logoFont = new WaterMarkHelper().GetFont(
                                                       config.wordlogofont.fontname, config.wordlogofont.fontstyle,
                                                       fsize, GraphicsUnit.Pixel)
                                   )
                                {
                                    rightBitmap = CreateWordImage(
                                        camermake, sf, logoFont,
                                       GetColor(config.fillcolor), GetColor(config.infofont.fontcolor),
                                        new Size(drawWidth, 0), leftpadding, toppadding, 0);
                                }
                                break;
                            case nameof(LogoTypeEnum.none):
                                break;
                            default:
                                break;
                        }
                    }
                    #endregion

                    #region 计算绘制的图片大小并创建Bitmap

                    var fillW = 0;
                    var fillH = 0;
                    var fillratio = config.fillratio;
                    if (fillratio > 0 && fillratio <= 1)
                    {
                        fillW = (int)Math.Round(originBitmap.Width * fillratio, 0);
                        fillH = (int)Math.Round(originBitmap.Height * fillratio, 0);
                    }
                    else
                    {
                        fillW = (int)fillratio * 2;
                        fillH = (int)fillratio * 2;
                    }

                    var bottomH = 0;
                    var bottomMargin = 0;

                    if (leftBitmap is not null || leftinfoBitmap is not null
                        || rightBitmap is not null || rightinfoBitmap is not null)
                    {
                        bottomH = Math.Max(
                            Math.Max(leftBitmap == null ? 0 : leftBitmap.Height, leftinfoBitmap == null ? 0 : leftinfoBitmap.Height),
                            Math.Max(rightBitmap == null ? 0 : rightBitmap.Height, rightinfoBitmap == null ? 0 : rightinfoBitmap.Height)
                            );
                        if (config.bottommargin > 0) bottomMargin = config.bottommargin + fillH / 2;
                    }

                    //总高度 = 原图缩放后的高度 + fillratio * 2(顶部+底部) +  bottomBitmap + bottommargin + fillratio（中间）
                    //总宽度 = 原图缩放后的宽度 + fillratio * 2 

                    var totalHeight = originBitmap.Height + fillH + bottomH + bottomMargin + fillH / 2;
                    var totalWidth = originBitmap.Width + fillW;

                    exportBitmap = new Bitmap(totalWidth, totalHeight);
                    //绘制起点
                    var x = fillW / 2;
                    var y = fillH / 2;
                    #endregion

                    #region  graphics 对象初始化
                    using Graphics graphics = Graphics.FromImage(exportBitmap);
                    //图像字体质量
                    graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                    graphics.TextContrast = 4;//高对比度,default : 4 
                    //图像质量
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    //背景颜色设置
                    graphics.Clear(GetColor(config.fillcolor));
                    #endregion

                    #region 绘制

                    graphics.DrawImage(originBitmap, x, y, originBitmap.Width, originBitmap.Height);
                    if (leftBitmap is not null)
                    {
                        x = fillW / 2;
                        y = originBitmap.Height + fillH + bottomMargin
                            + (leftinfoBitmap == null ? 0 : (rightinfoBitmap.Height - leftBitmap.Height) / 2);
                        graphics.DrawImage(leftBitmap, x, y, leftBitmap.Width, leftBitmap.Height);
                    }
                    if (leftinfoBitmap is not null)
                    {
                        x = fillW / 2 + (leftBitmap == null ? 0 : leftBitmap.Width) + settings.splitpadding;
                        y = originBitmap.Height + fillH + bottomMargin;//+ (bottomH - leftinfoBitmap.Height) / 2;
                        graphics.DrawLine(
                            new Pen(GetColor(settings.splitlinecolor)),
                            x, y,
                            x, y + leftinfoBitmap.Height);

                        x = fillW / 2 + (leftBitmap == null ? 0 : leftBitmap.Width) + settings.splitpadding * 2;
                        y = originBitmap.Height + fillH + bottomMargin;//+ (bottomH - leftinfoBitmap.Height) / 2;
                        graphics.DrawImage(leftinfoBitmap, x, y, leftinfoBitmap.Width, leftinfoBitmap.Height);
                    }

                    if (rightBitmap is not null)
                    {
                        x = originBitmap.Width + fillW / 2 - rightBitmap.Width - (rightinfoBitmap == null ? 0 : rightinfoBitmap.Width) - (settings.splitpadding * 2);
                        y = originBitmap.Height + fillH + bottomMargin
                            + (rightinfoBitmap == null ? 0 : (rightinfoBitmap.Height - rightBitmap.Height) / 2);
                        graphics.DrawImage(rightBitmap, x, y, rightBitmap.Width, rightBitmap.Height);
                    }
                    if (rightinfoBitmap is not null)
                    {
                        x = originBitmap.Width + fillW / 2 - rightinfoBitmap.Width - settings.splitpadding;
                        y = originBitmap.Height + fillH + bottomMargin;//+ (bottomH - leftinfoBitmap.Height) / 2;
                        graphics.DrawLine(
                            new Pen(GetColor(settings.splitlinecolor)),
                            x, y,
                            x, y + rightinfoBitmap.Height);

                        x = originBitmap.Width + fillW / 2 - rightinfoBitmap.Width;
                        y = originBitmap.Height + fillH + bottomMargin;
                        graphics.DrawImage(rightinfoBitmap, x, y, rightinfoBitmap.Width, rightinfoBitmap.Height);
                    }
                    graphics.Flush();
                    graphics.Dispose();
                    #endregion

                    #region 导出保存

                    if (exportfolder is null)
                    {
                        var outPath = Path.Combine(AppContext.BaseDirectory, settings.exportfolder);
                        if (!Directory.Exists(outPath)) Directory.CreateDirectory(outPath);
                        exportfolder = new DirectoryInfo(outPath);
                    }
                    SaveImageFile(
                        exportBitmap,
                        fileinfo.Name.Replace(fileinfo.Extension, ""),
                        settings.exporttype,
                        exportfolder.FullName,
                        settings.exportquality
                        );
                    #endregion
                }
                return Task.CompletedTask;
            }
            catch
            {
                throw;
            }
            finally
            {
                leftBitmap?.Dispose();
                leftinfoBitmap?.Dispose();
                rightBitmap?.Dispose();
                rightinfoBitmap?.Dispose();
                originBitmap?.Dispose();
                exportBitmap?.Dispose();
            }
        }
        [Obsolete("功能已合并至Bottom类型")]
        /// <summary>
        /// 创建水印图片在底部，布局为包围
        /// </summary>
        /// <param name="fileinfo"></param>
        /// <param name="settings"></param>
        /// <param name="direction"></param>
        /// <param name="exportfolder"></param>
        public static void CreateWaterMark_Surround(FileInfo fileinfo, Settings settings, DrawDirectionEnum direction, DirectoryInfo exportfolder)
        {
            System.Drawing.Image leftBitmap = null, leftinfoBitmap = null,
                        rightBitmap = null, rightinfoBitmap = null,
                        originBitmap = null, exportBitmap = null;
            try
            {
                var config = settings.surround;
                using (FileStream stream = fileinfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    #region EXIF 信息
                    //exif info data
                    var fields = typeof(InfoTag).GetFields(BindingFlags.Public | BindingFlags.Static);
                    var infoDict = ImageMetaDataHelper.GetMetaData(
                        stream
                        , new[] {
                            new KeyValuePair<string, string>("sign", settings.sign)
                        }
                        , fields.Select(p => p.Name).ToArray());
                    //string format
                    var sf = GetStringFormat(direction);
                    #endregion

                    /** 布局思路
                     * 计算底部可绘制的宽度 = 图片宽度
                     * 左右对半分，计算绘制文字图片，
                     * 优先info 模块，以info模块的高度作为logo的最大高度
                     * logo宽高比 1：1,如果高度超过剩余宽度，则不显示logo
                     * */

                    #region logo Font

                    #endregion

                    #region origin bitmap 
                    originBitmap = FillImage(
                        stream,
                        GetColor(config.fillcolor),
                        settings.exportsize,
                        0);
                    if (originBitmap is null) throw new Exception("无法解析原图对象！");
                    #endregion

                    #region 计算底部宽度分配

                    var leftW = (int)Math.Round(originBitmap.Width * config.bottomratio.left.value);
                    var rightW = (int)Math.Round(originBitmap.Width * config.bottomratio.right.value);

                    #endregion

                    #region    get left  info Bitmap || get right info Bitmap
                    //font

                    var leftpadding = 10;
                    var toppadding = 10;
                    var splitPadding = settings.splitpadding;

                    if (splitPadding > 0)
                        leftpadding = 0; toppadding = splitPadding;

                    //info
                    var leftinfo = FormatterInfo(config.leftinfo, infoDict);
                    var rightinfo = FormatterInfo(config.rightinfo, infoDict);
                    //size
                    var leftinfoSize = new Size(leftW, config.bottommaxsize.Height);
                    var rightinfoSize = new Size(rightW, config.bottommaxsize.Height);

                    using (var font = new WaterMarkHelper().GetFont(
                        config.infofont.fontname, config.infofont.fontstyle,
                        config.infofont.fontsize, config.infofont.fontunit))
                    {
                        leftinfoBitmap = CreateWordImage(
                        leftinfo,
                        sf,
                        font,
                        GetColor(config.fillcolor),
                        GetColor(config.infofont.fontcolor),
                        leftinfoSize,
                        leftpadding,
                        toppadding,
                        settings.lineheight);
                    }
                    using (var font = new WaterMarkHelper().GetFont(
                        config.infofont.fontname, config.infofont.fontstyle,
                        config.infofont.fontsize, config.infofont.fontunit))
                    {
                        rightinfoBitmap = CreateWordImage(
                        rightinfo,
                        sf,
                        font,
                        GetColor(config.fillcolor),
                        GetColor(config.infofont.fontcolor),
                        rightinfoSize,
                        leftpadding,
                        toppadding,
                        settings.lineheight);
                    }
                    #endregion

                    #region 判断是否显示logo,并计算logo的宽高

                    var leftinfoW = leftinfoBitmap.Width;
                    var rightinfoW = rightinfoBitmap.Width;

                    var maxheight = Math.Max(leftinfoBitmap.Height, rightinfoBitmap.Height);


                    var leftlogoW = leftW - leftinfoW;
                    //logo宽高比 1：1
                    var leftMaxHeight = leftlogoW;
                    if (maxheight <= leftlogoW) leftMaxHeight = maxheight;

                    var rightlogoW = rightW - rightinfoW;
                    //logo宽高比 1：1
                    var rightMaxHeight = rightlogoW;
                    if (maxheight <= rightlogoW) rightMaxHeight = maxheight;

                    #endregion

                    #region get left logo Bitmap

                    if (leftlogoW > 0)
                    {
                        var camermake = infoDict[nameof(InfoTag.camermake)];
                        switch (config.leftlogo)
                        {
                            case nameof(LogoTypeEnum.imglogo):
                                var logofile = GetLogos(LogosFolder, camermake)?.FullName;
                                if (logofile is not null) leftBitmap = GetLogo(logofile, 0, leftMaxHeight);
                                break;
                            case nameof(LogoTypeEnum.wordlogo):
                                //根据最大高度重新计算字体大小
                                var drawHeight = leftMaxHeight + rightW;
                                var fsize = drawHeight / camermake.Length;
                                using (var logoFont = new WaterMarkHelper().GetFont(
                                                        config.wordlogofont.fontname, config.wordlogofont.fontstyle,
                                                        fsize, GraphicsUnit.Pixel)
                                    )
                                {

                                    leftBitmap = CreateWordImage(
                                        camermake,
                                        sf,
                                        logoFont,
                                        GetColor(config.fillcolor),
                                        GetColor(config.wordlogofont.fontcolor),
                                        new Size(0, drawHeight), leftpadding, toppadding, 0);
                                }
                                break;
                            case nameof(LogoTypeEnum.none):
                                break;
                            default:
                                break;
                        }
                    }

                    #endregion

                    #region get right logo Bitmap
                    if (rightlogoW > 0)
                    {

                        var camermake = infoDict[nameof(InfoTag.camermake)];
                        var lensmake = infoDict[nameof(InfoTag.lensmake)];
                        switch (config.rightlogo)
                        {
                            case nameof(LogoTypeEnum.imglogo):
                                var logofile = GetLogos(LogosFolder, lensmake)?.FullName;
                                if (logofile is not null) rightBitmap = GetLogo(logofile, 0, rightMaxHeight);
                                break;
                            case nameof(LogoTypeEnum.wordlogo):
                                //根据最大高度重新计算字体大小
                                //根据公式：绘制宽度 = 字体大小 * 字符数量
                                //字体大小 = 绘制的最大宽度 / 字符数量【避免换行】
                                var drawHeight = rightMaxHeight + (leftW - leftMaxHeight - leftinfoW);
                                var fsize = drawHeight / camermake.Length;
                                using (var logoFont = new WaterMarkHelper().GetFont(
                                                       config.wordlogofont.fontname, config.wordlogofont.fontstyle,
                                                       fsize, GraphicsUnit.Pixel)
                                   )
                                {
                                    rightBitmap = CreateWordImage(
                                        camermake, sf, logoFont,
                                       GetColor(config.fillcolor), GetColor(config.infofont.fontcolor),
                                        new Size(0, drawHeight), leftpadding, toppadding, 0);
                                }
                                break;
                            case nameof(LogoTypeEnum.none):
                                break;
                            default:
                                break;
                        }
                    }
                    #endregion

                    #region 计算绘制的图片大小并创建Bitmap

                    var fillW = 0;
                    var fillH = 0;
                    var fillratio = config.fillratio;
                    if (fillratio > 0 && fillratio <= 1)
                    {
                        fillW = (int)Math.Round(originBitmap.Width * fillratio, 0);
                        fillH = (int)Math.Round(originBitmap.Height * fillratio, 0);
                    }
                    else
                    {
                        fillW = (int)fillratio * 2;
                        fillH = (int)fillratio * 2;
                    }

                    var bottomH = 0;
                    var bottomMargin = 0;

                    if (leftBitmap is not null || leftinfoBitmap is not null
                        || rightBitmap is not null || rightinfoBitmap is not null)
                    {
                        bottomH = Math.Max(
                            Math.Max(leftBitmap == null ? 0 : leftBitmap.Height, leftinfoBitmap == null ? 0 : leftinfoBitmap.Height),
                            Math.Max(rightBitmap == null ? 0 : rightBitmap.Height, rightinfoBitmap == null ? 0 : rightinfoBitmap.Height)
                            );
                        if (config.bottommargin > 0) bottomMargin = config.bottommargin + fillH / 2;
                    }

                    //总高度 = 原图缩放后的高度 + fillratio * 2(顶部+底部) +  bottomBitmap + bottommargin + fillratio（中间）
                    //总宽度 = 原图缩放后的宽度 + fillratio * 2 

                    var totalHeight = originBitmap.Height + fillH + bottomH + bottomMargin + fillH / 2;
                    var totalWidth = originBitmap.Width + fillW;

                    exportBitmap = new Bitmap(totalWidth, totalHeight);
                    //绘制起点
                    var x = fillW / 2;
                    var y = fillH / 2;
                    #endregion

                    #region  graphics 对象初始化
                    using Graphics graphics = Graphics.FromImage(exportBitmap);
                    //图像字体质量
                    graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                    graphics.TextContrast = 4;//高对比度,default : 4 
                    //图像质量
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    //背景颜色设置
                    graphics.Clear(GetColor(config.fillcolor));
                    #endregion

                    #region 绘制

                    graphics.DrawImage(originBitmap, x, y, originBitmap.Width, originBitmap.Height);
                    if (leftBitmap is not null)
                    {
                        x = fillW / 2;
                        y = originBitmap.Height + fillH + bottomMargin;// + (bottomH - leftBitmap.Height) / 2;
                        graphics.DrawImage(leftBitmap, x, y, leftBitmap.Width, leftBitmap.Height);
                    }
                    if (leftinfoBitmap is not null)
                    {
                        x = fillW / 2 + (leftBitmap == null ? 0 : leftBitmap.Width) + settings.splitpadding;
                        y = originBitmap.Height + fillH + bottomMargin;//+ (bottomH - leftinfoBitmap.Height) / 2;
                        graphics.DrawLine(
                            new Pen(GetColor(settings.splitlinecolor)),
                            x, y,
                            x, y + leftinfoBitmap.Height);

                        x = fillW / 2 + (leftBitmap == null ? 0 : leftBitmap.Width) + settings.splitpadding * 2;
                        y = originBitmap.Height + fillH + bottomMargin;//+ (bottomH - leftinfoBitmap.Height) / 2;
                        graphics.DrawImage(leftinfoBitmap, x, y, leftinfoBitmap.Width, leftinfoBitmap.Height);
                    }

                    if (rightBitmap is not null)
                    {
                        x = originBitmap.Width + fillW / 2 - rightBitmap.Width - (rightinfoBitmap == null ? 0 : rightinfoBitmap.Width) - (settings.splitpadding * 2);
                        y = originBitmap.Height + fillH + bottomMargin;//+ (bottomH - rightBitmap.Height) / 2;
                        graphics.DrawImage(rightBitmap, x, y, rightBitmap.Width, rightBitmap.Height);
                    }
                    if (rightinfoBitmap is not null)
                    {
                        x = originBitmap.Width + fillW / 2 - rightinfoBitmap.Width - settings.splitpadding;
                        y = originBitmap.Height + fillH + bottomMargin;//+ (bottomH - leftinfoBitmap.Height) / 2;
                        graphics.DrawLine(
                            new Pen(GetColor(settings.splitlinecolor)),
                            x, y,
                            x, y + rightinfoBitmap.Height);

                        x = originBitmap.Width + fillW / 2 - rightinfoBitmap.Width;
                        y = originBitmap.Height + fillH + bottomMargin;//+ (bottomH - rightinfoBitmap.Height) / 2;
                        graphics.DrawImage(rightinfoBitmap, x, y, rightinfoBitmap.Width, rightinfoBitmap.Height);
                    }
                    graphics.Flush();
                    graphics.Dispose();
                    #endregion

                    #region 导出保存

                    if (exportfolder is null)
                    {
                        var outPath = Path.Combine(AppContext.BaseDirectory, settings.exportfolder);
                        if (!Directory.Exists(outPath)) Directory.CreateDirectory(outPath);
                        exportfolder = new DirectoryInfo(outPath);
                    }
                    SaveImageFile(
                        exportBitmap,
                        fileinfo.Name.Replace(fileinfo.Extension, ""),
                        settings.exporttype,
                        exportfolder.FullName,
                        settings.exportquality
                        );
                    #endregion
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                leftBitmap.Dispose();
                leftinfoBitmap.Dispose();
                rightBitmap.Dispose();
                rightinfoBitmap.Dispose();
                originBitmap.Dispose();
                exportBitmap.Dispose();
            }
        }

        /// <summary>
        /// 只填充图片
        /// </summary>
        /// <param name="fileinfo"></param>
        /// <param name="settings"></param>
        /// <param name="direction"></param>
        /// <param name="exportfolder"></param>
        public static Task CreateWaterMark_OnlyFill(FileInfo fileinfo, Settings settings, DrawDirectionEnum direction, DirectoryInfo exportfolder, Size exportSize)
        {
            try
            {
                var config = settings.onlyfill;
                using (FileStream stream = fileinfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var bitmap = FillImage(
                        stream,
                        GetColor(config.fillcolor),
                        exportSize,
                        config.fillratio);

                    if (exportfolder is null)
                    {
                        var outPath = Path.Combine(AppContext.BaseDirectory, settings.exportfolder);
                        if (!Directory.Exists(outPath)) Directory.CreateDirectory(outPath);
                        exportfolder = new DirectoryInfo(outPath);
                    }
                    SaveImageFile(
                        bitmap,
                        fileinfo.Name.Replace(fileinfo.Extension, ""),
                        settings.exporttype,
                        exportfolder.FullName,
                        settings.exportquality
                        );
                    bitmap.Dispose();
                }
                return Task.CompletedTask;
            }
            catch
            {
                throw;
            }
        }


        /******************************************Common Method***********************************************/
        /// <summary>
        /// 获取logo,根据参数进行等比缩放
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="maxWidth"></param>
        /// <param name="maxHeight"></param>
        /// <returns></returns>
        public static Bitmap GetLogo(string filepath, int maxWidth, int maxHeight)
        {
            var logoBitmap = Bitmap.FromFile(filepath);
            var logoRatio = (double)logoBitmap.Width / logoBitmap.Height;
            if (maxWidth > 0 && maxHeight <= 0)
            {
                var resize = new Size(
                maxWidth,
                (int)(Math.Round(maxWidth / logoRatio, 0))
                );
                logoBitmap = new Bitmap(logoBitmap, resize);
            }
            else if (maxWidth <= 0 && maxHeight > 0)
            {
                var resize = new Size(
                    (int)(Math.Round(maxHeight * logoRatio, 0)),
                    maxHeight);
                logoBitmap = new Bitmap(logoBitmap, resize);
            }
            else if (maxWidth == 0 && maxHeight == 0)
            {
            }
            else
            {
            }
            return (Bitmap)logoBitmap;
        }
        /// <summary>
        /// 调整图片大小根据目标尺寸
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="fillcolor"></param>
        /// <param name="targetSize"></param>
        /// <param name="fillratio"></param>
        /// <remarks>
        /// 保持原图比例进行调整，相差的部份使用指定颜色填充
        /// </remarks>
        /// <returns></returns>
        public static Bitmap FillImage(Stream stream, Color fillcolor, Size targetSize, double fillratio = 0.1)
        {
            try
            {
                var image = Bitmap.FromStream(stream);
                if (targetSize == Size.Empty || targetSize == image.Size)
                {
                    targetSize = image.Size;
                    Bitmap bitmap = new Bitmap(targetSize.Width, targetSize.Height);
                    using Graphics graphics = Graphics.FromImage(bitmap);
                    //图像质量
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    //背景设置
                    graphics.Clear(fillcolor);

                    #region 计算坐标 (默认原图缩放10%的空间为空白填充)
                    var x = 0;
                    var y = 0;
                    if (fillratio > 0 && fillratio <= 1)
                    {
                        x = (int)Math.Ceiling(targetSize.Width * fillratio / 2);
                        y = (int)Math.Ceiling(targetSize.Height * fillratio / 2);
                    }
                    else
                    {
                        x = (int)fillratio;
                        y = (int)fillratio;
                    }
                    #endregion
                    graphics.DrawImage(image,
                                       x,
                                       y,
                                       (targetSize.Width - (x * 2)),
                                       (targetSize.Height - (y * 2)));
                    graphics.Flush();
                    graphics.Dispose();

                    return bitmap;
                }
                else
                {
                    var scaleSize = Size.Empty;
                    var originRatio = (double)image.Width / image.Height;
                    #region 宽/原图的宽高比 = 高 
                    if (targetSize.Width > 0 && targetSize.Height <= 0)
                    {
                        targetSize = new Size()
                        {
                            Width = targetSize.Width,
                            Height = (int)Math.Round(targetSize.Width / originRatio, 0)
                        };
                        scaleSize = targetSize;
                    }
                    #endregion
                    #region 原图的宽高比 * 高 = 宽
                    else if (targetSize.Width <= 0 && targetSize.Height > 0)
                    {
                        targetSize = new Size()
                        {
                            Width = (int)Math.Round(targetSize.Height * originRatio, 0),
                            Height = targetSize.Height
                        };
                        scaleSize = targetSize;
                    }
                    #endregion
                    else
                    {
                        //目标尺寸指定宽高
                        //获取可以绘制的最大图像缩放尺寸
                        scaleSize = ComputerMaxScaleSize(image.Size, targetSize);
                    }


                    Bitmap bitmap = new Bitmap(targetSize.Width, targetSize.Height);
                    using Graphics graphics = Graphics.FromImage(bitmap);
                    //图像质量
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    //背景设置
                    graphics.Clear(fillcolor);


                    //计算坐标，为了保持输出尺寸，将图像居中放置，忽略填充比率值
                    var x = (int)Math.Ceiling(((double)targetSize.Width - scaleSize.Width) / 2);
                    var y = (int)Math.Ceiling((double)(targetSize.Height - scaleSize.Height) / 2);

                    graphics.DrawImage(image,
                                       x, y,
                                       scaleSize.Width, scaleSize.Height);
                    graphics.Flush();
                    graphics.Dispose();
                    return bitmap;
                }
            }
            catch
            {
                throw;
            }
        }
        #region 绘制文字图片

        /// <summary>
        /// 绘制文字logo
        /// 默认会有间距
        /// </summary>
        /// <param name="word"></param>
        /// <param name="sf"></param>
        /// <param name="font"></param>
        /// <param name="fillcolor">填充颜色</param>
        /// <param name="leftPadding">左右间距</param>
        /// <param name="topPadding">上下间距</param>
        /// <param name="splitLineFlag">是否分割线</param>
        /// <returns></returns>
        public static Bitmap CreateWordImage(string word, StringFormat sf, Font font, Color fillcolor,
            Color pencolor, Size size, int leftPadding = 0, int topPadding = 0, double lineHeight = 1.5)
        {
            Bitmap bitmap = null;
            try
            {

                //默认宽度不设限
                var maxWidth = 9000;
                if (size != Size.Empty)
                {
                    //优先宽度
                    maxWidth = size.Width;
                    if (size.Width > 0 && size.Height <= 0) maxWidth = size.Width;
                    if (size.Width <= 0 && size.Height > 0) maxWidth = size.Height;
                }

                sf ??= StringFormat.GenericTypographic;
                //需要换行的数据，替换 ‘|’ 为换行符
                word = word.Replace("|", "\n");
                var lineH = (int)Math.Ceiling(lineHeight * font.Height);
                var lineArray = word.Split('\n');
                //总行高
                var totalLineHeight = lineH * (lineArray.Length - 1);
                //图片总宽：绘制的宽度 + 左右间隔宽度
                var totalWidth = 0;
                //图片总高：绘制的高度 + 上下间隔高度
                var totalHeight = 0;
                var maxwidth = 0;
                var maxheight = 0;
                //绘制bitmap尺寸
                var drawInfoSize = SizeF.Empty;
                foreach (var linestr in lineArray)
                {
                    //固定宽度，计算估测的图片高度【换行】
                    drawInfoSize = ComputerDrawStringSize(linestr, font, maxWidth, sf);
                    maxwidth = drawInfoSize.Width > maxwidth ? (int)Math.Ceiling(drawInfoSize.Width) : maxwidth;
                    maxheight += (int)Math.Ceiling(drawInfoSize.Height);
                }

                //图片总宽：绘制的最大宽度 + 左右间隔宽度
                totalWidth = 2 * leftPadding + maxwidth;
                //图片总高：绘制的总高度 + 上下间隔高度+行高
                totalHeight = totalLineHeight + maxheight + 2 * topPadding;

                //创建绘制的矩形
                //在矩形中绘制，会自动换行
                var drawRectangle = new RectangleF()
                {
                    //尺寸
                    Size = new Size(totalWidth, totalHeight),
                    //绘制开始点
                    Location = new PointF(leftPadding, topPadding),
                };

                //创建位图对象
                bitmap = new Bitmap(totalWidth, totalHeight);
                //创建图形对象
                using Graphics graphics = Graphics.FromImage(bitmap);
                #region 绘制图形质量配置
                //字体质量
                graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                graphics.TextContrast = 4;//高对比度,default : 4 
                //图形质量
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                #endregion
                #region 绘制方向 通过旋转图片绘制垂直方向的文字，ref:https://stackoverflow.com/questions/8970807
                if (sf.FormatFlags.HasFlag(StringFormatFlags.DirectionVertical))
                {
                    //结合旋转，可以实现从下到上书写，左对齐。
                    //sf.FormatFlags = StringFormatFlags.DirectionVertical|StringFormatFlags.DirectionRightToLeft;
                    //设置绘制点为右下角
                    //graphics.TranslateTransform(drawRectangle.Right, drawRectangle.Bottom);
                    //旋转180
                    //graphics.RotateTransform(180);
                }
                else
                {
                    //水平绘制，左对齐
                }
                #endregion
                //背景设置填充色
                graphics.Clear(fillcolor);

                using SolidBrush pen = new SolidBrush(pencolor);
                //绘制文字
                foreach (var linestr in lineArray)
                {
                    graphics.DrawString(linestr, font, pen, drawRectangle, sf);

                    drawRectangle.Location = new PointF(drawRectangle.Location.X, drawRectangle.Location.Y + lineH + font.Height);
                }

                graphics.Flush();

                if (Environment.GetEnvironmentVariable("INFOPIC") is not null)
                {
                    SaveImageFile(bitmap, "info", ImageFormat.Jpeg, Path.Combine(AppContext.BaseDirectory, "infoExport"));
                }
                return bitmap;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        #endregion
        /// <summary>
        /// 计算绘制文字的图片高度和宽度【不换行】
        /// </summary>
        /// <param name="strContent"></param>
        /// <param name="font"></param>
        /// <param name="stringFormat"></param>
        /// <param name="sizeF"></param>
        /// <param name="chartNum"></param>
        /// <param name="lineNum"></param>
        /// <returns></returns>
        public static SizeF ComputerDrawStringSize(string strContent, Font font, StringFormat stringFormat, SizeF sizeF, out int chartNum, out int lineNum)
        {
            Graphics graphics = Graphics.FromImage(new Bitmap((int)sizeF.Width, (int)sizeF.Height));
            try
            {
                var drawStringSize = graphics.MeasureString(strContent, font, sizeF, stringFormat, out chartNum, out lineNum);
                //var TextRenderdrawStringSize =TextRenderer.MeasureText(strContent, font);
                //drawStringSize.Width = TextRenderdrawStringSize.Width;
                //drawStringSize.Height = TextRenderdrawStringSize.Height;
                return drawStringSize;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                graphics?.Dispose();
            }
        }
        /// <summary>
        /// 指定最大宽度，计算绘制文字图像高度
        /// </summary>
        /// <param name="strContent"></param>
        /// <param name="font"></param>
        /// <param name="maxWidth"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        public static SizeF ComputerDrawStringSize(string strContent, Font font, int maxWidth, StringFormat stringFormat)
        {
            var tempBit = new Bitmap(maxWidth, 9000);
            try
            {
                using Graphics graphics = Graphics.FromImage(tempBit);
                var drawStringSize = graphics.MeasureString(strContent, font, maxWidth, stringFormat ?? StringFormat.GenericDefault);
                return drawStringSize;
            }
            catch
            {
                throw;
            }
            finally
            {
                tempBit.Dispose();
            }
        }

        #region Common Method
        /// <summary>
        /// 保存文件
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="imgname"></param>
        /// <param name="exporttype"></param>
        /// <param name="exportpath"></param>
        public static void SaveImageFile(System.Drawing.Image bitmap, string imgname, ImageFormat exporttype, string exportpath, long qualityNum = 100L)
        {
            //文件类型
            var imageFormat = exporttype;
            //名称 原图名称_yyMMddHHmmss.jpeg/bmp
            var filename = $"{imgname}_{DateTime.Now:yyMMddHHmmss}.{exporttype}";

            if (!Directory.Exists(exportpath)) Directory.CreateDirectory(exportpath);

            using FileStream filestream = File.OpenWrite(Path.Combine(exportpath, filename));

            EncoderParameters encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, qualityNum);

            var encoder = ImageCodecInfo.GetImageEncoders().FirstOrDefault(x => x.FormatID == imageFormat.Guid);

            bitmap.Save(filestream, encoder, encoderParameters);

            //bitmap.Save(filestream, imageFormat);
        }
        /// <summary>
        /// 格式化信息内容
        /// </summary>
        /// <param name="formatInfo"></param>
        /// <param name="dict"></param>
        /// <returns></returns>
        public static string FormatterInfo(string formatInfo, IReadOnlyDictionary<string, string> dict)
        {
            try
            {
                var formatterStr = formatInfo;
                var matchReg = @"(\{[\d\w\s]+\})";
                foreach (Match match in Regex.Matches(formatInfo, matchReg,
                                                        RegexOptions.None,
                                                        TimeSpan.FromSeconds(1)))
                {
                    var key = match.Value.Replace("{", "").Replace("}", "");
                    formatterStr = formatterStr.Replace(match.Value, dict[key] ?? string.Empty);
                }
                return formatterStr;
            }
            catch (Exception)
            {
                return formatInfo;
            }
        }
        /// <summary>
        /// 在指定文件夹下过滤获取logos
        /// </summary>
        /// <param name="make"></param>
        /// <returns></returns>
        public static FileInfo GetLogos(string folderpath, string make)
        {
            try
            {
                FileInfo fileinfo = null;
                foreach (var item in new DirectoryInfo(folderpath).GetFiles())
                {

                    var flag = make.ToLower().Contains(item.Name.Replace(item.Extension, "").ToLower());
                    if (flag) return item;
                }
                if (fileinfo is null)
                {
                    fileinfo = new DirectoryInfo(folderpath).GetFiles($"{make.ToLower()}*").FirstOrDefault();
                }
                return fileinfo;
            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// 获取字符绘制格式
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static StringFormat GetStringFormat(DrawDirectionEnum direction)
        {
            StringFormat sf = null;
            if (direction == DrawDirectionEnum.lefttoright)
            {
                sf = new StringFormat(StringFormat.GenericTypographic);
                sf.Alignment = StringAlignment.Near;
                sf.LineAlignment = StringAlignment.Near;
                sf.FormatFlags = StringFormatFlags.MeasureTrailingSpaces;
            }
            else if (direction == DrawDirectionEnum.toptobottom)
            {
                sf = new StringFormat(StringFormatFlags.DirectionVertical | StringFormatFlags.DirectionRightToLeft);
                sf.Alignment = StringAlignment.Near;
                sf.LineAlignment = StringAlignment.Near;
            }
            else
            {
                sf = new StringFormat(StringFormat.GenericTypographic);
                sf.Alignment = StringAlignment.Near;
                sf.LineAlignment = StringAlignment.Near;
                sf.FormatFlags = StringFormatFlags.MeasureTrailingSpaces;
            }
            return sf;
        }
        /// <summary>
        /// Hex Convert To Argb Color
        /// </summary>
        /// <param name="Hex"></param>
        /// <returns></returns>
        public static Color GetColor(string Hex)
        {
            var b = Hex.ToCharArray();
            var R = Convert.ToInt32(b[0].ToString() + b[1].ToString(), 16);
            var G = Convert.ToInt32(b[2].ToString() + b[3].ToString(), 16);
            var B = Convert.ToInt32(b[4].ToString() + b[5].ToString(), 16);
            var A = Convert.ToInt32(b[6].ToString() + b[7].ToString(), 16);//透明度
            return Color.FromArgb(A, R, G, B);
        }
        /// <summary>
        /// 计算原图保持宽高比前提下，在指定尺寸下最大的缩放大小
        /// </summary>
        /// <param name="originSize"></param>
        /// <param name="targetSize"></param>
        /// <returns></returns>
        public static Size ComputerMaxScaleSize(Size originSize, Size targetSize, double keepBlank = 0.05)
        {

            try
            {
                var scaleSize = Size.Empty;

                var originWidth = originSize.Width;
                var originHeight = originSize.Height;
                var originRatio = (float)originWidth / originHeight;
                if (targetSize == Size.Empty)
                {
                    scaleSize = originSize;
                }
                else if (targetSize.Width > 0 && targetSize.Height <= 0)
                {
                    scaleSize = new Size()
                    {
                        Width = targetSize.Width,
                        Height = (int)Math.Round(targetSize.Width / originRatio, 0)
                    };
                }
                else if (targetSize.Width <= 0 && targetSize.Height > 0)
                {
                    scaleSize = new Size()
                    {
                        Width = (int)Math.Round(targetSize.Height * originRatio, 0),
                        Height = targetSize.Height
                    };
                }
                else
                {
                    var targetWidth = targetSize.Width;
                    var targetHeight = targetSize.Height;
                    var targetRatio = (float)targetWidth / targetHeight;

                    //目标尺寸宽高比与原图相同,不需要留白。
                    if (targetRatio == originRatio)
                    {
                        scaleSize = targetSize;
                    }
                    else
                    {
                        /**目标尺寸与原图转换：
                        /   使用原图宽、高公约数，获取缩放的基数。
                        /   根据缩放的基数，分别求出目标宽、高与原图的比例
                        /   取最小的缩放比例，分别计算缩放后的宽、高；【等比缩放，保持了原图宽高比】
                        /   避免只有宽或高有留白,保留一定比例像素的留白
                        */

                        //原图宽、高公约数
                        var gcd = GCD(originWidth, originHeight);
                        var minwidth = originWidth / gcd;
                        var minheight = originHeight / gcd;

                        //根据缩放的基数，分别求出目标宽、高与原图的比例
                        var scaleW = (double)targetWidth / minwidth;
                        var scaleH = (double)targetHeight / minheight;
                        //取最小的缩放比例【保持原图宽高比，等比缩放】
                        var commonQuotient = Math.Min(scaleW, scaleH);
                        //计算缩放后的宽、高
                        var resizeW = minwidth * commonQuotient;
                        var resizeH = minheight * commonQuotient;

                        //避免只有宽或高有留白,保留一定比例像素的留白
                        if (resizeH == targetHeight && resizeW < targetWidth)
                        {
                            resizeH = resizeH - (int)Math.Round(resizeH * keepBlank, 0);
                            resizeW = (int)Math.Round(resizeH * originRatio, 0);
                        }
                        else if (resizeH < targetHeight && resizeW == targetWidth)
                        {
                            resizeW = resizeW - (int)Math.Round(resizeW * keepBlank, 0);
                            resizeH = (int)Math.Round(resizeW / originRatio, 0);
                        }

                        scaleSize = new Size((int)resizeW, (int)resizeH);
                    }
                }
                return scaleSize;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 公约数
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int GCD(int a, int b)
        {
            if (0 != b) while (0 != (a %= b) && 0 != (b %= a)) ;
            return a + b;
        }
        #endregion

    }
}
