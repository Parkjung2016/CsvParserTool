# Third-Party Notices

PJDev Data Tool includes or uses the components listed below. Their original
licenses and copyright notices remain in effect. The project license does not
replace those licenses.

Full common license texts are provided in `licenses/MIT.txt` and
`licenses/Apache-2.0.txt`. Package links point to the corresponding project or
package page where package-specific notices can be reviewed.

## Runtime and bundled components

| Component | Version | License | Copyright / project notice |
|---|---:|---|---|
| [ClosedXML](https://www.nuget.org/packages/ClosedXML/0.105.0) | 0.105.0 | MIT | ClosedXML contributors |
| [ClosedXML.Parser](https://www.nuget.org/packages/ClosedXML.Parser/2.0.0) | 2.0.0 | MIT | ClosedXML contributors |
| [DocumentFormat.OpenXml](https://www.nuget.org/packages/DocumentFormat.OpenXml/3.1.1) | 3.1.1 | MIT | Microsoft Corporation |
| [DocumentFormat.OpenXml.Framework](https://www.nuget.org/packages/DocumentFormat.OpenXml.Framework/3.1.1) | 3.1.1 | MIT | Microsoft Corporation |
| [ExcelNumberFormat](https://www.nuget.org/packages/ExcelNumberFormat/1.1.0) | 1.1.0 | MIT | ExcelNumberFormat contributors |
| [MessagePack](https://www.nuget.org/packages/MessagePack/3.1.8) | 3.1.8 | MIT | Yoshifumi Kawai and contributors |
| [MessagePack.Annotations](https://www.nuget.org/packages/MessagePack.Annotations/3.1.8) | 3.1.8 | MIT | Yoshifumi Kawai and contributors |
| [Microsoft.Bcl.AsyncInterfaces](https://www.nuget.org/packages/Microsoft.Bcl.AsyncInterfaces/8.0.0) | 8.0.0 | MIT | Microsoft Corporation |
| [Microsoft.Bcl.HashCode](https://www.nuget.org/packages/Microsoft.Bcl.HashCode/1.1.1) | 1.1.1 | MIT | Microsoft Corporation |
| [Microsoft.NET.StringTools](https://www.nuget.org/packages/Microsoft.NET.StringTools/17.11.4) | 17.11.4 | MIT | Microsoft Corporation |
| [RBush.Signed](https://www.nuget.org/packages/RBush.Signed/4.0.0) | 4.0.0 | MIT | Copyright © 2017-2024 Turning Code, LLC and contributors |
| [SixLabors.Fonts](https://www.nuget.org/packages/SixLabors.Fonts/1.0.0) | 1.0.0 | Apache-2.0 | Six Labors |
| [System.Buffers](https://www.nuget.org/packages/System.Buffers/4.5.1) | 4.5.1 | MIT | Microsoft Corporation |
| [System.Collections.Immutable](https://www.nuget.org/packages/System.Collections.Immutable/8.0.0) | 8.0.0 | MIT | Microsoft Corporation |
| [System.Memory](https://www.nuget.org/packages/System.Memory/4.5.5) | 4.5.5 | MIT | Microsoft Corporation |
| [System.Numerics.Vectors](https://www.nuget.org/packages/System.Numerics.Vectors/4.5.0) | 4.5.0 | MIT | Microsoft Corporation |
| [System.Runtime.CompilerServices.Unsafe](https://www.nuget.org/packages/System.Runtime.CompilerServices.Unsafe/6.0.0) | 6.0.0 | MIT | Microsoft Corporation |
| [System.Threading.Tasks.Extensions](https://www.nuget.org/packages/System.Threading.Tasks.Extensions/4.5.4) | 4.5.4 | MIT | Microsoft Corporation |
| [Windows API Code Pack for .NET](https://www.nuget.org/packages/WindowsAPICodePack/8.0.15.2) | 8.0.15.2 | MIT | Copyright © 2009-2010 Microsoft Corporation; modifications by Jacob Slusser, 2014, and Peter William Wagner, 2017-2026 |

## Build-time components

These packages are used while building the application. Some perform bundling
or code weaving, but are not presented as project-authored software.

| Component | Version | License | Copyright / project notice |
|---|---:|---|---|
| [Costura.Fody](https://www.nuget.org/packages/Costura.Fody/6.2.0) | 6.2.0 | MIT | Costura.Fody contributors |
| [Fody](https://www.nuget.org/packages/Fody/6.9.3) | 6.9.3 | MIT | Fody contributors |
| [MessagePackAnalyzer](https://www.nuget.org/packages/MessagePackAnalyzer/3.1.8) | 3.1.8 | MIT | Yoshifumi Kawai and contributors |
| [Microsoft.NETFramework.ReferenceAssemblies](https://www.nuget.org/packages/Microsoft.NETFramework.ReferenceAssemblies/1.0.3) | 1.0.3 | MIT | Microsoft Corporation |
| [Microsoft.NETFramework.ReferenceAssemblies.net481](https://www.nuget.org/packages/Microsoft.NETFramework.ReferenceAssemblies.net481/1.0.3) | 1.0.3 | MIT | Microsoft Corporation |
| [System.Drawing.Common](https://www.nuget.org/packages/System.Drawing.Common/8.0.0) | 8.0.0 | MIT | Microsoft Corporation |

No system font files are bundled by this repository; the application only
requests fonts installed on the user's operating system.
