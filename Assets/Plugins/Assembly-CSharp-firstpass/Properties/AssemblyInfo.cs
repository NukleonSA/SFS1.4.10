using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using Sirenix.Serialization;

[assembly: RegisterFormatter(typeof(Vector2IntFormatter), 0)]
[assembly: RegisterFormatter(typeof(Vector3IntFormatter), 0)]
[assembly: AssemblyVersion("0.0.0.0")]
