﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Dieser Code wurde von einem Tool generiert.
//     Laufzeitversion:4.0.30319.42000
//
//     Änderungen an dieser Datei können falsches Verhalten verursachen und gehen verloren, wenn
//     der Code erneut generiert wird.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SonarQube.Plugins.Common {
    using System;
    
    
    /// <summary>
    ///   Eine stark typisierte Ressourcenklasse zum Suchen von lokalisierten Zeichenfolgen usw.
    /// </summary>
    // Diese Klasse wurde von der StronglyTypedResourceBuilder automatisch generiert
    // -Klasse über ein Tool wie ResGen oder Visual Studio automatisch generiert.
    // Um einen Member hinzuzufügen oder zu entfernen, bearbeiten Sie die .ResX-Datei und führen dann ResGen
    // mit der /str-Option erneut aus, oder Sie erstellen Ihr VS-Projekt neu.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Gibt die zwischengespeicherte ResourceManager-Instanz zurück, die von dieser Klasse verwendet wird.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SonarQube.Plugins.Common.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Überschreibt die CurrentUICulture-Eigenschaft des aktuellen Threads für alle
        ///   Ressourcenzuordnungen, die diese stark typisierte Ressourcenklasse verwenden.
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
        ///   Sucht eine lokalisierte Zeichenfolge, die Assembly located: {0} ähnelt.
        /// </summary>
        internal static string Resolver_AssemblyLocated {
            get {
                return ResourceManager.GetString("Resolver_AssemblyLocated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die No search paths were supplied. ähnelt.
        /// </summary>
        internal static string Resolver_ConstructorNoPaths {
            get {
                return ResourceManager.GetString("Resolver_ConstructorNoPaths", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Removed AssemblyResolver from current AppDomain assembly resolution. ähnelt.
        /// </summary>
        internal static string Resolver_Dispose {
            get {
                return ResourceManager.GetString("Resolver_Dispose", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Failed to resolve assembly. ähnelt.
        /// </summary>
        internal static string Resolver_FailedToResolveAssembly {
            get {
                return ResourceManager.GetString("Resolver_FailedToResolveAssembly", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Adding AssemblyResolver to current AppDomain assembly resolution. ähnelt.
        /// </summary>
        internal static string Resolver_Initialize {
            get {
                return ResourceManager.GetString("Resolver_Initialize", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Assembly rejected: incorrect version &apos;{0}&apos; ähnelt.
        /// </summary>
        internal static string Resolver_RejectedAssembly {
            get {
                return ResourceManager.GetString("Resolver_RejectedAssembly", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Attempting to resolve assembly &apos;{0}&apos;, requested by &apos;{1}&apos; ähnelt.
        /// </summary>
        internal static string Resolver_ResolvingAssembly {
            get {
                return ResourceManager.GetString("Resolver_ResolvingAssembly", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die {unknown} ähnelt.
        /// </summary>
        internal static string Resolver_UnspecifiedRequestingAssembly {
            get {
                return ResourceManager.GetString("Resolver_UnspecifiedRequestingAssembly", resourceCulture);
            }
        }
    }
}
