# pixini
An Ini File parser written in C#.
Currently, documentation is (mostly) included in the Pixini.cs source file.
For now, check out the example project and unit tests for examples on how to use Pixini.

For Unity3d Users
------------------
Note: If you are using Unity, you might run into some issues particularly with WebGL builds. To solve that, simply add any Converters you plan to a link.xml file located in Assets/link.xml. Ex:

```xml
 <linker>
    <assembly fullname="System">
		<type fullname="System.ComponentModel.StringConverter" preserve="all"/>
		<type fullname="System.ComponentModel.SingleConverter" preserve="all"/>
		<type fullname="System.ComponentModel.BooleanConverter" preserve="all"/>
		<type fullname="System.ComponentModel.Int32Converter" preserve="all"/>
	</assembly>
</linker>
```