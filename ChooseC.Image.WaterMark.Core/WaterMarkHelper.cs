using ChooseC.Image.WaterMark.Core.Enum;
using ChooseC.Image.WaterMark.Core.Model;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace ChooseC.Image.WaterMark.Core
{
    public class WaterMarkHelper
    {
        public static readonly string SourceFolder = Path.Combine(AppContext.BaseDirectory, "Source");
        public static readonly string FontsFolder = Path.Combine(SourceFolder, "Fonts");
        public static readonly string LogosFolder = Path.Combine(SourceFolder, "Logos");
        /// <summary>
        /// 创建水印图片在底部，布局为上下
        /// </summary>
        /// <param name="fileinfo"></param>
        /// <param name="settings"></param>
        /// <param name="direction"></param>
        /// <param name="exportfolder"></param>
        public static void CreateWaterMark_TopBottom(FileInfo fileinfo, Settings settings, DrawDirectionEnum direction, DirectoryInfo exportfolder)
        {
            try
            {
                var config = settings.topbottom;
                using (FileStream stream = fileinfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    //exif info data
                    var fields = typeof(InfoTag).GetFields(BindingFlags.Public | BindingFlags.Static);
                    var infoDict = ImageMetaDataHelper.GetMetaData(stream, fields.Select(p => p.Name).ToArray());
                    infoDict.Append(new KeyValuePair<string, string>("sign", settings.sign));
                    //string format
                    var strFormatter = GetStringFormat(direction);

                    System.Drawing.Image topBitmap = null;
                    System.Drawing.Image bottomBitmap = null;
                    #region get top logo
                    var strCamermake = infoDict[nameof(InfoTag.camermake)];
                    switch (config.toplogo)
                    {
                        case nameof(LogoTypeEnum.imglogo):
                            var logofile = GetLogos(LogosFolder, strCamermake)?.FullName;
                            if (logofile is not null) topBitmap = Bitmap.FromFile(logofile);
                            break;
                        case nameof(LogoTypeEnum.wordlogo):
                            var logoFont = GetFont(config.wordlogofont.fontname,
                                config.wordlogofont.fontstyle,
                                config.wordlogofont.fontsize,
                                config.wordlogofont.fontunit);

                            topBitmap = WaterMarkHelper.CreateWordImage(
                                strCamermake,
                                strFormatter,
                                logoFont,
                                GetColor(config.fillcolor),
                                GetColor(config.wordlogofont.fontcolor),
                                10,
                                10,
                                false);
                            break;
                        case nameof(LogoTypeEnum.none):
                            break;
                        default:
                            break;
                    }
                    #endregion
                    #region get bottom info
                    //font
                    var font = GetFont(
                        config.infofont.fontname,
                        config.infofont.fontstyle,
                        config.infofont.fontsize,
                        config.infofont.fontunit);
                    //info
                    var bottominfo = FormatterInfo(config.bottominfo, infoDict);

                    bottomBitmap = WaterMarkHelper.CreateWordImage(
                        bottominfo,
                        strFormatter,
                        font,
                        GetColor(config.fillcolor),
                        GetColor(config.infofont.fontcolor),
                        10,
                        10,
                        false);
                    #endregion
                    // origin bitmap 
                    var originBitmap = Bitmap.FromStream(stream);

                    if (config.logmaxheight > 0)
                    {
                        var maxheight = config.logmaxheight<=1 ? 
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
                    var totalHeight = originBitmap.Height + (topBitmap == null ? 0 : topBitmap.Height) + (bottomBitmap == null ? 0 : bottomBitmap.Height);
                    var totalWidth = Math.Max(
                        Math.Max(originBitmap.Width, topBitmap == null ? 0 : topBitmap.Height),
                        bottomBitmap == null ? 0 : bottomBitmap.Height);
                    Bitmap exportBitmap = new Bitmap(totalWidth, totalHeight);

                    #region 绘制
                    Graphics graphics = Graphics.FromImage(exportBitmap);
                    //图像字体质量
                    graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                    graphics.TextContrast = 4;//高对比度,default : 4 
                                              //图像质量
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    //背景设置白色
                    graphics.Clear(GetColor(config.fillcolor));
                    #endregion
                    var x =( totalWidth - originBitmap.Width) / 2;
                    var y = 0;
                    graphics.DrawImage(originBitmap, x, y, originBitmap.Width, originBitmap.Height);
                    if (topBitmap is not null)
                    {
                        x = (originBitmap.Width - topBitmap.Width) / 2;
                        y = originBitmap.Height;
                        graphics.DrawImage(topBitmap, x, y, topBitmap.Width, topBitmap.Height);
                    }
                    if (bottomBitmap is not null)
                    {
                        x = (originBitmap.Width - bottomBitmap.Width) / 2;
                        y = originBitmap.Height + (topBitmap == null ? 0 : topBitmap.Height);
                        graphics.DrawImage(bottomBitmap, x, y, bottomBitmap.Width, bottomBitmap.Height);
                    }

                    graphics.Flush();
                    graphics.Dispose();

                    if (exportfolder is null)
                    {
                        var outPath = Path.Combine(AppContext.BaseDirectory, settings.exportfolder);
                        if (!Directory.Exists(outPath)) Directory.CreateDirectory(outPath);
                        exportfolder = new DirectoryInfo(outPath);
                    }
                    SaveImageFile(
                        exportBitmap,
                        fileinfo.Name.Replace(fileinfo.Extension, ""),
                        settings.exporttype, exportfolder.FullName
                        );

                    exportBitmap.Dispose();
                    topBitmap.Dispose();
                    bottomBitmap.Dispose();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary>
        /// 创建水印图片在底部，布局为左右
        /// </summary>
        /// <param name="fileinfo"></param>
        /// <param name="settings"></param>
        /// <param name="direction"></param>
        /// <param name="exportfolder"></param>
        public static void CreateWaterMark_Bottom(FileInfo fileinfo, Settings settings, DrawDirectionEnum direction, DirectoryInfo exportfolder)
        {
            try
            {
                var config = settings.bottom;
                using (FileStream stream = fileinfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    //exif info data
                    var fields = typeof(InfoTag).GetFields(BindingFlags.Public | BindingFlags.Static);
                    var infoDict = ImageMetaDataHelper.GetMetaData(stream, fields.Select(p => p.Name).ToArray());
                    infoDict.Append(new KeyValuePair<string, string>("sign", settings.sign));
                    //string format
                    var sf = GetStringFormat(direction);

                    System.Drawing.Image leftBitmap = null,
                        leftinfoBitmap = null,
                        rightBitmap = null,
                        rightinfoBitmap = null;

                    var logoFont = GetFont(
                        config.wordlogofont.fontname,
                        config.wordlogofont.fontstyle,
                        config.wordlogofont.fontsize,
                        config.wordlogofont.fontunit);

                    #region get left logo
                    var camermake = infoDict[nameof(InfoTag.camermake)];
                    switch (config.leftlogo)
                    {
                        case nameof(LogoTypeEnum.imglogo):
                            var logofile = GetLogos(LogosFolder, camermake)?.FullName;
                            if (logofile is not null) leftBitmap = Bitmap.FromFile(logofile);
                            break;
                        case nameof(LogoTypeEnum.wordlogo):
                            leftBitmap = WaterMarkHelper.CreateWordImage(
                                camermake,
                                sf,
                                logoFont,
                                GetColor(config.fillcolor),
                                GetColor(config.wordlogofont.fontcolor),
                                10,
                                10,
                                false);
                            break;
                        case nameof(LogoTypeEnum.none):
                            break;
                        default:
                            break;
                    }
                    #endregion

                    #region get right logo
                    var lensmake = infoDict[nameof(InfoTag.lensmake)];
                    switch (config.rightlogo)
                    {
                        case nameof(LogoTypeEnum.imglogo):
                            var logofile = GetLogos(LogosFolder, lensmake)?.FullName;
                            if (logofile is not null) rightBitmap = Bitmap.FromFile(logofile);
                            break;
                        case nameof(LogoTypeEnum.wordlogo):
                            rightBitmap = WaterMarkHelper.CreateWordImage(
                                camermake,
                                sf,
                                logoFont,
                                GetColor(config.fillcolor),
                                GetColor(config.infofont.fontcolor),
                                10,
                                10,
                                false);
                            break;
                        case nameof(LogoTypeEnum.none):
                            break;
                        default:
                            break;
                    }
                    #endregion

                    //font
                    var font = GetFont(
                        config.infofont.fontname,
                        config.infofont.fontstyle,
                        config.infofont.fontsize,
                        config.infofont.fontunit);
                    //info
                    var leftinfo = FormatterInfo(config.leftinfo, infoDict);
                    var rightinfo = FormatterInfo(config.rightinfo, infoDict);
                    //get left  info
                    leftinfoBitmap = WaterMarkHelper.CreateWordImage(
                        leftinfo,
                        sf,
                        font,
                        GetColor(config.fillcolor),
                        GetColor(config.infofont.fontcolor),
                        10,
                        10,
                        leftBitmap != null);
                    //get right info
                    rightinfoBitmap = WaterMarkHelper.CreateWordImage(
                        rightinfo,
                        sf,
                        font,
                        GetColor(config.fillcolor),
                        GetColor(config.infofont.fontcolor),
                        10,
                        10,
                        rightBitmap != null);

                    #region origin bitmap 
                    var originBitmap = Bitmap.FromStream(stream);
                    #endregion

                    #region 缩放logo 

                    if (config.logmaxheight > 0)
                    {
                        var maxheight = config.logmaxheight <= 1 ?
                            (int)Math.Round(originBitmap.Height * config.logmaxheight, 0)
                            :
                            (int)config.logmaxheight;
                        if (leftBitmap != null && leftBitmap.Height > maxheight)
                        {
                            var logoRatio = (double)leftBitmap.Width / leftBitmap.Height;
                            var resize = new Size(
                                (int)(Math.Round(logoRatio * maxheight, 0)),
                                maxheight);
                            leftBitmap = new Bitmap(leftBitmap, resize);
                        }

                        if (rightBitmap != null && rightBitmap.Height > maxheight)
                        {
                            var logoRatio = (double)rightBitmap.Width / rightBitmap.Height;
                            var resize = new Size(
                                (int)(Math.Round(logoRatio * maxheight, 0)),
                                maxheight);
                            rightBitmap = new Bitmap(rightBitmap, resize);
                        }
                    }
                    #endregion

                    var maxInfoHeigt = Math.Max(
                            Math.Max(leftBitmap == null ? 0 : leftBitmap.Height, leftinfoBitmap == null ? 0 : leftinfoBitmap.Height),
                            Math.Max(rightBitmap == null ? 0 : rightBitmap.Height, rightinfoBitmap == null ? 0 : rightinfoBitmap.Height)
                            );
                    var totalHeight = originBitmap.Height + maxInfoHeigt;
                    var totalWidth = originBitmap.Width;
                    Bitmap exportBitmap = new Bitmap(totalWidth, totalHeight);

                    #region 绘制
                    Graphics graphics = Graphics.FromImage(exportBitmap);
                    //图像字体质量
                    graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                    graphics.TextContrast = 4;//高对比度,default : 4 

                    //图像质量
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    //背景设置白色
                    graphics.Clear(GetColor(config.fillcolor));
                    #endregion
                    var fillratio = config.fillratio;
                    var x = (int)Math.Round(originBitmap.Width * fillratio / 2, 0);
                    var y = (int)Math.Round(originBitmap.Height * fillratio / 2, 0);

                    graphics.DrawImage(originBitmap, x, y, originBitmap.Width - (x * 2), originBitmap.Height - (y * 2));
                    if (leftBitmap is not null)
                    {
                        x = 0;
                        y = originBitmap.Height;
                        graphics.DrawImage(leftBitmap, x, y, leftBitmap.Width, leftBitmap.Height);
                    }
                    if (leftinfoBitmap is not null)
                    {
                        x = leftBitmap == null ? 0 : leftBitmap.Width;
                        y = originBitmap.Height;
                        graphics.DrawImage(leftinfoBitmap, x, y, leftinfoBitmap.Width, leftinfoBitmap.Height);
                    }
                    if (rightBitmap is not null)
                    {
                        x = originBitmap.Width - rightBitmap.Width - (rightinfoBitmap == null ? 0 : rightinfoBitmap.Width);
                        y = originBitmap.Height;
                        graphics.DrawImage(rightBitmap, x, y, rightBitmap.Width, rightBitmap.Height);
                    }
                    if (rightinfoBitmap is not null)
                    {
                        x = originBitmap.Width - rightinfoBitmap.Width;
                        y = originBitmap.Height;
                        graphics.DrawImage(rightinfoBitmap, x, y, rightinfoBitmap.Width, rightinfoBitmap.Height);
                    }
                    graphics.Flush();
                    graphics.Dispose();

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
                        exportfolder.FullName
                        );

                    exportBitmap.Dispose();
                    leftBitmap.Dispose();
                    leftinfoBitmap.Dispose();
                    rightBitmap.Dispose();
                    rightinfoBitmap.Dispose();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        /// <summary>
        /// 创建水印图片在底部，布局为包围
        /// </summary>
        /// <param name="fileinfo"></param>
        /// <param name="settings"></param>
        /// <param name="direction"></param>
        /// <param name="exportfolder"></param>
        public static void CreateWaterMark_Surround(FileInfo fileinfo, Settings settings, DrawDirectionEnum direction, DirectoryInfo exportfolder)
        {
            try
            {
                var config = settings.surround;
                using (FileStream stream = fileinfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    //exif info data
                    var fields = typeof(InfoTag).GetFields(BindingFlags.Public | BindingFlags.Static);
                    var infoDict = ImageMetaDataHelper.GetMetaData(stream, fields.Select(p => p.Name).ToArray());
                    infoDict.Append(new KeyValuePair<string, string>("sign", settings.sign));
                    //string format
                    var sf = GetStringFormat(direction);

                    System.Drawing.Image leftBitmap = null,
                        leftinfoBitmap = null,
                        rightBitmap = null,
                        rightinfoBitmap = null;

                    var logoFont = GetFont(
                        config.wordlogofont.fontname, 
                        config.wordlogofont.fontstyle,
                        config.wordlogofont.fontsize,
                        config.wordlogofont.fontunit
                        );

                    #region get left logo
                    var camermake = infoDict[nameof(InfoTag.camermake)];
                    switch (config.leftlogo)
                    {
                        case nameof(LogoTypeEnum.imglogo):
                            var logofile = GetLogos(LogosFolder,camermake)?.FullName;
                            if (logofile is not null) leftBitmap = Bitmap.FromFile(logofile);
                            break;
                        case nameof(LogoTypeEnum.wordlogo):
                            leftBitmap = WaterMarkHelper.CreateWordImage(
                                camermake,
                                sf, 
                                logoFont,
                                GetColor(config.fillcolor),
                                GetColor(config.wordlogofont.fontcolor),
                                10,
                                10,
                                false);
                            break;
                        case nameof(LogoTypeEnum.none):
                            break;
                        default:
                            break;
                    }
                    #endregion

                    #region get right logo
                    var lensmake = infoDict[nameof(InfoTag.lensmake)];
                    switch (config.rightlogo)
                    {
                        case nameof(LogoTypeEnum.imglogo):
                            var logofile = GetLogos(LogosFolder, lensmake)?.FullName;
                            if (logofile is not null) rightBitmap = Bitmap.FromFile(logofile);
                            break;
                        case nameof(LogoTypeEnum.wordlogo):
                            rightBitmap = WaterMarkHelper.CreateWordImage(
                                camermake, 
                                sf, 
                                logoFont,
                                GetColor(config.fillcolor),
                                GetColor(config.wordlogofont.fontcolor),
                                10, 
                                10,
                                false);
                            break;
                        case nameof(LogoTypeEnum.none):
                            break;
                        default:
                            break;
                    }
                    #endregion

                    //font
                    var font = GetFont(
                        config.infofont.fontname,
                        config.infofont.fontstyle,
                        config.infofont.fontsize, 
                        config.infofont.fontunit
                        );
                    //info
                    var leftinfo = FormatterInfo(
                        config.leftinfo, 
                        infoDict
                        );
                    var rightinfo = FormatterInfo(
                        config.rightinfo, 
                        infoDict
                        );
                    //get left  info
                    leftinfoBitmap = WaterMarkHelper.CreateWordImage(
                        leftinfo,
                        sf, 
                        font,
                        GetColor(config.fillcolor),
                        GetColor(config.infofont.fontcolor),
                        10, 
                        10, 
                        leftBitmap != null);
                    //get right info
                    rightinfoBitmap = WaterMarkHelper.CreateWordImage(
                        rightinfo, 
                        sf, 
                        font,
                        GetColor(config.fillcolor),
                        GetColor(config.infofont.fontcolor),
                        10, 
                        10, 
                        rightBitmap != null);

                    #region origin bitmap 
                    var originBitmap = Bitmap.FromStream(stream);
                    #endregion

                    #region 缩放logo 

                    if (config.logmaxheight > 0)
                    {
                        var maxheight = config.logmaxheight <= 1 ?
                            (int)Math.Round(originBitmap.Height * config.logmaxheight, 0)
                            :
                            (int)config.logmaxheight;
                        if (leftBitmap != null && leftBitmap.Height > maxheight)
                        {
                            var logoRatio = (double)leftBitmap.Width / leftBitmap.Height;
                            var resize = new Size(
                                (int)(Math.Round(logoRatio * maxheight, 0)),
                                maxheight);
                            leftBitmap = new Bitmap(leftBitmap, resize);
                        }

                        if (rightBitmap != null && rightBitmap.Height > maxheight)
                        {
                            var logoRatio = (double)rightBitmap.Width / rightBitmap.Height;
                            var resize = new Size(
                                (int)(Math.Round(logoRatio * maxheight, 0)),
                                maxheight);
                            rightBitmap = new Bitmap(rightBitmap, resize);
                        }
                    }
                    #endregion

                    var maxInfoHeigt = Math.Max(
                            Math.Max(leftBitmap == null ? 0 : leftBitmap.Height, leftinfoBitmap == null ? 0 : leftinfoBitmap.Height),
                            Math.Max(rightBitmap == null ? 0 : rightBitmap.Height, rightinfoBitmap == null ? 0 : rightinfoBitmap.Height)
                            );
                    var totalHeight = originBitmap.Height + maxInfoHeigt;
                    var totalWidth = originBitmap.Width;
                    Bitmap exportBitmap = new Bitmap(totalWidth, totalHeight);

                    #region 绘制
                    Graphics graphics = Graphics.FromImage(exportBitmap);
                    //图像字体质量
                    graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                    graphics.TextContrast = 4;//高对比度,default : 4 

                    //图像质量
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    //背景设置白色
                    graphics.Clear(GetColor(config.fillcolor));
                    #endregion
                    var fillratio = config.fillratio;
                    var x = (int)Math.Round(originBitmap.Width * fillratio / 2, 0);
                    var y = (int)Math.Round(originBitmap.Height * fillratio / 2, 0);

                    graphics.DrawImage(originBitmap, x, y, originBitmap.Width - (x * 2), originBitmap.Height - (y * 2));
                    if (leftBitmap is not null)
                    {
                        x = 0;
                        y = originBitmap.Height;
                        graphics.DrawImage(leftBitmap, x, y, leftBitmap.Width, leftBitmap.Height);
                    }
                    if (leftinfoBitmap is not null)
                    {
                        x = leftBitmap == null ? 0 : leftBitmap.Width;
                        y = originBitmap.Height;
                        graphics.DrawImage(leftinfoBitmap, x, y, leftinfoBitmap.Width, leftinfoBitmap.Height);
                    }
                    if (rightBitmap is not null)
                    {
                        x = originBitmap.Width - rightBitmap.Width - (rightinfoBitmap == null ? 0 : rightinfoBitmap.Width);
                        y = originBitmap.Height;
                        graphics.DrawImage(rightBitmap, x, y, rightBitmap.Width, rightBitmap.Height);
                    }
                    if (rightinfoBitmap is not null)
                    {
                        x = originBitmap.Width - rightinfoBitmap.Width;
                        y = originBitmap.Height;
                        graphics.DrawImage(rightinfoBitmap, x, y, rightinfoBitmap.Width, rightinfoBitmap.Height);
                    }
                    graphics.Flush();
                    graphics.Dispose();

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
                        exportfolder.FullName
                        );

                    exportBitmap.Dispose();
                    leftBitmap.Dispose();
                    leftinfoBitmap.Dispose();
                    rightBitmap.Dispose();
                    rightinfoBitmap.Dispose();
                }
            }
            catch
            {
                throw;
            }
        }


        /// <summary>
        /// 只填充图片
        /// </summary>
        /// <param name="fileinfo"></param>
        /// <param name="settings"></param>
        /// <param name="direction"></param>
        /// <param name="exportfolder"></param>
        public static void CreateWaterMark_OnlyFill(FileInfo fileinfo, Settings settings, DrawDirectionEnum direction, DirectoryInfo exportfolder,Size exportSize)
        {
            try
            {
                var config = settings.onlyfill;
                using (FileStream stream = fileinfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var bitmap = WaterMarkHelper.FillImage(
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
                        exportfolder.FullName
                        );
                    bitmap.Dispose();
                }
            }
            catch
            {
                throw;
            }
        }


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
        public static Bitmap CreateWordImage(string word, StringFormat sf, Font font, Color fillcolor,Color pencolor, int leftPadding = 10, int topPadding = 10, bool splitLineFlag = false)
        {
            try
            {
                //计算估测的图片高度和宽度
                var drawInfoSize = ComputerDrawStringSize(word, font, sf, new SizeF(9000F, 9000F), out int chartNum, out int lineNum);
                //计算图片总大小
                var totalWidth = (int)Math.Round(
                drawInfoSize.Width + (2 * leftPadding)
                , 0);
                var totalHeight = (int)Math.Round(
                    drawInfoSize.Height + (2 * topPadding)
                    , 0);
                //创建绘制图像
                Bitmap bitmap = new Bitmap(totalWidth, totalHeight);
                using Graphics graphics = Graphics.FromImage(bitmap);
                //图像字体质量
                graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                graphics.TextContrast = 4;//高对比度,default : 4 

                //图像质量
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                //计算绘制开始点
                float x = leftPadding;
                float y = topPadding;
                //背景设置白色
                graphics.Clear(fillcolor);
                //绘制画笔颜色
                SolidBrush pen = new SolidBrush(pencolor);
                graphics.DrawString(word, font, pen, x, y, sf);
                //分割线
                if (splitLineFlag)
                {
                    graphics.DrawLine(new Pen(Color.FromArgb(228, 228, 228)), 0, topPadding, 0, topPadding + (int)drawInfoSize.Height);
                }
                graphics.Flush();

                SaveImageFile(bitmap, $"info", ImageFormat.Jpeg, Path.Combine(AppContext.BaseDirectory,"infoExport"));

                return bitmap;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /// <summary>
        /// 计算绘制文字的图片高度和宽度
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
            using Graphics graphics = Graphics.FromImage(new Bitmap((int)sizeF.Width, (int)sizeF.Height));

            var drawStringSize = graphics.MeasureString(strContent, font, sizeF, stringFormat, out chartNum, out lineNum);
            //var TextRenderdrawStringSize =TextRenderer.MeasureText(strContent, font);
            //drawStringSize.Width = TextRenderdrawStringSize.Width;
            //drawStringSize.Height = TextRenderdrawStringSize.Height;
            return drawStringSize;
        }

        /// <summary>
        /// 填充图片
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="fillcolor"></param>
        /// <param name="size"></param>
        /// <param name="fillratio"></param>
        /// <returns></returns>
        public static Bitmap FillImage(Stream stream, Color fillcolor, Size size, double fillratio = 0.1)
        {
            try
            {
                var image = System.Drawing.Image.FromStream(stream);
                if (size == Size.Empty || size == image.Size)
                {
                    size = image.Size;
                    using Bitmap bitmap = new Bitmap(size.Width, size.Height);

                    using Graphics graphics = Graphics.FromImage(bitmap);
                    //图像质量
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    //背景设置白色
                    graphics.Clear(Color.White);

                    //计算坐标 (默认原图缩放10%的空间为空白填充)
                    var x = (int)Math.Ceiling(size.Width * fillratio / 2);
                    var y = (int)Math.Ceiling(size.Height * fillratio / 2);

                    graphics.DrawImage(image,
                                       x,
                                       y,
                                       (size.Width - (x * 2)),
                                       (size.Height - (y * 2)));
                    graphics.Flush();
                    graphics.Dispose();

                    return bitmap;
                }
                else
                {

                    using Bitmap bitmap = new Bitmap(size.Width, size.Height);
                    using Graphics graphics = Graphics.FromImage(bitmap);
                    //图像质量
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    //背景设置白色
                    graphics.Clear(Color.White);

                    //计算绘制的图像大小
                    ComputerKeepRatioZoomSize(image.Size, size, out Size ZoomSize);
                    //计算坐标 (默认原图缩放10%的空间为空白填充)

                    var x = (int)Math.Ceiling(((double)size.Width - ZoomSize.Width) / 2);
                    var y = (int)Math.Ceiling((double)(size.Height - ZoomSize.Height) / 2);

                    graphics.DrawImage(image,
                                       x,
                                       y,
                                       ZoomSize.Width,
                                       ZoomSize.Height);
                    graphics.Flush();
                    graphics.Dispose();

                    return bitmap;
                }

            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// 计算保持宽高比下最大缩放图片尺寸
        /// </summary>
        /// <param name="originSize"></param>
        /// <param name="targetSize"></param>
        /// <param name="computerSize"></param>
        public static void ComputerKeepRatioZoomSize(Size originSize, Size targetSize, out Size computerSize)
        {
            try
            {
                var maxwidth = targetSize.Width;
                var maxheight = targetSize.Height;
                var maxratio = (float)maxwidth / maxheight;

                var width = originSize.Width;
                var height = originSize.Height;
                var ratio = (float)width / height;

                //目标大小的宽高比与原图相同
                if (maxratio == ratio)
                {
                    computerSize = targetSize;
                }
                else
                {
                    var gcd = GCD(width, height);

                    var minwidth = width / gcd;
                    var minheight = height / gcd;


                    var quotientW = maxwidth / minwidth;
                    var quotientH = maxheight / minheight;

                    var commonQuotient = Math.Min(quotientW, quotientH);

                    var resizeW = minwidth * commonQuotient;
                    var resizeH = minheight * commonQuotient;

                    computerSize = new Size(resizeW, resizeH);
                }
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


        /******************************************Common Method***********************************************/
        
        #region Common Method
        /// <summary>
        /// 保存文件
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="imgname"></param>
        /// <param name="exporttype"></param>
        /// <param name="exportpath"></param>
        public static void SaveImageFile(Bitmap bitmap, string imgname, ImageFormat exporttype, string exportpath)
        {
            //文件类型
            var imageFormat = exporttype;
            //名称 原图名称_yyMMddHHmmss.jpeg/bmp
            var filename = $"{imgname}_{DateTime.Now:yyMMddHHmmss}.{exporttype}";

            if (!Directory.Exists(exportpath)) Directory.CreateDirectory(exportpath);

            using FileStream filestream = File.OpenWrite(Path.Combine(exportpath, filename));

            EncoderParameters encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);

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
        public static FileInfo GetLogos(string folderpath,string make)
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
        /// 获取字体
        /// </summary>
        /// <param name="font"></param>
        /// <param name="fontstyle"></param>
        /// <param name="size"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static Font GetFont(string font, FontStyle fontstyle, int size, GraphicsUnit unit)
        {
            var fontFileName = $"{font}-{System.Enum.GetName(typeof(FontStyle), fontstyle)}.*";
            var fontFileInfo = new DirectoryInfo(FontsFolder).GetFiles(fontFileName).FirstOrDefault();
            return FontHelper.GetFont(fontFileInfo.FullName, size, fontstyle, unit);
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
            var R = Convert.ToInt32(b[0].ToString() + b[1].ToString(),16);
            var G = Convert.ToInt32(b[2].ToString() + b[3].ToString(),16);
            var B = Convert.ToInt32(b[4].ToString() + b[5].ToString(),16);
            var A = Convert.ToInt32(b[6].ToString() + b[7].ToString(), 16);//透明度
            return Color.FromArgb(A,R,G,B);
        }
        #endregion

    }
}
