## ChooseC.Image.WaterMark

**练手项目**

用于给图像添加相框，可以将照片信息【焦距、光圈、快门速度、ISO...】绘制添加到相框中里。

### 项目结构

```batch project.tree

功能代码：ChooseC.Image.WaterMark.Core

控制台：ChooseC.Image.WaterMark.ConsoleApp
```

### 功能

+ 照片水印框布局
  + [x] 底部布局
    + [x] 上下排版
    + [x] 左右排版
  + [x] 底部环绕
    + [x] 左右排版
  + [x] 空白填充

+ 照片信息【需要存在EXIF数据】
  + [x] 焦距
  + [x] 光圈
  + [x] 快门
  + [x] ISO
  + [x] 照片创建时间
  + [x] 原始拍摄时间
  + [x] 相机厂商
  + [x] 相机型号
  + [x] 镜头厂商
  + [x] 镜头型号
  + [] 镜头规格

+ 导出图片格式
  + [x] Bmp
  + [x] Png
  + [x] Jpeg【默认】

+ [x] 导出尺寸
  + [x] 自定义

### 配置说明

+ 配置文件【appsettings.json默认覆盖develop】
  + [appsettings.develop.json](ChooseC.Image.WaterMark.ConsoleApp/appsettings.develop.json)
  + [appsettings.json](ChooseC.Image.WaterMark.ConsoleApp/appsettings.json)

#### Settings

##### exporttype

导出图片类型
Value：【[System.Drawing.Imaging.ImageFormat](https://learn.microsoft.com/zh-cn/dotnet/api/system.drawing.imaging.imageformat)】

##### exportquality

导出质量

##### exportfolder

导出路径，默认父目录为程序运行目录

##### exportsize

图片导出尺寸，保持原图宽高比缩放，差距部份使用空白填充
尺寸为实际输出图片中除去水印相框的尺寸【OnlyFill为实际输出图片尺寸，其它为图像内容尺寸】

+ exportsize
  + width
  + height
+ width、height都为0，原尺寸输出，不进行缩放
+ 只有width 或 heiht,根据原图宽高比进行缩放

##### sign

个性签名

##### topbottom

###### toplogo 上方logo

###### bottominfo 下方信息

使用`{tag}`进行信息格式串编写,eg:`{lensmodel} | {focallenth} {aperture} {exposure} {iso}`
`tag`支持：

+ [x] tag
  + [x] camermake【相机厂商】
  + [x] camermodel【相机型号】
  + [x] focallenth【焦距】
  + [x] aperture【光圈】
  + [x] exposure【快门/曝光】
  + [x] iso【ISO】
  + [x] origintime【源拍摄时间】
  + [x] createtime【文件创建时间】
  + [x] location【地点】
  + [x] lensmake【镜头厂商】
  + [x] lensmodel【镜头型号】

###### fillcolor：填充颜色

8位16进制数，最后两位代表透明度

###### fillratio：填充比率

###### logmaxheight：logo最大高度

###### wordlogofont|infofont：logo字体|水印信息字体

###### fontname：字体名称

###### fontcolor：字体颜色

###### fontstyle：字体样式

###### fontsize：字体大小

###### fontunit：字体绘制单位

##### bottom|surround

###### leftlogo：左边logo

###### leftinfo：左边水印信息

###### rightlogo：右边logo

###### rightinfo：右边信息

###### fillcolor：填充颜色

8位16进制数，最后两位代表透明度

###### fillratio：填充比率

###### logmaxheight：logo最大高度

###### wordlogofont|infofont：logo字体|水印信息字体

###### fontname：字体名称

###### fontcolor：字体颜色

###### fontstyle：字体样式

###### fontsize：字体大小

###### fontunit：字体绘制单位

##### onlyfill：包围填充

###### fillcolor：填充颜色

###### fillratio：填充相对原图占比
