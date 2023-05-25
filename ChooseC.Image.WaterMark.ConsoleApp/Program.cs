using ChooseC.Image.WaterMark.ConsoleApp.CustomBinder;
using ChooseC.Image.WaterMark.Core;
using ChooseC.Image.WaterMark.Core.Enum;
using ChooseC.Image.WaterMark.Core.Model;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ChooseC.Image.WaterMark.ConsoleApp
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            #region 选项
            #region “--layout” 水印布局
            var layoutOption = new Option<LayoutEnum>(
                name: "--layout",
                description: "水印布局",
                getDefaultValue: () => LayoutEnum.bottom
                )
            { Arity = ArgumentArity.ExactlyOne };
            #endregion

            #region “--direction” 水印绘制方向
            var directionOption = new Option<DrawDirectionEnum>(
                name: "--direction",
                description: "水印绘制方向",
                getDefaultValue: () => DrawDirectionEnum.lefttoright
                );
            #endregion

            #region "--size" 大小
            var sizeOption = new Option<Size>(
                name: "--size",
                description: "大小",
                getDefaultValue: () => new(0, 0)
                )
            { Arity = ArgumentArity.ExactlyOne };
            #endregion

            #region "--operation -O" 操作类型
            var operationOption = new Option<OperationTypeEnum?>(
                name: "--operation",
                description: "操作类型",
                getDefaultValue: () => OperationTypeEnum.Single
                );
            operationOption.AddAlias("O");
            #endregion

            #region "--path" 路径
            var pathOption = new Option<FileSystemInfo?>(
                name: "--path",
                description: "文件/文件夹路径",
                isDefault: true,
                parseArgument: (result) =>
                {
                    if (result.Tokens.Count == 0)
                    {
                        result.ErrorMessage = "无效文件/文件夹路径！";
                        return null;
                    }
                    string? filePath = result.Tokens.Single().Value;
                    if (!Directory.Exists(filePath))
                    {
                        if (!File.Exists(filePath))
                        {
                            result.ErrorMessage = "无效文件/文件夹路径！";
                            return null;
                        }
                        else
                        {
                            return new DirectoryInfo(filePath);
                        }
                    }
                    else
                    {
                        return new DirectoryInfo(filePath);
                    }
                }
                );
            #endregion

            #region "--type" 路径
            var typeOption = new Option<ImageTypeEnum[]?>(
                name: "--type",
                description: "图片类型",
                getDefaultValue: () => { return new[] { ImageTypeEnum.jpg, ImageTypeEnum.png }; }
                )
            { Arity = ArgumentArity.OneOrMore };
            #endregion

            #region “--export-folder -ef” 导出文件夹路径选项
            var exportfolderOption = new Option<DirectoryInfo?>(
                name: "--export-folder",
                description: "导出文件夹路径",
                isDefault: true,
                parseArgument: (result) =>
                {
                    if (result.Tokens.Count == 0)
                    {
                        //result.ErrorMessage = "无效文件夹路径！";
                        return null;
                    }
                    string? filePath = result.Tokens.Single().Value;
                    if (!Directory.Exists(filePath))
                    {
                        //result.ErrorMessage = "无效文件夹路径！";
                        return null;
                    }
                    else
                    {
                        return new DirectoryInfo(filePath);
                    }
                }
                );
            exportfolderOption.AddAlias("--ef");
            #endregion

            #region "--folder" 文件夹路径
            var folderpathOption = new Option<DirectoryInfo?>(
                name: "--folder",
                description: "文件夹路径"
                );
            #endregion

            #region “--file” 文件路径选项
            //文件路径
            var filepathOption = new Option<FileInfo?>(
                name: "--file",
                description: "文件路径",
                isDefault: true,
                parseArgument: (result) =>
                {
                    if (result.Tokens.Count == 0)
                    {
                        result.ErrorMessage = "无效文件路径！";
                        return null;
                    }
                    string? filePath = result.Tokens.Single().Value;
                    if (!File.Exists(filePath))
                    {
                        result.ErrorMessage = "文件不存在！";
                        return null;
                    }
                    else
                    {
                        return new FileInfo(filePath);
                    }
                });
            #endregion

            #endregion

            #region 子命令

            #region "run | R" 执行子命令

            //操作方式 ：批量 ，单项
            var operationCommand = new Command(
                name: "run",
                description: "执行子命令"
                );

            operationCommand.AddAlias("R");
            operationCommand.AddOption(pathOption);
            operationCommand.AddOption(typeOption);
            operationCommand.AddOption(exportfolderOption);
            operationCommand.AddOption(layoutOption);
            operationCommand.AddOption(directionOption);
            operationCommand.AddOption(sizeOption);

            #region MyRegion
            /**
            operationCommand.Handler = CommandHandler.Create<FileSystemInfo, ImageTypeEnum[], DirectoryInfo,
                LayoutEnum, DrawDirectionEnum, Size, ILogger, IConfigurationRoot>
            (async (path, type, exportfolder, layout, direction, size, logger, config) =>
            {
                try
                {
                    IEnumerable<string> typefilter = type.Select(p => nameof(p));
                    logger.Information("-------start---------");
                    switch (path)
                    {
                        case FileInfo file:
                            if (typefilter.Contains(file.Extension.ToLower()))
                            {
                                logger.Information($"---------{file.Name}-----------");
                                await Process(file, exportfolder, layout, direction, size, logger, config);
                            }
                            else
                            {
                                logger.Warning("无效文件！");
                            }
                            break;
                        case DirectoryInfo directory:

                            foreach (var item in typefilter)
                            {
                                var list = directory.GetFiles($"*.{item}");
                                foreach (var ls in list)
                                {
                                    logger.Information($"---------{ls.Name}-----------");
                                    await Process(ls, exportfolder, layout, direction, size, logger, config);
                                }
                            }
                            break;
                        default:
                            logger.Warning("无效文件或文件夹路径！");
                            break;
                    }

                }
                catch (Exception ex)
                {
                    logger.Error("执行异常：", ex);
                }
            });
            */
            #endregion

            operationCommand.SetHandler(async (path, type, exportfolder, layout, direction, size, logger, config) =>
            {
                try
                {
                    IEnumerable<string> typefilter = type.Select(p => Enum.GetName(typeof(ImageTypeEnum), p));
                    logger.Information("-------start---------");
                    switch (path.Attributes)
                    {
                        case FileAttributes.Directory:

                            foreach (var item in typefilter)
                            {
                                var list = (path as DirectoryInfo).GetFiles($"*.{item}");
                                foreach (var ls in list)
                                {
                                    await Process(ls.FullName, exportfolder, layout, direction, size, logger, config);
                                
                                }
                            }
                            break;
                        default:
                            if (typefilter.Contains(path.Extension.ToLower().Replace(".", "")))
                            {
                                await Process(path.FullName, exportfolder, layout, direction, size, logger, config);
                            }
                            else
                            {
                                logger.Warning("无效文件！");
                            }
                            break;
                    }

                }
                catch (Exception ex)
                {
                    logger.Fatal(ex, "执行异常：");
                }
            }, pathOption, typeOption, exportfolderOption, layoutOption, directionOption, sizeOption, new LoggerCustomBinder(), new ConfigCustomBinder());
            #endregion

            #endregion

            #region 根命令
            //根节点
            var rootCommand = new RootCommand("图片水印处理程序");
            //子命令
            rootCommand.AddCommand(operationCommand);
            #endregion

            return await rootCommand.InvokeAsync(args);
        }

        static async Task Process(string filepath, DirectoryInfo exportfolder, LayoutEnum layout, DrawDirectionEnum direction, Size exportSize, ILogger logger, IConfigurationRoot config)
        {
            try
            {
                logger.Information($"----------{filepath} Start-------------");
                var settings = config.GetSection(nameof(Settings)).Get<Settings>();
                var fileinfo = new FileInfo(filepath);
                //获取Exif 
                switch (layout)
                {
                    case LayoutEnum.topbottom:
                        WaterMarkHelper.CreateWaterMark_TopBottom(fileinfo, settings, direction, exportfolder);
                        break;
                    case LayoutEnum.bottom:
                        WaterMarkHelper.CreateWaterMark_Bottom(fileinfo, settings, direction, exportfolder);
                        break;
                    case LayoutEnum.surround:
                        WaterMarkHelper.CreateWaterMark_Surround(fileinfo, settings, direction, exportfolder);
                        break;
                    case LayoutEnum.onlyfill:
                        WaterMarkHelper.CreateWaterMark_OnlyFill(fileinfo, settings, direction, exportfolder, exportSize);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "执行异常");
                //logger.Error($"执行异常：{ex.Message}\n{ex.StackTrace}", ex);
            }
            finally
            {
                logger.Information($"----------{filepath} END-------------");
            }
        }
    }
}