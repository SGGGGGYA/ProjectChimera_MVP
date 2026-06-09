using System.Runtime.CompilerServices;

// 拆 asmdef 后，spine-unity 跨程序集访问 spine-csharp 的 internal 字段/方法
// (uvs / regionUVs / triangles / hulllength / r/g/b/a / worldVerticesLength /
//  regionOffsetX.../inheritDeform 等)，需要通过 InternalsVisibleTo 授权。
// 第二个授权是给 Unity 自动拆出来的 spine-unity.Editor 程序集用的。
// 第三个授权给 ProjectChimera.Core（UnitData 直接读写 Skeleton.r/g/b/a 用作"染色/闪光"）。
[assembly: InternalsVisibleTo("spine-unity")]
[assembly: InternalsVisibleTo("spine-unity.Editor")]
[assembly: InternalsVisibleTo("ProjectChimera.Core")]
