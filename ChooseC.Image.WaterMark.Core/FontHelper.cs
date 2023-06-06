using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace ChooseC.Image.WaterMark.Core
{
    public class FontHelper
    {
        #region 安装字体

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int WriteProfileString(string lpszSection, string lpszKeyName, string lpszString);

        [DllImport("gdi32")]
        public static extern int AddFontResource(string lpFileName);

        /// <summary>
        /// 安装字体
        /// </summary>
        /// <param name="fontFilePath">字体文件全路径</param>
        /// <returns>是否成功安装字体</returns>
        /// <exception cref="UnauthorizedAccessException">不是管理员运行程序</exception>
        /// <exception cref="Exception">字体安装失败</exception>
        public static bool InstallFont(string fontFilePath)
        {
            try
            {
                System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent();

                System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);
                //判断当前登录用户是否为管理员
                if (principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator) == false)
                {
                    throw new UnauthorizedAccessException("当前用户无管理员权限，无法安装字体。");
                }
                //获取Windows字体文件夹路径
                string fontPath = Path.Combine(System.Environment.GetEnvironmentVariable("WINDIR"), "fonts", Path.GetFileName(fontFilePath));
                //检测系统是否已安装该字体
                if (!File.Exists(fontPath))
                {
                    // File.Copy(System.Windows.Forms.Application.StartupPath + "\\font\\" + FontFileName, FontPath); //font是程序目录下放字体的文件夹
                    //将某路径下的字体拷贝到系统字体文件夹下
                    File.Copy(fontFilePath, fontPath); //font是程序目录下放字体的文件夹
                    AddFontResource(fontPath);

                    //Res = SendMessage(HWND_BROADCAST, WM_FONTCHANGE, 0, 0); 
                    //WIN7下编译会出错，不清楚什么问题。注释就行了。  
                    //安装字体
                    WriteProfileString("fonts", Path.GetFileNameWithoutExtension(fontFilePath) + "(TrueType)", Path.GetFileName(fontFilePath));
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format($"[{Path.GetFileNameWithoutExtension(fontFilePath)}] 字体安装失败！原因：{ex.Message}"));
            }
            return true;
        }

        #endregion

        /// <summary>
        /// 从项目资源文件中加载字体
        /// 如何使用资源文件中的字体，无安装无释放
        /// 该方法需要开发者将字体文件以资源的形式放入项目资源文件中。不用安装到字体库中，其他程序如果需要使用，就需要自己安装或者加载。此时可以使用以下代码创建程序所需字体
        /// </summary>
        /// <param name="bytes">资源文件中的字体文件</param>
        /// <returns></returns>
        public Font GetResoruceFont(byte[] bytes, int fontSize = 18, FontStyle fontStyle = FontStyle.Regular, GraphicsUnit graphicsUnit = GraphicsUnit.Pixel)
        {
            System.Drawing.Text.PrivateFontCollection pfc = new System.Drawing.Text.PrivateFontCollection();
            IntPtr MeAdd = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, MeAdd, bytes.Length);
            pfc.AddMemoryFont(MeAdd, bytes.Length);
            return new Font(pfc.Families[0], fontSize, fontStyle, graphicsUnit);
        }
        /// <summary>
        /// 通过文件获取字体
        /// 设置好某个字体的路径，然后加载字体文件，从而创建字体。
        /// 不用安装到字体库中，其他程序如果需要使用，就需要自己安装或者加载。 
        /// </summary>
        /// <param name="path">文件全路径</param>
        /// <param name="fontSize"></param>
        /// <param name="fontStyle"></param>
        /// <param name="graphicsUnit"></param>
        /// <returns><see cref="Font"/></returns>
        public static Font GetFontByPath(string path, int fontSize = 18, FontStyle fontStyle = FontStyle.Regular, GraphicsUnit graphicsUnit = GraphicsUnit.Pixel)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    return new Font(new InstalledFontCollection().Families.First(), fontSize, fontStyle, graphicsUnit, 0);
                }
                else
                {
                    //程序直接调用字体文件，不用安装到系统字库中。
                    System.Drawing.Text.PrivateFontCollection pfc = new System.Drawing.Text.PrivateFontCollection();
                    pfc.AddFontFile(path);//字体文件的路径
                    return new Font(pfc.Families[0], fontSize, fontStyle, graphicsUnit, 0);
                }
            }
            catch (Exception)
            {
                return new Font(new InstalledFontCollection().Families.First(), fontSize, fontStyle, graphicsUnit, 0);
            }
        }
        /// <summary>
        /// 获取本地安装字体
        /// </summary>
        /// <param name="familyName">字体名称</param>
        /// <returns></returns>
        public static Font GetFont(string familyName,int fontSize = 18, FontStyle fontStyle = FontStyle.Regular, GraphicsUnit graphicsUnit = GraphicsUnit.Pixel)
        {
            if (CheckFont(familyName))
            {
                return new Font(familyName, fontSize, fontStyle, graphicsUnit);
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// 检查字体是否存在
        /// </summary>
        /// <param name="familyName">字体名称</param>
        /// <returns></returns>
        public static bool CheckFont(string familyName)
        {
            using (var ifc = new InstalledFontCollection())
            {
                return ifc.Families.FirstOrDefault(p=>p.Name.ToLower() == familyName.ToLower()) != default(FontFamily);
            }
        }

        /// <summary>
        /// 检测某种字体样式是否可用
        /// </summary>
        /// <param name="familyName">字体名称</param>
        /// <param name="fontStyle">字体样式</param>
        /// <returns></returns>
        public static bool CheckFont(string familyName, FontStyle fontStyle = FontStyle.Regular)
        {
            InstalledFontCollection installedFontCollection = new InstalledFontCollection();
            FontFamily[] fontFamilies = installedFontCollection.Families;
            foreach (var item in fontFamilies)
            {
                if (item.Name.ToLower().Equals(familyName.ToLower()))
                {
                    return item.IsStyleAvailable(fontStyle);
                }
            }
            return false;
        }
    }
}
