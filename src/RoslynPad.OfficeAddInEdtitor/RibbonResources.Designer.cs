﻿//------------------------------------------------------------------------------
// <auto-generated>
//     此代码由工具生成。
//     运行时版本:4.0.30319.42000
//
//     对此文件的更改可能会导致不正确的行为，并且如果
//     重新生成代码，这些更改将会丢失。
// </auto-generated>
//------------------------------------------------------------------------------

namespace RoslynPad.OfficeAddInEdtitor {
    using System;
    
    
    /// <summary>
    ///   一个强类型的资源类，用于查找本地化的字符串等。
    /// </summary>
    // 此类是由 StronglyTypedResourceBuilder
    // 类通过类似于 ResGen 或 Visual Studio 的工具自动生成的。
    // 若要添加或移除成员，请编辑 .ResX 文件，然后重新运行 ResGen
    // (以 /str 作为命令选项)，或重新生成 VS 项目。
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class RibbonResources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal RibbonResources() {
        }
        
        /// <summary>
        ///   返回此类使用的缓存的 ResourceManager 实例。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("RoslynPad.OfficeAddInEdtitor.RibbonResources", typeof(RibbonResources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   重写当前线程的 CurrentUICulture 属性，对
        ///   使用此强类型资源类的所有资源查找执行重写。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   查找 System.Byte[] 类型的本地化资源。
        /// </summary>
        internal static byte[] Image1 {
            get {
                object obj = ResourceManager.GetObject("Image1", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   查找类似 &lt;?xml version=&quot;1.0&quot; encoding=&quot;utf-8&quot; ?&gt;
        ///&lt;customUI xmlns=&apos;http://schemas.microsoft.com/office/2009/07/customui&apos; loadImage=&apos;LoadImage&apos; onLoad=&apos;onload&apos;&gt;
        /// &lt;ribbon&gt;
        ///  &lt;tabs&gt;
        ///   &lt;tab id=&apos;tab1&apos; label=&apos;ExcelDnaWpfDemo&apos;&gt;
        ///    &lt;group id=&apos;group1&apos; label=&apos;My Group&apos;&gt;
        ///     &lt;button id=&apos;button1&apos; label=&apos;UI多线程&apos; onAction=&apos;OnButtonPressed&apos; image=&apos;Image1&apos;/&gt;
        ///     &lt;button id=&apos;button2&apos; label=&apos;Ui单线程&apos; onAction=&apos;OnButtonPressedSingle&apos; image=&apos;Image1&apos;/&gt;
        ///     &lt;button id=&apos;button3&apos; label=&apos;任务窗格&apos; onAction=&apos;OnButtonPressedCTP&apos; image=&apos;I [字符串的其余部分被截断]&quot;; 的本地化字符串。
        /// </summary>
        internal static string Ribbon {
            get {
                return ResourceManager.GetString("Ribbon", resourceCulture);
            }
        }
    }
}
