﻿//------------------------------------------------------------------------------
// <auto-generated>
//     このコードはツールによって生成されました。
//     ランタイム バージョン:4.0.30319.42000
//
//     このファイルへの変更は、以下の状況下で不正な動作の原因になったり、
//     コードが再生成されるときに損失したりします。
// </auto-generated>
//------------------------------------------------------------------------------

namespace FFXIV_ACT_Helper_Plugin.Properties {
    using System;
    
    
    /// <summary>
    ///   ローカライズされた文字列などを検索するための、厳密に型指定されたリソース クラスです。
    /// </summary>
    // このクラスは StronglyTypedResourceBuilder クラスが ResGen
    // または Visual Studio のようなツールを使用して自動生成されました。
    // メンバーを追加または削除するには、.ResX ファイルを編集して、/str オプションと共に
    // ResGen を実行し直すか、または VS プロジェクトをビルドし直します。
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   このクラスで使用されているキャッシュされた ResourceManager インスタンスを返します。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("FFXIV_ACT_Helper_Plugin.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   すべてについて、現在のスレッドの CurrentUICulture プロパティをオーバーライドします
        ///   現在のスレッドの CurrentUICulture プロパティをオーバーライドします。
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
        ///   0A|Potion of Strength|33559026
        ///0B|Potion of Dexterity|33559027
        ///0C|Potion of Vitality|33559028
        ///0D|Potion of Intelligence|33559029
        ///0E|Potion of Mind|33559030
        ///10|Hi-Potion of Strength|33559031
        ///11|Hi-Potion of Dexterity|33559032
        ///12|Hi-Potion of Vitality|33559033
        ///13|Hi-Potion of Intelligence|33559034
        ///14|Hi-Potion of Mind|33559035
        ///1D|Mega-Potion of Strength|33559036
        ///1E|Mega-Potion of Dexterity|33559037
        ///1F|Mega-Potion of Vitality|33559038
        ///20|Mega-Potion of Intelligence|33559039
        ///21|Mega-Potion of Mind [残りの文字列は切り詰められました]&quot;; に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string medicated_items {
            get {
                return ResourceManager.GetString("medicated_items", resourceCulture);
            }
        }
    }
}